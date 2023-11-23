﻿using System.Reflection;
using Aiursoft.Canon;
using Aiursoft.Scanner;
using Aiursoft.WebTools.Models;

namespace Aiursoft.ManHours;

public class Startup : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
        services
            .AddControllersWithViews()
            .AddApplicationPart(Assembly.GetExecutingAssembly());

        services.AddTaskCanon();
        services.AddLibraryDependencies();
    }

    public void Configure(WebApplication app)
    {
        app.UseRouting();
        app.MapDefaultControllerRoute();
    }
}