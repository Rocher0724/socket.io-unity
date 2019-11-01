using System;

namespace Socket.Newtonsoft.Json.Serialization {
  public interface ISerializationBinder {
    Type BindToType(string assemblyName, string typeName);

    void BindToName(Type serializedType, out string assemblyName, out string typeName);
  }
}