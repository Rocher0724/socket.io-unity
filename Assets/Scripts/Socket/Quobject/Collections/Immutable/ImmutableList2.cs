using System;
using System.Collections.Generic;
using Socket.WebSocket4Net;
using Socket.WebSocket4Net.System.Linq;

namespace Socket.Quobject.Collections.Immutable {
  public static class ImmutableList
  {
    public static ImmutableList<T> Create<T>()
    {
      return ImmutableList<T>.Empty;
    }

    public static ImmutableList<T> Create<T>(
      IEqualityComparer<T> equalityComparer,
      params T[] items)
    {
      return ImmutableList<T>.Empty.WithComparer(equalityComparer).AddRange((IEnumerable<T>) items);
    }

    public static ImmutableList<T> Create<T>(params T[] items)
    {
      return Create<T>((IEqualityComparer<T>) EqualityComparer<T>.Default, items);
    }

    public static ImmutableList<T> Create<T>(
      IEqualityComparer<T> equalityComparer,
      IEnumerable<T> items)
    {
      return Create<T>(equalityComparer, items.ToArray<T>());
    }

    public static ImmutableList<T> Create<T>(IEnumerable<T> items)
    {
      return Create<T>(items.ToArray<T>());
    }

    public static ImmutableList<T> Create<T>(
      IEqualityComparer<T> equalityComparer,
      T item)
    {
      return ImmutableList<T>.Empty.WithComparer(equalityComparer).Add(item);
    }

    public static ImmutableList<T> Create<T>(T item)
    {
      return Create<T>((IEqualityComparer<T>) EqualityComparer<T>.Default, item);
    }

    public static ImmutableList<T> Create<T>(IEqualityComparer<T> equalityComparer)
    {
      return Create<T>().WithComparer(equalityComparer);
    }

    public static ImmutableList<T> ToImmutableList<T>(this IEnumerable<T> source)
    {
      if (source == null)
        throw new ArgumentNullException(nameof (source));
      return Create<T>().AddRange(source);
    }

    public static ImmutableList<T> ToImmutableList<T>(
      this IEnumerable<T> source,
      IEqualityComparer<T> equalityComparer)
    {
      if (source == null)
        throw new ArgumentNullException(nameof (source));
      return Create<T>().WithComparer(equalityComparer).AddRange(source);
    }
  }
}
