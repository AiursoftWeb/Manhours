using Aiursoft.CSTools.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Extensions.Hosting;
using static Aiursoft.WebTools.Extends;

namespace Aiursoft.ManHours.Tests.IntegrationTests;

[TestClass]
public class BasicTests
{
    private readonly string _endpointUrl;
    private readonly int _port;
    private readonly HttpClient _http;
    private IHost? _server;

    public BasicTests()
    {
        _port = Network.GetAvailablePort();
        _endpointUrl = $"http://localhost:{_port}";
        _http = new HttpClient();
    }

    [TestInitialize]
    public async Task CreateServer()
    {
        _server = App<Startup>(Array.Empty<string>(), port: _port);
        await _server.StartAsync();
    }

    [TestCleanup]
    public async Task CleanServer()
    {
        if (_server == null) return;
        await _server.StopAsync();
        _server.Dispose();
    }

    [TestMethod]
    [DataRow("/r/gitlab.aiursoft.cn/aiursoft/webtools.svg")]
    [DataRow("/r/gitlab.aiursoft.cn/anduin/flyclass.svg")]
    [DataRow("/r/gitlab.aiursoft.cn/anduin/flyclass.json")]
    [DataRow("/r/github.com/ediwang/moonglade.json")]
    public async Task GetBadge(string url)
    {
        var response = await _http.GetAsync(_endpointUrl + url);
        response.EnsureSuccessStatusCode(); // Status Code 200-299
        if (url.EndsWith(".json"))
        {
            Assert.AreEqual("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());
        }
        else if (url.EndsWith(".svg"))
        {
            Assert.AreEqual("image/svg+xml", response.Content.Headers.ContentType?.ToString());
        }
        else
        {
            Assert.Fail();
        }
    }

    [TestMethod]
    [DataRow("/r/gitlab.aiursoft.cn/aiursoft/webtools.ssss")]
    [DataRow("/r/gitlab.aiursoft.cn/anduin/flyclass")]
    [DataRow("/r/gitlab.aiursoft.cn/anduin")]
    public async Task GetBadgeFailed(string url)
    {
        var response = await _http.GetAsync(_endpointUrl + url);
        try
        {
            response.EnsureSuccessStatusCode(); // Status Code 200-299
            Assert.Fail();
        }
        catch (Exception)
        {
            // ignored
        }
    }
}