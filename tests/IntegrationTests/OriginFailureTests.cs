using System.Net;
using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Aiursoft.Canon;
using Aiursoft.CSTools.Services;
using Aiursoft.GitRunner;
using Aiursoft.GitRunner.Services;
using Aiursoft.Manhours.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Xml.Linq;
using Aiursoft.GitRunner.Exceptions;
using Aiursoft.Manhours.Models;
using Aiursoft.Manhours.Entities;
using Aiursoft.Manhours.Services.Background;
using Microsoft.EntityFrameworkCore;
using Aiursoft.Manhours.Services;
using Microsoft.Extensions.Caching.Memory;

namespace Aiursoft.Manhours.Tests.IntegrationTests;

[TestClass]
public class OriginFailureTests : TestBase
{
    private sealed class CaptureProvider : ILoggerProvider
    {
        public ConcurrentQueue<(LogLevel Level, string Category)> Entries { get; } = new();
        public ILogger CreateLogger(string categoryName) => new CaptureLogger(this, categoryName);
        public void Dispose() { }
        private sealed class CaptureLogger(CaptureProvider owner, string category) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => owner.Entries.Enqueue((logLevel, category));
        }
    }

    private sealed class FakeCommandService(string failure) : CommandService
    {
        public override Task<(int code, string output, string error)> RunCommandAsync(string bin, string arg, string path, TimeSpan? timeout = null, bool killTimeoutProcess = true, IDictionary<string, string?>? environmentVariables = null)
        {
            if (arg.Contains("clone ", StringComparison.Ordinal))
            {
                if (failure == "timeout") throw new TimeoutException("Execute command: git clone origin timed out");
                return Task.FromResult((128, "", failure));
            }
            return Task.FromResult((128, "", "fatal: not a git repository"));
        }
    }

    [TestMethod]
    [DataRow("The requested URL returned error: 401", "svg")]
    [DataRow("The requested URL returned error: 403", "git")]
    [DataRow("The requested URL returned error: 404", "svg")]
    [DataRow("timeout", "git")]
    public async Task SimulatedOriginFailureProducesBadgeAndNoHighSeverityLogs(string failure, string extension)
    {
        var capture = new CaptureProvider();
        using var logs = LoggerFactory.Create(builder => builder.SetMinimumLevel(LogLevel.Trace).AddProvider(capture));
        using var scope = Server!.Services.CreateScope();
        var sp = scope.ServiceProvider;
        var retry = new RetryEngine(logs.CreateLogger<RetryEngine>());
        var runner = new GitCommandRunner(new FakeCommandService(failure), logs.CreateLogger<GitCommandRunner>());
        var workspace = new WorkspaceManager(logs.CreateLogger<WorkspaceManager>(), retry, runner);
        var service = new RepoService(retry, sp.GetRequiredService<ManhoursDbContext>(), logs.CreateLogger<RepoService>(), workspace,
            sp.GetRequiredService<IConfiguration>(), sp.GetRequiredService<IBackgroundTaskQueue>(), sp.GetRequiredService<IMemoryCache>(), sp.GetRequiredService<RepoWhitelistService>());
        var controller = new BadgeController(logs.CreateLogger<BadgeController>(), service, null!)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() }
        };
        var result = await controller.RenderRepo($"fake-origin.test/repo.{extension}");
        var file = result as FileContentResult;
        Assert.IsNotNull(file);
        Assert.AreEqual("image/svg+xml", file.ContentType);
        var svg = System.Text.Encoding.UTF8.GetString(file.FileContents);
        XDocument.Parse(svg);
        Assert.Contains("man-hours", svg);
        Assert.Contains("error", svg);
        Assert.Contains("e05d44", svg);
        Assert.DoesNotContain("Exception", svg);
        Assert.IsTrue(capture.Entries.Any(e => e.Level == LogLevel.Warning));
        Assert.IsFalse(capture.Entries.Any(e => e.Level >= LogLevel.Error));
    }

    [TestMethod]
    public async Task ConnectionFailureEmitsNoErrorOrCritical()
    {
        var capture = new CaptureProvider();
        GetService<ILoggerFactory>().AddProvider(capture);
        var response = await Http.GetAsync("/r/localhost/log-test.svg");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("error", await response.Content.ReadAsStringAsync());
        Assert.IsTrue(capture.Entries.Any(e => e.Level == LogLevel.Warning));
        Assert.IsFalse(capture.Entries.Any(e => e.Level >= LogLevel.Error), string.Join(", ", capture.Entries.Where(e => e.Level >= LogLevel.Error)));
    }

    [TestMethod]
    [DataRow("Failed to connect to localhost port 443: Connection refused")]
    [DataRow("The requested URL returned error: 401")]
    [DataRow("The requested URL returned error: 403")]
    [DataRow("repository not found")]
    [DataRow("Connection timed out")]
    public void ClassifiesRemoteFailures(string error)
    {
        Assert.IsTrue(OriginFailure.IsOriginFailure(new GitCommandException("failed", "clone https://example.test/repo .", "", error, "/tmp")));
    }

    [TestMethod]
    public void DoesNotClassifyLocalFailuresOrCancellation()
    {
        Assert.IsFalse(OriginFailure.IsOriginFailure(new GitCommandException("failed", "config --get remote.origin.url", "", "not found", "/tmp")));
        Assert.IsFalse(OriginFailure.IsOriginFailure(new GitCommandException("failed", "clone https://example.test/repo .", "", "No space left on device", "/tmp")));
        Assert.IsFalse(OriginFailure.IsOriginFailure(new OperationCanceledException()));
        Assert.IsTrue(OriginFailure.IsOriginFailure(new RemoteTimeoutException(new TimeoutException())));
    }

    [TestMethod]
    [DataRow("svg")]
    [DataRow("git")]
    public async Task UnreachableOriginReturnsErrorBadge(string extension)
    {
        var response = await Http.GetAsync($"/r/localhost/origin-test.{extension}");
        Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        Assert.AreEqual("image/svg+xml", response.Content.Headers.ContentType?.MediaType);
        var text = await response.Content.ReadAsStringAsync();
        XDocument.Parse(text);
        Assert.Contains("man-hours", text);
        Assert.Contains("error", text);
        Assert.Contains("e05d44", text);
        Assert.DoesNotContain("Exception", text);
    }

    [TestMethod]
    public async Task FailedRefreshPreservesExistingBareClone()
    {
        var configuration = GetService<IConfiguration>();
        var path = Path.Combine(configuration["Storage:Path"]!, "Repos", "localhost", "preserve");
        Directory.CreateDirectory(path);
        using var init = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git")
        {
            ArgumentList = { "init", "--bare", path },
            RedirectStandardError = true,
            RedirectStandardOutput = true
        })!;
        await init.WaitForExitAsync();
        Assert.AreEqual(0, init.ExitCode);
        using var remote = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git")
        {
            ArgumentList = { "-C", path, "remote", "add", "origin", "https://localhost/preserve.git" },
            RedirectStandardError = true,
            RedirectStandardOutput = true
        })!;
        await remote.WaitForExitAsync();
        Assert.AreEqual(0, remote.ExitCode);
        var sentinel = Path.Combine(path, "preserve-marker");
        await File.WriteAllTextAsync(sentinel, "keep");
        using var scope = Server!.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<RepoService>();
        await Assert.ThrowsAsync<GitCommandException>(() => service.GetRepoStatsAsync("localhost/preserve", "https://localhost/preserve.git", force: true));
        Assert.IsTrue(File.Exists(sentinel));
    }

    [TestMethod]
    public async Task LegacyWorktreeIsNotTouchedOnFailedRefresh()
    {
        var path = Path.Combine(GetService<IConfiguration>()["Storage:Path"]!, "Repos", "localhost", "legacy");
        Directory.CreateDirectory(path);
        using var init = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("git")
        {
            ArgumentList = { "init", path }, RedirectStandardError = true, RedirectStandardOutput = true
        })!;
        await init.WaitForExitAsync();
        Assert.AreEqual(0, init.ExitCode);
        var sentinel = Path.Combine(path, "untracked-file");
        await File.WriteAllTextAsync(sentinel, "preserve");
        using var scope = Server!.Services.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<RepoService>();
        await Assert.ThrowsAsync<GitCommandException>(() => service.GetRepoStatsAsync("localhost/legacy", "https://localhost/legacy.git", force: true));
        Assert.AreEqual("preserve", await File.ReadAllTextAsync(sentinel));
        Assert.IsTrue(Directory.Exists(Path.Combine(path, ".git")));
    }

    [TestMethod]
    public async Task WorkerContinuesAfterFailureAndPreservesStats()
    {
        var capture = new CaptureProvider();
        GetService<ILoggerFactory>().AddProvider(capture);
        const string failedUrl = "https://localhost/worker-failed.git";
        const string nextUrl = "https://localhost/worker-next.git";
        var updated = DateTime.UtcNow.AddDays(-30);
        using (var scope = Server!.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<ManhoursDbContext>();
            var repo = new Repo { Id = Guid.NewGuid(), Url = failedUrl, LastUpdateTime = updated };
            var contributor = new Contributor { Id = Guid.NewGuid(), Email = "worker@example.test" };
            db.Repos.Add(repo);
            db.Contributors.Add(contributor);
            db.RepoContributions.Add(new RepoContribution { Id = Guid.NewGuid(), RepoId = repo.Id, ContributorId = contributor.Id, TotalWorkHours = 42, CommitCount = 7 });
            await db.SaveChangesAsync();
        }
        var queue = GetService<IBackgroundTaskQueue>();
        await queue.QueueBackgroundWorkItemAsync(new RepoUpdateTask { RepoName = "localhost/worker-failed", RepoUrl = failedUrl });
        await queue.QueueBackgroundWorkItemAsync(new RepoUpdateTask { RepoName = "localhost/worker-next", RepoUrl = nextUrl });
        var nextPath = Path.Combine(GetService<IConfiguration>()["Storage:Path"]!, "Repos", "localhost", "worker-next");
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (!Directory.Exists(nextPath))
        {
            await Task.Delay(50, timeout.Token);
        }
        using var verificationScope = Server.Services.CreateScope();
        var verificationDb = verificationScope.ServiceProvider.GetRequiredService<ManhoursDbContext>();
        var stored = await verificationDb.Repos.AsNoTracking().SingleAsync(r => r.Url == failedUrl);
        Assert.AreEqual(updated, stored.LastUpdateTime);
        var contribution = await verificationDb.RepoContributions.AsNoTracking().SingleAsync(c => c.RepoId == stored.Id);
        Assert.AreEqual(42d, contribution.TotalWorkHours);
        Assert.AreEqual(7, contribution.CommitCount);
        Assert.IsTrue(capture.Entries.Any(e => e.Level == LogLevel.Warning && e.Category.Contains("RepoUpdateWorker")));
        Assert.IsFalse(capture.Entries.Any(e => e.Level >= LogLevel.Error));
    }

    [TestMethod]
    public async Task CachedBadgeRemainsNumeric()
    {
        var cache = GetService<IMemoryCache>();
        cache.Set("RepoStatsFullMemory_https://localhost/cached.git", new RepoStats { TotalWorkTime = TimeSpan.FromHours(42) });
        var response = await Http.GetAsync("/r/localhost/cached.svg");
        response.EnsureSuccessStatusCode();
        var text = await response.Content.ReadAsStringAsync();
        XDocument.Parse(text);
        Assert.Contains("42", text);
        Assert.DoesNotContain("error", text);
    }
}
