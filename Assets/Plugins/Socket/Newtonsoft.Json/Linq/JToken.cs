using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Xml;
using Socket.Newtonsoft.Json.Linq.JsonPath;
using Socket.Newtonsoft.Json.Utilities;
using Socket.Newtonsoft.Json.Utilities.LinqBridge;

namespace Socket.Newtonsoft.Json.Linq {
  public abstract class JToken : IJEnumerable<JToken>, IEnumerable<JToken>, IEnumerable, IJsonLineInfo, ICloneable {
    private static readonly JTokenType[] BooleanTypes = new JTokenType[6] {
      JTokenType.Integer,
      JTokenType.Float,
      JTokenType.String,
      JTokenType.Comment,
      JTokenType.Raw,
      JTokenType.Boolean
    };

    private static readonly JTokenType[] NumberTypes = new JTokenType[6] {
      JTokenType.Integer,
      JTokenType.Float,
      JTokenType.String,
      JTokenType.Comment,
      JTokenType.Raw,
      JTokenType.Boolean
    };

    private static readonly JTokenType[] StringTypes = new JTokenType[11] {
      JTokenType.Date,
      JTokenType.Integer,
      JTokenType.Float,
      JTokenType.String,
      JTokenType.Comment,
      JTokenType.Raw,
      JTokenType.Boolean,
      JTokenType.Bytes,
      JTokenType.Guid,
      JTokenType.TimeSpan,
      JTokenType.Uri
    };

    private static readonly JTokenType[] GuidTypes = new JTokenType[5] {
      JTokenType.String,
      JTokenType.Comment,
      JTokenType.Raw,
      JTokenType.Guid,
      JTokenType.Bytes
    };

    private static readonly JTokenType[] TimeSpanTypes = new JTokenType[4] {
      JTokenType.String,
      JTokenType.Comment,
      JTokenType.Raw,
      JTokenType.TimeSpan
    };

    private static readonly JTokenType[] UriTypes = new JTokenType[4] {
      JTokenType.String,
      JTokenType.Comment,
      JTokenType.Raw,
      JTokenType.Uri
    };

    private static readonly JTokenType[] CharTypes = new JTokenType[5] {
      JTokenType.Integer,
      JTokenType.Float,
      JTokenType.String,
      JTokenType.Comment,
      JTokenType.Raw
    };

    private static readonly JTokenType[] DateTimeTypes = new JTokenType[4] {
      JTokenType.Date,
      JTokenType.String,
      JTokenType.Comment,
      JTokenType.Raw
    };

    private static readonly JTokenType[] BytesTypes = new JTokenType[5] {
      JTokenType.Bytes,
      JTokenType.String,
      JTokenType.Comment,
      JTokenType.Raw,
      JTokenType.Integer
    };

    private static JTokenEqualityComparer _equalityComparer;
    private JContainer _parent;
    private JToken _previous;
    private JToken _next;
    private object _annotations;

    public static JTokenEqualityComparer EqualityComparer {
      get {
        if (JToken._equalityComparer == null)
          JToken._equalityComparer = new JTokenEqualityComparer();
        return JToken._equalityComparer;
      }
    }

    public JContainer Parent {
      [DebuggerStepThrough] get { return this._parent; }
      internal set { this._parent = value; }
    }

    public JToken Root {
      get {
        JContainer parent = this.Parent;
        if (parent == null)
          return this;
        while (parent.Parent != null)
          parent = parent.Parent;
        return (JToken) parent;
      }
    }

    internal abstract JToken CloneToken();

    internal abstract bool DeepEquals(JToken node);

    public abstract JTokenType Type { get; }

    public abstract bool HasValues { get; }

    public static bool DeepEquals(JToken t1, JToken t2) {
      if (t1 == t2)
        return true;
      if (t1 != null && t2 != null)
        return t1.DeepEquals(t2);
      return false;
    }

    public JToken Next {
      get { return this._next; }
      internal set { this._next = value; }
    }

    public JToken Previous {
      get { return this._previous; }
      internal set { this._previous = value; }
    }

