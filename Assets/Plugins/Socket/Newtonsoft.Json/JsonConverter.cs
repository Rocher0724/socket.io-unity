using System;

namespace Socket.Newtonsoft.Json {
  public abstract class JsonConverter {
    public abstract void WriteJson(JsonWriter writer, object value, JsonSerializer serializer);

    public abstract object ReadJson(
      JsonReader reader,
      Type objectType,
      object existingValue,
      JsonSerializer serializer);

    public abstract bool CanConvert(Type objectType);

    public virtual bool CanRead {
      get { return true; }
    }

    public virtual bool CanWrite {
      get { return true; }
    }
  }
}