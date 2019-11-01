using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Socket.Newtonsoft.Json.Serialization {
  public delegate IEnumerable<KeyValuePair<object, object>> ExtensionDataGetter(object o);
  public delegate void ExtensionDataSetter(object o, string key, object value);
  public delegate object ObjectConstructor<T>(params object[] args);
  public delegate void SerializationCallback(object o, StreamingContext context);
  public delegate void SerializationErrorCallback(
    object o,
    StreamingContext context,
    ErrorContext errorContext);
  
}