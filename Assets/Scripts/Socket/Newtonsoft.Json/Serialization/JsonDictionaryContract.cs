using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Serialization {
  public class JsonDictionaryContract : JsonContainerContract {
    private readonly Type _genericCollectionDefinitionType;
    private Type _genericWrapperType;
    private ObjectConstructor<object> _genericWrapperCreator;
    private Func<object> _genericTemporaryDictionaryCreator;
    private readonly ConstructorInfo _parameterizedConstructor;
    private ObjectConstructor<object> _overrideCreator;
    private ObjectConstructor<object> _parameterizedCreator;

    public Func<string, string> DictionaryKeyResolver { get; set; }

    public Type DictionaryKeyType { get; }

    public Type DictionaryValueType { get; }

    internal JsonContract KeyContract { get; set; }

    internal bool ShouldCreateWrapper { get; }

    internal ObjectConstructor<object> ParameterizedCreator {
      get {
        if (this._parameterizedCreator == null)
          this._parameterizedCreator =
            JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(
              (MethodBase) this._parameterizedConstructor);
        return this._parameterizedCreator;
      }
    }

    public ObjectConstructor<object> OverrideCreator {
      get { return this._overrideCreator; }
      set { this._overrideCreator = value; }
    }

    public bool HasParameterizedCreator { get; set; }

    internal bool HasParameterizedCreatorInternal {
      get {
        if (!this.HasParameterizedCreator && this._parameterizedCreator == null)
          return this._parameterizedConstructor != null;
        return true;
      }
    }

    public JsonDictionaryContract(Type underlyingType)
      : base(underlyingType) {
      this.ContractType = JsonContractType.Dictionary;
      Type keyType;
      Type valueType;
      if (ReflectionUtils.ImplementsGenericDefinition(underlyingType, typeof(IDictionary<,>),
        out this._genericCollectionDefinitionType)) {
        keyType = this._genericCollectionDefinitionType.GetGenericArguments()[0];
        valueType = this._genericCollectionDefinitionType.GetGenericArguments()[1];
        if (ReflectionUtils.IsGenericDefinition(this.UnderlyingType, typeof(IDictionary<,>)))
          this.CreatedType = typeof(Dictionary<,>).MakeGenericType(keyType, valueType);
      } else {
        ReflectionUtils.GetDictionaryKeyValueTypes(this.UnderlyingType, out keyType, out valueType);
        if (this.UnderlyingType == typeof(IDictionary))
          this.CreatedType = typeof(Dictionary<object, object>);
      }

      if (keyType != null && valueType != null)
        this._parameterizedConstructor = CollectionUtils.ResolveEnumerableCollectionConstructor(this.CreatedType,
          typeof(KeyValuePair<,>).MakeGenericType(keyType, valueType),
          typeof(IDictionary<,>).MakeGenericType(keyType, valueType));
      this.ShouldCreateWrapper = !typeof(IDictionary).IsAssignableFrom(this.CreatedType);
      this.DictionaryKeyType = keyType;
      this.DictionaryValueType = valueType;
      Type implementingType;
      if (this.DictionaryValueType == null || !ReflectionUtils.IsNullableType(this.DictionaryValueType) ||
          !ReflectionUtils.InheritsGenericDefinition(this.CreatedType, typeof(Dictionary<,>), out implementingType))
        return;
      this.ShouldCreateWrapper = true;
    }

    internal IWrappedDictionary CreateWrapper(object dictionary) {
      if (this._genericWrapperCreator == null) {
        this._genericWrapperType =
          typeof(DictionaryWrapper<,>).MakeGenericType(this.DictionaryKeyType, this.DictionaryValueType);
        this._genericWrapperCreator = JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(
          (MethodBase) this._genericWrapperType.GetConstructor(new Type[1] {
            this._genericCollectionDefinitionType
          }));
      }

      return (IWrappedDictionary) this._genericWrapperCreator(new object[1] {
        dictionary
      });
    }

    internal IDictionary CreateTemporaryDictionary() {
      if (this._genericTemporaryDictionaryCreator == null)
        this._genericTemporaryDictionaryCreator =
          JsonTypeReflector.ReflectionDelegateFactory.CreateDefaultConstructor<object>(
            typeof(Dictionary<,>).MakeGenericType(this.DictionaryKeyType ?? typeof(object),
              this.DictionaryValueType ?? typeof(object)));
      return (IDictionary) this._genericTemporaryDictionaryCreator();
    }
  }
}