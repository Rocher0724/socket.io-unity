using System;
using System.Globalization;
using Socket.Newtonsoft.Json.Bson;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Converters {
  [Obsolete(
    "BSON reading and writing has been moved to its own package. See https://www.nuget.org/packages/Newtonsoft.Json.Bson for more details.")]
  public class BsonObjectIdConverter : JsonConverter {
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      BsonObjectId bsonObjectId = (BsonObjectId) value;
      BsonWriter bsonWriter = writer as BsonWriter;
      if (bsonWriter != null)
        bsonWriter.WriteObjectId(bsonObjectId.Value);
      else
        writer.WriteValue(bsonObjectId.Value);
    }

    public override object ReadJson(
      JsonReader reader,
      Type objectType,
      object existingValue,
      JsonSerializer serializer) {
      if (reader.TokenType != JsonToken.Bytes)
        throw new JsonSerializationException(
          "Expected Bytes but got {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) reader.TokenType));
      return (object) new BsonObjectId((byte[]) reader.Value);
    }

    public override bool CanConvert(Type objectType) {
      return objectType == typeof(BsonObjectId);
    }
  }
}