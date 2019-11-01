using System;
using System.Collections;
using System.Collections.Generic;

namespace Socket.Quobject.Collections.Immutable {
  public interface IImmutableList<T> : IReadOnlyList<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable
  {
    IEqualityComparer<T> ValueComparer { get; }

    IImmutableList<T> Add(T value);

    IImmutableList<T> AddRange(IEnumerable<T> items);

    IImmutableList<T> Clear();

    bool Contains(T value);

    int IndexOf(T value);

    IImmutableList<T> Insert(int index, T element);

    IImmutableList<T> InsertRange(int index, IEnumerable<T> items);

    IImmutableList<T> Remove(T value);

    IImmutableList<T> RemoveAll(Predicate<T> match);

    IImmutableList<T> RemoveAt(int index);

    IImmutableList<T> RemoveRange(int index, int count);

    IImmutableList<T> RemoveRange(IEnumerable<T> items);

    IImmutableList<T> Replace(T oldValue, T newValue);

    IImmutableList<T> SetItem(int index, T value);

    IImmutableList<T> WithComparer(IEqualityComparer<T> equalityComparer);
  }
}
