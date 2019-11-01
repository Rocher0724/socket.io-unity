using System;

namespace Socket.Newtonsoft.Json {
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property |
                  AttributeTargets.Field | AttributeTargets.Interface | AttributeTargets.Parameter, AllowMultiple =
    false)]
  public sealed class JsonConverterAttribute : Attribute {
    private readonly Type _converterType;

    public Type ConverterType {
      get { return this._converterType; }
    }

    public object[] ConverterParameters { get; }

    public JsonConverterAttribute(Type converterType) {
      if (converterType == null)
        throw new ArgumentNullException(nameof(converterType));
      this._converterType = converterType;
    }

    public JsonConverterAttribute(Type converterType, params object[] converterParameters)
      : this(converterType) {
      this.ConverterParameters = converterParameters;
    }
  }
}