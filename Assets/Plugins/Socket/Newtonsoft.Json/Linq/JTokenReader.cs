using System;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Linq {
  public class JTokenReader : JsonReader, IJsonLineInfo {
    private readonly JToken _root;
    private string _initialPath;
    private JToken _parent;
    private JToken _current;

    public JToken CurrentToken {
      get { return this._current; }
    }

    public JTokenReader(JToken token) {
      ValidationUtils.ArgumentNotNull((object) token, nameof(token));
      this._root = token;
    }

    internal JTokenReader(JToken token, string initialPath)
      : this(token) {
      this._initialPath = initialPath;
    }

    public override bool Read() {
      if (this.CurrentState != JsonReader.State.Start) {
        if (this._current == null)
          return false;
        JContainer current = this._current as JContainer;
        if (current != null && this._parent != current)
          return this.ReadInto(current);
        return this.ReadOver(this._current);
      }

      this._current = this._root;
      this.SetToken(this._current);
      return true;
    }

    private bool ReadOver(JToken t) {
      if (t == this._root)
        return this.ReadToEnd();
      JToken next = t.Next;
      if (next == null || next == t || t == t.Parent.Last) {
        if (t.Parent == null)
          return this.ReadToEnd();
        return this.SetEnd(t.Parent);
      }

      this._current = next;
      this.SetToken(this._current);
      return true;
    }

    private bool ReadToEnd() {
      this._current = (JToken) null;
      this.SetToken(JsonToken.None);
      return false;
    }

    private JsonToken? GetEndToken(JContainer c) {
      switch (c.Type) {
        case JTokenType.Object:
          return new JsonToken?(JsonToken.EndObject);
        case JTokenType.Array:
          return new JsonToken?(JsonToken.EndArray);
        case JTokenType.Constructor:
          return new JsonToken?(JsonToken.EndConstructor);
        case JTokenType.Property:
          return new JsonToken?();
        default:
          throw MiscellaneousUtils.CreateArgumentOutOfRangeException("Type", (object) c.Type,
            "Unexpected JContainer type.");
      }
    }

    private bool ReadInto(JContainer c) {
      JToken first = c.First;
      if (first == null)
        return this.SetEnd(c);
      this.SetToken(first);
      this._current = first;
      this._parent = (JToken) c;
      return true;
    }

    private bool SetEnd(JContainer c) {
      JsonToken? endToken = this.GetEndToken(c);
      if (!endToken.HasValue)
        return this.ReadOver((JToken) c);
      this.SetToken(endToken.GetValueOrDefault());
      this._current = (JToken) c;
      this._parent = (JToken) c;
      return true;
    }

    private void SetToken(JToken token) {
      switch (token.Type) {
        case JTokenType.Object:
          this.SetToken(JsonToken.StartObject);
          break;
        case JTokenType.Array:
          this.SetToken(JsonToken.StartArray);
          break;
        case JTokenType.Constructor:
          this.SetToken(JsonToken.StartConstructor, (object) ((JConstructor) token).Name);
          break;
        case JTokenType.Property:
          this.SetToken(JsonToken.PropertyName, (object) ((JProperty) token).Name);
          break;
        case JTokenType.Comment:
          this.SetToken(JsonToken.Comment, ((JValue) token).Value);
          break;
        case JTokenType.Integer:
          this.SetToken(JsonToken.Integer, ((JValue) token).Value);
          break;
        case JTokenType.Float:
          this.SetToken(JsonToken.Float, ((JValue) token).Value);
          break;
        case JTokenType.String:
          this.SetToken(JsonToken.String, ((JValue) token).Value);
          break;
        case JTokenType.Boolean:
          this.SetToken(JsonToken.Boolean, ((JValue) token).Value);
          break;
        case JTokenType.Null:
          this.SetToken(JsonToken.Null, ((JValue) token).Value);
          break;
        case JTokenType.Undefined:
          this.SetToken(JsonToken.Undefined, ((JValue) token).Value);
          break;
        case JTokenType.Date:
          this.SetToken(JsonToken.Date, ((JValue) token).Value);
          break;
        case JTokenType.Raw:
          this.SetToken(JsonToken.Raw, ((JValue) token).Value);
          break;
        case JTokenType.Bytes:
          this.SetToken(JsonToken.Bytes, ((JValue) token).Value);
          break;
        case JTokenType.Guid:
          this.SetToken(JsonToken.String, (object) this.SafeToString(((JValue) token).Value));
          break;
        case JTokenType.Uri:
          object obj = ((JValue) token).Value;
          Uri uri = obj as Uri;
          this.SetToken(JsonToken.String,
            uri != (Uri) null ? (object) uri.OriginalString : (object) this.SafeToString(obj));
          break;
        case JTokenType.TimeSpan:
          this.SetToken(JsonToken.String, (object) this.SafeToString(((JValue) token).Value));
          break;
        default:
          throw MiscellaneousUtils.CreateArgumentOutOfRangeException("Type", (object) token.Type,
            "Unexpected JTokenType.");
      }
    }

    private string SafeToString(object value) {
      return value?.ToString();
    }

    bool IJsonLineInfo.HasLineInfo() {
      if (this.CurrentState == JsonReader.State.Start)
        return false;
      IJsonLineInfo current = (IJsonLineInfo) this._current;
      if (current != null)
        return current.HasLineInfo();
      return false;
    }

    int IJsonLineInfo.LineNumber {
      get {
        if (this.CurrentState == JsonReader.State.Start)
          return 0;
        IJsonLineInfo current = (IJsonLineInfo) this._current;
        if (current != null)
          return current.LineNumber;
        return 0;
      }
    }

    int IJsonLineInfo.LinePosition {
      get {
        if (this.CurrentState == JsonReader.State.Start)
          return 0;
        IJsonLineInfo current = (IJsonLineInfo) this._current;
        if (current != null)
          return current.LinePosition;
        return 0;
      }
    }

    public override string Path {
      get {
        string source = base.Path;
        if (this._initialPath == null)
          this._initialPath = this._root.Path;
        if (!string.IsNullOrEmpty(this._initialPath)) {
          if (string.IsNullOrEmpty(source))
            return this._initialPath;
          source = !source.StartsWith('[') ? this._initialPath + "." + source : this._initialPath + source;
        }

        return source;
      }
    }
  }
}