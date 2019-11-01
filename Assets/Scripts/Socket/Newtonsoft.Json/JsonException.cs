using System;
using System.Runtime.Serialization;

namespace Socket.Newtonsoft.Json {
  [Serializable]
  public class JsonException : Exception {
    public JsonException() {
    }

    public JsonException(string message)
      : base(message) {
    }

    public JsonException(string message, Exception innerException)
      : base(message, innerException) {
    }

    public JsonException(SerializationInfo info, StreamingContext context)
      : base(info, context) {
    }

    internal static JsonException Create(
      IJsonLineInfo lineInfo,
      string path,
      string message) {
      message = JsonPosition.FormatMessage(lineInfo, path, message);
      return new JsonException(message);
    }
  }
}