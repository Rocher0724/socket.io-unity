using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Linq {
  internal class JPropertyKeyedCollection : Collection<JToken>
  {
    private static readonly IEqualityComparer<string> Comparer = (IEqualityComparer<string>) StringComparer.Ordinal;
    private Dictionary<string, JToken> _dictionary;

    public JPropertyKeyedCollection()
      : base((IList<JToken>) new List<JToken>())
    {
    }

    private void AddKey(string key, JToken item)
    {
      this.EnsureDictionary();
      this._dictionary[key] = item;
    }

    protected void ChangeItemKey(JToken item, string newKey)
    {
      if (!this.ContainsItem(item))
        throw new ArgumentException("The specified item does not exist in this KeyedCollection.");
      string keyForItem = this.GetKeyForItem(item);
      if (JPropertyKeyedCollection.Comparer.Equals(keyForItem, newKey))
        return;
      if (newKey != null)
        this.AddKey(newKey, item);
      if (keyForItem == null)
        return;
      this.RemoveKey(keyForItem);
    }

    protected override void ClearItems()
    {
      base.ClearItems();
      this._dictionary?.Clear();
    }

    public bool Contains(string key)
    {
      if (key == null)
        throw new ArgumentNullException(nameof (key));
      if (this._dictionary != null)
        return this._dictionary.ContainsKey(key);
      return false;
    }

    private bool ContainsItem(JToken item)
    {
      if (this._dictionary == null)
        return false;
      JToken jtoken;
      return this._dictionary.TryGetValue(this.GetKeyForItem(item), out jtoken);
    }

    private void EnsureDictionary()
    {
      if (this._dictionary != null)
        return;
      this._dictionary = new Dictionary<string, JToken>(JPropertyKeyedCollection.Comparer);
    }

    private string GetKeyForItem(JToken item)
    {
      return ((JProperty) item).Name;
    }

    protected override void InsertItem(int index, JToken item)
    {
      this.AddKey(this.GetKeyForItem(item), item);
      base.InsertItem(index, item);
    }

    public bool Remove(string key)
    {
      if (key == null)
        throw new ArgumentNullException(nameof (key));
      if (this._dictionary != null && this._dictionary.ContainsKey(key))
        return this.Remove(this._dictionary[key]);
      return false;
    }

    protected override void RemoveItem(int index)
    {
      this.RemoveKey(this.GetKeyForItem(this.Items[index]));
      base.RemoveItem(index);
    }

    private void RemoveKey(string key)
    {
      this._dictionary?.Remove(key);
    }

    protected override void SetItem(int index, JToken item)
    {
      string keyForItem1 = this.GetKeyForItem(item);
      string keyForItem2 = this.GetKeyForItem(this.Items[index]);
      if (JPropertyKeyedCollection.Comparer.Equals(keyForItem2, keyForItem1))
      {
        if (this._dictionary != null)
          this._dictionary[keyForItem1] = item;
      }
      else
      {
        this.AddKey(keyForItem1, item);
        if (keyForItem2 != null)
          this.RemoveKey(keyForItem2);
      }
      base.SetItem(index, item);
    }

    public JToken this[string key]
    {
      get
      {
        if (key == null)
          throw new ArgumentNullException(nameof (key));
        if (this._dictionary != null)
          return this._dictionary[key];
        throw new KeyNotFoundException();
      }
    }

    public bool TryGetValue(string key, out JToken value)
    {
      if (this._dictionary != null)
        return this._dictionary.TryGetValue(key, out value);
      value = (JToken) null;
      return false;
    }

    public ICollection<string> Keys
    {
      get
      {
        this.EnsureDictionary();
        return (ICollection<string>) this._dictionary.Keys;
      }
    }

    public ICollection<JToken> Values
    {
      get
      {
        this.EnsureDictionary();
        return (ICollection<JToken>) this._dictionary.Values;
      }
    }

    public int IndexOfReference(JToken t)
    {
      return ((List<JToken>) this.Items).IndexOfReference<JToken>(t);
    }

    public bool Compare(JPropertyKeyedCollection other)
    {
      if (this == other)
        return true;
      Dictionary<string, JToken> dictionary1 = this._dictionary;
      Dictionary<string, JToken> dictionary2 = other._dictionary;
      if (dictionary1 == null && dictionary2 == null)
        return true;
      if (dictionary1 == null)
        return dictionary2.Count == 0;
      if (dictionary2 == null)
        return dictionary1.Count == 0;
      if (dictionary1.Count != dictionary2.Count)
        return false;
      foreach (KeyValuePair<string, JToken> keyValuePair in dictionary1)
      {
        JToken jtoken;
        if (!dictionary2.TryGetValue(keyValuePair.Key, out jtoken))
          return false;
        JProperty jproperty1 = (JProperty) keyValuePair.Value;
        JProperty jproperty2 = (JProperty) jtoken;
        if (jproperty1.Value == null)
          return jproperty2.Value == null;
        if (!jproperty1.Value.DeepEquals(jproperty2.Value))
          return false;
      }
      return true;
    }
  }
}
