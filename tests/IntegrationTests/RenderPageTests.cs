using System.Net;
using Aiursoft.CSTools.Tools;
using Aiursoft.DbTools;
using Aiursoft.Manhours.Entities;
using static Aiursoft.WebTools.Extends;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net.Http;
using System.Threading.Tasks;
using Aiursoft.Manhours;

namespace Aiursoft.Manhours.Tests.IntegrationTests;

[TestClass]
public class RenderPageTests
{
    private readonly int _port;
    private readonly HttpClient _http;
    private IHost? _server;

    public RenderPageTests()
    {
        _port = Network.GetAvailablePort();
        _http = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
        {
            BaseAddress = new Uri($"http://localhost:{_port}")
        };
    }

    [TestInitialize]
    public async Task CreateServer()
    {
        _server = await AppAsync<Startup>([], port: _port);
        await _server.StartAsync();
    }

    [TestCleanup]
    public async Task CleanServer()
    {
        if (_server != null)
        {
            await _server.StopAsync();
            _server.Dispose();
        }
    }

    [TestMethod]
    [Timeout(300000, CooperativeCancellation = true)]
    public async Task TestRenderPage()
    {
        // Use a very small repo for testing
        var url = "/r/github.com/anduin2017/howtocook.html";
        var response = await _http.GetAsync(url);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Console.WriteLine($"Status Code: {response.StatusCode}");
            Console.WriteLine($"Response Content (first 500 chars): {content.Substring(0, Math.Min(500, content.Length))}");
        }

        response.EnsureSuccessStatusCode();

        Assert.Contains("Repository ManHours Statistics", content, "Should contain statistics header");
        Assert.Contains("github.com/anduin2017/howtocook", content, "Should contain repo name");
        Assert.Contains("Total Man Hours", content, "Should contain total hours label");
    }
}
