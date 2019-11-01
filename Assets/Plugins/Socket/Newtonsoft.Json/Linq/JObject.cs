using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using Socket.Newtonsoft.Json.Utilities;
using Socket.Newtonsoft.Json.Utilities.LinqBridge;

namespace Socket.Newtonsoft.Json.Linq {
  public class JObject : JContainer, IDictionary<string, JToken>, ICollection<KeyValuePair<string, JToken>>, IEnumerable<KeyValuePair<string, JToken>>, IEnumerable, INotifyPropertyChanged, ICustomTypeDescriptor
  {
    private readonly JPropertyKeyedCollection _properties = new JPropertyKeyedCollection();

    protected override IList<JToken> ChildrenTokens
    {
      get
      {
        return (IList<JToken>) this._properties;
      }
    }

    public event PropertyChangedEventHandler PropertyChanged;

    public JObject()
    {
    }

    public JObject(JObject other)
      : base((JContainer) other)
    {
    }

    public JObject(params object[] content)
      : this((object) content)
    {
    }

    public JObject(object content)
    {
      this.Add(content);
    }

    internal override bool DeepEquals(JToken node)
    {
      JObject jobject = node as JObject;
      if (jobject == null)
        return false;
      return this._properties.Compare(jobject._properties);
    }

    internal override int IndexOfItem(JToken item)
    {
      return this._properties.IndexOfReference(item);
    }

    internal override void InsertItem(int index, JToken item, bool skipParentCheck)
    {
      if (item != null && item.Type == JTokenType.Comment)
        return;
      base.InsertItem(index, item, skipParentCheck);
    }