    public string Path {
      get {
        if (this.Parent == null)
          return string.Empty;
        List<JsonPosition> positions = new List<JsonPosition>();
        JToken jtoken1 = (JToken) null;
        for (JToken jtoken2 = this; jtoken2 != null; jtoken2 = (JToken) jtoken2.Parent) {
          JsonPosition jsonPosition1;
          switch (jtoken2.Type) {
            case JTokenType.Array:
            case JTokenType.Constructor:
              if (jtoken1 != null) {
                int num = ((IList<JToken>) jtoken2).IndexOf(jtoken1);
                List<JsonPosition> jsonPositionList = positions;
                jsonPosition1 = new JsonPosition(JsonContainerType.Array);
                jsonPosition1.Position = num;
                JsonPosition jsonPosition2 = jsonPosition1;
                jsonPositionList.Add(jsonPosition2);
                break;
              }

              break;
            case JTokenType.Property:
              JProperty jproperty = (JProperty) jtoken2;
              List<JsonPosition> jsonPositionList1 = positions;
              jsonPosition1 = new JsonPosition(JsonContainerType.Object);
              jsonPosition1.PropertyName = jproperty.Name;
              JsonPosition jsonPosition3 = jsonPosition1;
              jsonPositionList1.Add(jsonPosition3);
              break;
          }

          jtoken1 = jtoken2;
        }

        positions.Reverse();
        return JsonPosition.BuildPath(positions, new JsonPosition?());
      }
    }

    internal JToken() {
    }

    public void AddAfterSelf(object content) {
      if (this._parent == null)
        throw new InvalidOperationException("The parent is missing.");
      this._parent.AddInternal(this._parent.IndexOfItem(this) + 1, content, false);
    }

    public void AddBeforeSelf(object content) {
      if (this._parent == null)
        throw new InvalidOperationException("The parent is missing.");
      this._parent.AddInternal(this._parent.IndexOfItem(this), content, false);
    }

    public IEnumerable<JToken> Ancestors() {
      return this.GetAncestors(false);
    }

    public IEnumerable<JToken> AncestorsAndSelf() {
      return this.GetAncestors(true);
    }

    internal IEnumerable<JToken> GetAncestors(bool self) {
      JToken current;
      for (current = self ? this : (JToken) this.Parent; current != null; current = (JToken) current.Parent)
        yield return current;
      current = (JToken) null;
    }

    public IEnumerable<JToken> AfterSelf() {
      if (this.Parent != null) {
        JToken o;
        for (o = this.Next; o != null; o = o.Next)
          yield return o;
        o = (JToken) null;
      }
    }

    public IEnumerable<JToken> BeforeSelf() {
      JToken o;
      for (o = this.Parent.First; o != this; o = o.Next)
        yield return o;
      o = (JToken) null;
    }

