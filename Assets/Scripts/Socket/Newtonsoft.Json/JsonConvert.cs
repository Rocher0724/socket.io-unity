using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Xml;
using Socket.Newtonsoft.Json.Converters;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json {
  public static class JsonConvert {
    public static readonly string True = "true";
    public static readonly string False = "false";
    public static readonly string Null = "null";
    public static readonly string Undefined = "undefined";
    public static readonly string PositiveInfinity = "Infinity";
    public static readonly string NegativeInfinity = "-Infinity";
    public static readonly string NaN = nameof(NaN);

    public static Func<JsonSerializerSettings> DefaultSettings { get; set; }

    public static string ToString(DateTime value) {
      return JsonConvert.ToString(value, DateFormatHandling.IsoDateFormat, DateTimeZoneHandling.RoundtripKind);
    }

    public static string ToString(
      DateTime value,
      DateFormatHandling format,
      DateTimeZoneHandling timeZoneHandling) {
      DateTime dateTime = DateTimeUtils.EnsureDateTime(value, timeZoneHandling);
      using (StringWriter stringWriter = StringUtils.CreateStringWriter(64)) {
        stringWriter.Write('"');
        DateTimeUtils.WriteDateTimeString((TextWriter) stringWriter, dateTime, format, (string) null,
          CultureInfo.InvariantCulture);
        stringWriter.Write('"');
        return stringWriter.ToString();
      }
    }

    public static string ToString(bool value) {
      if (!value)
        return JsonConvert.False;
      return JsonConvert.True;
    }

    public static string ToString(char value) {
      return JsonConvert.ToString(char.ToString(value));
    }

    public static string ToString(Enum value) {
      return value.ToString("D");
    }

    public static string ToString(int value) {
      return value.ToString((string) null, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    public static string ToString(short value) {
      return value.ToString((string) null, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    [CLSCompliant(false)]
    public static string ToString(ushort value) {
      return value.ToString((string) null, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    [CLSCompliant(false)]
    public static string ToString(uint value) {
      return value.ToString((string) null, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    public static string ToString(long value) {
      return value.ToString((string) null, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    [CLSCompliant(false)]
    public static string ToString(ulong value) {
      return value.ToString((string) null, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    public static string ToString(float value) {
      return JsonConvert.EnsureDecimalPlace((double) value,
        value.ToString("R", (IFormatProvider) CultureInfo.InvariantCulture));
    }

    internal static string ToString(
      float value,
      FloatFormatHandling floatFormatHandling,
      char quoteChar,
      bool nullable) {
      return JsonConvert.EnsureFloatFormat((double) value,
        JsonConvert.EnsureDecimalPlace((double) value,
          value.ToString("R", (IFormatProvider) CultureInfo.InvariantCulture)), floatFormatHandling, quoteChar,
        nullable);
    }

    private static string EnsureFloatFormat(
      double value,
      string text,
      FloatFormatHandling floatFormatHandling,
      char quoteChar,
      bool nullable) {
      if (floatFormatHandling == FloatFormatHandling.Symbol || !double.IsInfinity(value) && !double.IsNaN(value))
        return text;
      if (floatFormatHandling != FloatFormatHandling.DefaultValue)
        return quoteChar.ToString() + text + quoteChar.ToString();
      if (nullable)
        return JsonConvert.Null;
      return "0.0";
    }

    public static string ToString(double value) {
      return JsonConvert.EnsureDecimalPlace(value, value.ToString("R", (IFormatProvider) CultureInfo.InvariantCulture));
    }

    internal static string ToString(
      double value,
      FloatFormatHandling floatFormatHandling,
      char quoteChar,
      bool nullable) {
      return JsonConvert.EnsureFloatFormat(value,
        JsonConvert.EnsureDecimalPlace(value, value.ToString("R", (IFormatProvider) CultureInfo.InvariantCulture)),
        floatFormatHandling, quoteChar, nullable);
    }

    private static string EnsureDecimalPlace(double value, string text) {
      if (double.IsNaN(value) || double.IsInfinity(value) || (text.IndexOf('.') != -1 || text.IndexOf('E') != -1) ||
          text.IndexOf('e') != -1)
        return text;
      return text + ".0";
    }

    private static string EnsureDecimalPlace(string text) {
      if (text.IndexOf('.') != -1)
        return text;
      return text + ".0";
    }

    public static string ToString(byte value) {
      return value.ToString((string) null, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    [CLSCompliant(false)]
    public static string ToString(sbyte value) {
      return value.ToString((string) null, (IFormatProvider) CultureInfo.InvariantCulture);
    }

    public static string ToString(Decimal value) {
      return JsonConvert.EnsureDecimalPlace(value.ToString((string) null,
        (IFormatProvider) CultureInfo.InvariantCulture));
    }

    public static string ToString(Guid value) {
      return JsonConvert.ToString(value, '"');
    }

    internal static string ToString(Guid value, char quoteChar) {
      string str1 = value.ToString("D", (IFormatProvider) CultureInfo.InvariantCulture);
      string str2 = quoteChar.ToString((IFormatProvider) CultureInfo.InvariantCulture);
      return str2 + str1 + str2;
    }

    public static string ToString(TimeSpan value) {
      return JsonConvert.ToString(value, '"');
    }

    internal static string ToString(TimeSpan value, char quoteChar) {
      return JsonConvert.ToString(value.ToString(), quoteChar);
    }

    public static string ToString(Uri value) {
      if (value == (Uri) null)
        return JsonConvert.Null;
      return JsonConvert.ToString(value, '"');
    }

    internal static string ToString(Uri value, char quoteChar) {
      return JsonConvert.ToString(value.OriginalString, quoteChar);
    }

    public static string ToString(string value) {
      return JsonConvert.ToString(value, '"');
    }

    public static string ToString(string value, char delimiter) {
      return JsonConvert.ToString(value, delimiter, StringEscapeHandling.Default);
    }

    public static string ToString(
      string value,
      char delimiter,
      StringEscapeHandling stringEscapeHandling) {
      if (delimiter != '"' && delimiter != '\'')
        throw new ArgumentException("Delimiter must be a single or double quote.", nameof(delimiter));
      return JavaScriptUtils.ToEscapedJavaScriptString(value, delimiter, true, stringEscapeHandling);
    }

    public static string ToString(object value) {
      if (value == null)
        return JsonConvert.Null;
      switch (ConvertUtils.GetTypeCode(value.GetType())) {
        case PrimitiveTypeCode.Char:
          return JsonConvert.ToString((char) value);
        case PrimitiveTypeCode.Boolean:
          return JsonConvert.ToString((bool) value);
        case PrimitiveTypeCode.SByte:
          return JsonConvert.ToString((sbyte) value);
        case PrimitiveTypeCode.Int16:
          return JsonConvert.ToString((short) value);
        case PrimitiveTypeCode.UInt16:
          return JsonConvert.ToString((ushort) value);
        case PrimitiveTypeCode.Int32:
          return JsonConvert.ToString((int) value);
        case PrimitiveTypeCode.Byte:
          return JsonConvert.ToString((byte) value);
        case PrimitiveTypeCode.UInt32:
          return JsonConvert.ToString((uint) value);
        case PrimitiveTypeCode.Int64:
          return JsonConvert.ToString((long) value);
        case PrimitiveTypeCode.UInt64:
          return JsonConvert.ToString((ulong) value);
        case PrimitiveTypeCode.Single:
          return JsonConvert.ToString((float) value);
        case PrimitiveTypeCode.Double:
          return JsonConvert.ToString((double) value);
        case PrimitiveTypeCode.DateTime:
          return JsonConvert.ToString((DateTime) value);
        case PrimitiveTypeCode.Decimal:
          return JsonConvert.ToString((Decimal) value);
        case PrimitiveTypeCode.Guid:
          return JsonConvert.ToString((Guid) value);
        case PrimitiveTypeCode.TimeSpan:
          return JsonConvert.ToString((TimeSpan) value);
        case PrimitiveTypeCode.Uri:
          return JsonConvert.ToString((Uri) value);
        case PrimitiveTypeCode.String:
          return JsonConvert.ToString((string) value);
        case PrimitiveTypeCode.DBNull:
          return JsonConvert.Null;
        default:
          throw new ArgumentException(
            "Unsupported type: {0}. Use the JsonSerializer class to get the object's JSON representation.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) value.GetType()));
      }
    }

    public static string SerializeObject(object value) {
      return JsonConvert.SerializeObject(value, (Type) null, (JsonSerializerSettings) null);
    }

    public static string SerializeObject(object value, Formatting formatting) {
      return JsonConvert.SerializeObject(value, formatting, (JsonSerializerSettings) null);
    }

    public static string SerializeObject(object value, params JsonConverter[] converters) {
      JsonSerializerSettings serializerSettings;
      if (converters == null || converters.Length == 0)
        serializerSettings = (JsonSerializerSettings) null;
      else
        serializerSettings = new JsonSerializerSettings() {
          Converters = (IList<JsonConverter>) converters
        };
      JsonSerializerSettings settings = serializerSettings;
      return JsonConvert.SerializeObject(value, (Type) null, settings);
    }

    public static string SerializeObject(
      object value,
      Formatting formatting,
      params JsonConverter[] converters) {
      JsonSerializerSettings serializerSettings;
      if (converters == null || converters.Length == 0)
        serializerSettings = (JsonSerializerSettings) null;
      else
        serializerSettings = new JsonSerializerSettings() {
          Converters = (IList<JsonConverter>) converters
        };
      JsonSerializerSettings settings = serializerSettings;
      return JsonConvert.SerializeObject(value, (Type) null, formatting, settings);
    }

    public static string SerializeObject(object value, JsonSerializerSettings settings) {
      return JsonConvert.SerializeObject(value, (Type) null, settings);
    }

    public static string SerializeObject(object value, Type type, JsonSerializerSettings settings) {
      JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(settings);
      return JsonConvert.SerializeObjectInternal(value, type, jsonSerializer);
    }

    public static string SerializeObject(
      object value,
      Formatting formatting,
      JsonSerializerSettings settings) {
      return JsonConvert.SerializeObject(value, (Type) null, formatting, settings);
    }

    public static string SerializeObject(
      object value,
      Type type,
      Formatting formatting,
      JsonSerializerSettings settings) {
      JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(settings);
      jsonSerializer.Formatting = formatting;
      return JsonConvert.SerializeObjectInternal(value, type, jsonSerializer);
    }

    private static string SerializeObjectInternal(
      object value,
      Type type,
      JsonSerializer jsonSerializer) {
      StringWriter stringWriter =
        new StringWriter(new StringBuilder(256), (IFormatProvider) CultureInfo.InvariantCulture);
      using (JsonTextWriter jsonTextWriter = new JsonTextWriter((TextWriter) stringWriter)) {
        jsonTextWriter.Formatting = jsonSerializer.Formatting;
        jsonSerializer.Serialize((JsonWriter) jsonTextWriter, value, type);
      }

      return stringWriter.ToString();
    }

    public static object DeserializeObject(string value) {
      return JsonConvert.DeserializeObject(value, (Type) null, (JsonSerializerSettings) null);
    }

    public static object DeserializeObject(string value, JsonSerializerSettings settings) {
      return JsonConvert.DeserializeObject(value, (Type) null, settings);
    }

    public static object DeserializeObject(string value, Type type) {
      return JsonConvert.DeserializeObject(value, type, (JsonSerializerSettings) null);
    }

    public static T DeserializeObject<T>(string value) {
      return JsonConvert.DeserializeObject<T>(value, (JsonSerializerSettings) null);
    }

    public static T DeserializeAnonymousType<T>(string value, T anonymousTypeObject) {
      return JsonConvert.DeserializeObject<T>(value);
    }

    public static T DeserializeAnonymousType<T>(
      string value,
      T anonymousTypeObject,
      JsonSerializerSettings settings) {
      return JsonConvert.DeserializeObject<T>(value, settings);
    }

    public static T DeserializeObject<T>(string value, params JsonConverter[] converters) {
      return (T) JsonConvert.DeserializeObject(value, typeof(T), converters);
    }

    public static T DeserializeObject<T>(string value, JsonSerializerSettings settings) {
      return (T) JsonConvert.DeserializeObject(value, typeof(T), settings);
    }

    public static object DeserializeObject(
      string value,
      Type type,
      params JsonConverter[] converters) {
      JsonSerializerSettings serializerSettings;
      if (converters == null || converters.Length == 0)
        serializerSettings = (JsonSerializerSettings) null;
      else
        serializerSettings = new JsonSerializerSettings() {
          Converters = (IList<JsonConverter>) converters
        };
      JsonSerializerSettings settings = serializerSettings;
      return JsonConvert.DeserializeObject(value, type, settings);
    }

    public static object DeserializeObject(
      string value,
      Type type,
      JsonSerializerSettings settings) {
      ValidationUtils.ArgumentNotNull((object) value, nameof(value));
      JsonSerializer jsonSerializer = JsonSerializer.CreateDefault(settings);
      if (!jsonSerializer.IsCheckAdditionalContentSet())
        jsonSerializer.CheckAdditionalContent = true;
      using (JsonTextReader jsonTextReader = new JsonTextReader((TextReader) new StringReader(value)))
        return jsonSerializer.Deserialize((JsonReader) jsonTextReader, type);
    }

    public static void PopulateObject(string value, object target) {
      JsonConvert.PopulateObject(value, target, (JsonSerializerSettings) null);
    }

    public static void PopulateObject(string value, object target, JsonSerializerSettings settings) {
      using (JsonReader reader = (JsonReader) new JsonTextReader((TextReader) new StringReader(value))) {
        JsonSerializer.CreateDefault(settings).Populate(reader, target);
        if (settings == null || !settings.CheckAdditionalContent)
          return;
        while (reader.Read()) {
          if (reader.TokenType != JsonToken.Comment)
            throw JsonSerializationException.Create(reader,
              "Additional text found in JSON string after finishing deserializing object.");
        }
      }
    }

    public static string SerializeXmlNode(XmlNode node) {
      return JsonConvert.SerializeXmlNode(node, Formatting.None);
    }

    public static string SerializeXmlNode(XmlNode node, Formatting formatting) {
      XmlNodeConverter xmlNodeConverter = new XmlNodeConverter();
      return JsonConvert.SerializeObject((object) node, formatting, (JsonConverter) xmlNodeConverter);
    }

    public static string SerializeXmlNode(XmlNode node, Formatting formatting, bool omitRootObject) {
      XmlNodeConverter xmlNodeConverter = new XmlNodeConverter() {
        OmitRootObject = omitRootObject
      };
      return JsonConvert.SerializeObject((object) node, formatting, (JsonConverter) xmlNodeConverter);
    }

    public static XmlDocument DeserializeXmlNode(string value) {
      return JsonConvert.DeserializeXmlNode(value, (string) null);
    }

    public static XmlDocument DeserializeXmlNode(
      string value,
      string deserializeRootElementName) {
      return JsonConvert.DeserializeXmlNode(value, deserializeRootElementName, false);
    }

    public static XmlDocument DeserializeXmlNode(
      string value,
      string deserializeRootElementName,
      bool writeArrayAttribute) {
      return (XmlDocument) JsonConvert.DeserializeObject(value, typeof(XmlDocument),
        (JsonConverter) new XmlNodeConverter() {
          DeserializeRootElementName = deserializeRootElementName,
          WriteArrayAttribute = writeArrayAttribute
        });
    }
  }
}