    internal override void ValidateToken(JToken o, JToken existing)
    {
      ValidationUtils.ArgumentNotNull((object) o, nameof (o));
      if (o.Type != JTokenType.Property)
        throw new ArgumentException("Can not add {0} to {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) o.GetType(), (object) this.GetType()));
      JProperty jproperty1 = (JProperty) o;
      if (existing != null)
      {
        JProperty jproperty2 = (JProperty) existing;
        if (jproperty1.Name == jproperty2.Name)
          return;
      }
      if (this._properties.TryGetValue(jproperty1.Name, out existing))
        throw new ArgumentException("Can not add property {0} to {1}. Property with the same name already exists on object.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) jproperty1.Name, (object) this.GetType()));
    }

    internal override void MergeItem(object content, JsonMergeSettings settings)
    {
      JObject jobject = content as JObject;
      if (jobject == null)
        return;
      foreach (KeyValuePair<string, JToken> keyValuePair in jobject)
      {
        JProperty jproperty = this.Property(keyValuePair.Key);
        if (jproperty == null)
          this.Add(keyValuePair.Key, keyValuePair.Value);
        else if (keyValuePair.Value != null)
        {
          JContainer jcontainer = jproperty.Value as JContainer;
          if (jcontainer == null || jcontainer.Type != keyValuePair.Value.Type)
          {
            if (keyValuePair.Value.Type != JTokenType.Null || settings != null && settings.MergeNullValueHandling == MergeNullValueHandling.Merge)
              jproperty.Value = keyValuePair.Value;
          }
          else
            jcontainer.Merge((object) keyValuePair.Value, settings);
        }
      }
    }

    internal void InternalPropertyChanged(JProperty childProperty)
    {
      this.OnPropertyChanged(childProperty.Name);
      if (this._listChanged == null)
        return;
      this.OnListChanged(new ListChangedEventArgs(ListChangedType.ItemChanged, this.IndexOfItem((JToken) childProperty)));
    }

    internal void InternalPropertyChanging(JProperty childProperty)
    {
    }

    internal override JToken CloneToken()
    {
      return (JToken) new JObject(this);
    }

    public override JTokenType Type
    {
      get
      {
        return JTokenType.Object;
      }
    }

    public IEnumerable<JProperty> Properties()
    {
      return this._properties.Cast<JProperty>();
    }

    public JProperty Property(string name)
    {
      if (name == null)
        return (JProperty) null;
      JToken jtoken;
      this._properties.TryGetValue(name, out jtoken);
      return (JProperty) jtoken;
    }

    public JEnumerable<JToken> PropertyValues()
    {
      return new JEnumerable<JToken>(this.Properties().Select<JProperty, JToken>((Func<JProperty, JToken>) (p => p.Value)));
    }

    public override JToken this[object key]
    {
      get
      {
        ValidationUtils.ArgumentNotNull(key, nameof (key));
        string index = key as string;
        if (index == null)
          throw new ArgumentException("Accessed JObject values with invalid key value: {0}. Object property name expected.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) MiscellaneousUtils.ToString(key)));
        return this[index];
      }
      set
      {
        ValidationUtils.ArgumentNotNull(key, nameof (key));
        string index = key as string;
        if (index == null)
          throw new ArgumentException("Set JObject values with invalid key value: {0}. Object property name expected.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) MiscellaneousUtils.ToString(key)));
        this[index] = value;
      }
    }

    public JToken this[string propertyName]
    {
      get
      {
        ValidationUtils.ArgumentNotNull((object) propertyName, nameof (propertyName));
        return this.Property(propertyName)?.Value;
      }
      set
      {
        JProperty jproperty = this.Property(propertyName);
        if (jproperty != null)
        {
          jproperty.Value = value;
        }
        else
        {
          this.Add((object) new JProperty(propertyName, (object) value));
          this.OnPropertyChanged(propertyName);
        }
      }
    }

    public static JObject Load(JsonReader reader)
    {
      return JObject.Load(reader, (JsonLoadSettings) null);
    }

    public static JObject Load(JsonReader reader, JsonLoadSettings settings)
    {
      ValidationUtils.ArgumentNotNull((object) reader, nameof (reader));
      if (reader.TokenType == JsonToken.None && !reader.Read())
        throw JsonReaderException.Create(reader, "Error reading JObject from JsonReader.");
      reader.MoveToContent();
      if (reader.TokenType != JsonToken.StartObject)
        throw JsonReaderException.Create(reader, "Error reading JObject from JsonReader. Current JsonReader item is not an object: {0}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) reader.TokenType));
      JObject jobject = new JObject();
      jobject.SetLineInfo(reader as IJsonLineInfo, settings);
      jobject.ReadTokenFrom(reader, settings);
      return jobject;
    }

    public static JObject Parse(string json)
    {
      return JObject.Parse(json, (JsonLoadSettings) null);
    }

    public static JObject Parse(string json, JsonLoadSettings settings)
    {
      using (JsonReader reader = (JsonReader) new JsonTextReader((TextReader) new StringReader(json)))
      {
        JObject jobject = JObject.Load(reader, settings);
        do
          ;
        while (reader.Read());
        return jobject;
      }
    }

    public static JObject FromObject(object o)
    {
      return JObject.FromObject(o, JsonSerializer.CreateDefault());
    }

    public static JObject FromObject(object o, JsonSerializer jsonSerializer)
    {
      JToken jtoken = JToken.FromObjectInternal(o, jsonSerializer);
      if (jtoken != null && jtoken.Type != JTokenType.Object)
        throw new ArgumentException("Object serialized to {0}. JObject instance expected.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) jtoken.Type));
      return (JObject) jtoken;
    }

    public override void WriteTo(JsonWriter writer, params JsonConverter[] converters)
    {
      writer.WriteStartObject();
      for (int index = 0; index < this._properties.Count; ++index)
        this._properties[index].WriteTo(writer, converters);
      writer.WriteEndObject();
    }

    public JToken GetValue(string propertyName)
    {
      return this.GetValue(propertyName, StringComparison.Ordinal);
    }

    public JToken GetValue(string propertyName, StringComparison comparison)
    {
      if (propertyName == null)
        return (JToken) null;
      JProperty jproperty = this.Property(propertyName);
      if (jproperty != null)
        return jproperty.Value;
      if (comparison != StringComparison.Ordinal)
      {
        foreach (JProperty property in (Collection<JToken>) this._properties)
        {
          if (string.Equals(property.Name, propertyName, comparison))
            return property.Value;
        }
      }
      return (JToken) null;
    }

    public bool TryGetValue(string propertyName, StringComparison comparison, out JToken value)
    {
      value = this.GetValue(propertyName, comparison);
      return value != null;
    }

    public void Add(string propertyName, JToken value)
    {
      this.Add((object) new JProperty(propertyName, (object) value));
    }

    bool IDictionary<string, JToken>.ContainsKey(string key)
    {
      return this._properties.Contains(key);
    }

    ICollection<string> IDictionary<string, JToken>.Keys
    {
      get
      {
        return this._properties.Keys;
      }
    }

    public bool Remove(string propertyName)
    {
      JProperty jproperty = this.Property(propertyName);
      if (jproperty == null)
        return false;
      jproperty.Remove();
      return true;
    }

    public bool TryGetValue(string propertyName, out JToken value)
    {
      JProperty jproperty = this.Property(propertyName);
      if (jproperty == null)
      {
        value = (JToken) null;
        return false;
      }
      value = jproperty.Value;
      return true;
    }

    ICollection<JToken> IDictionary<string, JToken>.Values
    {
      get
      {
        throw new NotImplementedException();
      }
    }

    void ICollection<KeyValuePair<string, JToken>>.Add(
      KeyValuePair<string, JToken> item)
    {
      this.Add((object) new JProperty(item.Key, (object) item.Value));
    }

    void ICollection<KeyValuePair<string, JToken>>.Clear()
    {
      this.RemoveAll();
    }

    bool ICollection<KeyValuePair<string, JToken>>.Contains(
      KeyValuePair<string, JToken> item)
    {
      JProperty jproperty = this.Property(item.Key);
      if (jproperty == null)
        return false;
      return jproperty.Value == item.Value;
    }

    void ICollection<KeyValuePair<string, JToken>>.CopyTo(
      KeyValuePair<string, JToken>[] array,
      int arrayIndex)
    {
      if (array == null)
        throw new ArgumentNullException(nameof (array));
      if (arrayIndex < 0)
        throw new ArgumentOutOfRangeException(nameof (arrayIndex), "arrayIndex is less than 0.");
      if (arrayIndex >= array.Length && arrayIndex != 0)
        throw new ArgumentException("arrayIndex is equal to or greater than the length of array.");
      if (this.Count > array.Length - arrayIndex)
        throw new ArgumentException("The number of elements in the source JObject is greater than the available space from arrayIndex to the end of the destination array.");
      int num = 0;
      foreach (JProperty property in (Collection<JToken>) this._properties)
      {
        array[arrayIndex + num] = new KeyValuePair<string, JToken>(property.Name, property.Value);
        ++num;
      }
    }

    bool ICollection<KeyValuePair<string, JToken>>.IsReadOnly
    {
      get
      {
        return false;
      }
    }

    bool ICollection<KeyValuePair<string, JToken>>.Remove(
      KeyValuePair<string, JToken> item)
    {
      if (!((ICollection<KeyValuePair<string, JToken>>) this).Contains(item))
        return false;
      this.Remove(item.Key);
      return true;
    }

    internal override int GetDeepHashCode()
    {
      return this.ContentsHashCode();
    }

    public IEnumerator<KeyValuePair<string, JToken>> GetEnumerator()
    {
      foreach (JProperty property in (Collection<JToken>) this._properties)
        yield return new KeyValuePair<string, JToken>(property.Name, property.Value);
    }

    protected virtual void OnPropertyChanged(string propertyName)
    {
      PropertyChangedEventHandler propertyChanged = this.PropertyChanged;
      if (propertyChanged == null)
        return;
      propertyChanged((object) this, new PropertyChangedEventArgs(propertyName));
    }

    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties()
    {
      return ((ICustomTypeDescriptor) this).GetProperties((Attribute[]) null);
    }

    PropertyDescriptorCollection ICustomTypeDescriptor.GetProperties(
      Attribute[] attributes)
    {
      PropertyDescriptorCollection descriptorCollection = new PropertyDescriptorCollection((PropertyDescriptor[]) null);
      foreach (KeyValuePair<string, JToken> keyValuePair in this)
        descriptorCollection.Add((PropertyDescriptor) new JPropertyDescriptor(keyValuePair.Key));
      return descriptorCollection;
    }

    AttributeCollection ICustomTypeDescriptor.GetAttributes()
    {
      return AttributeCollection.Empty;
    }

    string ICustomTypeDescriptor.GetClassName()
    {
      return (string) null;
    }

    string ICustomTypeDescriptor.GetComponentName()
    {
      return (string) null;
    }

    TypeConverter ICustomTypeDescriptor.GetConverter()
    {
      return new TypeConverter();
    }

    EventDescriptor ICustomTypeDescriptor.GetDefaultEvent()
    {
      return (EventDescriptor) null;
    }

    PropertyDescriptor ICustomTypeDescriptor.GetDefaultProperty()
    {
      return (PropertyDescriptor) null;
    }

    object ICustomTypeDescriptor.GetEditor(System.Type editorBaseType)
    {
      return (object) null;
    }

    EventDescriptorCollection ICustomTypeDescriptor.GetEvents(
      Attribute[] attributes)
    {
      return EventDescriptorCollection.Empty;
    }

    EventDescriptorCollection ICustomTypeDescriptor.GetEvents()
    {
      return EventDescriptorCollection.Empty;
    }

    object ICustomTypeDescriptor.GetPropertyOwner(PropertyDescriptor pd)
    {
      return (object) null;
    }
  }
}
