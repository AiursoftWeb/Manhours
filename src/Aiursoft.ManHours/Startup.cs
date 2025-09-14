using Aiursoft.Canon;
using Aiursoft.Scanner;
using Aiursoft.UiStack.Layout;
using Aiursoft.WebTools.Abstractions.Models;
using Microsoft.AspNetCore.Mvc.Razor;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Aiursoft.ManHours;

public class Startup : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
        services
            .AddControllersWithViews()
            .AddApplicationPart(typeof(Startup).Assembly)
            .AddApplicationPart(typeof(UiStackLayoutViewModel).Assembly);

        services.AddTaskCanon();
        services.AddLibraryDependencies();
        // Controllers and localization
        services.AddControllersWithViews()
            .AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.DateTimeZoneHandling = DateTimeZoneHandling.Utc;
                options.SerializerSettings.ContractResolver = new DefaultContractResolver();
            })
            .AddApplicationPart(typeof(Startup).Assembly)
            .AddApplicationPart(typeof(UiStackLayoutViewModel).Assembly)
            .AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization();
    }

    public void Configure(WebApplication app)
    {
        app.UseStaticFiles();
        app.UseRouting();
        app.MapDefaultControllerRoute();
    }
}
