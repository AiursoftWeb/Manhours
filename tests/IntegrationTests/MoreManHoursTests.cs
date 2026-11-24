using Aiursoft.Manhours.Configuration;
using Aiursoft.Manhours.Entities;
using Aiursoft.Manhours.Models.ContributionsViewModels;
using Aiursoft.Manhours.Services;
using Aiursoft.Manhours.Services.BackgroundJobs;
using Aiursoft.Manhours.Services.FileStorage;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewComponents;

namespace Aiursoft.Manhours.Tests.IntegrationTests;

[TestClass]
public class MoreManHoursTests : TestBase
{
    [TestMethod]
    public async Task TestProgramExtends()
    {
        await Server!.CopyAvatarFileAsync();
        await Server!.CopyAvatarFileAsync();
    }

    [TestMethod]
    public void TestPathResolver()
    {
        var testDir = Path.Combine(Path.GetTempPath(), "PathResolverTests_" + Guid.NewGuid());
        Directory.CreateDirectory(testDir);
        try
        {
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Storage:Path"] = testDir
                })
                .Build();

            var resolver = new PathResolver(config);

            var workspaceFile = Path.Combine(testDir, "Workspace", "sub", "test.png");
            Directory.CreateDirectory(Path.GetDirectoryName(workspaceFile)!);
            File.WriteAllText(workspaceFile, "fake content");

            var cleared = resolver.GetClearedExifAbsolutePath(workspaceFile);
            Assert.IsTrue(cleared.Contains("ClearExif"));
            Assert.IsTrue(cleared.EndsWith("test.png"));

            var compressedBoth = resolver.GetCompressedAbsolutePath(workspaceFile, 100, 200);
            Assert.IsTrue(compressedBoth.Contains("Compressed"));
            Assert.IsTrue(compressedBoth.Contains("_w100_h200.png"));

            var compressedW = resolver.GetCompressedAbsolutePath(workspaceFile, 100, 0);
            Assert.IsTrue(compressedW.Contains("_w100.png"));

            var compressedH = resolver.GetCompressedAbsolutePath(workspaceFile, 0, 200);
            Assert.IsTrue(compressedH.Contains("_h200.png"));

