using System;
using System.Collections.Generic;

namespace Socket.Quobject.Collections.Immutable {
  public static class ImmutableDictionary {
    public static bool Contains<TKey, TValue>(
      this IImmutableDictionary<TKey, TValue> dictionary,
      TKey key,
      TValue value) {
      if (dictionary == null)
        throw new ArgumentNullException(nameof(dictionary));
      return dictionary.Contains(new KeyValuePair<TKey, TValue>(key, value));
    }

    public static ImmutableDictionary<TKey, TValue> Create<TKey, TValue>() where TKey : IComparable<TKey> {
      return ImmutableDictionary<TKey, TValue>.Empty;
    }

    public static ImmutableDictionary<TKey, TValue> Create<TKey, TValue>(
      IEqualityComparer<TKey> keyComparer,
      IEqualityComparer<TValue> valueComparer,
      IEnumerable<KeyValuePair<TKey, TValue>> items)
      where TKey : IComparable<TKey> {
      return Create<TKey, TValue>(keyComparer, valueComparer).AddRange(items);
    }

    public static ImmutableDictionary<TKey, TValue> Create<TKey, TValue>(
      IEqualityComparer<TKey> keyComparer,
      IEnumerable<KeyValuePair<TKey, TValue>> items)
      where TKey : IComparable<TKey> {
      return Create<TKey, TValue>(keyComparer).AddRange(items);
    }

    public static ImmutableDictionary<TKey, TValue> Create<TKey, TValue>(
      IEnumerable<KeyValuePair<TKey, TValue>> items)
      where TKey : IComparable<TKey> {
      return Create<TKey, TValue>().AddRange(items);
    }

    public static ImmutableDictionary<TKey, TValue> Create<TKey, TValue>(
      IEqualityComparer<TKey> keyComparer,
      IEqualityComparer<TValue> valueComparer)
      where TKey : IComparable<TKey> {
      return Create<TKey, TValue>().WithComparers(keyComparer, valueComparer);
    }

    public static ImmutableDictionary<TKey, TValue> Create<TKey, TValue>(
      IEqualityComparer<TKey> keyComparer)
      where TKey : IComparable<TKey> {
      return Create<TKey, TValue>().WithComparers(keyComparer);
    }

    public static TValue GetValueOrDefault<TKey, TValue>(
      this ImmutableDictionary<TKey, TValue> dictionary,
      TKey key)
      where TKey : IComparable<TKey> {
      return dictionary.GetValueOrDefault<TKey, TValue>(key, default(TValue));
    }

    public static TValue GetValueOrDefault<TKey, TValue>(
      this ImmutableDictionary<TKey, TValue> dictionary,
      TKey key,
      TValue defaultValue)
      where TKey : IComparable<TKey> {
      if (dictionary == null)
        throw new ArgumentNullException(nameof(dictionary));
      TValue obj;
      if (dictionary.TryGetValue(key, out obj))
        return obj;
      return defaultValue;
    }

    public static TValue GetValueOrDefault<TKey, TValue>(
      this IDictionary<TKey, TValue> dictionary,
      TKey key)
      where TKey : IComparable<TKey> {
      return dictionary.GetValueOrDefault<TKey, TValue>(key, default(TValue));
    }

    public static TValue GetValueOrDefault<TKey, TValue>(
      this IDictionary<TKey, TValue> dictionary,
      TKey key,
      TValue defaultValue)
      where TKey : IComparable<TKey> {
      if (dictionary == null)
        throw new ArgumentNullException(nameof(dictionary));
      TValue obj;
      if (dictionary.TryGetValue(key, out obj))
        return obj;
      return defaultValue;
    }

    public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(
      this IEnumerable<KeyValuePair<TKey, TValue>> source,
      IEqualityComparer<TKey> keyComparer)
      where TKey : IComparable<TKey> {
      return source.ToImmutableDictionary<TKey, TValue>(keyComparer, (IEqualityComparer<TValue>) null);
    }

    public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(
      this IEnumerable<KeyValuePair<TKey, TValue>> source)
      where TKey : IComparable<TKey> {
      return source.ToImmutableDictionary<TKey, TValue>((IEqualityComparer<TKey>) null,
        (IEqualityComparer<TValue>) null);
    }

    public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TKey, TValue>(
      this IEnumerable<KeyValuePair<TKey, TValue>> source,
      IEqualityComparer<TKey> keyComparer,
      IEqualityComparer<TValue> valueComparer)
      where TKey : IComparable<TKey> {
      if (source == null)
        throw new ArgumentNullException("dictionary");
      return Create<TKey, TValue>(keyComparer, valueComparer).AddRange(source);
    }

    public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TSource, TKey, TValue>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      Func<TSource, TValue> elementSelector,
      IEqualityComparer<TKey> keyComparer)
      where TKey : IComparable<TKey> {
      return source.ToImmutableDictionary<TSource, TKey, TValue>(keySelector, elementSelector, keyComparer,
        (IEqualityComparer<TValue>) null);
    }

    public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TSource, TKey, TValue>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      Func<TSource, TValue> elementSelector,
      IEqualityComparer<TKey> keyComparer,
      IEqualityComparer<TValue> valueComparer)
      where TKey : IComparable<TKey> {
      if (source == null)
        throw new ArgumentNullException("dictionary");
      ImmutableList<TSource> immutableList = source.ToImmutableList<TSource>();
      List<KeyValuePair<TKey, TValue>> keyValuePairList = new List<KeyValuePair<TKey, TValue>>();
      foreach (TSource t in immutableList)
        keyValuePairList.Add(new KeyValuePair<TKey, TValue>(keySelector(t), elementSelector(t)));
      return Create<TKey, TValue>(keyComparer, valueComparer)
        .AddRange((IEnumerable<KeyValuePair<TKey, TValue>>) keyValuePairList);
    }

    public static ImmutableDictionary<TKey, TValue> ToImmutableDictionary<TSource, TKey, TValue>(
      this IEnumerable<TSource> source,
      Func<TSource, TKey> keySelector,
      Func<TSource, TValue> elementSelector)
      where TKey : IComparable<TKey> {
      return source.ToImmutableDictionary<TSource, TKey, TValue>(keySelector, elementSelector,
        (IEqualityComparer<TKey>) null, (IEqualityComparer<TValue>) null);
    }
  }
}