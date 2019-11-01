using System;
using System.Collections;
using System.Collections.Generic;

namespace Socket.Quobject.Collections.Immutable {
  public class ImmutableDictionary<TKey, TValue> : IImmutableDictionary<TKey, TValue>, IReadOnlyDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
    where TKey : IComparable<TKey>
  {
    internal static readonly ImmutableDictionary<TKey, TValue> Empty = new ImmutableDictionary<TKey, TValue>();
    private AvlNode<KeyValuePair<TKey, TValue>> root = AvlNode<KeyValuePair<TKey, TValue>>.Empty;
    private readonly IEqualityComparer<TKey> keyComparer;
    private readonly IEqualityComparer<TValue> valueComparer;

    internal ImmutableDictionary()
    {
    }

    internal ImmutableDictionary(
      AvlNode<KeyValuePair<TKey, TValue>> root,
      IEqualityComparer<TKey> keyComparer,
      IEqualityComparer<TValue> valueComparer)
    {
      this.root = root;
      this.keyComparer = keyComparer;
      this.valueComparer = valueComparer;
    }

    public ImmutableDictionary<TKey, TValue> WithComparers(
      IEqualityComparer<TKey> keyComparer,
      IEqualityComparer<TValue> valueComparer)
    {
      return new ImmutableDictionary<TKey, TValue>(this.root, keyComparer, valueComparer);
    }

    public ImmutableDictionary<TKey, TValue> WithComparers(
      IEqualityComparer<TKey> keyComparer)
    {
      return this.WithComparers(keyComparer, this.valueComparer);
    }

    public ImmutableDictionary<TKey, TValue> Add(TKey key, TValue value)
    {
      return new ImmutableDictionary<TKey, TValue>(this.root.InsertIntoNew(new KeyValuePair<TKey, TValue>(key, value), new Comparison<KeyValuePair<TKey, TValue>>(ImmutableDictionary<TKey, TValue>.CompareKV)), this.keyComparer, this.valueComparer);
    }

    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Add(
      TKey key,
      TValue value)
    {
      return (IImmutableDictionary<TKey, TValue>) this.Add(key, value);
    }

    public ImmutableDictionary<TKey, TValue> AddRange(
      IEnumerable<KeyValuePair<TKey, TValue>> pairs)
    {
      ImmutableDictionary<TKey, TValue> immutableDictionary = this;
      foreach (KeyValuePair<TKey, TValue> pair in pairs)
        immutableDictionary = immutableDictionary.Add(pair.Key, pair.Value);
      return immutableDictionary;
    }

    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.AddRange(
      IEnumerable<KeyValuePair<TKey, TValue>> pairs)
    {
      return (IImmutableDictionary<TKey, TValue>) this.AddRange(pairs);
    }

    public ImmutableDictionary<TKey, TValue> Clear()
    {
      return ImmutableDictionary<TKey, TValue>.Empty;
    }

    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Clear()
    {
      return (IImmutableDictionary<TKey, TValue>) this.Clear();
    }

    private static int CompareKV(KeyValuePair<TKey, TValue> left, KeyValuePair<TKey, TValue> right)
    {
      return left.Key.CompareTo(right.Key);
    }

    public bool Contains(KeyValuePair<TKey, TValue> kv)
    {
      AvlNode<KeyValuePair<TKey, TValue>> avlNode = this.root.SearchNode(kv, new Comparison<KeyValuePair<TKey, TValue>>(ImmutableDictionary<TKey, TValue>.CompareKV));
      return !avlNode.IsEmpty && this.valueComparer.Equals(avlNode.Value.Value, kv.Value);
    }

    public ImmutableDictionary<TKey, TValue> Remove(TKey key)
    {
      bool found;
      return new ImmutableDictionary<TKey, TValue>(this.root.RemoveFromNew(new KeyValuePair<TKey, TValue>(key, default (TValue)), new Comparison<KeyValuePair<TKey, TValue>>(ImmutableDictionary<TKey, TValue>.CompareKV), out found), this.keyComparer, this.valueComparer);
    }

    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Remove(
      TKey key)
    {
      return (IImmutableDictionary<TKey, TValue>) this.Remove(key);
    }

    public IImmutableDictionary<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
    {
      IImmutableDictionary<TKey, TValue> immutableDictionary = (IImmutableDictionary<TKey, TValue>) this;
      foreach (TKey key in keys)
        immutableDictionary = immutableDictionary.Remove(key);
      return immutableDictionary;
    }

    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.RemoveRange(
      IEnumerable<TKey> keys)
    {
      return this.RemoveRange(keys);
    }

    public ImmutableDictionary<TKey, TValue> SetItem(TKey key, TValue value)
    {
      ImmutableDictionary<TKey, TValue> immutableDictionary = this;
      if (immutableDictionary.ContainsKey(key))
        immutableDictionary = immutableDictionary.Remove(key);
      return immutableDictionary.Add(key, value);
    }

    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItem(
      TKey key,
      TValue value)
    {
      return (IImmutableDictionary<TKey, TValue>) this.SetItem(key, value);
    }

    public IImmutableDictionary<TKey, TValue> SetItems(
      IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
      ImmutableDictionary<TKey, TValue> immutableDictionary = this;
      foreach (KeyValuePair<TKey, TValue> keyValuePair in items)
        immutableDictionary = immutableDictionary.SetItem(keyValuePair.Key, keyValuePair.Value);
      return (IImmutableDictionary<TKey, TValue>) immutableDictionary;
    }

    IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItems(
      IEnumerable<KeyValuePair<TKey, TValue>> items)
    {
      return this.SetItems(items);
    }

    public IEqualityComparer<TKey> KeyComparer
    {
      get
      {
        return this.keyComparer;
      }
    }

    public IEqualityComparer<TValue> ValueComparer
    {
      get
      {
        return this.valueComparer;
      }
    }

    public bool ContainsKey(TKey key)
    {
      return !this.root.SearchNode(new KeyValuePair<TKey, TValue>(key, default (TValue)), new Comparison<KeyValuePair<TKey, TValue>>(ImmutableDictionary<TKey, TValue>.CompareKV)).IsEmpty;
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
      AvlNode<KeyValuePair<TKey, TValue>> avlNode = this.root.SearchNode(new KeyValuePair<TKey, TValue>(key, default (TValue)), new Comparison<KeyValuePair<TKey, TValue>>(ImmutableDictionary<TKey, TValue>.CompareKV));
      if (avlNode.IsEmpty)
      {
        value = default (TValue);
        return false;
      }
      value = avlNode.Value.Value;
      return true;
    }

    public TValue this[TKey key]
    {
      get
      {
        TValue obj;
        if (this.TryGetValue(key, out obj))
          return obj;
        throw new KeyNotFoundException(string.Format("Key: {0}", (object) key));
      }
    }

    public IEnumerable<TKey> Keys
    {
      get
      {
        foreach (KeyValuePair<TKey, TValue> keyValuePair in this)
        {
          KeyValuePair<TKey, TValue> kv = keyValuePair;
          yield return kv.Key;
          kv = new KeyValuePair<TKey, TValue>();
        }
      }
    }

    public IEnumerable<TValue> Values
    {
      get
      {
        foreach (KeyValuePair<TKey, TValue> keyValuePair in this)
        {
          KeyValuePair<TKey, TValue> kv = keyValuePair;
          yield return kv.Value;
          kv = new KeyValuePair<TKey, TValue>();
        }
      }
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
      Stack<AvlNode<KeyValuePair<TKey, TValue>>> to_visit = new Stack<AvlNode<KeyValuePair<TKey, TValue>>>();
      to_visit.Push(this.root);
      while (to_visit.Count > 0)
      {
        AvlNode<KeyValuePair<TKey, TValue>> this_d = to_visit.Pop();
        if (!this_d.IsEmpty)
        {
          if (this_d.Left.IsEmpty)
          {
            yield return this_d.Value;
            to_visit.Push(this_d.Right);
          }
          else
          {
            to_visit.Push(this_d.Right);
            to_visit.Push(new AvlNode<KeyValuePair<TKey, TValue>>(this_d.Value));
            to_visit.Push(this_d.Left);
          }
          this_d = (AvlNode<KeyValuePair<TKey, TValue>>) null;
        }
      }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return (IEnumerator) this.GetEnumerator();
    }

    public int Count
    {
      get
      {
        return this.root.Count;
      }
    }

    public ImmutableDictionary<TKey, TValue>.Builder ToBuilder()
    {
      return new ImmutableDictionary<TKey, TValue>.Builder(this.root, this.keyComparer, this.valueComparer);
    }

    public sealed class Builder : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable
    {
      private AvlNode<KeyValuePair<TKey, TValue>> root;
      private IEqualityComparer<TKey> keyComparer;
      private IEqualityComparer<TValue> valueComparer;

      public IEqualityComparer<TKey> KeyComparer
      {
        get
        {
          return this.keyComparer;
        }
        set
        {
          this.keyComparer = value;
        }
      }

      public IEqualityComparer<TValue> ValueComparer
      {
        get
        {
          return this.valueComparer;
        }
        set
        {
          this.valueComparer = value;
        }
      }

      internal Builder(
        AvlNode<KeyValuePair<TKey, TValue>> root,
        IEqualityComparer<TKey> keyComparer,
        IEqualityComparer<TValue> valueComparer)
      {
        this.root = root.ToMutable();
        this.keyComparer = keyComparer;
        this.valueComparer = valueComparer;
      }

      public ImmutableDictionary<TKey, TValue> ToImmutable()
      {
        return new ImmutableDictionary<TKey, TValue>(this.root, this.keyComparer, this.valueComparer);
      }

      public void Add(TKey key, TValue value)
      {
        this.Add(new KeyValuePair<TKey, TValue>(key, value));
      }

      public bool ContainsKey(TKey key)
      {
        return !this.root.SearchNode(new KeyValuePair<TKey, TValue>(key, default (TValue)), new Comparison<KeyValuePair<TKey, TValue>>(ImmutableDictionary<TKey, TValue>.CompareKV)).IsEmpty;
      }

      public bool Remove(TKey key)
      {
        bool found;
        this.root = this.root.RemoveFromNew(new KeyValuePair<TKey, TValue>(key, default (TValue)), new Comparison<KeyValuePair<TKey, TValue>>(ImmutableDictionary<TKey, TValue>.CompareKV), out found);
        return found;
      }

      public void SetItem(TKey key, TValue value)
      {
        if (this.ContainsKey(key))
          this.Remove(key);
        this.Add(key, value);
      }

      public bool TryGetValue(TKey key, out TValue value)
      {
        AvlNode<KeyValuePair<TKey, TValue>> avlNode = this.root.SearchNode(new KeyValuePair<TKey, TValue>(key, default (TValue)), new Comparison<KeyValuePair<TKey, TValue>>(ImmutableDictionary<TKey, TValue>.CompareKV));
        if (avlNode.IsEmpty)
        {
          value = default (TValue);
          return false;
        }
        value = avlNode.Value.Value;
        return true;
      }

      public TValue this[TKey key]
      {
        get
        {
          TValue obj;
          if (this.TryGetValue(key, out obj))
            return obj;
          throw new KeyNotFoundException(string.Format("Key: {0}", (object) key));
        }
        set
        {
          if (this.ContainsKey(key))
            this.Remove(key);
          this.Add(key, value);
        }
      }

      ICollection<TKey> IDictionary<TKey, TValue>.Keys
      {
        get
        {
          return (ICollection<TKey>) new List<TKey>(this.Keys);
        }
      }

      public IEnumerable<TKey> Keys
      {
        get
        {
          foreach (KeyValuePair<TKey, TValue> keyValuePair in this)
          {
            KeyValuePair<TKey, TValue> kv = keyValuePair;
            yield return kv.Key;
            kv = new KeyValuePair<TKey, TValue>();
          }
        }
      }

      ICollection<TValue> IDictionary<TKey, TValue>.Values
      {
        get
        {
          return (ICollection<TValue>) new List<TValue>(this.Values);
        }
      }

      public IEnumerable<TValue> Values
      {
        get
        {
          foreach (KeyValuePair<TKey, TValue> keyValuePair in this)
          {
            KeyValuePair<TKey, TValue> kv = keyValuePair;
            yield return kv.Value;
            kv = new KeyValuePair<TKey, TValue>();
          }
        }
      }

      public void Add(KeyValuePair<TKey, TValue> item)
      {
        this.root = this.root.InsertIntoNew(item, new Comparison<KeyValuePair<TKey, TValue>>(ImmutableDictionary<TKey, TValue>.CompareKV));
      }

      public void Clear()
      {
        this.root = new AvlNode<KeyValuePair<TKey, TValue>>().ToMutable();
      }

      public bool Contains(KeyValuePair<TKey, TValue> item)
      {
        TValue x;
        if (!this.TryGetValue(item.Key, out x))
          return false;
        return this.valueComparer.Equals(x, item.Value);
      }

      public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
      {
        if (arrayIndex < 0 || arrayIndex + this.Count > array.Length)
          throw new ArgumentOutOfRangeException(nameof (arrayIndex));
        foreach (KeyValuePair<TKey, TValue> keyValuePair in this)
          array[arrayIndex++] = keyValuePair;
      }

      public bool Remove(KeyValuePair<TKey, TValue> item)
      {
        if (!this.Contains(item))
          return false;
        this.Remove(item.Key);
        return true;
      }

      public int Count
      {
        get
        {
          return this.root.Count;
        }
      }

      public bool IsReadOnly
      {
        get
        {
          return false;
        }
      }

      public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
      {
        return this.root.GetEnumerator(false);
      }

      IEnumerator IEnumerable.GetEnumerator()
      {
        return (IEnumerator) this.GetEnumerator();
      }
    }
  }
}
