using System;
using System.Globalization;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Converters {
  public class JavaScriptDateTimeConverter : DateTimeConverterBase {
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      if (!(value is DateTime))
        throw new JsonSerializationException("Expected date object value.");
      long javaScriptTicks = DateTimeUtils.ConvertDateTimeToJavaScriptTicks(((DateTime) value).ToUniversalTime());
      writer.WriteStartConstructor("Date");
      writer.WriteValue(javaScriptTicks);
      writer.WriteEndConstructor();
    }

    public override object ReadJson(
      JsonReader reader,
      Type objectType,
      object existingValue,
      JsonSerializer serializer) {
      if (reader.TokenType == JsonToken.Null) {
        if (!ReflectionUtils.IsNullable(objectType))
          throw JsonSerializationException.Create(reader,
            "Cannot convert null value to {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) objectType));
        return (object) null;
      }

      if (reader.TokenType != JsonToken.StartConstructor ||
          !string.Equals(reader.Value.ToString(), "Date", StringComparison.Ordinal))
        throw JsonSerializationException.Create(reader,
          "Unexpected token or value when parsing date. Token: {0}, Value: {1}".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) reader.TokenType, reader.Value));
      reader.Read();
      if (reader.TokenType != JsonToken.Integer)
        throw JsonSerializationException.Create(reader,
          "Unexpected token parsing date. Expected Integer, got {0}.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) reader.TokenType));
      DateTime dateTime = DateTimeUtils.ConvertJavaScriptTicksToDateTime((long) reader.Value);
      reader.Read();
      if (reader.TokenType != JsonToken.EndConstructor)
        throw JsonSerializationException.Create(reader,
          "Unexpected token parsing date. Expected EndConstructor, got {0}.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) reader.TokenType));
      if (ReflectionUtils.IsNullableType(objectType))
        Nullable.GetUnderlyingType(objectType);
      return (object) dateTime;
    }
  }
}