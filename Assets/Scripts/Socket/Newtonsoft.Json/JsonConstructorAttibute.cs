using System;

namespace Socket.Newtonsoft.Json {
  [AttributeUsage(AttributeTargets.Constructor, AllowMultiple = false)]
  public sealed class JsonConstructorAttribute : Attribute {
  }
}