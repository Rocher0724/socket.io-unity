using System;

namespace Socket.Newtonsoft.Json {
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Interface, AllowMultiple = false)]
  public sealed class JsonObjectAttribute : JsonContainerAttribute {
    private MemberSerialization _memberSerialization;
    internal Required? _itemRequired;

    public MemberSerialization MemberSerialization {
      get { return this._memberSerialization; }
      set { this._memberSerialization = value; }
    }

    public Required ItemRequired {
      get { return this._itemRequired ?? Required.Default; }
      set { this._itemRequired = new Required?(value); }
    }

    public JsonObjectAttribute() {
    }

    public JsonObjectAttribute(MemberSerialization memberSerialization) {
      this.MemberSerialization = memberSerialization;
    }

    public JsonObjectAttribute(string id)
      : base(id) {
    }
  }
}
