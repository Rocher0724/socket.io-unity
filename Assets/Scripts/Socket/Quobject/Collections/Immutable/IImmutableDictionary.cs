using System.Collections;
using System.Collections.Generic;

namespace Socket.Quobject.Collections.Immutable {
  public interface IImmutableDictionary<TKey, TValue> : IReadOnlyDictionary<TKey, TValue>,
    IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable {
    IEqualityComparer<TValue> ValueComparer { get; }

    IImmutableDictionary<TKey, TValue> Add(TKey key, TValue value);

    IImmutableDictionary<TKey, TValue> AddRange(
      IEnumerable<KeyValuePair<TKey, TValue>> pairs);

    IImmutableDictionary<TKey, TValue> Clear();

    bool Contains(KeyValuePair<TKey, TValue> pair);

    IImmutableDictionary<TKey, TValue> Remove(TKey key);

    IImmutableDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys);

    IImmutableDictionary<TKey, TValue> SetItem(TKey key, TValue value);

    IImmutableDictionary<TKey, TValue> SetItems(
      IEnumerable<KeyValuePair<TKey, TValue>> items);
  }
}