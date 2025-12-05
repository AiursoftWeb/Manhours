using Aiursoft.CSTools.Tools;
using static Aiursoft.WebTools.Extends;
using System.Net;

namespace Aiursoft.Manhours.Tests.IntegrationTests;

[TestClass]
public class SecurityTests
{
    private readonly int _port;
    private readonly HttpClient _http;
    private IHost? _server;

    public SecurityTests()
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
    public async Task TestInvalidCharactersRejection()
    {
        // Test space injection
        var url = "/r/github.com/user/repo space.html";
        var response = await _http.GetAsync(url);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, "Should reject spaces");

        // Test semicolon injection
        url = "/r/github.com/user/repo;id.html";
        response = await _http.GetAsync(url);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, "Should reject semicolons");

        // Test pipe injection
        url = "/r/github.com/user/repo|id.html";
        response = await _http.GetAsync(url);
        Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode, "Should reject pipes");

        // Test valid repo
        // We expect 404 or 200, but NOT 400.
        // Since we don't have internet access in tests usually, it might fail with 500 or 404.
        // But definitely not 400.
        url = "/r/github.com/aiursoft/manhours.html";
        response = await _http.GetAsync(url);
        Assert.AreNotEqual(HttpStatusCode.BadRequest, response.StatusCode, "Should accept valid repo characters");
    }
}
