using System.Collections.Concurrent;

namespace MyDash.Hub.Services;

public sealed class PendingAuthStore
{
    private readonly ConcurrentDictionary<string, DateTimeOffset> _tokens = new();

    public string Issue()
    {
        var token = Guid.NewGuid().ToString("N");
        _tokens[token] = DateTimeOffset.UtcNow.AddSeconds(30);
        return token;
    }

    public bool Consume(string token)
    {
        if (_tokens.TryRemove(token, out var expires))
            return expires > DateTimeOffset.UtcNow;
        return false;
    }
}
