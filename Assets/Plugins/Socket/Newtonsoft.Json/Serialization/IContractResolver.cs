using System;

namespace Socket.Newtonsoft.Json.Serialization {
  public interface IContractResolver {
    JsonContract ResolveContract(Type type);
  }
}