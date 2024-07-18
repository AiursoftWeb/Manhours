using System.Reflection;
using Aiursoft.Canon;
using Aiursoft.Scanner;
using Aiursoft.WebTools.Abstractions.Models;
using Microsoft.AspNetCore.Mvc.Razor;

namespace Aiursoft.ManHours;

public class Startup : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
        services
            .AddControllersWithViews()
            .AddApplicationPart(Assembly.GetExecutingAssembly());
        services.Configure<RazorViewEngineOptions>(options =>
        {
            options.ViewLocationFormats.Add("/Views/Shared/_Layout.cshtml");
        });
        services.AddTaskCanon();
        services.AddLibraryDependencies();
    }

    public void Configure(WebApplication app)
    {
        app.UseStaticFiles();
        app.UseRouting();
        app.MapDefaultControllerRoute();
    }
}