            var compressedNone = resolver.GetCompressedAbsolutePath(workspaceFile, 0, 0);
            Assert.IsTrue(compressedNone.EndsWith("test.png"));
        }
        finally
        {
            if (Directory.Exists(testDir))
            {
                Directory.Delete(testDir, true);
            }
        }
    }

    [TestMethod]
    public void TestMissingConfigThrows()
    {
        var config = new ConfigurationBuilder().Build();
        try
        {
            _ = new PathResolver(config);
            Assert.Fail("Should have thrown InvalidDataException");
        }
        catch (InvalidDataException)
        {
            // Expected
        }
    }

    [TestMethod]
    public void TestRepoDeduplication()
    {
        var repo1 = new Repo { Url = "https://gitlab.aiursoft.com/aiursoft/proj.git" };
        var repo2 = new Repo { Url = "https://github.com/aiursoft/proj.git" };
        var repo3 = new Repo { Url = "https://github.com/aiursoft/other.git" };

        var c1 = new RepoContribution { Id = Guid.NewGuid(), Repo = repo1, CommitCount = 10 };
        var c2 = new RepoContribution { Id = Guid.NewGuid(), Repo = repo2, CommitCount = 9 };
        var c3 = new RepoContribution { Id = Guid.NewGuid(), Repo = repo3, CommitCount = 5 };

        var deduplicated = RepoDeduplicationService.Deduplicate([c1, c2, c3]);
        Assert.AreEqual(2, deduplicated.Count);

        var w1 = new WeeklyRepoContribution { Repo = repo1, CommitCount = 10 };
        var w2 = new WeeklyRepoContribution { Repo = repo2, CommitCount = 9 };
        var w4 = new WeeklyRepoContribution { Repo = repo3, CommitCount = 5 };

        var deduplicatedWeekly = RepoDeduplicationService.Deduplicate([w1, w2, w4]);
        Assert.AreEqual(2, deduplicatedWeekly.Count);

        // Edge case: empty url
        var emptyRepo = new Repo { Url = "" };
        var cEmpty = new RepoContribution { Id = Guid.NewGuid(), Repo = emptyRepo, CommitCount = 1 };
        var resEmpty = RepoDeduplicationService.Deduplicate([cEmpty]);
        Assert.AreEqual(1, resEmpty.Count);
    }

    [TestMethod]
    public async Task TestGlobalSettingsService()
    {
        var service = GetService<GlobalSettingsService>();
        await service.UpdateSettingAsync(SettingsMap.AllowUserAdjustNickname, "False");
        var val = await service.GetBoolSettingAsync(SettingsMap.AllowUserAdjustNickname);
        Assert.IsFalse(val);

        await service.UpdateSettingAsync(SettingsMap.AllowUserAdjustNickname, "True");
        var val2 = await service.GetBoolSettingAsync(SettingsMap.AllowUserAdjustNickname);
        Assert.IsTrue(val2);
    }

    [TestMethod]
    public async Task TestUpdateRepoStatsJob()
    {
        using var scope = Server!.Services.CreateScope();
        var job = scope.ServiceProvider.GetRequiredService<UpdateRepoStatsJob>();
        Assert.AreEqual("Update Repo Stats", job.Name);
        Assert.IsNotNull(job.Description);
        await job.ExecuteAsync();
    }

    [TestMethod]
    public async Task TestViewComponents()
    {
        using var scope = Server!.Services.CreateScope();
        var globalSettings = scope.ServiceProvider.GetRequiredService<GlobalSettingsService>();
        var storage = scope.ServiceProvider.GetRequiredService<StorageService>();

        var navbar = new Views.Shared.Components.MarketingNavbar.MarketingNavbar(globalSettings, storage)
        {
            ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext { HttpContext = new DefaultHttpContext() }
            }
        };
        var navRes = await navbar.InvokeAsync();
        Assert.IsNotNull(navRes);

        var footer = new Views.Shared.Components.MarketingFooter.MarketingFooter(globalSettings, storage)
        {
            ViewComponentContext = new ViewComponentContext
            {
                ViewContext = new ViewContext { HttpContext = new DefaultHttpContext() }
            }
        };
        var footRes = await footer.InvokeAsync();
        Assert.IsNotNull(footRes);
    }

    [TestMethod]
    public async Task TestContributionsEndpoints()
    {
        await RegisterAndLoginAsync();
        var resp1 = await Http.GetAsync("/contributions/mycontributions");
        Assert.AreEqual(System.Net.HttpStatusCode.OK, resp1.StatusCode);

        var resp2 = await Http.GetAsync("/contributions/myweeklyreport");
        Assert.AreEqual(System.Net.HttpStatusCode.OK, resp2.StatusCode);
    }

    [TestMethod]
    public async Task TestFilesControllerAuth()
    {
        var resp1 = await Http.PostAsync("/upload/test?token=invalid", new StringContent(""));
        Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, resp1.StatusCode);

        var resp2 = await Http.PostAsync("/upload-private/test?token=invalid", new StringContent(""));
        Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, resp2.StatusCode);

        var resp3 = await Http.GetAsync("/download/nonexistent.txt");
        Assert.AreEqual(System.Net.HttpStatusCode.NotFound, resp3.StatusCode);

        var resp4 = await Http.GetAsync("/download-private/nonexistent.txt?token=invalid");
        Assert.AreEqual(System.Net.HttpStatusCode.Unauthorized, resp4.StatusCode);
    }

    [TestMethod]
    public async Task TestValidateRepoInput()
    {
        using var scope = Server!.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<RepoService>();

        await Assert.ThrowsAsync<ArgumentException>(() => service.GetRepoStatsInRangeAsync("", "https://valid.com", DateTime.UtcNow, DateTime.UtcNow));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetRepoStatsInRangeAsync("valid..name", "https://valid.com", DateTime.UtcNow, DateTime.UtcNow));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetRepoStatsInRangeAsync("valid:name", "https://valid.com", DateTime.UtcNow, DateTime.UtcNow));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetRepoStatsInRangeAsync("valid", "ftp://invalid.com", DateTime.UtcNow, DateTime.UtcNow));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetRepoStatsInRangeAsync("valid", "https://valid.com;rm", DateTime.UtcNow, DateTime.UtcNow));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetRepoStatsInRangeAsync("valid", "https://valid.com$foo", DateTime.UtcNow, DateTime.UtcNow));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetRepoStatsInRangeAsync("valid", "https://valid.com&bar", DateTime.UtcNow, DateTime.UtcNow));
        await Assert.ThrowsAsync<ArgumentException>(() => service.GetRepoStatsInRangeAsync("valid", "https://valid.com|baz", DateTime.UtcNow, DateTime.UtcNow));
    }

    [TestMethod]
    public async Task TestAccountLoginGet()
    {
        var resp = await Http.GetAsync("/Account/Login");
        Assert.AreEqual(System.Net.HttpStatusCode.OK, resp.StatusCode);
    }

    [TestMethod]
    public async Task TestSaveFileFromPhysicalPath()
    {
        using var scope = Server!.Services.CreateScope();
        var storage = scope.ServiceProvider.GetRequiredService<StorageService>();
        var tempFile = Path.GetTempFileName();
        await File.WriteAllTextAsync(tempFile, "hello");
        var saved = await storage.SaveFileFromPhysicalPath(tempFile, "sub/temp.txt");
        Assert.IsNotNull(saved);
        File.Delete(tempFile);
    }
}
