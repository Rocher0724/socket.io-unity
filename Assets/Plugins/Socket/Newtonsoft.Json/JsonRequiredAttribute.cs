using System;

namespace Socket.Newtonsoft.Json {
  [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
  public sealed class JsonRequiredAttribute : Attribute
  {
  }
}
