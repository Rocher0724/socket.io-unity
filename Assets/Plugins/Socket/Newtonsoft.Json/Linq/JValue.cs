using System;
using System.Collections.Generic;
using System.Globalization;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Linq {
public class JValue : JToken, IEquatable<JValue>, IFormattable, IComparable, IComparable<JValue>, IConvertible
  {
    private JTokenType _valueType;
    private object _value;

    internal JValue(object value, JTokenType type)
    {
      this._value = value;
      this._valueType = type;
    }

    public JValue(JValue other)
      : this(other.Value, other.Type)
    {
    }

    public JValue(long value)
      : this((object) value, JTokenType.Integer)
    {
    }

    public JValue(Decimal value)
      : this((object) value, JTokenType.Float)
    {
    }

    public JValue(char value)
      : this((object) value, JTokenType.String)
    {
    }

    [CLSCompliant(false)]
    public JValue(ulong value)
      : this((object) value, JTokenType.Integer)
    {
    }

    public JValue(double value)
      : this((object) value, JTokenType.Float)
    {
    }

    public JValue(float value)
      : this((object) value, JTokenType.Float)
    {
    }

    public JValue(DateTime value)
      : this((object) value, JTokenType.Date)
    {
    }

    public JValue(bool value)
      : this((object) value, JTokenType.Boolean)
    {
    }

    public JValue(string value)
      : this((object) value, JTokenType.String)
    {
    }

    public JValue(Guid value)
      : this((object) value, JTokenType.Guid)
    {
    }

    public JValue(Uri value)
      : this((object) value, value != (Uri) null ? JTokenType.Uri : JTokenType.Null)
    {
    }

    public JValue(TimeSpan value)
      : this((object) value, JTokenType.TimeSpan)
    {
    }

    public JValue(object value)
      : this(value, JValue.GetValueType(new JTokenType?(), value))
    {
    }

    internal override bool DeepEquals(JToken node)
    {
      JValue v2 = node as JValue;
      if (v2 == null)
        return false;
      if (v2 == this)
        return true;
      return JValue.ValuesEquals(this, v2);
    }

    public override bool HasValues
    {
      get
      {
        return false;
      }
    }

    internal static int Compare(JTokenType valueType, object objA, object objB)
    {
      if (objA == objB)
        return 0;
      if (objB == null)
        return 1;
      if (objA == null)
        return -1;
      switch (valueType)
      {
        case JTokenType.Comment:
        case JTokenType.String:
        case JTokenType.Raw:
          return string.CompareOrdinal(Convert.ToString(objA, (IFormatProvider) CultureInfo.InvariantCulture), Convert.ToString(objB, (IFormatProvider) CultureInfo.InvariantCulture));
        case JTokenType.Integer:
          if (objA is ulong || objB is ulong || (objA is Decimal || objB is Decimal))
            return Convert.ToDecimal(objA, (IFormatProvider) CultureInfo.InvariantCulture).CompareTo(Convert.ToDecimal(objB, (IFormatProvider) CultureInfo.InvariantCulture));
          if (objA is float || objB is float || (objA is double || objB is double))
            return JValue.CompareFloat(objA, objB);
          return Convert.ToInt64(objA, (IFormatProvider) CultureInfo.InvariantCulture).CompareTo(Convert.ToInt64(objB, (IFormatProvider) CultureInfo.InvariantCulture));
        case JTokenType.Float:
          return JValue.CompareFloat(objA, objB);
        case JTokenType.Boolean:
          return Convert.ToBoolean(objA, (IFormatProvider) CultureInfo.InvariantCulture).CompareTo(Convert.ToBoolean(objB, (IFormatProvider) CultureInfo.InvariantCulture));
        case JTokenType.Date:
          return ((DateTime) objA).CompareTo(Convert.ToDateTime(objB, (IFormatProvider) CultureInfo.InvariantCulture));
        case JTokenType.Bytes:
          byte[] a2 = objB as byte[];
          if (a2 == null)
            throw new ArgumentException("Object must be of type byte[].");
          return MiscellaneousUtils.ByteArrayCompare(objA as byte[], a2);
        case JTokenType.Guid:
          if (!(objB is Guid))
            throw new ArgumentException("Object must be of type Guid.");
          return ((Guid) objA).CompareTo((Guid) objB);
        case JTokenType.Uri:
          Uri uri = objB as Uri;
          if (uri == (Uri) null)
            throw new ArgumentException("Object must be of type Uri.");
          return Comparer<string>.Default.Compare(((Uri) objA).ToString(), uri.ToString());
        case JTokenType.TimeSpan:
          if (!(objB is TimeSpan))
            throw new ArgumentException("Object must be of type TimeSpan.");
          return ((TimeSpan) objA).CompareTo((TimeSpan) objB);
        default:
          throw MiscellaneousUtils.CreateArgumentOutOfRangeException(nameof (valueType), (object) valueType, "Unexpected value type: {0}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) valueType));
      }
    }

    private static int CompareFloat(object objA, object objB)
    {
      double d1 = Convert.ToDouble(objA, (IFormatProvider) CultureInfo.InvariantCulture);
      double d2 = Convert.ToDouble(objB, (IFormatProvider) CultureInfo.InvariantCulture);
      if (MathUtils.ApproxEquals(d1, d2))
        return 0;
      return d1.CompareTo(d2);
    }

    internal override JToken CloneToken()
    {
      return (JToken) new JValue(this);
    }

    public static JValue CreateComment(string value)
    {
      return new JValue((object) value, JTokenType.Comment);
    }

    public static JValue CreateString(string value)
    {
      return new JValue((object) value, JTokenType.String);
    }

    public static JValue CreateNull()
    {
      return new JValue((object) null, JTokenType.Null);
    }

    public static JValue CreateUndefined()
    {
      return new JValue((object) null, JTokenType.Undefined);
    }

    private static JTokenType GetValueType(JTokenType? current, object value)
    {
      if (value == null || value == DBNull.Value)
        return JTokenType.Null;
      if (value is string)
        return JValue.GetStringValueType(current);
      if (value is long || value is int || (value is short || value is sbyte) || (value is ulong || value is uint || (value is ushort || value is byte)) || value is Enum)
        return JTokenType.Integer;
      if (value is double || value is float || value is Decimal)
        return JTokenType.Float;
      if (value is DateTime)
        return JTokenType.Date;
      if (value is byte[])
        return JTokenType.Bytes;
      if (value is bool)
        return JTokenType.Boolean;
      if (value is Guid)
        return JTokenType.Guid;
      if ((object) (value as Uri) != null)
        return JTokenType.Uri;
      if (value is TimeSpan)
        return JTokenType.TimeSpan;
      throw new ArgumentException("Could not determine JSON object type for type {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) value.GetType()));
    }

    private static JTokenType GetStringValueType(JTokenType? current)
    {
      if (!current.HasValue)
        return JTokenType.String;
      switch (current.GetValueOrDefault())
      {
        case JTokenType.Comment:
        case JTokenType.String:
        case JTokenType.Raw:
          return current.GetValueOrDefault();
        default:
          return JTokenType.String;
      }
    }

    public override JTokenType Type
    {
      get
      {
        return this._valueType;
      }
    }

    public object Value
    {
      get
      {
        return this._value;
      }
      set
      {
        if (this._value?.GetType() != value?.GetType())
          this._valueType = JValue.GetValueType(new JTokenType?(this._valueType), value);
        this._value = value;
      }
    }

    public override void WriteTo(JsonWriter writer, params JsonConverter[] converters)
    {
      if (converters != null && converters.Length != 0 && this._value != null)
      {
        JsonConverter matchingConverter = JsonSerializer.GetMatchingConverter((IList<JsonConverter>) converters, this._value.GetType());
        if (matchingConverter != null && matchingConverter.CanWrite)
        {
          matchingConverter.WriteJson(writer, this._value, JsonSerializer.CreateDefault());
          return;
        }
      }
      switch (this._valueType)
      {
        case JTokenType.Comment:
          writer.WriteComment(this._value?.ToString());
          break;
        case JTokenType.Integer:
          if (this._value is int)
          {
            writer.WriteValue((int) this._value);
            break;
          }
          if (this._value is long)
          {
            writer.WriteValue((long) this._value);
            break;
          }
          if (this._value is ulong)
          {
            writer.WriteValue((ulong) this._value);
            break;
          }
          writer.WriteValue(Convert.ToInt64(this._value, (IFormatProvider) CultureInfo.InvariantCulture));
          break;
        case JTokenType.Float:
          if (this._value is Decimal)
          {
            writer.WriteValue((Decimal) this._value);
            break;
          }
          if (this._value is double)
          {
            writer.WriteValue((double) this._value);
            break;
          }
          if (this._value is float)
          {
            writer.WriteValue((float) this._value);
            break;
          }
          writer.WriteValue(Convert.ToDouble(this._value, (IFormatProvider) CultureInfo.InvariantCulture));
          break;
        case JTokenType.String:
          writer.WriteValue(this._value?.ToString());
          break;
        case JTokenType.Boolean:
          writer.WriteValue(Convert.ToBoolean(this._value, (IFormatProvider) CultureInfo.InvariantCulture));
          break;
        case JTokenType.Null:
          writer.WriteNull();
          break;
        case JTokenType.Undefined:
          writer.WriteUndefined();
          break;
        case JTokenType.Date:
          writer.WriteValue(Convert.ToDateTime(this._value, (IFormatProvider) CultureInfo.InvariantCulture));
          break;
        case JTokenType.Raw:
          writer.WriteRawValue(this._value?.ToString());
          break;
        case JTokenType.Bytes:
          writer.WriteValue((byte[]) this._value);
          break;
        case JTokenType.Guid:
          writer.WriteValue(this._value != null ? (Guid?) this._value : new Guid?());
          break;
        case JTokenType.Uri:
          writer.WriteValue((Uri) this._value);
          break;
        case JTokenType.TimeSpan:
          writer.WriteValue(this._value != null ? (TimeSpan?) this._value : new TimeSpan?());
          break;
        default:
          throw MiscellaneousUtils.CreateArgumentOutOfRangeException("Type", (object) this._valueType, "Unexpected token type.");
      }
    }

    internal override int GetDeepHashCode()
    {
      return ((int) this._valueType).GetHashCode() ^ (this._value != null ? this._value.GetHashCode() : 0);
    }

    private static bool ValuesEquals(JValue v1, JValue v2)
    {
      if (v1 == v2)
        return true;
      if (v1._valueType == v2._valueType)
        return JValue.Compare(v1._valueType, v1._value, v2._value) == 0;
      return false;
    }

    public bool Equals(JValue other)
    {
      if (other == null)
        return false;
      return JValue.ValuesEquals(this, other);
    }

    public override bool Equals(object obj)
    {
      return this.Equals(obj as JValue);
    }

    public override int GetHashCode()
    {
      if (this._value == null)
        return 0;
      return this._value.GetHashCode();
    }

    public override string ToString()
    {
      if (this._value == null)
        return string.Empty;
      return this._value.ToString();
    }

    public string ToString(string format)
    {
      return this.ToString(format, (IFormatProvider) CultureInfo.CurrentCulture);
    }

    public string ToString(IFormatProvider formatProvider)
    {
      return this.ToString((string) null, formatProvider);
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
      if (this._value == null)
        return string.Empty;
      IFormattable formattable = this._value as IFormattable;
      if (formattable != null)
        return formattable.ToString(format, formatProvider);
      return this._value.ToString();
    }

    int IComparable.CompareTo(object obj)
    {
      if (obj == null)
        return 1;
      JValue jvalue = obj as JValue;
      return JValue.Compare(this._valueType, this._value, jvalue != null ? jvalue.Value : obj);
    }

    public int CompareTo(JValue obj)
    {
      if (obj == null)
        return 1;
      return JValue.Compare(this._valueType, this._value, obj._value);
    }

    TypeCode IConvertible.GetTypeCode()
    {
      if (this._value == null)
        return TypeCode.Empty;
      IConvertible convertible = this._value as IConvertible;
      if (convertible == null)
        return TypeCode.Object;
      return convertible.GetTypeCode();
    }

    bool IConvertible.ToBoolean(IFormatProvider provider)
    {
      return (bool) ((JToken) this);
    }

    char IConvertible.ToChar(IFormatProvider provider)
    {
      return (char) ((JToken) this);
    }

    sbyte IConvertible.ToSByte(IFormatProvider provider)
    {
      return (sbyte) ((JToken) this);
    }

    byte IConvertible.ToByte(IFormatProvider provider)
    {
      return (byte) ((JToken) this);
    }

    short IConvertible.ToInt16(IFormatProvider provider)
    {
      return (short) ((JToken) this);
    }

    ushort IConvertible.ToUInt16(IFormatProvider provider)
    {
      return (ushort) ((JToken) this);
    }

    int IConvertible.ToInt32(IFormatProvider provider)
    {
      return (int) ((JToken) this);
    }

    uint IConvertible.ToUInt32(IFormatProvider provider)
    {
      return (uint) ((JToken) this);
    }

    long IConvertible.ToInt64(IFormatProvider provider)
    {
      return (long) ((JToken) this);
    }

    ulong IConvertible.ToUInt64(IFormatProvider provider)
    {
      return (ulong) ((JToken) this);
    }

    float IConvertible.ToSingle(IFormatProvider provider)
    {
      return (float) ((JToken) this);
    }

    double IConvertible.ToDouble(IFormatProvider provider)
    {
      return (double) ((JToken) this);
    }

    Decimal IConvertible.ToDecimal(IFormatProvider provider)
    {
      return (Decimal) ((JToken) this);
    }

    DateTime IConvertible.ToDateTime(IFormatProvider provider)
    {
      return (DateTime) ((JToken) this);
    }

    object IConvertible.ToType(System.Type conversionType, IFormatProvider provider)
    {
      return this.ToObject(conversionType);
    }
  }
}
