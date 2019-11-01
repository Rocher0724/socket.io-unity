using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Socket.Newtonsoft.Json.Utilities {
  internal class DictionaryWrapper<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>,
    IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IWrappedDictionary, IDictionary, ICollection {
    private readonly IDictionary _dictionary;
    private readonly IDictionary<TKey, TValue> _genericDictionary;
    private object _syncRoot;

    public DictionaryWrapper(IDictionary dictionary) {
      ValidationUtils.ArgumentNotNull((object) dictionary, nameof(dictionary));
      this._dictionary = dictionary;
    }

    public DictionaryWrapper(IDictionary<TKey, TValue> dictionary) {
      ValidationUtils.ArgumentNotNull((object) dictionary, nameof(dictionary));
      this._genericDictionary = dictionary;
    }

    public void Add(TKey key, TValue value) {
      if (this._dictionary != null) {
        this._dictionary.Add((object) key, (object) value);
      } else {
        if (this._genericDictionary == null)
          throw new NotSupportedException();
        this._genericDictionary.Add(key, value);
      }
    }

    public bool ContainsKey(TKey key) {
      if (this._dictionary != null)
        return this._dictionary.Contains((object) key);
      return this._genericDictionary.ContainsKey(key);
    }

    public ICollection<TKey> Keys {
      get {
        if (this._dictionary != null)
          return (ICollection<TKey>) this._dictionary.Keys.Cast<TKey>().ToList<TKey>();
        return this._genericDictionary.Keys;
      }
    }

    public bool Remove(TKey key) {
      if (this._dictionary == null)
        return this._genericDictionary.Remove(key);
      if (!this._dictionary.Contains((object) key))
        return false;
      this._dictionary.Remove((object) key);
      return true;
    }

    public bool TryGetValue(TKey key, out TValue value) {
      if (this._dictionary == null)
        return this._genericDictionary.TryGetValue(key, out value);
      if (!this._dictionary.Contains((object) key)) {
        value = default(TValue);
        return false;
      }

      value = (TValue) this._dictionary[(object) key];
      return true;
    }

    public ICollection<TValue> Values {
      get {
        if (this._dictionary != null)
          return (ICollection<TValue>) this._dictionary.Values.Cast<TValue>().ToList<TValue>();
        return this._genericDictionary.Values;
      }
    }

    public TValue this[TKey key] {
      get {
        if (this._dictionary != null)
          return (TValue) this._dictionary[(object) key];
        return this._genericDictionary[key];
      }
      set {
        if (this._dictionary != null)
          this._dictionary[(object) key] = (object) value;
        else
          this._genericDictionary[key] = value;
      }
    }

    public void Add(KeyValuePair<TKey, TValue> item) {
      if (this._dictionary != null)
        ((IList) this._dictionary).Add((object) item);
      else
        this._genericDictionary?.Add(item);
    }

    public void Clear() {
      if (this._dictionary != null)
        this._dictionary.Clear();
      else
        this._genericDictionary.Clear();
    }

    public bool Contains(KeyValuePair<TKey, TValue> item) {
      if (this._dictionary != null)
        return ((IList) this._dictionary).Contains((object) item);
      return this._genericDictionary.Contains(item);
    }

    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) {
      if (this._dictionary != null) {
        IDictionaryEnumerator enumerator = this._dictionary.GetEnumerator();
        try {
          while (enumerator.MoveNext()) {
            DictionaryEntry entry = enumerator.Entry;
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>((TKey) entry.Key, (TValue) entry.Value);
          }
        } finally {
          (enumerator as IDisposable)?.Dispose();
        }
      } else
        this._genericDictionary.CopyTo(array, arrayIndex);
    }

    public int Count {
      get {
        if (this._dictionary != null)
          return this._dictionary.Count;
        return this._genericDictionary.Count;
      }
    }

    public bool IsReadOnly {
      get {
        if (this._dictionary != null)
          return this._dictionary.IsReadOnly;
        return this._genericDictionary.IsReadOnly;
      }
    }

    public bool Remove(KeyValuePair<TKey, TValue> item) {
      if (this._dictionary == null)
        return ((ICollection<KeyValuePair<TKey, TValue>>) this._genericDictionary).Remove(item);
      if (!this._dictionary.Contains((object) item.Key))
        return true;
      if (!object.Equals(this._dictionary[(object) item.Key], (object) item.Value))
        return false;
      this._dictionary.Remove((object) item.Key);
      return true;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() {
      if (this._dictionary != null)
        return this._dictionary.Cast<DictionaryEntry>()
          .Select<DictionaryEntry, KeyValuePair<TKey, TValue>>(
            (Func<DictionaryEntry, KeyValuePair<TKey, TValue>>) (de =>
              new KeyValuePair<TKey, TValue>((TKey) de.Key, (TValue) de.Value))).GetEnumerator();
      return this._genericDictionary.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return (IEnumerator) this.GetEnumerator();
    }

    void IDictionary.Add(object key, object value) {
      if (this._dictionary != null)
        this._dictionary.Add(key, value);
      else
        this._genericDictionary.Add((TKey) key, (TValue) value);
    }

    object IDictionary.this[object key] {
      get {
        if (this._dictionary != null)
          return this._dictionary[key];
        return (object) this._genericDictionary[(TKey) key];
      }
      set {
        if (this._dictionary != null)
          this._dictionary[key] = value;
        else
          this._genericDictionary[(TKey) key] = (TValue) value;
      }
    }

    IDictionaryEnumerator IDictionary.GetEnumerator() {
      if (this._dictionary != null)
        return this._dictionary.GetEnumerator();
      return (IDictionaryEnumerator) new DictionaryWrapper<TKey, TValue>.DictionaryEnumerator<TKey, TValue>(
        this._genericDictionary.GetEnumerator());
    }

    bool IDictionary.Contains(object key) {
      if (this._genericDictionary != null)
        return this._genericDictionary.ContainsKey((TKey) key);
      return this._dictionary.Contains(key);
    }

    bool IDictionary.IsFixedSize {
      get {
        if (this._genericDictionary != null)
          return false;
        return this._dictionary.IsFixedSize;
      }
    }

    ICollection IDictionary.Keys {
      get {
        if (this._genericDictionary != null)
          return (ICollection) this._genericDictionary.Keys.ToList<TKey>();
        return this._dictionary.Keys;
      }
    }

    public void Remove(object key) {
      if (this._dictionary != null)
        this._dictionary.Remove(key);
      else
        this._genericDictionary.Remove((TKey) key);
    }

    ICollection IDictionary.Values {
      get {
        if (this._genericDictionary != null)
          return (ICollection) this._genericDictionary.Values.ToList<TValue>();
        return this._dictionary.Values;
      }
    }

    void ICollection.CopyTo(Array array, int index) {
      if (this._dictionary != null)
        this._dictionary.CopyTo(array, index);
      else
        this._genericDictionary.CopyTo((KeyValuePair<TKey, TValue>[]) array, index);
    }

    bool ICollection.IsSynchronized {
      get {
        if (this._dictionary != null)
          return this._dictionary.IsSynchronized;
        return false;
      }
    }

    object ICollection.SyncRoot {
      get {
        if (this._syncRoot == null)
          Interlocked.CompareExchange(ref this._syncRoot, new object(), (object) null);
        return this._syncRoot;
      }
    }

    public object UnderlyingDictionary {
      get {
        if (this._dictionary != null)
          return (object) this._dictionary;
        return (object) this._genericDictionary;
      }
    }

    private struct DictionaryEnumerator<TEnumeratorKey, TEnumeratorValue> : IDictionaryEnumerator, IEnumerator {
      private readonly IEnumerator<KeyValuePair<TEnumeratorKey, TEnumeratorValue>> _e;

      public DictionaryEnumerator(
        IEnumerator<KeyValuePair<TEnumeratorKey, TEnumeratorValue>> e) {
        ValidationUtils.ArgumentNotNull((object) e, nameof(e));
        this._e = e;
      }

      public DictionaryEntry Entry {
        get { return (DictionaryEntry) this.Current; }
      }

      public object Key {
        get { return this.Entry.Key; }
      }

      public object Value {
        get { return this.Entry.Value; }
      }

      public object Current {
        get {
          KeyValuePair<TEnumeratorKey, TEnumeratorValue> current = this._e.Current;
          var key = (object) current.Key;
          current = this._e.Current;
          var local = (object) current.Value;
          return (object) new DictionaryEntry((object) key, (object) local);
        }
      }

      public bool MoveNext() {
        return this._e.MoveNext();
      }

      public void Reset() {
        this._e.Reset();
      }
    }
  }
}