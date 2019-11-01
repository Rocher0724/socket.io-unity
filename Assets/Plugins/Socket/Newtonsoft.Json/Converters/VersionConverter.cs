using System;
using System.Globalization;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Converters {
  public class VersionConverter : JsonConverter {
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      if (value == null) {
        writer.WriteNull();
      } else {
        if ((object) (value as Version) == null)
          throw new JsonSerializationException("Expected Version object value");
        writer.WriteValue(value.ToString());
      }
    }

    public override object ReadJson(
      JsonReader reader,
      Type objectType,
      object existingValue,
      JsonSerializer serializer) {
      if (reader.TokenType == JsonToken.Null)
        return (object) null;
      if (reader.TokenType != JsonToken.String)
        throw JsonSerializationException.Create(reader,
          "Unexpected token or value when parsing version. Token: {0}, Value: {1}".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) reader.TokenType, reader.Value));
      try {
        return (object) new Version((string) reader.Value);
      } catch (Exception ex) {
        throw JsonSerializationException.Create(reader,
          "Error parsing version string: {0}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, reader.Value),
          ex);
      }
    }

    public override bool CanConvert(Type objectType) {
      return objectType == typeof(Version);
    }
  }
}