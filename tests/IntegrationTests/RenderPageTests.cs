using Aiursoft.CSTools.Tools;
using static Aiursoft.WebTools.Extends;

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

        response.EnsureSuccessStatusCode();

        Assert.Contains("Repository Stats", content, "Should contain statistics header");
        Assert.Contains("github.com/anduin2017/howtocook", content, "Should contain repo name");
        Assert.Contains("Total Hours", content, "Should contain total hours label");
        Assert.Contains("Commits", content, "Should contain commits label");
        Assert.Contains("Active Days", content, "Should contain active days label");
    }
}
