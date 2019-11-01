using System;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Socket.Newtonsoft.Json.Bson;
using Socket.Newtonsoft.Json.Serialization;

namespace Socket.Newtonsoft.Json.Converters {
  public class RegexConverter : JsonConverter {
    private const string PatternName = "Pattern";
    private const string OptionsName = "Options";

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      Regex regex = (Regex) value;
      BsonWriter writer1 = writer as BsonWriter;
      if (writer1 != null)
        this.WriteBson(writer1, regex);
      else
        this.WriteJson(writer, regex, serializer);
    }

    private bool HasFlag(RegexOptions options, RegexOptions flag) {
      return (options & flag) == flag;
    }

    private void WriteBson(BsonWriter writer, Regex regex) {
      string str = (string) null;
      if (this.HasFlag(regex.Options, RegexOptions.IgnoreCase))
        str += "i";
      if (this.HasFlag(regex.Options, RegexOptions.Multiline))
        str += "m";
      if (this.HasFlag(regex.Options, RegexOptions.Singleline))
        str += "s";
      string options = str + "u";
      if (this.HasFlag(regex.Options, RegexOptions.ExplicitCapture))
        options += "x";
      writer.WriteRegex(regex.ToString(), options);
    }

    private void WriteJson(JsonWriter writer, Regex regex, JsonSerializer serializer) {
      DefaultContractResolver contractResolver = serializer.ContractResolver as DefaultContractResolver;
      writer.WriteStartObject();
      writer.WritePropertyName(contractResolver != null
        ? contractResolver.GetResolvedPropertyName("Pattern")
        : "Pattern");
      writer.WriteValue(regex.ToString());
      writer.WritePropertyName(contractResolver != null
        ? contractResolver.GetResolvedPropertyName("Options")
        : "Options");
      serializer.Serialize(writer, (object) regex.Options);
      writer.WriteEndObject();
    }

    public override object ReadJson(
      JsonReader reader,
      Type objectType,
      object existingValue,
      JsonSerializer serializer) {
      switch (reader.TokenType) {
        case JsonToken.StartObject:
          return (object) this.ReadRegexObject(reader, serializer);
        case JsonToken.String:
          return this.ReadRegexString(reader);
        case JsonToken.Null:
          return (object) null;
        default:
          throw JsonSerializationException.Create(reader, "Unexpected token when reading Regex.");
      }
    }

    private object ReadRegexString(JsonReader reader) {
      string str1 = (string) reader.Value;
      int num = str1.LastIndexOf('/');
      string pattern = str1.Substring(1, num - 1);
      string str2 = str1.Substring(num + 1);
      RegexOptions options = RegexOptions.None;
      foreach (char ch in str2) {
        switch (ch) {
          case 'i':
            options |= RegexOptions.IgnoreCase;
            break;
          case 'm':
            options |= RegexOptions.Multiline;
            break;
          case 's':
            options |= RegexOptions.Singleline;
            break;
          case 'x':
            options |= RegexOptions.ExplicitCapture;
            break;
        }
      }

      return (object) new Regex(pattern, options);
    }

    private Regex ReadRegexObject(JsonReader reader, JsonSerializer serializer) {
      string str = (string) null;
      RegexOptions? nullable1 = new RegexOptions?();
      while (reader.Read()) {
        switch (reader.TokenType) {
          case JsonToken.PropertyName:
            string a = reader.Value.ToString();
            if (!reader.Read())
              throw JsonSerializationException.Create(reader, "Unexpected end when reading Regex.");
            if (string.Equals(a, "Pattern", StringComparison.OrdinalIgnoreCase)) {
              str = (string) reader.Value;
              continue;
            }

            if (string.Equals(a, "Options", StringComparison.OrdinalIgnoreCase)) {
              nullable1 = new RegexOptions?(serializer.Deserialize<RegexOptions>(reader));
              continue;
            }

            reader.Skip();
            continue;
          case JsonToken.EndObject:
            if (str == null)
              throw JsonSerializationException.Create(reader, "Error deserializing Regex. No pattern found.");
            string pattern = str;
            RegexOptions? nullable2 = nullable1;
            int num = nullable2.HasValue ? (int) nullable2.GetValueOrDefault() : 0;
            return new Regex(pattern, (RegexOptions) num);
          default:
            continue;
        }
      }

      throw JsonSerializationException.Create(reader, "Unexpected end when reading Regex.");
    }

    public override bool CanConvert(Type objectType) {
      if (objectType.Name == "Regex")
        return this.IsRegex(objectType);
      return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool IsRegex(Type objectType) {
      return objectType == typeof(Regex);
    }
  }
}