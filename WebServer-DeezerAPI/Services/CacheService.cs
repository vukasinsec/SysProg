using System.Collections.Generic;

namespace WebServer_DeezerAPI.Services
{
    public class LRUCache<TKey, TValue>
    {
        private readonly int _capacity;
        private readonly LinkedList<TKey> _accessOrder;
        private readonly Dictionary<TKey, TValue> _cache;

        public LRUCache(int capacity)
        {
            _capacity = capacity;
            _accessOrder = new LinkedList<TKey>();
            _cache = new Dictionary<TKey, TValue>();
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            lock (_accessOrder)
            {
                if (_cache.TryGetValue(key, out value))
                {
                    _accessOrder.Remove(key);
                    _accessOrder.AddLast(key);
                    return true;
                }
                return false;
            }
        }

        public void AddOrUpdate(TKey key, TValue value)
        {
            lock (_accessOrder)
            {
                if (_cache.ContainsKey(key))
                {
                    _accessOrder.Remove(key);
                }
                else if (_cache.Count >= _capacity)
                {
                    TKey leastUsed = _accessOrder.First.Value;
                    _cache.Remove(leastUsed);
                    _accessOrder.RemoveFirst();
                }
                _cache[key] = value;
                _accessOrder.AddLast(key);
            }
        }
    }
}
