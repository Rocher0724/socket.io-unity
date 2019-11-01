

using System;
using System.Collections.Generic;
using Socket.WebSocket4Net;
using System.Collections;
using Socket.WebSocket4Net.System.Linq;
using UnityEngine;

namespace Socket.Quobject.Collections.Immutable {
public class ImmutableList<T> : IImmutableList<T>, IReadOnlyList<T>, IReadOnlyCollection<T>, IEnumerable<T>, IEnumerable, IList<T>, ICollection<T>, IList, ICollection
  {
    public static readonly ImmutableList<T> Empty = new ImmutableList<T>();
    private readonly AvlNode<T> root = AvlNode<T>.Empty;
    private readonly IEqualityComparer<T> valueComparer;

    internal ImmutableList()
    {
      this.valueComparer = (IEqualityComparer<T>) EqualityComparer<T>.Default;
    }

    internal ImmutableList(AvlNode<T> root, IEqualityComparer<T> equalityComparer)
    {
      this.root = root;
      this.valueComparer = equalityComparer;
    }

    public void CopyTo(int index, T[] array, int arrayIndex, int count)
    {
      if (index < 0 || index >= this.Count)
        throw new ArgumentOutOfRangeException(nameof (index));
      if (array == null)
        throw new ArgumentNullException(nameof (array));
      if (arrayIndex < 0 || arrayIndex + count > array.Length)
        throw new ArgumentOutOfRangeException(nameof (arrayIndex));
      if (count < 0 || index + count > this.Count)
        throw new ArgumentOutOfRangeException(nameof (count));
      foreach (T obj in this.root.Enumerate(index, count, false))
        array[arrayIndex++] = obj;
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
      if (array == null)
        throw new ArgumentNullException(nameof (array));
      if (arrayIndex < 0 || arrayIndex + this.Count > array.Length)
        throw new ArgumentOutOfRangeException(nameof (arrayIndex));
      this.CopyTo(0, array, 0, this.Count);
    }

    public void CopyTo(T[] array)
    {
      if (array == null)
        throw new ArgumentNullException(nameof (array));
      this.CopyTo(array, 0);
    }

    public bool Exists(Predicate<T> match)
    {
      if (match == null)
        throw new ArgumentNullException(nameof (match));
      foreach (T obj in this)
      {
        if (match(obj))
          return true;
      }
      return false;
    }

    public T Find(Predicate<T> match)
    {
      if (match == null)
        throw new ArgumentNullException(nameof (match));
      return this.FirstOrDefault<T>((Predicate<T>) (i => match(i)));
    }

    public ImmutableList<T> FindAll(Predicate<T> match)
    {
      if (match == null)
        throw new ArgumentNullException(nameof (match));
      ImmutableList<T>.Builder builder = this.Clear().ToBuilder();
      foreach (T obj in this)
      {
        if (match(obj))
          builder.Add(obj);
      }
      return builder.ToImmutable();
    }

    public int FindIndex(int startIndex, int count, Predicate<T> match)
    {
      if (startIndex < 0 || startIndex >= this.Count)
        throw new ArgumentOutOfRangeException(nameof (startIndex));
      if (count < 0 || startIndex + count > this.Count)
        throw new ArgumentOutOfRangeException(nameof (count));
      if (match == null)
        throw new ArgumentNullException(nameof (match));
      int num = startIndex;
      foreach (T obj in this.root.Enumerate(startIndex, count, false))
      {
        if (match(obj))
          return num;
        ++num;
      }
      return -1;
    }

    public int FindIndex(Predicate<T> match)
    {
      if (match == null)
        throw new ArgumentNullException(nameof (match));
      return this.FindIndex(0, this.Count, match);
    }

    public int FindIndex(int startIndex, Predicate<T> match)
    {
      if (startIndex < 0 || startIndex >= this.Count)
        throw new ArgumentOutOfRangeException(nameof (startIndex));
      if (match == null)
        throw new ArgumentNullException(nameof (match));
      return this.FindIndex(startIndex, this.Count - startIndex, match);
    }

