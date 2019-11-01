using System;

namespace Socket.Newtonsoft.Json.Serialization {
  public class JsonISerializableContract : JsonContainerContract
  {
    public ObjectConstructor<object> ISerializableCreator { get; set; }

    public JsonISerializableContract(Type underlyingType)
      : base(underlyingType)
    {
      this.ContractType = JsonContractType.Serializable;
    }
  }
}
