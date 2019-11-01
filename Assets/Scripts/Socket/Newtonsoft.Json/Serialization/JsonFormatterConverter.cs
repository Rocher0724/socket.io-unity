using System;
using System.Globalization;
using System.Runtime.Serialization;
using Socket.Newtonsoft.Json.Linq;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Serialization {
  internal class JsonFormatterConverter : IFormatterConverter {
    private readonly JsonSerializerInternalReader _reader;
    private readonly JsonISerializableContract _contract;
    private readonly JsonProperty _member;

    public JsonFormatterConverter(
      JsonSerializerInternalReader reader,
      JsonISerializableContract contract,
      JsonProperty member) {
      ValidationUtils.ArgumentNotNull((object) reader, nameof(reader));
      ValidationUtils.ArgumentNotNull((object) contract, nameof(contract));
      this._reader = reader;
      this._contract = contract;
      this._member = member;
    }

    private T GetTokenValue<T>(object value) {
      ValidationUtils.ArgumentNotNull(value, nameof(value));
      return (T) System.Convert.ChangeType(((JValue) value).Value, typeof(T), (IFormatProvider) CultureInfo.InvariantCulture);
    }

    public object Convert(object value, Type type) {
      ValidationUtils.ArgumentNotNull(value, nameof(value));
      JToken token = value as JToken;
      if (token == null)
        throw new ArgumentException("Value is not a JToken.", nameof(value));
      return this._reader.CreateISerializableItem(token, type, this._contract, this._member);
    }

    public object Convert(object value, TypeCode typeCode) {
      ValidationUtils.ArgumentNotNull(value, nameof(value));
      if (value is JValue)
        value = ((JValue) value).Value;
      return System.Convert.ChangeType(value, typeCode, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    public bool ToBoolean(object value) {
      return this.GetTokenValue<bool>(value);
    }

    public byte ToByte(object value) {
      return this.GetTokenValue<byte>(value);
    }

    public char ToChar(object value) {
      return this.GetTokenValue<char>(value);
    }

    public DateTime ToDateTime(object value) {
      return this.GetTokenValue<DateTime>(value);
    }

    public Decimal ToDecimal(object value) {
      return this.GetTokenValue<Decimal>(value);
    }

    public double ToDouble(object value) {
      return this.GetTokenValue<double>(value);
    }

    public short ToInt16(object value) {
      return this.GetTokenValue<short>(value);
    }

    public int ToInt32(object value) {
      return this.GetTokenValue<int>(value);
    }

    public long ToInt64(object value) {
      return this.GetTokenValue<long>(value);
    }

    public sbyte ToSByte(object value) {
      return this.GetTokenValue<sbyte>(value);
    }

    public float ToSingle(object value) {
      return this.GetTokenValue<float>(value);
    }

    public string ToString(object value) {
      return this.GetTokenValue<string>(value);
    }

    public ushort ToUInt16(object value) {
      return this.GetTokenValue<ushort>(value);
    }

    public uint ToUInt32(object value) {
      return this.GetTokenValue<uint>(value);
    }

    public ulong ToUInt64(object value) {
      return this.GetTokenValue<ulong>(value);
    }
  }
}