    public virtual JToken this[object key] {
      get {
        throw new InvalidOperationException(
          "Cannot access child value on {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) this.GetType()));
      }
      set {
        throw new InvalidOperationException(
          "Cannot set child value on {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) this.GetType()));
      }
    }

    public virtual T Value<T>(object key) {
      JToken token = this[key];
      if (token != null)
        return token.Convert<JToken, T>();
      return default(T);
    }

    public virtual JToken First {
      get {
        throw new InvalidOperationException(
          "Cannot access child value on {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) this.GetType()));
      }
    }

    public virtual JToken Last {
      get {
        throw new InvalidOperationException(
          "Cannot access child value on {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) this.GetType()));
      }
    }

    public virtual JEnumerable<JToken> Children() {
      return JEnumerable<JToken>.Empty;
    }

    public JEnumerable<T> Children<T>() where T : JToken {
      return new JEnumerable<T>(this.Children().OfType<T>());
    }

    public virtual IEnumerable<T> Values<T>() {
      throw new InvalidOperationException(
        "Cannot access child value on {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) this.GetType()));
    }

    public void Remove() {
      if (this._parent == null)
        throw new InvalidOperationException("The parent is missing.");
      this._parent.RemoveItem(this);
    }

    public void Replace(JToken value) {
      if (this._parent == null)
        throw new InvalidOperationException("The parent is missing.");
      this._parent.ReplaceItem(this, value);
    }

    public abstract void WriteTo(JsonWriter writer, params JsonConverter[] converters);

    public override string ToString() {
      return this.ToString(Formatting.Indented);
    }

    public string ToString(Formatting formatting, params JsonConverter[] converters) {
      using (StringWriter stringWriter = new StringWriter((IFormatProvider) CultureInfo.InvariantCulture)) {
        JsonTextWriter jsonTextWriter = new JsonTextWriter((TextWriter) stringWriter);
        jsonTextWriter.Formatting = formatting;
        this.WriteTo((JsonWriter) jsonTextWriter, converters);
        return stringWriter.ToString();
      }
    }

    private static JValue EnsureValue(JToken value) {
      if (value == null)
        throw new ArgumentNullException(nameof(value));
      if (value is JProperty)
        value = ((JProperty) value).Value;
      return value as JValue;
    }

    private static string GetType(JToken token) {
      ValidationUtils.ArgumentNotNull((object) token, nameof(token));
      if (token is JProperty)
        token = ((JProperty) token).Value;
      return token.Type.ToString();
    }

    private static bool ValidateToken(JToken o, JTokenType[] validTypes, bool nullable) {
      if (Array.IndexOf<JTokenType>(validTypes, o.Type) != -1)
        return true;
      if (!nullable)
        return false;
      if (o.Type != JTokenType.Null)
        return o.Type == JTokenType.Undefined;
      return true;
    }

    public static explicit operator bool(JToken value) {
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.BooleanTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to Boolean.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      return Convert.ToBoolean(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    public static explicit operator bool?(JToken value) {
      if (value == null)
        return new bool?();
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.BooleanTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to Boolean.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return new bool?();
      return new bool?(Convert.ToBoolean(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
    }

    public static explicit operator long(JToken value) {
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to Int64.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      return Convert.ToInt64(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    public static explicit operator DateTime?(JToken value) {
      if (value == null)
        return new DateTime?();
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.DateTimeTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to DateTime.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return new DateTime?();
      return new DateTime?(Convert.ToDateTime(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
    }

    public static explicit operator Decimal?(JToken value) {
      if (value == null)
        return new Decimal?();
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to Decimal.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return new Decimal?();
      return new Decimal?(Convert.ToDecimal(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
    }

    public static explicit operator double?(JToken value) {
      if (value == null)
        return new double?();
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to Double.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return new double?();
      return new double?(Convert.ToDouble(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
    }

    public static explicit operator char?(JToken value) {
      if (value == null)
        return new char?();
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.CharTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to Char.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return new char?();
      return new char?(Convert.ToChar(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
    }

    public static explicit operator int(JToken value) {
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to Int32.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      return Convert.ToInt32(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    public static explicit operator short(JToken value) {
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to Int16.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      return Convert.ToInt16(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    [CLSCompliant(false)]
    public static explicit operator ushort(JToken value) {
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to UInt16.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      return Convert.ToUInt16(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    [CLSCompliant(false)]
    public static explicit operator char(JToken value) {
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.CharTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to Char.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      return Convert.ToChar(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    public static explicit operator byte(JToken value) {
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to Byte.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      return Convert.ToByte(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    [CLSCompliant(false)]
    public static explicit operator sbyte(JToken value) {
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to SByte.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      return Convert.ToSByte(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    public static explicit operator int?(JToken value) {
      if (value == null)
        return new int?();
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to Int32.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return new int?();
      return new int?(Convert.ToInt32(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
    }

    public static explicit operator short?(JToken value) {
      if (value == null)
        return new short?();
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to Int16.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return new short?();
      return new short?(Convert.ToInt16(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
    }

    [CLSCompliant(false)]
    public static explicit operator ushort?(JToken value) {
      if (value == null)
        return new ushort?();
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to UInt16.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return new ushort?();
      return new ushort?(Convert.ToUInt16(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
    }

    public static explicit operator byte?(JToken value) {
      if (value == null)
        return new byte?();
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to Byte.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return new byte?();
      return new byte?(Convert.ToByte(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
    }

    [CLSCompliant(false)]
    public static explicit operator sbyte?(JToken value) {
      if (value == null)
        return new sbyte?();
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to SByte.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return new sbyte?();
      return new sbyte?(Convert.ToSByte(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
    }

    public static explicit operator DateTime(JToken value) {
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.DateTimeTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to DateTime.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      return Convert.ToDateTime(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    public static explicit operator long?(JToken value) {
      if (value == null)
        return new long?();
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to Int64.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return new long?();
      return new long?(Convert.ToInt64(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
    }

    public static explicit operator float?(JToken value) {
      if (value == null)
        return new float?();
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to Single.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return new float?();
      return new float?(Convert.ToSingle(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
    }

    public static explicit operator Decimal(JToken value) {
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to Decimal.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      return Convert.ToDecimal(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    [CLSCompliant(false)]
    public static explicit operator uint?(JToken value) {
      if (value == null)
        return new uint?();
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to UInt32.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return new uint?();
      return new uint?(Convert.ToUInt32(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
    }

    [CLSCompliant(false)]
    public static explicit operator ulong?(JToken value) {
      if (value == null)
        return new ulong?();
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to UInt64.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return new ulong?();
      return new ulong?(Convert.ToUInt64(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
    }

    public static explicit operator double(JToken value) {
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to Double.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      return Convert.ToDouble(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    public static explicit operator float(JToken value) {
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to Single.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      return Convert.ToSingle(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    public static explicit operator string(JToken value) {
      if (value == null)
        return (string) null;
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.StringTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to String.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return (string) null;
      byte[] inArray = jvalue.Value as byte[];
      if (inArray != null)
        return Convert.ToBase64String(inArray);
      return Convert.ToString(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    [CLSCompliant(false)]
    public static explicit operator uint(JToken value) {
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to UInt32.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      return Convert.ToUInt32(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    [CLSCompliant(false)]
    public static explicit operator ulong(JToken value) {
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.NumberTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to UInt64.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      return Convert.ToUInt64(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    public static explicit operator byte[](JToken value) {
      if (value == null)
        return (byte[]) null;
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.BytesTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to byte array.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value is string)
        return Convert.FromBase64String(Convert.ToString(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
      byte[] numArray = jvalue.Value as byte[];
      if (numArray != null)
        return numArray;
      throw new ArgumentException(
        "Can not convert {0} to byte array.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) JToken.GetType(value)));
    }

    public static explicit operator Guid(JToken value) {
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.GuidTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to Guid.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      byte[] b = jvalue.Value as byte[];
      if (b != null)
        return new Guid(b);
      if (!(jvalue.Value is Guid))
        return new Guid(Convert.ToString(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
      return (Guid) jvalue.Value;
    }

    public static explicit operator Guid?(JToken value) {
      if (value == null)
        return new Guid?();
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.GuidTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to Guid.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return new Guid?();
      byte[] b = jvalue.Value as byte[];
      if (b != null)
        return new Guid?(new Guid(b));
      return new Guid?(jvalue.Value is Guid
        ? (Guid) jvalue.Value
        : new Guid(Convert.ToString(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture)));
    }

    public static explicit operator TimeSpan(JToken value) {
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.TimeSpanTypes, false))
        throw new ArgumentException(
          "Can not convert {0} to TimeSpan.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (!(jvalue.Value is TimeSpan))
        return ConvertUtils.ParseTimeSpan(
          Convert.ToString(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
      return (TimeSpan) jvalue.Value;
    }

    public static explicit operator TimeSpan?(JToken value) {
      if (value == null)
        return new TimeSpan?();
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.TimeSpanTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to TimeSpan.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return new TimeSpan?();
      return new TimeSpan?(jvalue.Value is TimeSpan
        ? (TimeSpan) jvalue.Value
        : ConvertUtils.ParseTimeSpan(Convert.ToString(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture)));
    }

    public static explicit operator Uri(JToken value) {
      if (value == null)
        return (Uri) null;
      JValue jvalue = JToken.EnsureValue(value);
      if (jvalue == null || !JToken.ValidateToken((JToken) jvalue, JToken.UriTypes, true))
        throw new ArgumentException(
          "Can not convert {0} to Uri.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) JToken.GetType(value)));
      if (jvalue.Value == null)
        return (Uri) null;
      if ((object) (jvalue.Value as Uri) == null)
        return new Uri(Convert.ToString(jvalue.Value, (IFormatProvider) CultureInfo.InvariantCulture));
      return (Uri) jvalue.Value;
    }

    public static implicit operator JToken(bool value) {
      return (JToken) new JValue(value);
    }

    public static implicit operator JToken(byte value) {
      return (JToken) new JValue((long) value);
    }

    public static implicit operator JToken(byte? value) {
      return (JToken) new JValue((object) value);
    }

    [CLSCompliant(false)]
    public static implicit operator JToken(sbyte value) {
      return (JToken) new JValue((long) value);
    }

    [CLSCompliant(false)]
    public static implicit operator JToken(sbyte? value) {
      return (JToken) new JValue((object) value);
    }

    public static implicit operator JToken(bool? value) {
      return (JToken) new JValue((object) value);
    }

    public static implicit operator JToken(long value) {
      return (JToken) new JValue(value);
    }

    public static implicit operator JToken(DateTime? value) {
      return (JToken) new JValue((object) value);
    }

    public static implicit operator JToken(Decimal? value) {
      return (JToken) new JValue((object) value);
    }

    public static implicit operator JToken(double? value) {
      return (JToken) new JValue((object) value);
    }

    [CLSCompliant(false)]
    public static implicit operator JToken(short value) {
      return (JToken) new JValue((long) value);
    }

    [CLSCompliant(false)]
    public static implicit operator JToken(ushort value) {
      return (JToken) new JValue((long) value);
    }

    public static implicit operator JToken(int value) {
      return (JToken) new JValue((long) value);
    }

    public static implicit operator JToken(int? value) {
      return (JToken) new JValue((object) value);
    }

    public static implicit operator JToken(DateTime value) {
      return (JToken) new JValue(value);
    }

    public static implicit operator JToken(long? value) {
      return (JToken) new JValue((object) value);
    }

    public static implicit operator JToken(float? value) {
      return (JToken) new JValue((object) value);
    }

    public static implicit operator JToken(Decimal value) {
      return (JToken) new JValue(value);
    }

    [CLSCompliant(false)]
    public static implicit operator JToken(short? value) {
      return (JToken) new JValue((object) value);
    }

    [CLSCompliant(false)]
    public static implicit operator JToken(ushort? value) {
      return (JToken) new JValue((object) value);
    }

    [CLSCompliant(false)]
    public static implicit operator JToken(uint? value) {
      return (JToken) new JValue((object) value);
    }

    [CLSCompliant(false)]
    public static implicit operator JToken(ulong? value) {
      return (JToken) new JValue((object) value);
    }

    public static implicit operator JToken(double value) {
      return (JToken) new JValue(value);
    }

    public static implicit operator JToken(float value) {
      return (JToken) new JValue(value);
    }

    public static implicit operator JToken(string value) {
      return (JToken) new JValue(value);
    }

    [CLSCompliant(false)]
    public static implicit operator JToken(uint value) {
      return (JToken) new JValue((long) value);
    }

    [CLSCompliant(false)]
    public static implicit operator JToken(ulong value) {
      return (JToken) new JValue(value);
    }

    public static implicit operator JToken(byte[] value) {
      return (JToken) new JValue((object) value);
    }

    public static implicit operator JToken(Uri value) {
      return (JToken) new JValue(value);
    }

    public static implicit operator JToken(TimeSpan value) {
      return (JToken) new JValue(value);
    }

    public static implicit operator JToken(TimeSpan? value) {
      return (JToken) new JValue((object) value);
    }

    public static implicit operator JToken(Guid value) {
      return (JToken) new JValue(value);
    }

    public static implicit operator JToken(Guid? value) {
      return (JToken) new JValue((object) value);
    }

    IEnumerator IEnumerable.GetEnumerator() {
      return (IEnumerator) ((IEnumerable<JToken>) this).GetEnumerator();
    }

    IEnumerator<JToken> IEnumerable<JToken>.GetEnumerator() {
      return this.Children().GetEnumerator();
    }

    internal abstract int GetDeepHashCode();

    IJEnumerable<JToken> IJEnumerable<JToken>.this[object key] {
      get { return (IJEnumerable<JToken>) this[key]; }
    }

    public JsonReader CreateReader() {
      return (JsonReader) new JTokenReader(this);
    }

    internal static JToken FromObjectInternal(object o, JsonSerializer jsonSerializer) {
      ValidationUtils.ArgumentNotNull(o, nameof(o));
      ValidationUtils.ArgumentNotNull((object) jsonSerializer, nameof(jsonSerializer));
      using (JTokenWriter jtokenWriter = new JTokenWriter()) {
        jsonSerializer.Serialize((JsonWriter) jtokenWriter, o);
        return jtokenWriter.Token;
      }
    }

    public static JToken FromObject(object o) {
      return JToken.FromObjectInternal(o, JsonSerializer.CreateDefault());
    }

    public static JToken FromObject(object o, JsonSerializer jsonSerializer) {
      return JToken.FromObjectInternal(o, jsonSerializer);
    }

    public T ToObject<T>() {
      return (T) this.ToObject(typeof(T));
    }

    public object ToObject(System.Type objectType) {
      if (JsonConvert.DefaultSettings == null) {
        bool isEnum;
        PrimitiveTypeCode typeCode = ConvertUtils.GetTypeCode(objectType, out isEnum);
        if (isEnum) {
          if (this.Type == JTokenType.String) {
            try {
              return this.ToObject(objectType, JsonSerializer.CreateDefault());
            } catch (Exception ex) {
              throw new ArgumentException(
                "Could not convert '{0}' to {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                  (object) (string) this,
                  (object) (objectType.IsEnum()
                    ? (MemberInfo) objectType
                    : (MemberInfo) Nullable.GetUnderlyingType(objectType)).Name), ex);
            }
          } else if (this.Type == JTokenType.Integer)
            return Enum.ToObject(objectType.IsEnum() ? objectType : Nullable.GetUnderlyingType(objectType),
              ((JValue) this).Value);
        }

        switch (typeCode) {
          case PrimitiveTypeCode.Char:
            return (object) (char) this;
          case PrimitiveTypeCode.CharNullable:
            return (object) (char?) this;
          case PrimitiveTypeCode.Boolean:
            return (object) (bool) this;
          case PrimitiveTypeCode.BooleanNullable:
            return (object) (bool?) this;
          case PrimitiveTypeCode.SByte:
            return (object) (sbyte) this;
          case PrimitiveTypeCode.SByteNullable:
            return (object) (sbyte?) this;
          case PrimitiveTypeCode.Int16:
            return (object) (short) this;
          case PrimitiveTypeCode.Int16Nullable:
            return (object) (short?) this;
          case PrimitiveTypeCode.UInt16:
            return (object) (ushort) this;
          case PrimitiveTypeCode.UInt16Nullable:
            return (object) (ushort?) this;
          case PrimitiveTypeCode.Int32:
            return (object) (int) this;
          case PrimitiveTypeCode.Int32Nullable:
            return (object) (int?) this;
          case PrimitiveTypeCode.Byte:
            return (object) (byte) this;
          case PrimitiveTypeCode.ByteNullable:
            return (object) (byte?) this;
          case PrimitiveTypeCode.UInt32:
            return (object) (uint) this;
          case PrimitiveTypeCode.UInt32Nullable:
            return (object) (uint?) this;
          case PrimitiveTypeCode.Int64:
            return (object) (long) this;
          case PrimitiveTypeCode.Int64Nullable:
            return (object) (long?) this;
          case PrimitiveTypeCode.UInt64:
            return (object) (ulong) this;
          case PrimitiveTypeCode.UInt64Nullable:
            return (object) (ulong?) this;
          case PrimitiveTypeCode.Single:
            return (object) (float) this;
          case PrimitiveTypeCode.SingleNullable:
            return (object) (float?) this;
          case PrimitiveTypeCode.Double:
            return (object) (double) this;
          case PrimitiveTypeCode.DoubleNullable:
            return (object) (double?) this;
          case PrimitiveTypeCode.DateTime:
            return (object) (DateTime) this;
          case PrimitiveTypeCode.DateTimeNullable:
            return (object) (DateTime?) this;
          case PrimitiveTypeCode.Decimal:
            return (object) (Decimal) this;
          case PrimitiveTypeCode.DecimalNullable:
            return (object) (Decimal?) this;
          case PrimitiveTypeCode.Guid:
            return (object) (Guid) this;
          case PrimitiveTypeCode.GuidNullable:
            return (object) (Guid?) this;
          case PrimitiveTypeCode.TimeSpan:
            return (object) (TimeSpan) this;
          case PrimitiveTypeCode.TimeSpanNullable:
            return (object) (TimeSpan?) this;
          case PrimitiveTypeCode.Uri:
            return (object) (Uri) this;
          case PrimitiveTypeCode.String:
            return (object) (string) this;
        }
      }

      return this.ToObject(objectType, JsonSerializer.CreateDefault());
    }

    public T ToObject<T>(JsonSerializer jsonSerializer) {
      return (T) this.ToObject(typeof(T), jsonSerializer);
    }

    public object ToObject(System.Type objectType, JsonSerializer jsonSerializer) {
      ValidationUtils.ArgumentNotNull((object) jsonSerializer, nameof(jsonSerializer));
      using (JTokenReader jtokenReader = new JTokenReader(this))
        return jsonSerializer.Deserialize((JsonReader) jtokenReader, objectType);
    }

    public static JToken ReadFrom(JsonReader reader) {
      return JToken.ReadFrom(reader, (JsonLoadSettings) null);
    }

    public static JToken ReadFrom(JsonReader reader, JsonLoadSettings settings) {
      ValidationUtils.ArgumentNotNull((object) reader, nameof(reader));
      if (reader.TokenType == JsonToken.None && (settings == null || settings.CommentHandling != CommentHandling.Ignore
            ? (reader.Read() ? 1 : 0)
            : (reader.ReadAndMoveToContent() ? 1 : 0)) == 0)
        throw JsonReaderException.Create(reader, "Error reading JToken from JsonReader.");
      IJsonLineInfo lineInfo = reader as IJsonLineInfo;
      switch (reader.TokenType) {
        case JsonToken.StartObject:
          return (JToken) JObject.Load(reader, settings);
        case JsonToken.StartArray:
          return (JToken) JArray.Load(reader, settings);
        case JsonToken.StartConstructor:
          return (JToken) JConstructor.Load(reader, settings);
        case JsonToken.PropertyName:
          return (JToken) JProperty.Load(reader, settings);
        case JsonToken.Comment:
          JValue comment = JValue.CreateComment(reader.Value.ToString());
          comment.SetLineInfo(lineInfo, settings);
          return (JToken) comment;
        case JsonToken.Integer:
        case JsonToken.Float:
        case JsonToken.String:
        case JsonToken.Boolean:
        case JsonToken.Date:
        case JsonToken.Bytes:
          JValue jvalue1 = new JValue(reader.Value);
          jvalue1.SetLineInfo(lineInfo, settings);
          return (JToken) jvalue1;
        case JsonToken.Null:
          JValue jvalue2 = JValue.CreateNull();
          jvalue2.SetLineInfo(lineInfo, settings);
          return (JToken) jvalue2;
        case JsonToken.Undefined:
          JValue undefined = JValue.CreateUndefined();
          undefined.SetLineInfo(lineInfo, settings);
          return (JToken) undefined;
        default:
          throw JsonReaderException.Create(reader,
            "Error reading JToken from JsonReader. Unexpected token: {0}".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) reader.TokenType));
      }
    }

    public static JToken Parse(string json) {
      return JToken.Parse(json, (JsonLoadSettings) null);
    }

    public static JToken Parse(string json, JsonLoadSettings settings) {
      using (JsonReader reader = (JsonReader) new JsonTextReader((TextReader) new StringReader(json))) {
        JToken jtoken = JToken.Load(reader, settings);
        do
          ;
        while (reader.Read());
        return jtoken;
      }
    }

    public static JToken Load(JsonReader reader, JsonLoadSettings settings) {
      return JToken.ReadFrom(reader, settings);
    }

    public static JToken Load(JsonReader reader) {
      return JToken.Load(reader, (JsonLoadSettings) null);
    }

    internal void SetLineInfo(IJsonLineInfo lineInfo, JsonLoadSettings settings) {
      if (settings != null && settings.LineInfoHandling != LineInfoHandling.Load ||
          (lineInfo == null || !lineInfo.HasLineInfo()))
        return;
      this.SetLineInfo(lineInfo.LineNumber, lineInfo.LinePosition);
    }

    internal void SetLineInfo(int lineNumber, int linePosition) {
      this.AddAnnotation((object) new JToken.LineInfoAnnotation(lineNumber, linePosition));
    }

    bool IJsonLineInfo.HasLineInfo() {
      return this.Annotation<JToken.LineInfoAnnotation>() != null;
    }

    int IJsonLineInfo.LineNumber {
      get {
        JToken.LineInfoAnnotation lineInfoAnnotation = this.Annotation<JToken.LineInfoAnnotation>();
        if (lineInfoAnnotation != null)
          return lineInfoAnnotation.LineNumber;
        return 0;
      }
    }

    int IJsonLineInfo.LinePosition {
      get {
        JToken.LineInfoAnnotation lineInfoAnnotation = this.Annotation<JToken.LineInfoAnnotation>();
        if (lineInfoAnnotation != null)
          return lineInfoAnnotation.LinePosition;
        return 0;
      }
    }

    public JToken SelectToken(string path) {
      return this.SelectToken(path, false);
    }

    public JToken SelectToken(string path, bool errorWhenNoMatch) {
      JPath jpath = new JPath(path);
      JToken jtoken1 = (JToken) null;
      int num = errorWhenNoMatch ? 1 : 0;
      foreach (JToken jtoken2 in jpath.Evaluate(this, this, num != 0)) {
        if (jtoken1 != null)
          throw new JsonException("Path returned multiple tokens.");
        jtoken1 = jtoken2;
      }

      return jtoken1;
    }

    public IEnumerable<JToken> SelectTokens(string path) {
      return this.SelectTokens(path, false);
    }

    public IEnumerable<JToken> SelectTokens(string path, bool errorWhenNoMatch) {
      return new JPath(path).Evaluate(this, this, errorWhenNoMatch);
    }

    object ICloneable.Clone() {
      return (object) this.DeepClone();
    }

    public JToken DeepClone() {
      return this.CloneToken();
    }

    public void AddAnnotation(object annotation) {
      if (annotation == null)
        throw new ArgumentNullException(nameof(annotation));
      if (this._annotations == null) {
        object obj;
        if (!(annotation is object[])) {
          obj = annotation;
        } else {
          obj = (object) new object[1];

          
          //obj[0] = annotation; 원래 이거였는데 수정됐음. 이런 방법은 지금 .net에 없네..
          obj = annotation;
        }

        this._annotations = obj;
      } else {
        object[] annotations = this._annotations as object[];
        if (annotations == null) {
          this._annotations = (object) new object[2] {
            this._annotations,
            annotation
          };
        } else {
          int index = 0;
          while (index < annotations.Length && annotations[index] != null)
            ++index;
          if (index == annotations.Length) {
            Array.Resize<object>(ref annotations, index * 2);
            this._annotations = (object) annotations;
          }

          annotations[index] = annotation;
        }
      }
    }

    public T Annotation<T>() where T : class {
      if (this._annotations != null) {
        object[] annotations = this._annotations as object[];
        if (annotations == null)
          return this._annotations as T;
        for (int index = 0; index < annotations.Length; ++index) {
          object obj1 = annotations[index];
          if (obj1 != null) {
            T obj2 = obj1 as T;
            if ((object) obj2 != null)
              return obj2;
          } else
            break;
        }
      }

      return default(T);
    }

    public object Annotation(System.Type type) {
      if (type == null)
        throw new ArgumentNullException(nameof(type));
      if (this._annotations != null) {
        object[] annotations = this._annotations as object[];
        if (annotations == null) {
          if (type.IsInstanceOfType(this._annotations))
            return this._annotations;
        } else {
          for (int index = 0; index < annotations.Length; ++index) {
            object o = annotations[index];
            if (o != null) {
              if (type.IsInstanceOfType(o))
                return o;
            } else
              break;
          }
        }
      }

      return (object) null;
    }

    public IEnumerable<T> Annotations<T>() where T : class {
      if (this._annotations != null) {
        object[] annotations = this._annotations as object[];
        if (annotations != null) {
          for (int i = 0; i < annotations.Length; ++i) {
            object obj1 = annotations[i];
            if (obj1 == null)
              break;
            T obj2 = obj1 as T;
            if ((object) obj2 != null)
              yield return obj2;
          }
        } else {
          T annotations1 = this._annotations as T;
          if ((object) annotations1 != null)
            yield return annotations1;
        }
      }
    }

    public IEnumerable<object> Annotations(System.Type type) {
      if (type == null)
        throw new ArgumentNullException(nameof(type));
      if (this._annotations != null) {
        object[] annotations = this._annotations as object[];
        if (annotations != null) {
          for (int i = 0; i < annotations.Length; ++i) {
            object o = annotations[i];
            if (o == null)
              break;
            if (type.IsInstanceOfType(o))
              yield return o;
          }
        } else if (type.IsInstanceOfType(this._annotations))
          yield return this._annotations;
      }
    }

    public void RemoveAnnotations<T>() where T : class {
      if (this._annotations == null)
        return;
      object[] annotations = this._annotations as object[];
      if (annotations == null) {
        if (!(this._annotations is T))
          return;
        this._annotations = (object) null;
      } else {
        int index = 0;
        int num = 0;
        for (; index < annotations.Length; ++index) {
          object obj = annotations[index];
          if (obj != null) {
            if (!(obj is T))
              annotations[num++] = obj;
          } else
            break;
        }

        if (num != 0) {
          while (num < index)
            annotations[num++] = (object) null;
        } else
          this._annotations = (object) null;
      }
    }

    public void RemoveAnnotations(System.Type type) {
      if (type == null)
        throw new ArgumentNullException(nameof(type));
      if (this._annotations == null)
        return;
      object[] annotations = this._annotations as object[];
      if (annotations == null) {
        if (!type.IsInstanceOfType(this._annotations))
          return;
        this._annotations = (object) null;
      } else {
        int index = 0;
        int num = 0;
        for (; index < annotations.Length; ++index) {
          object o = annotations[index];
          if (o != null) {
            if (!type.IsInstanceOfType(o))
              annotations[num++] = o;
          } else
            break;
        }

        if (num != 0) {
          while (num < index)
            annotations[num++] = (object) null;
        } else
          this._annotations = (object) null;
      }
    }

    private class LineInfoAnnotation {
      internal readonly int LineNumber;
      internal readonly int LinePosition;

      public LineInfoAnnotation(int lineNumber, int linePosition) {
        this.LineNumber = lineNumber;
        this.LinePosition = linePosition;
      }
    }
  }
}