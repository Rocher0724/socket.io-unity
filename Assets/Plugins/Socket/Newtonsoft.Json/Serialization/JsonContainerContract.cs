using Socket.Newtonsoft.Json.Utilities;
using System;

namespace Socket.Newtonsoft.Json.Serialization {
  public class JsonContainerContract : JsonContract {
    private JsonContract _itemContract;
    private JsonContract _finalItemContract;

    internal JsonContract ItemContract {
      get { return this._itemContract; }
      set {
        this._itemContract = value;
        if (this._itemContract != null)
          this._finalItemContract =
            this._itemContract.UnderlyingType.IsSealed() ? this._itemContract : (JsonContract) null;
        else
          this._finalItemContract = (JsonContract) null;
      }
    }

    internal JsonContract FinalItemContract {
      get { return this._finalItemContract; }
    }

    public JsonConverter ItemConverter { get; set; }

    public bool? ItemIsReference { get; set; }

    public ReferenceLoopHandling? ItemReferenceLoopHandling { get; set; }

    public TypeNameHandling? ItemTypeNameHandling { get; set; }

    internal JsonContainerContract(Type underlyingType)
      : base(underlyingType) {
      JsonContainerAttribute cachedAttribute =
        JsonTypeReflector.GetCachedAttribute<JsonContainerAttribute>((object) underlyingType);
      if (cachedAttribute == null)
        return;
      if (cachedAttribute.ItemConverterType != null)
        this.ItemConverter = JsonTypeReflector.CreateJsonConverterInstance(cachedAttribute.ItemConverterType,
          cachedAttribute.ItemConverterParameters);
      this.ItemIsReference = cachedAttribute._itemIsReference;
      this.ItemReferenceLoopHandling = cachedAttribute._itemReferenceLoopHandling;
      this.ItemTypeNameHandling = cachedAttribute._itemTypeNameHandling;
    }
  }
}