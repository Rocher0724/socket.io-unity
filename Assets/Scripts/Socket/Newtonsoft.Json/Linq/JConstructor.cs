using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Linq {
  public class JConstructor : JContainer {
    private readonly List<JToken> _values = new List<JToken>();
    private string _name;

    protected override IList<JToken> ChildrenTokens {
      get { return (IList<JToken>) this._values; }
    }

    internal override int IndexOfItem(JToken item) {
      return this._values.IndexOfReference<JToken>(item);
    }

    internal override void MergeItem(object content, JsonMergeSettings settings) {
      JConstructor jconstructor = content as JConstructor;
      if (jconstructor == null)
        return;
      if (jconstructor.Name != null)
        this.Name = jconstructor.Name;
      JContainer.MergeEnumerableContent((JContainer) this, (IEnumerable) jconstructor, settings);
    }

    public string Name {
      get { return this._name; }
      set { this._name = value; }
    }

    public override JTokenType Type {
      get { return JTokenType.Constructor; }
    }

    public JConstructor() {
    }

    public JConstructor(JConstructor other)
      : base((JContainer) other) {
      this._name = other.Name;
    }

    public JConstructor(string name, params object[] content)
      : this(name, (object) content) {
    }

    public JConstructor(string name, object content)
      : this(name) {
      this.Add(content);
    }

    public JConstructor(string name) {
      if (name == null)
        throw new ArgumentNullException(nameof(name));
      if (name.Length == 0)
        throw new ArgumentException("Constructor name cannot be empty.", nameof(name));
      this._name = name;
    }

    internal override bool DeepEquals(JToken node) {
      JConstructor jconstructor = node as JConstructor;
      if (jconstructor != null && this._name == jconstructor.Name)
        return this.ContentsEqual((JContainer) jconstructor);
      return false;
    }

    internal override JToken CloneToken() {
      return (JToken) new JConstructor(this);
    }

    public override void WriteTo(JsonWriter writer, params JsonConverter[] converters) {
      writer.WriteStartConstructor(this._name);
      int count = this._values.Count;
      for (int index = 0; index < count; ++index)
        this._values[index].WriteTo(writer, converters);
      writer.WriteEndConstructor();
    }

    public override JToken this[object key] {
      get {
        ValidationUtils.ArgumentNotNull(key, nameof(key));
        if (!(key is int))
          throw new ArgumentException(
            "Accessed JConstructor values with invalid key value: {0}. Argument position index expected.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) MiscellaneousUtils.ToString(key)));
        return this.GetItem((int) key);
      }
      set {
        ValidationUtils.ArgumentNotNull(key, nameof(key));
        if (!(key is int))
          throw new ArgumentException(
            "Set JConstructor values with invalid key value: {0}. Argument position index expected.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) MiscellaneousUtils.ToString(key)));
        this.SetItem((int) key, value);
      }
    }

    internal override int GetDeepHashCode() {
      return this._name.GetHashCode() ^ this.ContentsHashCode();
    }

    public static JConstructor Load(JsonReader reader) {
      return JConstructor.Load(reader, (JsonLoadSettings) null);
    }

    public static JConstructor Load(JsonReader reader, JsonLoadSettings settings) {
      if (reader.TokenType == JsonToken.None && !reader.Read())
        throw JsonReaderException.Create(reader, "Error reading JConstructor from JsonReader.");
      reader.MoveToContent();
      if (reader.TokenType != JsonToken.StartConstructor)
        throw JsonReaderException.Create(reader,
          "Error reading JConstructor from JsonReader. Current JsonReader item is not a constructor: {0}".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) reader.TokenType));
      JConstructor jconstructor = new JConstructor((string) reader.Value);
      jconstructor.SetLineInfo(reader as IJsonLineInfo, settings);
      jconstructor.ReadTokenFrom(reader, settings);
      return jconstructor;
    }
  }
}