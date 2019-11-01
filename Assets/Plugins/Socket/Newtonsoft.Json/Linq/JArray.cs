using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Linq {
  public class JArray : JContainer, IList<JToken>, ICollection<JToken>, IEnumerable<JToken>, IEnumerable {
    private readonly List<JToken> _values = new List<JToken>();

    protected override IList<JToken> ChildrenTokens {
      get { return (IList<JToken>) this._values; }
    }

    public override JTokenType Type {
      get { return JTokenType.Array; }
    }

    public JArray() {
    }

    public JArray(JArray other)
      : base((JContainer) other) {
    }

    public JArray(params object[] content)
      : this((object) content) {
    }

    public JArray(object content) {
      this.Add(content);
    }

    internal override bool DeepEquals(JToken node) {
      JArray jarray = node as JArray;
      if (jarray != null)
        return this.ContentsEqual((JContainer) jarray);
      return false;
    }

    internal override JToken CloneToken() {
      return (JToken) new JArray(this);
    }

    public static JArray Load(JsonReader reader) {
      return JArray.Load(reader, (JsonLoadSettings) null);
    }

    public static JArray Load(JsonReader reader, JsonLoadSettings settings) {
      if (reader.TokenType == JsonToken.None && !reader.Read())
        throw JsonReaderException.Create(reader, "Error reading JArray from JsonReader.");
      reader.MoveToContent();
      if (reader.TokenType != JsonToken.StartArray)
        throw JsonReaderException.Create(reader,
          "Error reading JArray from JsonReader. Current JsonReader item is not an array: {0}".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) reader.TokenType));
      JArray jarray = new JArray();
      jarray.SetLineInfo(reader as IJsonLineInfo, settings);
      jarray.ReadTokenFrom(reader, settings);
      return jarray;
    }

    public static JArray Parse(string json) {
      return JArray.Parse(json, (JsonLoadSettings) null);
    }

    public static JArray Parse(string json, JsonLoadSettings settings) {
      using (JsonReader reader = (JsonReader) new JsonTextReader((TextReader) new StringReader(json))) {
        JArray jarray = JArray.Load(reader, settings);
        do
          ;
        while (reader.Read());
        return jarray;
      }
    }

    public static JArray FromObject(object o) {
      return JArray.FromObject(o, JsonSerializer.CreateDefault());
    }

    public static JArray FromObject(object o, JsonSerializer jsonSerializer) {
      JToken jtoken = JToken.FromObjectInternal(o, jsonSerializer);
      if (jtoken.Type != JTokenType.Array)
        throw new ArgumentException(
          "Object serialized to {0}. JArray instance expected.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) jtoken.Type));
      return (JArray) jtoken;
    }

    public override void WriteTo(JsonWriter writer, params JsonConverter[] converters) {
      writer.WriteStartArray();
      for (int index = 0; index < this._values.Count; ++index)
        this._values[index].WriteTo(writer, converters);
      writer.WriteEndArray();
    }

    public override JToken this[object key] {
      get {
        ValidationUtils.ArgumentNotNull(key, nameof(key));
        if (!(key is int))
          throw new ArgumentException(
            "Accessed JArray values with invalid key value: {0}. Int32 array index expected.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) MiscellaneousUtils.ToString(key)));
        return this.GetItem((int) key);
      }
      set {
        ValidationUtils.ArgumentNotNull(key, nameof(key));
        if (!(key is int))
          throw new ArgumentException(
            "Set JArray values with invalid key value: {0}. Int32 array index expected.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) MiscellaneousUtils.ToString(key)));
        this.SetItem((int) key, value);
      }
    }

    public JToken this[int index] {
      get { return this.GetItem(index); }
      set { this.SetItem(index, value); }
    }

    internal override int IndexOfItem(JToken item) {
      return this._values.IndexOfReference<JToken>(item);
    }

    internal override void MergeItem(object content, JsonMergeSettings settings) {
      IEnumerable content1 = this.IsMultiContent(content) || content is JArray
        ? (IEnumerable) content
        : (IEnumerable) null;
      if (content1 == null)
        return;
      JContainer.MergeEnumerableContent((JContainer) this, content1, settings);
    }

    public int IndexOf(JToken item) {
      return this.IndexOfItem(item);
    }

    public void Insert(int index, JToken item) {
      this.InsertItem(index, item, false);
    }

    public void RemoveAt(int index) {
      this.RemoveItemAt(index);
    }

    public IEnumerator<JToken> GetEnumerator() {
      return this.Children().GetEnumerator();
    }

    public void Add(JToken item) {
      this.Add((object) item);
    }

    public void Clear() {
      this.ClearItems();
    }

    public bool Contains(JToken item) {
      return this.ContainsItem(item);
    }

    public void CopyTo(JToken[] array, int arrayIndex) {
      this.CopyItemsTo((Array) array, arrayIndex);
    }

    public bool IsReadOnly {
      get { return false; }
    }

    public bool Remove(JToken item) {
      return this.RemoveItem(item);
    }

    internal override int GetDeepHashCode() {
      return this.ContentsHashCode();
    }
  }
}