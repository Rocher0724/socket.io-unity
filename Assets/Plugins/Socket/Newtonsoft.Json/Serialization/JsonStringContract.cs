using System;

namespace Socket.Newtonsoft.Json.Serialization {
  public class JsonStringContract : JsonPrimitiveContract {
    public JsonStringContract(Type underlyingType)
      : base(underlyingType) {
      this.ContractType = JsonContractType.String;
    }
  }
}