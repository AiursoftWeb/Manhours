using System.Diagnostics.CodeAnalysis;

namespace Aiursoft.ManHours;

[ExcludeFromCodeCoverage]
public class Program
{
    public static async Task Main(string[] args)
    {
        var app = WebTools.Extends.App<Startup>(args);
        await app.RunAsync();
    }
}