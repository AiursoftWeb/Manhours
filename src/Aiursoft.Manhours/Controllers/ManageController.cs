using Aiursoft.Manhours.Configuration;
using Aiursoft.Manhours.Entities;
using Aiursoft.Manhours.Models.ManageViewModels;
using Aiursoft.Manhours.Services;
using Aiursoft.Manhours.Services.FileStorage;
using Aiursoft.UiStack.Navigation;
using Aiursoft.WebTools.Attributes;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace Aiursoft.Manhours.Controllers;

/// <summary>
/// This controller is used to handle user related actions like change password, change avatar.
/// </summary>
[Authorize]
[LimitPerMin]
public class ManageController(
    ImageProcessingService image,
    StorageService storageService,
    IStringLocalizer<ManageController> localizer,
    IOptions<AppSettings> appSettings,
    UserManager<User> userManager,
    SignInManager<User> signInManager,
    GlobalSettingsService settingsService,
    ILogger<ManageController> logger,
    ManhoursDbContext dbContext)
    : Controller
{
    //
    // GET: /Manage/Index
    [RenderInNavBar(
        NavGroupName = "Settings",
        NavGroupOrder = 9998,
        CascadedLinksGroupName = "Personal",
        CascadedLinksIcon = "user-circle",
        CascadedLinksOrder = 1,
        LinkText = "Profile Settings",
        LinkOrder = 3)]
    [HttpGet]
    public async Task<IActionResult> Index(ManageMessageId? message = null)
    {
        ViewData["StatusMessage"] =
            message == ManageMessageId.ChangeProfileSuccess ? localizer["Your profile has been saved."] :
            message == ManageMessageId.ChangeAvatarSuccess ? localizer["Your avatar has been saved."] :
            message == ManageMessageId.ChangePasswordSuccess ? localizer["Your password has been changed."] :
            message == ManageMessageId.Error ? localizer["An error has occurred."]
            : "";

        var model = new IndexViewModel
        {
            AllowUserAdjustNickname = await settingsService.GetBoolSettingAsync(SettingsMap.AllowUserAdjustNickname)
        };
        return this.StackView(model);
    }

    //
    // GET: /Manage/ChangePassword
    [HttpGet]
    public IActionResult ChangePassword()
    {
        if (appSettings.Value.OIDCEnabled)
        {
            return BadRequest("Local password is disabled when OIDC authentication is enabled.");
        }
        return this.StackView(new ChangePasswordViewModel());
    }

    //
    // POST: /Manage/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (appSettings.Value.OIDCEnabled)
        {
            return BadRequest("Local password is disabled when OIDC authentication is enabled.");
        }
        if (!ModelState.IsValid)
        {
            return this.StackView(model);
        }
        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            var result = await userManager.ChangePasswordAsync(user, model.OldPassword!, model.NewPassword!);
            if (result.Succeeded)
            {
                await signInManager.SignInAsync(user, isPersistent: false);
                logger.LogInformation(3, "User changed their password successfully");
                return RedirectToAction(nameof(Index), new { Message = ManageMessageId.ChangePasswordSuccess });
            }
            AddErrors(result);
            return this.StackView(model);
        }
        return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
    }

    //
    // GET: /Manage/ChangeProfile
    [HttpGet]
    public async Task<IActionResult> ChangeProfile()
    {
        if (!await settingsService.GetBoolSettingAsync(SettingsMap.AllowUserAdjustNickname))
        {
            return BadRequest("Adjusting nickname is disabled by administrator.");
        }

        var user = await GetCurrentUserAsync();
        return this.StackView(new ChangeProfileViewModel
        {
            Name = user!.DisplayName
        });
    }

    //
    // POST: /Manage/ChangeProfile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeProfile(ChangeProfileViewModel model)
    {
        if (!await settingsService.GetBoolSettingAsync(SettingsMap.AllowUserAdjustNickname))
        {
            return BadRequest("Adjusting nickname is disabled by administrator.");
        }

        if (!ModelState.IsValid)
        {
            return this.StackView(model);
        }

        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            user.DisplayName = model.Name;
            await userManager.UpdateAsync(user);
            await signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.ChangeProfileSuccess });
        }
        return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });
    }

    //
    // GET: /Manage/ChangeAvatar
    [HttpGet]
    public async Task<IActionResult> ChangeAvatar()
    {
        var user = await GetCurrentUserAsync();
        return this.StackView(new ChangeAvatarViewModel
        {
            AvatarUrl = user!.AvatarRelativePath
        });
    }

    //
    // POST: /Manage/ChangeAvatar
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangeAvatar(ChangeAvatarViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return this.StackView(model);
        }

        // Make sure the file is actually a photo.
        var absolutePath = storageService.GetFilePhysicalPath(model.AvatarUrl);
        if (!await image.IsValidImageAsync(absolutePath))
        {
            ModelState.AddModelError(string.Empty, localizer["The file is not a valid image."]);
            return this.StackView(model);
        }

        // Save the new avatar in the database.
        var user = await GetCurrentUserAsync();
        if (user != null)
        {
            user.AvatarRelativePath = model.AvatarUrl;
            await userManager.UpdateAsync(user);

            // Sign in the user to refresh the avatar.
            await signInManager.SignInAsync(user, isPersistent: false);
            return RedirectToAction(nameof(Index), new { Message = ManageMessageId.ChangeAvatarSuccess });
        }

        return this.StackView(model);
    }

    //
    // GET: /Manage/MyEmails
    [HttpGet]
    public async Task<IActionResult> MyEmails()
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });

        var emails = await dbContext.UserEmails
            .Where(e => e.UserId == user.Id)
            .OrderBy(e => e.CreatedTime)
            .ToListAsync();

        return this.StackView(new EmailsViewModel
        {
            Emails = emails,
            LoginEmail = user.Email ?? string.Empty
        });
    }

    //
    // POST: /Manage/AddEmail
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddEmail(string email)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });

        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError(string.Empty, localizer["Email address is required."]);
            var emails = await dbContext.UserEmails.Where(e => e.UserId == user.Id).OrderBy(e => e.CreatedTime).ToListAsync();
            return this.StackView(new EmailsViewModel { Emails = emails, LoginEmail = user.Email ?? string.Empty }, "MyEmails");
        }

        var normalizedEmail = email.Trim().ToUpperInvariant();

        if (await dbContext.UserEmails.AnyAsync(e => e.Email.ToUpper() == normalizedEmail))
        {
            ModelState.AddModelError(string.Empty, localizer["This email is already in use."]);
            var emails = await dbContext.UserEmails.Where(e => e.UserId == user.Id).OrderBy(e => e.CreatedTime).ToListAsync();
            return this.StackView(new EmailsViewModel { Emails = emails, LoginEmail = user.Email ?? string.Empty }, "MyEmails");
        }

        dbContext.UserEmails.Add(new UserEmail
        {
            Email = email.Trim(),
            UserId = user.Id
        });
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(MyEmails));
    }

    //
    // POST: /Manage/DeleteEmail
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEmail(int id)
    {
        var user = await GetCurrentUserAsync();
        if (user == null) return RedirectToAction(nameof(Index), new { Message = ManageMessageId.Error });

        var userEmails = await dbContext.UserEmails
            .Where(e => e.UserId == user.Id)
            .ToListAsync();

        var emailToDelete = userEmails.FirstOrDefault(e => e.Id == id);
        if (emailToDelete == null) return NotFound();

        if (string.Equals(emailToDelete.Email, user.Email, StringComparison.OrdinalIgnoreCase))
        {
            ModelState.AddModelError(string.Empty, localizer["You can't remove your login email address."]);
            return this.StackView(new EmailsViewModel { Emails = userEmails, LoginEmail = user.Email ?? string.Empty }, "MyEmails");
        }

        if (userEmails.Count <= 1)
        {
            ModelState.AddModelError(string.Empty, localizer["You must have at least one email address."]);
            return this.StackView(new EmailsViewModel { Emails = userEmails, LoginEmail = user.Email ?? string.Empty }, "MyEmails");
        }

        dbContext.UserEmails.Remove(emailToDelete);
        await dbContext.SaveChangesAsync();

        return RedirectToAction(nameof(MyEmails));
    }

    #region Helpers

    private void AddErrors(IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            ModelState.AddModelError(string.Empty, error.Description);
        }
    }

    public enum ManageMessageId
    {
        ChangeAvatarSuccess,
        ChangePasswordSuccess,
        ChangeProfileSuccess,
        Error
    }

    private Task<User?> GetCurrentUserAsync()
    {
        return userManager.GetUserAsync(HttpContext.User);
    }

    #endregion
}