    public T FindLast(Predicate<T> match)
    {
      if (match == null)
        throw new ArgumentNullException(nameof (match));
      int lastIndex = this.FindLastIndex(match);
      if (lastIndex >= 0)
        return this[lastIndex];
      return default (T);
    }

    public int FindLastIndex(Predicate<T> match)
    {
      if (match == null)
        throw new ArgumentNullException(nameof (match));
      return this.FindLastIndex(this.Count - 1, this.Count, match);
    }

    public int FindLastIndex(int startIndex, Predicate<T> match)
    {
      if (startIndex < 0 || startIndex >= this.Count)
        throw new ArgumentOutOfRangeException(nameof (startIndex));
      if (match == null)
        throw new ArgumentNullException(nameof (match));
      return this.FindLastIndex(startIndex, startIndex + 1, match);
    }

    public int FindLastIndex(int startIndex, int count, Predicate<T> match)
    {
      if (startIndex < 0 || startIndex >= this.Count)
        throw new ArgumentOutOfRangeException(nameof (startIndex));
      if (count > this.Count || startIndex - count + 1 < 0)
        throw new ArgumentOutOfRangeException(nameof (count));
      if (match == null)
        throw new ArgumentNullException(nameof (match));
      int num = startIndex;
      foreach (T obj in this.root.Enumerate(startIndex, count, true))
      {
        if (match(obj))
          return num;
        --num;
      }
      return -1;
    }

    public void ForEach(Action<T> action)
    {
      if (action == null)
        throw new ArgumentNullException(nameof (action));
      foreach (T obj in this)
        action(obj);
    }

    public ImmutableList<T> GetRange(int index, int count)
    {
      return ImmutableList.Create<T>(this.valueComparer, this.root.Enumerate(index, count, false));
    }

    public int IndexOf(T value)
    {
      return this.IndexOf(value, 0, this.Count);
    }

    public int IndexOf(T value, int index)
    {
      if (index < 0 || index >= this.Count)
        throw new ArgumentOutOfRangeException(nameof (index));
      return this.IndexOf(value, 0, this.Count - index);
    }

    public int IndexOf(T value, int index, int count)
    {
      if (index < 0 || index >= this.Count)
        throw new ArgumentOutOfRangeException(nameof (index));
      if (count < 0 || index + count > this.Count)
        throw new ArgumentOutOfRangeException(nameof (count));
      return this.FindIndex(index, count, (Predicate<T>) (i => this.valueComparer.Equals(value, i)));
    }

    public int LastIndexOf(T item, int index)
    {
      if (index < 0 || index >= this.Count)
        throw new ArgumentOutOfRangeException(nameof (index));
      return this.LastIndexOf(item, index, index + 1);
    }

    public int LastIndexOf(T item)
    {
      return this.LastIndexOf(item, this.Count - 1, this.Count);
    }

    public int LastIndexOf(T item, int index, int count)
    {
      if (index < 0 || index >= this.Count)
        throw new ArgumentOutOfRangeException(nameof (index));
      if (count > this.Count || index - count + 1 < 0)
        throw new ArgumentOutOfRangeException(nameof (count));
      return this.FindLastIndex(index, count, (Predicate<T>) (i => this.valueComparer.Equals(item, i)));
    }

    int IList.Add(object value)
    {
      throw new NotSupportedException();
    }

    void IList.Clear()
    {
      throw new NotSupportedException();
    }

    bool IList.Contains(object value)
    {
      return this.Contains((T) value);
    }

    int IList.IndexOf(object value)
    {
      return this.IndexOf((T) value);
    }

    void IList.Insert(int index, object value)
    {
      throw new NotSupportedException();
    }

    void IList.Remove(object value)
    {
      throw new NotSupportedException();
    }

    void IList.RemoveAt(int index)
    {
      throw new NotSupportedException();
    }

    bool IList.IsFixedSize
    {
      get
      {
        return true;
      }
    }

    object IList.this[int index]
    {
      get
      {
        return (object) this[index];
      }
      set
      {
        throw new NotSupportedException();
      }
    }

