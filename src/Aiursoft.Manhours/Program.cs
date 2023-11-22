using System.Reflection;
using Aiursoft.Canon;
using Aiursoft.Scanner;
using Aiursoft.WebTools.Models;

namespace Aiursoft.ManHours;

public class Program
{
    public static async Task Main(string[] args)
    {
        var app = WebTools.Extends.App<Startup>(args);
        await app.RunAsync();
    }
}

public class Startup : IWebStartup
{
    public void ConfigureServices(IConfiguration configuration, IWebHostEnvironment environment, IServiceCollection services)
    {
        services
            .AddControllers()
            .AddApplicationPart(Assembly.GetExecutingAssembly());

        services.AddTaskCanon();
        services.AddScannedDependencies();
    }

    public void Configure(WebApplication app)
    {
        app.UseRouting();
        app.MapDefaultControllerRoute();
    }
}
