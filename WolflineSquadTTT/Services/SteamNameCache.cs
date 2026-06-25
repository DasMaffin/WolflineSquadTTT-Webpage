using System.Diagnostics.CodeAnalysis;

namespace WolflineSquadTTT.Services
{
    // Global LRU cache of the most-recently-used SteamID -> persona name mappings, so recently
    // seen players don't trigger another Steam API lookup. Bounded to the last 100 ids.
    // Registered as a singleton (SteamService itself is a per-request typed HttpClient).
    public interface ISteamNameCache
    {
        bool TryGet(ulong steamId, [MaybeNullWhen(false)] out string name);
        void Set(ulong steamId, string name);
    }

    public class SteamNameCache : ISteamNameCache
    {
        private const int Capacity = 100;

        private readonly object _lock = new();
        private readonly Dictionary<ulong, LinkedListNode<KeyValuePair<ulong, string>>> _map = new();
        private readonly LinkedList<KeyValuePair<ulong, string>> _recent = new();

        public bool TryGet(ulong steamId, [MaybeNullWhen(false)] out string name)
        {
            lock (_lock)
            {
                if (_map.TryGetValue(steamId, out var node))
                {
                    _recent.Remove(node);
                    _recent.AddFirst(node); // mark most-recently-used
                    name = node.Value.Value;
                    return true;
                }
            }

            name = null;
            return false;
        }

        public void Set(ulong steamId, string name)
        {
            lock (_lock)
            {
                if (_map.TryGetValue(steamId, out var existing))
                    _recent.Remove(existing);

                var node = new LinkedListNode<KeyValuePair<ulong, string>>(new KeyValuePair<ulong, string>(steamId, name));
                _recent.AddFirst(node);
                _map[steamId] = node;

                // Evict least-recently-used beyond the cap.
                while (_map.Count > Capacity && _recent.Last != null)
                {
                    var lru = _recent.Last;
                    _recent.RemoveLast();
                    _map.Remove(lru.Value.Key);
                }
            }
        }
    }
}