    bool IList.IsReadOnly
    {
      get
      {
        return true;
      }
    }

    void ICollection.CopyTo(Array array, int index)
    {
      foreach (T obj in this)
        array.SetValue((object) obj, index++);
    }

    bool ICollection.IsSynchronized
    {
      get
      {
        return true;
      }
    }

    object ICollection.SyncRoot
    {
      get
      {
        return (object) this;
      }
    }

    T IList<T>.this[int index]
    {
      get
      {
        return this[index];
      }
      set
      {
        throw new NotSupportedException();
      }
    }

    void IList<T>.Insert(int index, T item)
    {
      throw new NotSupportedException();
    }

    void IList<T>.RemoveAt(int index)
    {
      throw new NotSupportedException();
    }

    void ICollection<T>.Add(T item)
    {
      throw new NotSupportedException();
    }

    void ICollection<T>.Clear()
    {
      throw new NotSupportedException();
    }

    void ICollection<T>.CopyTo(T[] array, int arrayIndex)
    {
      this.CopyTo(array, arrayIndex);
    }

    bool ICollection<T>.Remove(T item)
    {
      throw new NotSupportedException();
    }

    bool ICollection<T>.IsReadOnly
    {
      get
      {
        return true;
      }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return (IEnumerator) this.GetEnumerator();
    }

    public ImmutableList<T> Add(T value)
    {
      return this.Insert(this.Count, value);
    }

    IImmutableList<T> IImmutableList<T>.Add(T value)
    {
      return (IImmutableList<T>) this.Add(value);
    }

    public ImmutableList<T> AddRange(IEnumerable<T> items)
    {
      return this.InsertRange(this.Count, items);
    }

    IImmutableList<T> IImmutableList<T>.AddRange(IEnumerable<T> items)
    {
      return (IImmutableList<T>) this.AddRange(items);
    }

    public ImmutableList<T> Clear()
    {
      return ImmutableList<T>.Empty.WithComparer(this.valueComparer);
    }

    IImmutableList<T> IImmutableList<T>.Clear()
    {
      return (IImmutableList<T>) this.Clear();
    }

    public bool Contains(T value)
    {
      return this.IndexOf(value) != -1;
    }

    public ImmutableList<T> Insert(int index, T element)
    {
      if (index > this.Count)
        throw new ArgumentOutOfRangeException(nameof (index));
      return new ImmutableList<T>(this.root.InsertIntoNew(index, element), this.valueComparer);
    }

    IImmutableList<T> IImmutableList<T>.Insert(int index, T element)
    {
      return (IImmutableList<T>) this.Insert(index, element);
    }

    public ImmutableList<T> InsertRange(int index, IEnumerable<T> items)
    {
      ImmutableList<T> immutableList = this;
      foreach (T element in items)
        immutableList = immutableList.Insert(index++, element);
      return immutableList;
    }

    IImmutableList<T> IImmutableList<T>.InsertRange(
      int index,
      IEnumerable<T> items)
    {
      return (IImmutableList<T>) this.InsertRange(index, items);
    }

    public ImmutableList<T> Remove(T value)
    {
      int index = this.IndexOf(value);
      if (index != -1)
        return this.RemoveAt(index);
      return this;
    }

    IImmutableList<T> IImmutableList<T>.Remove(T value)
    {
      return (IImmutableList<T>) this.Remove(value);
    }

    public ImmutableList<T> RemoveAll(Predicate<T> match)
    {
      if (match == null)
        throw new ArgumentNullException(nameof (match));
      ImmutableList<T> immutableList = this;
      for (int index = 0; index < immutableList.Count; ++index)
      {
        if (match(immutableList[index]))
        {
          immutableList = immutableList.RemoveAt(index);
          --index;
        }
      }
      return immutableList;
    }

    IImmutableList<T> IImmutableList<T>.RemoveAll(Predicate<T> match)
    {
      return (IImmutableList<T>) this.RemoveAll(match);
    }

    public ImmutableList<T> RemoveAt(int index)
    {
      bool found;
      return new ImmutableList<T>(this.root.RemoveFromNew(index, out found), this.valueComparer);
    }

