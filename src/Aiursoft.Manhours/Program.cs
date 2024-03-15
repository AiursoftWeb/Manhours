using System.Diagnostics.CodeAnalysis;

namespace Aiursoft.ManHours;

[ExcludeFromCodeCoverage]
public class Program
{
    public static async Task Main(string[] args)
    {
        var app = await WebTools.Extends.AppAsync<Startup>(args);
        await app.RunAsync();
    }
}