using Aiursoft.GitRunner.Exceptions;

namespace Aiursoft.Manhours.Services;

// Only classify failures from the remote operation, not arbitrary local Git or application errors.
public static class OriginFailure
{
    public static bool IsOriginFailure(Exception exception)
    {
        if (exception is not GitCommandException git)
        {
            return exception is RemoteTimeoutException;
        }

        if (!git.Command.StartsWith("clone ", StringComparison.OrdinalIgnoreCase) &&
            !git.Command.StartsWith("fetch ", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var output = git.GitError + "\n" + git.GitOutput;
        string[] markers =
        [
            "could not resolve host", "could not resolve proxy", "failed to connect", "connection refused",
            "connection timed out", "connection reset", "operation timed out", "network is unreachable",
            // TLS 连接失败属于远端访问失败，不应删除本地仓库后重试。
            "gnutls, handshake failed", "gnutls_handshake() failed", "gnutls recv error",
            "ssl connect error", "ssl connection timeout", "ssl certificate problem",
            "server certificate verification failed", "ssl peer certificate",
            "openssl ssl_connect", "tls connect error",
            "authentication failed", "authentication required", "could not read username",
            "terminal prompts disabled", "repository not found", "repository does not exist",
            "returned error: 401", "returned error: 403", "returned error: 404",
            "the requested url returned error: 401", "the requested url returned error: 403",
            "the requested url returned error: 404", "access denied", "permission denied"
        ];
        return markers.Any(marker => output.Contains(marker, StringComparison.OrdinalIgnoreCase));
    }
}

public sealed class RemoteTimeoutException(TimeoutException innerException)
    : Exception("The remote repository operation timed out.", innerException);
