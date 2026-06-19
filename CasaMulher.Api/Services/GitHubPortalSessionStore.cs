using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace CasaMulher.Api.Services;

public sealed record GitHubPortalSession(
    string GitHubId,
    string GitHubUsername,
    string AccessToken,
    DateTimeOffset EmitidoEm);

public sealed class GitHubPortalSessionStore
{
    private static readonly TimeSpan Lifetime = TimeSpan.FromHours(12);
    private readonly ConcurrentDictionary<string, GitHubPortalSession> _sessions = new();

    public string Create(string githubId, string githubUsername, string accessToken)
    {
        LimparExpiradas();
        var sessionId = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        _sessions[sessionId] = new GitHubPortalSession(githubId, githubUsername, accessToken, DateTimeOffset.UtcNow);
        return sessionId;
    }

    public bool TryGet(string sessionId, out GitHubPortalSession? session)
    {
        session = null;

        if (!_sessions.TryGetValue(sessionId, out var stored))
        {
            return false;
        }

        if (DateTimeOffset.UtcNow - stored.EmitidoEm > Lifetime)
        {
            _sessions.TryRemove(sessionId, out _);
            return false;
        }

        session = stored;
        return true;
    }

    public void Remove(string sessionId) => _sessions.TryRemove(sessionId, out _);

    private void LimparExpiradas()
    {
        var limite = DateTimeOffset.UtcNow - Lifetime;

        foreach (var item in _sessions.Where(item => item.Value.EmitidoEm < limite))
        {
            _sessions.TryRemove(item.Key, out _);
        }
    }
}
