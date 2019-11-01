using System;
using System.Globalization;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Converters {
  public class IsoDateTimeConverter : DateTimeConverterBase {
    private DateTimeStyles _dateTimeStyles = DateTimeStyles.RoundtripKind;
    private const string DefaultDateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK";
    private string _dateTimeFormat;
    private CultureInfo _culture;

    public DateTimeStyles DateTimeStyles {
      get { return this._dateTimeStyles; }
      set { this._dateTimeStyles = value; }
    }

    public string DateTimeFormat {
      get { return this._dateTimeFormat ?? string.Empty; }
      set { this._dateTimeFormat = string.IsNullOrEmpty(value) ? (string) null : value; }
    }

    public CultureInfo Culture {
      get { return this._culture ?? CultureInfo.CurrentCulture; }
      set { this._culture = value; }
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      if (!(value is DateTime))
        throw new JsonSerializationException(
          "Unexpected value when converting date. Expected DateTime or DateTimeOffset, got {0}.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) ReflectionUtils.GetObjectType(value)));
      DateTime dateTime = (DateTime) value;
      if ((this._dateTimeStyles & DateTimeStyles.AdjustToUniversal) == DateTimeStyles.AdjustToUniversal ||
          (this._dateTimeStyles & DateTimeStyles.AssumeUniversal) == DateTimeStyles.AssumeUniversal)
        dateTime = dateTime.ToUniversalTime();
      string str = dateTime.ToString(this._dateTimeFormat ?? "yyyy'-'MM'-'dd'T'HH':'mm':'ss.FFFFFFFK",
        (IFormatProvider) this.Culture);
      writer.WriteValue(str);
    }

    public override object ReadJson(
      JsonReader reader,
      Type objectType,
      object existingValue,
      JsonSerializer serializer) {
      bool flag = ReflectionUtils.IsNullableType(objectType);
      if (flag)
        Nullable.GetUnderlyingType(objectType);
      if (reader.TokenType == JsonToken.Null) {
        if (!ReflectionUtils.IsNullableType(objectType))
          throw JsonSerializationException.Create(reader,
            "Cannot convert null value to {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) objectType));
        return (object) null;
      }

      if (reader.TokenType == JsonToken.Date)
        return reader.Value;
      if (reader.TokenType != JsonToken.String)
        throw JsonSerializationException.Create(reader,
          "Unexpected token parsing date. Expected String, got {0}.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) reader.TokenType));
      string s = reader.Value.ToString();
      if (string.IsNullOrEmpty(s) & flag)
        return (object) null;
      if (!string.IsNullOrEmpty(this._dateTimeFormat))
        return (object) DateTime.ParseExact(s, this._dateTimeFormat, (IFormatProvider) this.Culture,
          this._dateTimeStyles);
      return (object) DateTime.Parse(s, (IFormatProvider) this.Culture, this._dateTimeStyles);
    }
  }
}