    IImmutableList<T> IImmutableList<T>.RemoveAt(int index)
    {
      return (IImmutableList<T>) this.RemoveAt(index);
    }

    private void CheckRange(int idx, int count)
    {
      if (idx < 0)
        throw new ArgumentOutOfRangeException("index");
      if (count < 0)
        throw new ArgumentOutOfRangeException(nameof (count));
      if ((uint) (idx + count) > (uint) this.Count)
        throw new ArgumentException("index and count exceed length of list");
    }

    public ImmutableList<T> RemoveRange(int index, int count)
    {
      this.CheckRange(index, count);
      ImmutableList<T> immutableList = this;
      while (count-- > 0)
        immutableList = immutableList.RemoveAt(index);
      return immutableList;
    }

    IImmutableList<T> IImmutableList<T>.RemoveRange(
      int index,
      int count)
    {
      return (IImmutableList<T>) this.RemoveRange(index, count);
    }

    public ImmutableList<T> RemoveRange(IEnumerable<T> items)
    {
      ImmutableList<T> immutableList = this;
      foreach (T obj in items)
        immutableList = immutableList.Remove(obj);
      return immutableList;
    }

    IImmutableList<T> IImmutableList<T>.RemoveRange(IEnumerable<T> items)
    {
      return (IImmutableList<T>) this.RemoveRange(items);
    }

    public ImmutableList<T> Replace(T oldValue, T newValue)
    {
      int index = this.IndexOf(oldValue);
      if (index < 0)
        return this;
      return this.SetItem(index, newValue);
    }

    IImmutableList<T> IImmutableList<T>.Replace(T oldValue, T newValue)
    {
      return (IImmutableList<T>) this.Replace(oldValue, newValue);
    }

    public ImmutableList<T> SetItem(int index, T value)
    {
      if (index > this.Count)
        throw new ArgumentOutOfRangeException(nameof (index));
      return new ImmutableList<T>(this.root.SetItem(index, value), this.valueComparer);
    }

    IImmutableList<T> IImmutableList<T>.SetItem(int index, T value)
    {
      return (IImmutableList<T>) this.SetItem(index, value);
    }

    public ImmutableList<T> WithComparer(IEqualityComparer<T> equalityComparer)
    {
      return new ImmutableList<T>(this.root, equalityComparer);
    }

    IImmutableList<T> IImmutableList<T>.WithComparer(
      IEqualityComparer<T> equalityComparer)
    {
      return (IImmutableList<T>) this.WithComparer(equalityComparer);
    }

    public IEqualityComparer<T> ValueComparer
    {
      get
      {
        return this.valueComparer;
      }
    }

    public IEnumerator<T> GetEnumerator()
    {
      return this.root.GetEnumerator(false);
    }

    public T this[int index]
    {
      get
      {
        if (index >= this.Count)
          throw new ArgumentOutOfRangeException(nameof (index));
        return this.root.GetNodeAt(index).Value;
      }
    }

    public int Count
    {
      get
      {
        return this.root.Count;
      }
    }

    public ImmutableList<T>.Builder ToBuilder()
    {
      return new ImmutableList<T>.Builder(this.root, this.valueComparer);
    }

    public class Builder
    {
      private AvlNode<T> root;
      private readonly IEqualityComparer<T> valueComparer;

      internal Builder(AvlNode<T> immutableRoot, IEqualityComparer<T> comparer)
      {
        this.root = immutableRoot.ToMutable();
        this.valueComparer = comparer;
      }

      public ImmutableList<T> ToImmutable()
      {
        return new ImmutableList<T>(this.root.ToImmutable(), this.valueComparer);
      }

      public void Add(T value)
      {
        this.Insert(this.Count, value);
      }

      public void Insert(int index, T element)
      {
        if (index > this.Count)
          throw new ArgumentOutOfRangeException(nameof (index));
        this.root = this.root.InsertIntoNew(index, element);
        Debug.Assert(this.root.IsMutable);
      }

      public int Count
      {
        get
        {
          return this.root.Count;
        }
      }
    }
  }
}
