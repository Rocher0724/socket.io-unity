using System;
using System.Collections.Generic;
using System.Globalization;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json {
  public abstract class JsonReader : IDisposable {
    private JsonToken _tokenType;
    private object _value;
    internal char _quoteChar;
    internal JsonReader.State _currentState;
    private JsonPosition _currentPosition;
    private CultureInfo _culture;
    private DateTimeZoneHandling _dateTimeZoneHandling;
    private int? _maxDepth;
    private bool _hasExceededMaxDepth;
    internal DateParseHandling _dateParseHandling;
    internal FloatParseHandling _floatParseHandling;
    private string _dateFormatString;
    private List<JsonPosition> _stack;

    protected JsonReader.State CurrentState {
      get { return this._currentState; }
    }

    public bool CloseInput { get; set; }

    public bool SupportMultipleContent { get; set; }

    public virtual char QuoteChar {
      get { return this._quoteChar; }
      protected internal set { this._quoteChar = value; }
    }

    public DateTimeZoneHandling DateTimeZoneHandling {
      get { return this._dateTimeZoneHandling; }
      set {
        switch (value) {
          case DateTimeZoneHandling.Local:
          case DateTimeZoneHandling.Utc:
          case DateTimeZoneHandling.Unspecified:
          case DateTimeZoneHandling.RoundtripKind:
            this._dateTimeZoneHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof(value));
        }
      }
    }

    public DateParseHandling DateParseHandling {
      get { return this._dateParseHandling; }
      set {
        switch (value) {
          case DateParseHandling.None:
          case DateParseHandling.DateTime:
            this._dateParseHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof(value));
        }
      }
    }

    public FloatParseHandling FloatParseHandling {
      get { return this._floatParseHandling; }
      set {
        switch (value) {
          case FloatParseHandling.Double:
          case FloatParseHandling.Decimal:
            this._floatParseHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof(value));
        }
      }
    }

    public string DateFormatString {
      get { return this._dateFormatString; }
      set { this._dateFormatString = value; }
    }

    public int? MaxDepth {
      get { return this._maxDepth; }
      set {
        int? nullable = value;
        int num = 0;
        if ((nullable.GetValueOrDefault() <= num ? (nullable.HasValue ? 1 : 0) : 0) != 0)
          throw new ArgumentException("Value must be positive.", nameof(value));
        this._maxDepth = value;
      }
    }

    public virtual JsonToken TokenType {
      get { return this._tokenType; }
    }

    public virtual object Value {
      get { return this._value; }
    }

    public virtual Type ValueType {
      get { return this._value?.GetType(); }
    }

    public virtual int Depth {
      get {
        int num = this._stack != null ? this._stack.Count : 0;
        if (JsonTokenUtils.IsStartToken(this.TokenType) || this._currentPosition.Type == JsonContainerType.None)
          return num;
        return num + 1;
      }
    }

    public virtual string Path {
      get {
        if (this._currentPosition.Type == JsonContainerType.None)
          return string.Empty;
        return JsonPosition.BuildPath(this._stack,
          (this._currentState == JsonReader.State.ArrayStart || this._currentState == JsonReader.State.ConstructorStart
            ? 0
            : (this._currentState != JsonReader.State.ObjectStart ? 1 : 0)) != 0
            ? new JsonPosition?(this._currentPosition)
            : new JsonPosition?());
      }
    }

    public CultureInfo Culture {
      get { return this._culture ?? CultureInfo.InvariantCulture; }
      set { this._culture = value; }
    }

    internal JsonPosition GetPosition(int depth) {
      if (this._stack != null && depth < this._stack.Count)
        return this._stack[depth];
      return this._currentPosition;
    }

    protected JsonReader() {
      this._currentState = JsonReader.State.Start;
      this._dateTimeZoneHandling = DateTimeZoneHandling.RoundtripKind;
      this._dateParseHandling = DateParseHandling.DateTime;
      this._floatParseHandling = FloatParseHandling.Double;
      this.CloseInput = true;
    }

    private void Push(JsonContainerType value) {
      this.UpdateScopeWithFinishedValue();
      if (this._currentPosition.Type == JsonContainerType.None) {
        this._currentPosition = new JsonPosition(value);
      } else {
        if (this._stack == null)
          this._stack = new List<JsonPosition>();
        this._stack.Add(this._currentPosition);
        this._currentPosition = new JsonPosition(value);
        if (!this._maxDepth.HasValue)
          return;
        int num = this.Depth + 1;
        int? maxDepth = this._maxDepth;
        int valueOrDefault = maxDepth.GetValueOrDefault();
        if ((num > valueOrDefault ? (maxDepth.HasValue ? 1 : 0) : 0) != 0 && !this._hasExceededMaxDepth) {
          this._hasExceededMaxDepth = true;
          throw JsonReaderException.Create(this,
            "The reader's MaxDepth of {0} has been exceeded.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) this._maxDepth));
        }
      }
    }

    private JsonContainerType Pop() {
      JsonPosition currentPosition;
      if (this._stack != null && this._stack.Count > 0) {
        currentPosition = this._currentPosition;
        this._currentPosition = this._stack[this._stack.Count - 1];
        this._stack.RemoveAt(this._stack.Count - 1);
      } else {
        currentPosition = this._currentPosition;
        this._currentPosition = new JsonPosition();
      }

      if (this._maxDepth.HasValue) {
        int depth = this.Depth;
        int? maxDepth = this._maxDepth;
        int valueOrDefault = maxDepth.GetValueOrDefault();
        if ((depth <= valueOrDefault ? (maxDepth.HasValue ? 1 : 0) : 0) != 0)
          this._hasExceededMaxDepth = false;
      }

      return currentPosition.Type;
    }

    private JsonContainerType Peek() {
      return this._currentPosition.Type;
    }

    public abstract bool Read();

    public virtual int? ReadAsInt32() {
      JsonToken contentToken = this.GetContentToken();
      switch (contentToken) {
        case JsonToken.None:
        case JsonToken.Null:
        case JsonToken.EndArray:
          return new int?();
        case JsonToken.Integer:
        case JsonToken.Float:
          if (!(this.Value is int))
            this.SetToken(JsonToken.Integer,
              (object) Convert.ToInt32(this.Value, (IFormatProvider) CultureInfo.InvariantCulture), false);
          return new int?((int) this.Value);
        case JsonToken.String:
          return this.ReadInt32String((string) this.Value);
        default:
          throw JsonReaderException.Create(this,
            "Error reading integer. Unexpected token: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) contentToken));
      }
    }

    internal int? ReadInt32String(string s) {
      if (string.IsNullOrEmpty(s)) {
        this.SetToken(JsonToken.Null, (object) null, false);
        return new int?();
      }

      int result;
      if (int.TryParse(s, NumberStyles.Integer, (IFormatProvider) this.Culture, out result)) {
        this.SetToken(JsonToken.Integer, (object) result, false);
        return new int?(result);
      }

      this.SetToken(JsonToken.String, (object) s, false);
      throw JsonReaderException.Create(this,
        "Could not convert string to integer: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) s));
    }

    public virtual string ReadAsString() {
      JsonToken contentToken = this.GetContentToken();
      switch (contentToken) {
        case JsonToken.None:
        case JsonToken.Null:
        case JsonToken.EndArray:
          return (string) null;
        case JsonToken.String:
          return (string) this.Value;
        default:
          if (!JsonTokenUtils.IsPrimitiveToken(contentToken) || this.Value == null)
            throw JsonReaderException.Create(this,
              "Error reading string. Unexpected token: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                (object) contentToken));
          IFormattable formattable = this.Value as IFormattable;
          string str;
          if (formattable != null) {
            str = formattable.ToString((string) null, (IFormatProvider) this.Culture);
          } else {
            Uri uri = this.Value as Uri;
            str = uri != (Uri) null ? uri.OriginalString : this.Value.ToString();
          }

          this.SetToken(JsonToken.String, (object) str, false);
          return str;
      }
    }

    public virtual byte[] ReadAsBytes() {
      JsonToken contentToken = this.GetContentToken();
      switch (contentToken) {
        case JsonToken.None:
        case JsonToken.Null:
        case JsonToken.EndArray:
          return (byte[]) null;
        case JsonToken.StartObject:
          this.ReadIntoWrappedTypeObject();
          byte[] numArray1 = this.ReadAsBytes();
          this.ReaderReadAndAssert();
          if (this.TokenType != JsonToken.EndObject)
            throw JsonReaderException.Create(this,
              "Error reading bytes. Unexpected token: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                (object) this.TokenType));
          this.SetToken(JsonToken.Bytes, (object) numArray1, false);
          return numArray1;
        case JsonToken.StartArray:
          return this.ReadArrayIntoByteArray();
        case JsonToken.String:
          string s = (string) this.Value;
          Guid g;
          byte[] numArray2 = s.Length != 0
            ? (!ConvertUtils.TryConvertGuid(s, out g) ? Convert.FromBase64String(s) : g.ToByteArray())
            : CollectionUtils.ArrayEmpty<byte>();
          this.SetToken(JsonToken.Bytes, (object) numArray2, false);
          return numArray2;
        case JsonToken.Bytes:
          if (this.ValueType != typeof(Guid))
            return (byte[]) this.Value;
          byte[] byteArray = ((Guid) this.Value).ToByteArray();
          this.SetToken(JsonToken.Bytes, (object) byteArray, false);
          return byteArray;
        default:
          throw JsonReaderException.Create(this,
            "Error reading bytes. Unexpected token: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) contentToken));
      }
    }

    internal byte[] ReadArrayIntoByteArray() {
      List<byte> buffer = new List<byte>();
      do {
        if (!this.Read())
          this.SetToken(JsonToken.None);
      } while (!this.ReadArrayElementIntoByteArrayReportDone(buffer));

      byte[] array = buffer.ToArray();
      this.SetToken(JsonToken.Bytes, (object) array, false);
      return array;
    }

    private bool ReadArrayElementIntoByteArrayReportDone(List<byte> buffer) {
      switch (this.TokenType) {
        case JsonToken.None:
          throw JsonReaderException.Create(this, "Unexpected end when reading bytes.");
        case JsonToken.Comment:
          return false;
        case JsonToken.Integer:
          buffer.Add(Convert.ToByte(this.Value, (IFormatProvider) CultureInfo.InvariantCulture));
          return false;
        case JsonToken.EndArray:
          return true;
        default:
          throw JsonReaderException.Create(this,
            "Unexpected token when reading bytes: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) this.TokenType));
      }
    }

    public virtual double? ReadAsDouble() {
      JsonToken contentToken = this.GetContentToken();
      switch (contentToken) {
        case JsonToken.None:
        case JsonToken.Null:
        case JsonToken.EndArray:
          return new double?();
        case JsonToken.Integer:
        case JsonToken.Float:
          if (!(this.Value is double))
            this.SetToken(JsonToken.Float,
              (object) Convert.ToDouble(this.Value, (IFormatProvider) CultureInfo.InvariantCulture), false);
          return new double?((double) this.Value);
        case JsonToken.String:
          return this.ReadDoubleString((string) this.Value);
        default:
          throw JsonReaderException.Create(this,
            "Error reading double. Unexpected token: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) contentToken));
      }
    }

    internal double? ReadDoubleString(string s) {
      if (string.IsNullOrEmpty(s)) {
        this.SetToken(JsonToken.Null, (object) null, false);
        return new double?();
      }

      double result;
      if (double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, (IFormatProvider) this.Culture,
        out result)) {
        this.SetToken(JsonToken.Float, (object) result, false);
        return new double?(result);
      }

      this.SetToken(JsonToken.String, (object) s, false);
      throw JsonReaderException.Create(this,
        "Could not convert string to double: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) s));
    }

    public virtual bool? ReadAsBoolean() {
      JsonToken contentToken = this.GetContentToken();
      switch (contentToken) {
        case JsonToken.None:
        case JsonToken.Null:
        case JsonToken.EndArray:
          return new bool?();
        case JsonToken.Integer:
        case JsonToken.Float:
          bool boolean = Convert.ToBoolean(this.Value, (IFormatProvider) CultureInfo.InvariantCulture);
          this.SetToken(JsonToken.Boolean, (object) boolean, false);
          return new bool?(boolean);
        case JsonToken.String:
          return this.ReadBooleanString((string) this.Value);
        case JsonToken.Boolean:
          return new bool?((bool) this.Value);
        default:
          throw JsonReaderException.Create(this,
            "Error reading boolean. Unexpected token: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) contentToken));
      }
    }

    internal bool? ReadBooleanString(string s) {
      if (string.IsNullOrEmpty(s)) {
        this.SetToken(JsonToken.Null, (object) null, false);
        return new bool?();
      }

      bool result;
      if (bool.TryParse(s, out result)) {
        this.SetToken(JsonToken.Boolean, (object) result, false);
        return new bool?(result);
      }

      this.SetToken(JsonToken.String, (object) s, false);
      throw JsonReaderException.Create(this,
        "Could not convert string to boolean: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) s));
    }

    public virtual Decimal? ReadAsDecimal() {
      JsonToken contentToken = this.GetContentToken();
      switch (contentToken) {
        case JsonToken.None:
        case JsonToken.Null:
        case JsonToken.EndArray:
          return new Decimal?();
        case JsonToken.Integer:
        case JsonToken.Float:
          if (!(this.Value is Decimal))
            this.SetToken(JsonToken.Float,
              (object) Convert.ToDecimal(this.Value, (IFormatProvider) CultureInfo.InvariantCulture), false);
          return new Decimal?((Decimal) this.Value);
        case JsonToken.String:
          return this.ReadDecimalString((string) this.Value);
        default:
          throw JsonReaderException.Create(this,
            "Error reading decimal. Unexpected token: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) contentToken));
      }
    }

    internal Decimal? ReadDecimalString(string s) {
      if (string.IsNullOrEmpty(s)) {
        this.SetToken(JsonToken.Null, (object) null, false);
        return new Decimal?();
      }

      Decimal result;
      if (Decimal.TryParse(s, NumberStyles.Number, (IFormatProvider) this.Culture, out result)) {
        this.SetToken(JsonToken.Float, (object) result, false);
        return new Decimal?(result);
      }

      this.SetToken(JsonToken.String, (object) s, false);
      throw JsonReaderException.Create(this,
        "Could not convert string to decimal: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) s));
    }

    public virtual DateTime? ReadAsDateTime() {
      switch (this.GetContentToken()) {
        case JsonToken.None:
        case JsonToken.Null:
        case JsonToken.EndArray:
          return new DateTime?();
        case JsonToken.String:
          return this.ReadDateTimeString((string) this.Value);
        case JsonToken.Date:
          return new DateTime?((DateTime) this.Value);
        default:
          throw JsonReaderException.Create(this,
            "Error reading date. Unexpected token: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) this.TokenType));
      }
    }

    internal DateTime? ReadDateTimeString(string s) {
      if (string.IsNullOrEmpty(s)) {
        this.SetToken(JsonToken.Null, (object) null, false);
        return new DateTime?();
      }

      DateTime dateTime;
      if (DateTimeUtils.TryParseDateTime(s, this.DateTimeZoneHandling, this._dateFormatString, this.Culture,
        out dateTime)) {
        dateTime = DateTimeUtils.EnsureDateTime(dateTime, this.DateTimeZoneHandling);
        this.SetToken(JsonToken.Date, (object) dateTime, false);
        return new DateTime?(dateTime);
      }

      if (!DateTime.TryParse(s, (IFormatProvider) this.Culture, DateTimeStyles.RoundtripKind, out dateTime))
        throw JsonReaderException.Create(this,
          "Could not convert string to DateTime: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) s));
      dateTime = DateTimeUtils.EnsureDateTime(dateTime, this.DateTimeZoneHandling);
      this.SetToken(JsonToken.Date, (object) dateTime, false);
      return new DateTime?(dateTime);
    }

    internal void ReaderReadAndAssert() {
      if (!this.Read())
        throw this.CreateUnexpectedEndException();
    }

    internal JsonReaderException CreateUnexpectedEndException() {
      return JsonReaderException.Create(this, "Unexpected end when reading JSON.");
    }

    internal void ReadIntoWrappedTypeObject() {
      this.ReaderReadAndAssert();
      if (this.Value != null && this.Value.ToString() == "$type") {
        this.ReaderReadAndAssert();
        if (this.Value != null && this.Value.ToString().StartsWith("System.Byte[]", StringComparison.Ordinal)) {
          this.ReaderReadAndAssert();
          if (this.Value.ToString() == "$value")
            return;
        }
      }

      throw JsonReaderException.Create(this,
        "Error reading bytes. Unexpected token: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) JsonToken.StartObject));
    }

    public void Skip() {
      if (this.TokenType == JsonToken.PropertyName)
        this.Read();
      if (!JsonTokenUtils.IsStartToken(this.TokenType))
        return;
      int depth = this.Depth;
      do
        ;
      while (this.Read() && depth < this.Depth);
    }

    protected void SetToken(JsonToken newToken) {
      this.SetToken(newToken, (object) null, true);
    }

    protected void SetToken(JsonToken newToken, object value) {
      this.SetToken(newToken, value, true);
    }

    protected void SetToken(JsonToken newToken, object value, bool updateIndex) {
      this._tokenType = newToken;
      this._value = value;
      switch (newToken) {
        case JsonToken.StartObject:
          this._currentState = JsonReader.State.ObjectStart;
          this.Push(JsonContainerType.Object);
          break;
        case JsonToken.StartArray:
          this._currentState = JsonReader.State.ArrayStart;
          this.Push(JsonContainerType.Array);
          break;
        case JsonToken.StartConstructor:
          this._currentState = JsonReader.State.ConstructorStart;
          this.Push(JsonContainerType.Constructor);
          break;
        case JsonToken.PropertyName:
          this._currentState = JsonReader.State.Property;
          this._currentPosition.PropertyName = (string) value;
          break;
        case JsonToken.Raw:
        case JsonToken.Integer:
        case JsonToken.Float:
        case JsonToken.String:
        case JsonToken.Boolean:
        case JsonToken.Null:
        case JsonToken.Undefined:
        case JsonToken.Date:
        case JsonToken.Bytes:
          this.SetPostValueState(updateIndex);
          break;
        case JsonToken.EndObject:
          this.ValidateEnd(JsonToken.EndObject);
          break;
        case JsonToken.EndArray:
          this.ValidateEnd(JsonToken.EndArray);
          break;
        case JsonToken.EndConstructor:
          this.ValidateEnd(JsonToken.EndConstructor);
          break;
      }
    }

    internal void SetPostValueState(bool updateIndex) {
      if (this.Peek() != JsonContainerType.None)
        this._currentState = JsonReader.State.PostValue;
      else
        this.SetFinished();
      if (!updateIndex)
        return;
      this.UpdateScopeWithFinishedValue();
    }

    private void UpdateScopeWithFinishedValue() {
      if (!this._currentPosition.HasIndex)
        return;
      ++this._currentPosition.Position;
    }

    private void ValidateEnd(JsonToken endToken) {
      JsonContainerType jsonContainerType = this.Pop();
      if (this.GetTypeForCloseToken(endToken) != jsonContainerType)
        throw JsonReaderException.Create(this,
          "JsonToken {0} is not valid for closing JsonType {1}.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) endToken, (object) jsonContainerType));
      if (this.Peek() != JsonContainerType.None)
        this._currentState = JsonReader.State.PostValue;
      else
        this.SetFinished();
    }

    protected void SetStateBasedOnCurrent() {
      JsonContainerType jsonContainerType = this.Peek();
      switch (jsonContainerType) {
        case JsonContainerType.None:
          this.SetFinished();
          break;
        case JsonContainerType.Object:
          this._currentState = JsonReader.State.Object;
          break;
        case JsonContainerType.Array:
          this._currentState = JsonReader.State.Array;
          break;
        case JsonContainerType.Constructor:
          this._currentState = JsonReader.State.Constructor;
          break;
        default:
          throw JsonReaderException.Create(this,
            "While setting the reader state back to current object an unexpected JsonType was encountered: {0}"
              .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) jsonContainerType));
      }
    }

    private void SetFinished() {
      if (this.SupportMultipleContent)
        this._currentState = JsonReader.State.Start;
      else
        this._currentState = JsonReader.State.Finished;
    }

    private JsonContainerType GetTypeForCloseToken(JsonToken token) {
      switch (token) {
        case JsonToken.EndObject:
          return JsonContainerType.Object;
        case JsonToken.EndArray:
          return JsonContainerType.Array;
        case JsonToken.EndConstructor:
          return JsonContainerType.Constructor;
        default:
          throw JsonReaderException.Create(this,
            "Not a valid close JsonToken: {0}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) token));
      }
    }

    void IDisposable.Dispose() {
      this.Dispose(true);
      GC.SuppressFinalize((object) this);
    }

    protected virtual void Dispose(bool disposing) {
      if (!(this._currentState != JsonReader.State.Closed & disposing))
        return;
      this.Close();
    }

    public virtual void Close() {
      this._currentState = JsonReader.State.Closed;
      this._tokenType = JsonToken.None;
      this._value = (object) null;
    }

    internal void ReadAndAssert() {
      if (!this.Read())
        throw JsonSerializationException.Create(this, "Unexpected end when reading JSON.");
    }

    internal bool ReadAndMoveToContent() {
      if (this.Read())
        return this.MoveToContent();
      return false;
    }

    internal bool MoveToContent() {
      for (JsonToken tokenType = this.TokenType;
        tokenType == JsonToken.None || tokenType == JsonToken.Comment;
        tokenType = this.TokenType) {
        if (!this.Read())
          return false;
      }

      return true;
    }

    private JsonToken GetContentToken() {
      while (this.Read()) {
        JsonToken tokenType = this.TokenType;
        if (tokenType != JsonToken.Comment)
          return tokenType;
      }

      this.SetToken(JsonToken.None);
      return JsonToken.None;
    }

    protected internal enum State {
      Start,
      Complete,
      Property,
      ObjectStart,
      Object,
      ArrayStart,
      Array,
      Closed,
      PostValue,
      ConstructorStart,
      Constructor,
      Error,
      Finished,
    }
  }
}