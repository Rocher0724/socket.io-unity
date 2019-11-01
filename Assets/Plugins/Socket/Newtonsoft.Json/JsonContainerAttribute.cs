using System;
using Socket.Newtonsoft.Json.Serialization;

namespace Socket.Newtonsoft.Json {
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = false)]
  public abstract class JsonContainerAttribute : Attribute {
    internal bool? _isReference;
    internal bool? _itemIsReference;
    internal ReferenceLoopHandling? _itemReferenceLoopHandling;
    internal TypeNameHandling? _itemTypeNameHandling;
    private Type _namingStrategyType;
    private object[] _namingStrategyParameters;

    public string Id { get; set; }

    public string Title { get; set; }

    public string Description { get; set; }

    public Type ItemConverterType { get; set; }

    public object[] ItemConverterParameters { get; set; }

    public Type NamingStrategyType {
      get { return this._namingStrategyType; }
      set {
        this._namingStrategyType = value;
        this.NamingStrategyInstance = (NamingStrategy) null;
      }
    }

    public object[] NamingStrategyParameters {
      get { return this._namingStrategyParameters; }
      set {
        this._namingStrategyParameters = value;
        this.NamingStrategyInstance = (NamingStrategy) null;
      }
    }

    internal NamingStrategy NamingStrategyInstance { get; set; }

    public bool IsReference {
      get { return this._isReference ?? false; }
      set { this._isReference = new bool?(value); }
    }

    public bool ItemIsReference {
      get { return this._itemIsReference ?? false; }
      set { this._itemIsReference = new bool?(value); }
    }

    public ReferenceLoopHandling ItemReferenceLoopHandling {
      get { return this._itemReferenceLoopHandling ?? ReferenceLoopHandling.Error; }
      set { this._itemReferenceLoopHandling = new ReferenceLoopHandling?(value); }
    }

    public TypeNameHandling ItemTypeNameHandling {
      get { return this._itemTypeNameHandling ?? TypeNameHandling.None; }
      set { this._itemTypeNameHandling = new TypeNameHandling?(value); }
    }

    protected JsonContainerAttribute() {
    }

    protected JsonContainerAttribute(string id) {
      this.Id = id;
    }
  }
}