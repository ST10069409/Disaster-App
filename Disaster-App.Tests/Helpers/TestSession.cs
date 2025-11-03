using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;

namespace Disaster_App.Tests.Helpers
{
    /// <summary>
    /// A test implementation of ISession that stores data in memory
    /// </summary>
    public class TestSession : ISession
    {
        private readonly ConcurrentDictionary<string, byte[]> _store = new();

        public string Id { get; set; } = Guid.NewGuid().ToString();
        public bool IsAvailable => true;
        public IEnumerable<string> Keys => _store.Keys;

        public void Clear()
        {
            _store.Clear();
        }

        public Task CommitAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public void Remove(string key)
        {
            _store.TryRemove(key, out _);
        }

        public void Set(string key, byte[] value)
        {
            if (value == null)
            {
                _store.TryRemove(key, out _);
            }
            else
            {
                _store[key] = value;
            }
        }

        public bool TryGetValue(string key, out byte[] value)
        {
            return _store.TryGetValue(key, out value);
        }
    }
}

