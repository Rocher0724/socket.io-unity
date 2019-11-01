using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Globalization;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Converters {
  public class BinaryConverter : JsonConverter {
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      if (value == null) {
        writer.WriteNull();
      } else {
        byte[] byteArray = this.GetByteArray(value);
        writer.WriteValue(byteArray);
      }
    }

    private byte[] GetByteArray(object value) {
      if (value is SqlBinary)
        return ((SqlBinary) value).Value;
      throw new JsonSerializationException(
        "Unexpected value type when writing binary: {0}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) value.GetType()));
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

      byte[] numArray;
      if (reader.TokenType == JsonToken.StartArray) {
        numArray = this.ReadByteArray(reader);
      } else {
        if (reader.TokenType != JsonToken.String)
          throw JsonSerializationException.Create(reader,
            "Unexpected token parsing binary. Expected String or StartArray, got {0}.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) reader.TokenType));
        numArray = Convert.FromBase64String(reader.Value.ToString());
      }

      if ((ReflectionUtils.IsNullableType(objectType) ? Nullable.GetUnderlyingType(objectType) : objectType) ==
          typeof(SqlBinary))
        return (object) new SqlBinary(numArray);
      throw JsonSerializationException.Create(reader,
        "Unexpected object type when writing binary: {0}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) objectType));
    }

    private byte[] ReadByteArray(JsonReader reader) {
      List<byte> byteList = new List<byte>();
      while (reader.Read()) {
        switch (reader.TokenType) {
          case JsonToken.Comment:
            continue;
          case JsonToken.Integer:
            byteList.Add(Convert.ToByte(reader.Value, (IFormatProvider) CultureInfo.InvariantCulture));
            continue;
          case JsonToken.EndArray:
            return byteList.ToArray();
          default:
            throw JsonSerializationException.Create(reader,
              "Unexpected token when reading bytes: {0}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                (object) reader.TokenType));
        }
      }

      throw JsonSerializationException.Create(reader, "Unexpected end when reading bytes.");
    }

    public override bool CanConvert(Type objectType) {
      return objectType == typeof(SqlBinary) || objectType == typeof(SqlBinary?);
    }
  }
}