using System;

namespace Socket.Newtonsoft.Json {
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
  public sealed class JsonDictionaryAttribute : JsonContainerAttribute {
    public JsonDictionaryAttribute() {
    }

    public JsonDictionaryAttribute(string id)
      : base(id) {
    }
  }
}