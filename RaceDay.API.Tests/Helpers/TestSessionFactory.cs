using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace RaceDay.API.Tests.Helpers;

public static class TestSessionFactory
{
    public static ISession Build()
    {
        var cache = new MemoryDistributedCache(
            new OptionsWrapper<MemoryDistributedCacheOptions>(
                new MemoryDistributedCacheOptions()));

        return new TestSession(cache);
    }

    private class TestSession : ISession
    {
        private readonly IDistributedCache _cache;

        public TestSession(IDistributedCache cache) => _cache = cache;

        public bool IsAvailable => true;
        public string Id => "test-session";
        public IEnumerable<string> Keys => Array.Empty<string>();

        public void Clear() { }

        public Task CommitAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task LoadAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Remove(string key) => _cache.Remove(key);

        public void Set(string key, byte[] value) =>
            _cache.Set(key, value, new DistributedCacheEntryOptions());

        public bool TryGetValue(string key, out byte[] value)
        {
            var v = _cache.Get(key);
            if (v == null) { value = Array.Empty<byte>(); return false; }
            value = v;
            return true;
        }
    }
}
