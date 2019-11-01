using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Socket.Newtonsoft.Json.Converters;
using Socket.Newtonsoft.Json.Linq;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Serialization {
  public class DefaultContractResolver : IContractResolver {
    private static readonly IContractResolver _instance = (IContractResolver) new DefaultContractResolver();

    private static readonly JsonConverter[] BuiltInConverters = new JsonConverter[7] {
      (JsonConverter) new XmlNodeConverter(),
      (JsonConverter) new BinaryConverter(),
      (JsonConverter) new DataSetConverter(),
      (JsonConverter) new DataTableConverter(),
      (JsonConverter) new KeyValuePairConverter(),
      (JsonConverter) new BsonObjectIdConverter(),
      (JsonConverter) new RegexConverter()
    };

    private readonly object _typeContractCacheLock = new object();
    private readonly PropertyNameTable _nameTable = new PropertyNameTable();
    private Dictionary<Type, JsonContract> _contractCache;

    internal static IContractResolver Instance {
      get { return DefaultContractResolver._instance; }
    }

    public bool DynamicCodeGeneration {
      get { return JsonTypeReflector.DynamicCodeGeneration; }
    }

    [Obsolete(
      "DefaultMembersSearchFlags is obsolete. To modify the members serialized inherit from DefaultContractResolver and override the GetSerializableMembers method instead.")]
    public BindingFlags DefaultMembersSearchFlags { get; set; }

    public bool SerializeCompilerGeneratedMembers { get; set; }

    public bool IgnoreSerializableInterface { get; set; }

    public bool IgnoreSerializableAttribute { get; set; }

    public NamingStrategy NamingStrategy { get; set; }

    public DefaultContractResolver() {
      this.IgnoreSerializableAttribute = true;
      this.DefaultMembersSearchFlags = BindingFlags.Instance | BindingFlags.Public;
    }

    public virtual JsonContract ResolveContract(Type type) {
      if (type == null)
        throw new ArgumentNullException(nameof(type));
      Dictionary<Type, JsonContract> contractCache1 = this._contractCache;
      JsonContract contract;
      if (contractCache1 == null || !contractCache1.TryGetValue(type, out contract)) {
        contract = this.CreateContract(type);
        lock (this._typeContractCacheLock) {
          Dictionary<Type, JsonContract> contractCache2 = this._contractCache;
          Dictionary<Type, JsonContract> dictionary = contractCache2 != null
            ? new Dictionary<Type, JsonContract>((IDictionary<Type, JsonContract>) contractCache2)
            : new Dictionary<Type, JsonContract>();
          dictionary[type] = contract;
          this._contractCache = dictionary;
        }
      }

      return contract;
    }

    protected virtual List<MemberInfo> GetSerializableMembers(Type objectType) {
      bool serializableAttribute = this.IgnoreSerializableAttribute;
      MemberSerialization memberSerialization =
        JsonTypeReflector.GetObjectMemberSerialization(objectType, serializableAttribute);
      IEnumerable<MemberInfo> memberInfos = ReflectionUtils
        .GetFieldsAndProperties(objectType,
          BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
        .Where<MemberInfo>((Func<MemberInfo, bool>) (m => !ReflectionUtils.IsIndexedProperty(m)));
      List<MemberInfo> memberInfoList = new List<MemberInfo>();
      if (memberSerialization != MemberSerialization.Fields) {
        List<MemberInfo> list = ReflectionUtils.GetFieldsAndProperties(objectType, this.DefaultMembersSearchFlags)
          .Where<MemberInfo>((Func<MemberInfo, bool>) (m => !ReflectionUtils.IsIndexedProperty(m)))
          .ToList<MemberInfo>();
        foreach (MemberInfo memberInfo in memberInfos) {
          if (this.SerializeCompilerGeneratedMembers ||
              !memberInfo.IsDefined(typeof(CompilerGeneratedAttribute), true)) {
            if (list.Contains(memberInfo))
              memberInfoList.Add(memberInfo);
            else if (JsonTypeReflector.GetAttribute<JsonPropertyAttribute>((object) memberInfo) != null)
              memberInfoList.Add(memberInfo);
            else if (JsonTypeReflector.GetAttribute<JsonRequiredAttribute>((object) memberInfo) != null)
              memberInfoList.Add(memberInfo);
            else if (memberSerialization == MemberSerialization.Fields && memberInfo.MemberType() == MemberTypes.Field)
              memberInfoList.Add(memberInfo);
          }
        }
      } else {
        foreach (MemberInfo memberInfo in memberInfos) {
          FieldInfo fieldInfo = memberInfo as FieldInfo;
          if (fieldInfo != null && !fieldInfo.IsStatic)
            memberInfoList.Add(memberInfo);
        }
      }

      return memberInfoList;
    }

    protected virtual JsonObjectContract CreateObjectContract(Type objectType) {
      JsonObjectContract contract = new JsonObjectContract(objectType);
      this.InitializeContract((JsonContract) contract);
      bool serializableAttribute = this.IgnoreSerializableAttribute;
      contract.MemberSerialization =
        JsonTypeReflector.GetObjectMemberSerialization(contract.NonNullableUnderlyingType, serializableAttribute);
      contract.Properties.AddRange<JsonProperty>(
        (IEnumerable<JsonProperty>) this.CreateProperties(contract.NonNullableUnderlyingType,
          contract.MemberSerialization));
      Func<string, string> func = (Func<string, string>) null;
      JsonObjectAttribute cachedAttribute =
        JsonTypeReflector.GetCachedAttribute<JsonObjectAttribute>((object) contract.NonNullableUnderlyingType);
      if (cachedAttribute != null) {
        contract.ItemRequired = cachedAttribute._itemRequired;
        if (cachedAttribute.NamingStrategyType != null) {
          NamingStrategy namingStrategy =
            JsonTypeReflector.GetContainerNamingStrategy((JsonContainerAttribute) cachedAttribute);
          func = (Func<string, string>) (s => namingStrategy.GetDictionaryKey(s));
        }
      }

      if (func == null)
        func = new Func<string, string>(this.ResolveExtensionDataName);
      contract.ExtensionDataNameResolver = func;
      if (contract.IsInstantiable) {
        ConstructorInfo attributeConstructor = this.GetAttributeConstructor(contract.NonNullableUnderlyingType);
        if (attributeConstructor != null) {
          contract.OverrideCreator =
            JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(
              (MethodBase) attributeConstructor);
          contract.CreatorParameters.AddRange<JsonProperty>(
            (IEnumerable<JsonProperty>) this.CreateConstructorParameters(attributeConstructor, contract.Properties));
        } else if (contract.MemberSerialization == MemberSerialization.Fields) {
          if (JsonTypeReflector.FullyTrusted)
            contract.DefaultCreator = new Func<object>(contract.GetUninitializedObject);
        } else if (contract.DefaultCreator == null || contract.DefaultCreatorNonPublic) {
          ConstructorInfo parameterizedConstructor =
            this.GetParameterizedConstructor(contract.NonNullableUnderlyingType);
          if (parameterizedConstructor != null) {
            contract.ParameterizedCreator =
              JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(
                (MethodBase) parameterizedConstructor);
            contract.CreatorParameters.AddRange<JsonProperty>(
              (IEnumerable<JsonProperty>) this.CreateConstructorParameters(parameterizedConstructor,
                contract.Properties));
          }
        } else if (contract.NonNullableUnderlyingType.IsValueType()) {
          ConstructorInfo immutableConstructor =
            this.GetImmutableConstructor(contract.NonNullableUnderlyingType, contract.Properties);
          if (immutableConstructor != null) {
            contract.OverrideCreator =
              JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(
                (MethodBase) immutableConstructor);
            contract.CreatorParameters.AddRange<JsonProperty>(
              (IEnumerable<JsonProperty>) this.CreateConstructorParameters(immutableConstructor, contract.Properties));
          }
        }
      }

      MemberInfo dataMemberForType = this.GetExtensionDataMemberForType(contract.NonNullableUnderlyingType);
      if (dataMemberForType != null)
        DefaultContractResolver.SetExtensionDataDelegates(contract, dataMemberForType);
      return contract;
    }

    private MemberInfo GetExtensionDataMemberForType(Type type) {
      return this.GetClassHierarchyForType(type).SelectMany<Type, MemberInfo>(
        (Func<Type, IEnumerable<MemberInfo>>) (baseType => {
          List<MemberInfo> initial = new List<MemberInfo>();
          initial.AddRange<MemberInfo>((IEnumerable<MemberInfo>) baseType.GetProperties(
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
          initial.AddRange<MemberInfo>((IEnumerable<MemberInfo>) baseType.GetFields(
            BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
          return (IEnumerable<MemberInfo>) initial;
        })).LastOrDefault<MemberInfo>((Func<MemberInfo, bool>) (m => {
        switch (m.MemberType()) {
          case MemberTypes.Field:
          case MemberTypes.Property:
            if (!m.IsDefined(typeof(JsonExtensionDataAttribute), false))
              return false;
            if (!ReflectionUtils.CanReadMemberValue(m, true))
              throw new JsonException(
                "Invalid extension data attribute on '{0}'. Member '{1}' must have a getter.".FormatWith(
                  (IFormatProvider) CultureInfo.InvariantCulture,
                  (object) DefaultContractResolver.GetClrTypeFullName(m.DeclaringType), (object) m.Name));
            Type implementingType;
            if (ReflectionUtils.ImplementsGenericDefinition(ReflectionUtils.GetMemberUnderlyingType(m),
              typeof(IDictionary<,>), out implementingType)) {
              Type genericArgument1 = implementingType.GetGenericArguments()[0];
              Type genericArgument2 = implementingType.GetGenericArguments()[1];
              Type c = typeof(string);
              if (genericArgument1.IsAssignableFrom(c) && genericArgument2.IsAssignableFrom(typeof(JToken)))
                return true;
            }

            throw new JsonException(
              "Invalid extension data attribute on '{0}'. Member '{1}' type must implement IDictionary<string, JToken>."
                .FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                  (object) DefaultContractResolver.GetClrTypeFullName(m.DeclaringType), (object) m.Name));
          default:
            return false;
        }
      }));
    }

    private static void SetExtensionDataDelegates(JsonObjectContract contract, MemberInfo member) {
      JsonExtensionDataAttribute attribute = ReflectionUtils.GetAttribute<JsonExtensionDataAttribute>((object) member);
      if (attribute == null)
        return;
      Type memberUnderlyingType = ReflectionUtils.GetMemberUnderlyingType(member);
      Type implementingType;
      ReflectionUtils.ImplementsGenericDefinition(memberUnderlyingType, typeof(IDictionary<,>), out implementingType);
      Type genericArgument1 = implementingType.GetGenericArguments()[0];
      Type genericArgument2 = implementingType.GetGenericArguments()[1];
      Type type;
      if (ReflectionUtils.IsGenericDefinition(memberUnderlyingType, typeof(IDictionary<,>)))
        type = typeof(Dictionary<,>).MakeGenericType(genericArgument1, genericArgument2);
      else
        type = memberUnderlyingType;
      Func<object, object> getExtensionDataDictionary =
        JsonTypeReflector.ReflectionDelegateFactory.CreateGet<object>(member);
      if (attribute.ReadData) {
        Action<object, object> setExtensionDataDictionary = ReflectionUtils.CanSetMemberValue(member, true, false)
          ? JsonTypeReflector.ReflectionDelegateFactory.CreateSet<object>(member)
          : (Action<object, object>) null;
        Func<object> createExtensionDataDictionary =
          JsonTypeReflector.ReflectionDelegateFactory.CreateDefaultConstructor<object>(type);
        MethodCall<object, object> setExtensionDataDictionaryValue =
          JsonTypeReflector.ReflectionDelegateFactory.CreateMethodCall<object>(
            (MethodBase) memberUnderlyingType.GetMethod("Add", new Type[2] {
              genericArgument1,
              genericArgument2
            }));
        ExtensionDataSetter extensionDataSetter = (ExtensionDataSetter) ((o, key, value) => {
          object target = getExtensionDataDictionary(o);
          if (target == null) {
            if (setExtensionDataDictionary == null)
              throw new JsonSerializationException(
                "Cannot set value onto extension data member '{0}'. The extension data collection is null and it cannot be set."
                  .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) member.Name));
            target = createExtensionDataDictionary();
            setExtensionDataDictionary(o, target);
          }

          object obj = setExtensionDataDictionaryValue(target, new object[2] {
            (object) key,
            value
          });
        });
        contract.ExtensionDataSetter = extensionDataSetter;
      }

      if (attribute.WriteData) {
        ObjectConstructor<object> createEnumerableWrapper =
          JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(
            (MethodBase) ((IEnumerable<ConstructorInfo>) typeof(DefaultContractResolver.EnumerableDictionaryWrapper<,>)
              .MakeGenericType(genericArgument1, genericArgument2).GetConstructors()).First<ConstructorInfo>());
        ExtensionDataGetter extensionDataGetter = (ExtensionDataGetter) (o => {
          object obj = getExtensionDataDictionary(o);
          if (obj == null)
            return (IEnumerable<KeyValuePair<object, object>>) null;
          return (IEnumerable<KeyValuePair<object, object>>) createEnumerableWrapper(new object[1] {
            obj
          });
        });
        contract.ExtensionDataGetter = extensionDataGetter;
      }

      contract.ExtensionDataValueType = genericArgument2;
    }

    private ConstructorInfo GetAttributeConstructor(Type objectType) {
      IEnumerator<ConstructorInfo> enumerator =
        ((IEnumerable<ConstructorInfo>) objectType.GetConstructors(
          BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
        .Where<ConstructorInfo>(
          (Func<ConstructorInfo, bool>) (c => c.IsDefined(typeof(JsonConstructorAttribute), true))).GetEnumerator();
      if (enumerator.MoveNext()) {
        ConstructorInfo current = enumerator.Current;
        if (enumerator.MoveNext())
          throw new JsonException("Multiple constructors with the JsonConstructorAttribute.");
        return current;
      }

      if (objectType != typeof(Version))
        return (ConstructorInfo) null;
      return objectType.GetConstructor(new Type[4] {
        typeof(int),
        typeof(int),
        typeof(int),
        typeof(int)
      });
    }

    private ConstructorInfo GetImmutableConstructor(
      Type objectType,
      JsonPropertyCollection memberProperties) {
      IEnumerator<ConstructorInfo> enumerator =
        ((IEnumerable<ConstructorInfo>) objectType.GetConstructors()).GetEnumerator();
      if (enumerator.MoveNext()) {
        ConstructorInfo current = enumerator.Current;
        if (!enumerator.MoveNext()) {
          ParameterInfo[] parameters = current.GetParameters();
          if (parameters.Length != 0) {
            foreach (ParameterInfo parameterInfo in parameters) {
              JsonProperty jsonProperty =
                this.MatchProperty(memberProperties, parameterInfo.Name, parameterInfo.ParameterType);
              if (jsonProperty == null || jsonProperty.Writable)
                return (ConstructorInfo) null;
            }

            return current;
          }
        }
      }

      return (ConstructorInfo) null;
    }

    private ConstructorInfo GetParameterizedConstructor(Type objectType) {
      ConstructorInfo[] constructors = objectType.GetConstructors(BindingFlags.Instance | BindingFlags.Public);
      if (constructors.Length == 1)
        return constructors[0];
      return (ConstructorInfo) null;
    }

    protected virtual IList<JsonProperty> CreateConstructorParameters(
      ConstructorInfo constructor,
      JsonPropertyCollection memberProperties) {
      ParameterInfo[] parameters = constructor.GetParameters();
      JsonPropertyCollection propertyCollection = new JsonPropertyCollection(constructor.DeclaringType);
      foreach (ParameterInfo parameterInfo in parameters) {
        JsonProperty matchingMemberProperty =
          this.MatchProperty(memberProperties, parameterInfo.Name, parameterInfo.ParameterType);
        if (matchingMemberProperty != null || parameterInfo.Name != null) {
          JsonProperty constructorParameter =
            this.CreatePropertyFromConstructorParameter(matchingMemberProperty, parameterInfo);
          if (constructorParameter != null)
            propertyCollection.AddProperty(constructorParameter);
        }
      }

      return (IList<JsonProperty>) propertyCollection;
    }

    private JsonProperty MatchProperty(
      JsonPropertyCollection properties,
      string name,
      Type type) {
      if (name == null)
        return (JsonProperty) null;
      JsonProperty closestMatchProperty = properties.GetClosestMatchProperty(name);
      if (closestMatchProperty == null || closestMatchProperty.PropertyType != type)
        return (JsonProperty) null;
      return closestMatchProperty;
    }

    protected virtual JsonProperty CreatePropertyFromConstructorParameter(
      JsonProperty matchingMemberProperty,
      ParameterInfo parameterInfo) {
      JsonProperty property = new JsonProperty();
      property.PropertyType = parameterInfo.ParameterType;
      property.AttributeProvider = (IAttributeProvider) new ReflectionAttributeProvider((object) parameterInfo);
      bool allowNonPublicAccess;
      this.SetPropertySettingsFromAttributes(property, (object) parameterInfo, parameterInfo.Name,
        parameterInfo.Member.DeclaringType, MemberSerialization.OptOut, out allowNonPublicAccess);
      property.Readable = false;
      property.Writable = true;
      if (matchingMemberProperty != null) {
        property.PropertyName = property.PropertyName != parameterInfo.Name
          ? property.PropertyName
          : matchingMemberProperty.PropertyName;
        property.Converter = property.Converter ?? matchingMemberProperty.Converter;
        property.MemberConverter = property.MemberConverter ?? matchingMemberProperty.MemberConverter;
        if (!property._hasExplicitDefaultValue && matchingMemberProperty._hasExplicitDefaultValue)
          property.DefaultValue = matchingMemberProperty.DefaultValue;
        JsonProperty jsonProperty1 = property;
        Required? required = property._required;
        Required? nullable1 = required.HasValue ? required : matchingMemberProperty._required;
        jsonProperty1._required = nullable1;
        JsonProperty jsonProperty2 = property;
        bool? isReference = property.IsReference;
        bool? nullable2 = isReference.HasValue ? isReference : matchingMemberProperty.IsReference;
        jsonProperty2.IsReference = nullable2;
        JsonProperty jsonProperty3 = property;
        NullValueHandling? nullValueHandling = property.NullValueHandling;
        NullValueHandling? nullable3 =
          nullValueHandling.HasValue ? nullValueHandling : matchingMemberProperty.NullValueHandling;
        jsonProperty3.NullValueHandling = nullable3;
        JsonProperty jsonProperty4 = property;
        DefaultValueHandling? defaultValueHandling = property.DefaultValueHandling;
        DefaultValueHandling? nullable4 = defaultValueHandling.HasValue
          ? defaultValueHandling
          : matchingMemberProperty.DefaultValueHandling;
        jsonProperty4.DefaultValueHandling = nullable4;
        JsonProperty jsonProperty5 = property;
        ReferenceLoopHandling? referenceLoopHandling = property.ReferenceLoopHandling;
        ReferenceLoopHandling? nullable5 = referenceLoopHandling.HasValue
          ? referenceLoopHandling
          : matchingMemberProperty.ReferenceLoopHandling;
        jsonProperty5.ReferenceLoopHandling = nullable5;
        JsonProperty jsonProperty6 = property;
        ObjectCreationHandling? creationHandling = property.ObjectCreationHandling;
        ObjectCreationHandling? nullable6 = creationHandling.HasValue
          ? creationHandling
          : matchingMemberProperty.ObjectCreationHandling;
        jsonProperty6.ObjectCreationHandling = nullable6;
        JsonProperty jsonProperty7 = property;
        TypeNameHandling? typeNameHandling = property.TypeNameHandling;
        TypeNameHandling? nullable7 =
          typeNameHandling.HasValue ? typeNameHandling : matchingMemberProperty.TypeNameHandling;
        jsonProperty7.TypeNameHandling = nullable7;
      }

      return property;
    }

    protected virtual JsonConverter ResolveContractConverter(Type objectType) {
      return JsonTypeReflector.GetJsonConverter((object) objectType);
    }

    private Func<object> GetDefaultCreator(Type createdType) {
      return JsonTypeReflector.ReflectionDelegateFactory.CreateDefaultConstructor<object>(createdType);
    }

    private void InitializeContract(JsonContract contract) {
      JsonContainerAttribute cachedAttribute =
        JsonTypeReflector.GetCachedAttribute<JsonContainerAttribute>((object) contract.NonNullableUnderlyingType);
      if (cachedAttribute != null)
        contract.IsReference = cachedAttribute._isReference;
      contract.Converter = this.ResolveContractConverter(contract.NonNullableUnderlyingType);
      contract.InternalConverter = JsonSerializer.GetMatchingConverter(
        (IList<JsonConverter>) DefaultContractResolver.BuiltInConverters, contract.NonNullableUnderlyingType);
      if (contract.IsInstantiable && (ReflectionUtils.HasDefaultConstructor(contract.CreatedType, true) ||
                                      contract.CreatedType.IsValueType())) {
        contract.DefaultCreator = this.GetDefaultCreator(contract.CreatedType);
        contract.DefaultCreatorNonPublic = !contract.CreatedType.IsValueType() &&
                                           ReflectionUtils.GetDefaultConstructor(contract.CreatedType) == null;
      }

      this.ResolveCallbackMethods(contract, contract.NonNullableUnderlyingType);
    }

    private void ResolveCallbackMethods(JsonContract contract, Type t) {
      List<SerializationCallback> onSerializing;
      List<SerializationCallback> onSerialized;
      List<SerializationCallback> onDeserializing;
      List<SerializationCallback> onDeserialized;
      List<SerializationErrorCallback> onError;
      this.GetCallbackMethodsForType(t, out onSerializing, out onSerialized, out onDeserializing, out onDeserialized,
        out onError);
      if (onSerializing != null)
        contract.OnSerializingCallbacks.AddRange<SerializationCallback>(
          (IEnumerable<SerializationCallback>) onSerializing);
      if (onSerialized != null)
        contract.OnSerializedCallbacks.AddRange<SerializationCallback>(
          (IEnumerable<SerializationCallback>) onSerialized);
      if (onDeserializing != null)
        contract.OnDeserializingCallbacks.AddRange<SerializationCallback>(
          (IEnumerable<SerializationCallback>) onDeserializing);
      if (onDeserialized != null)
        contract.OnDeserializedCallbacks.AddRange<SerializationCallback>(
          (IEnumerable<SerializationCallback>) onDeserialized);
      if (onError == null)
        return;
      contract.OnErrorCallbacks.AddRange<SerializationErrorCallback>((IEnumerable<SerializationErrorCallback>) onError);
    }

    private void GetCallbackMethodsForType(
      Type type,
      out List<SerializationCallback> onSerializing,
      out List<SerializationCallback> onSerialized,
      out List<SerializationCallback> onDeserializing,
      out List<SerializationCallback> onDeserialized,
      out List<SerializationErrorCallback> onError) {
      onSerializing = (List<SerializationCallback>) null;
      onSerialized = (List<SerializationCallback>) null;
      onDeserializing = (List<SerializationCallback>) null;
      onDeserialized = (List<SerializationCallback>) null;
      onError = (List<SerializationErrorCallback>) null;
      foreach (Type t in this.GetClassHierarchyForType(type)) {
        MethodInfo currentCallback1 = (MethodInfo) null;
        MethodInfo currentCallback2 = (MethodInfo) null;
        MethodInfo currentCallback3 = (MethodInfo) null;
        MethodInfo currentCallback4 = (MethodInfo) null;
        MethodInfo currentCallback5 = (MethodInfo) null;
        bool flag1 = DefaultContractResolver.ShouldSkipSerializing(t);
        bool flag2 = DefaultContractResolver.ShouldSkipDeserialized(t);
        foreach (MethodInfo method in t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance |
                                                   BindingFlags.Public | BindingFlags.NonPublic)) {
          if (!method.ContainsGenericParameters) {
            Type prevAttributeType = (Type) null;
            ParameterInfo[] parameters = method.GetParameters();
            if (!flag1 && DefaultContractResolver.IsValidCallback(method, parameters, typeof(OnSerializingAttribute),
                  currentCallback1, ref prevAttributeType)) {
              onSerializing = onSerializing ?? new List<SerializationCallback>();
              onSerializing.Add(JsonContract.CreateSerializationCallback(method));
              currentCallback1 = method;
            }

            if (DefaultContractResolver.IsValidCallback(method, parameters, typeof(OnSerializedAttribute),
              currentCallback2, ref prevAttributeType)) {
              onSerialized = onSerialized ?? new List<SerializationCallback>();
              onSerialized.Add(JsonContract.CreateSerializationCallback(method));
              currentCallback2 = method;
            }

            if (DefaultContractResolver.IsValidCallback(method, parameters, typeof(OnDeserializingAttribute),
              currentCallback3, ref prevAttributeType)) {
              onDeserializing = onDeserializing ?? new List<SerializationCallback>();
              onDeserializing.Add(JsonContract.CreateSerializationCallback(method));
              currentCallback3 = method;
            }

            if (!flag2 && DefaultContractResolver.IsValidCallback(method, parameters, typeof(OnDeserializedAttribute),
                  currentCallback4, ref prevAttributeType)) {
              onDeserialized = onDeserialized ?? new List<SerializationCallback>();
              onDeserialized.Add(JsonContract.CreateSerializationCallback(method));
              currentCallback4 = method;
            }

            if (DefaultContractResolver.IsValidCallback(method, parameters, typeof(OnErrorAttribute), currentCallback5,
              ref prevAttributeType)) {
              onError = onError ?? new List<SerializationErrorCallback>();
              onError.Add(JsonContract.CreateSerializationErrorCallback(method));
              currentCallback5 = method;
            }
          }
        }
      }
    }

    private static bool ShouldSkipDeserialized(Type t) {
      return false;
    }

    private static bool ShouldSkipSerializing(Type t) {
      return false;
    }

    private List<Type> GetClassHierarchyForType(Type type) {
      List<Type> typeList = new List<Type>();
      for (Type type1 = type; type1 != null && type1 != typeof(object); type1 = type1.BaseType())
        typeList.Add(type1);
      typeList.Reverse();
      return typeList;
    }

    protected virtual JsonDictionaryContract CreateDictionaryContract(
      Type objectType) {
      JsonDictionaryContract dictionaryContract = new JsonDictionaryContract(objectType);
      this.InitializeContract((JsonContract) dictionaryContract);
      JsonContainerAttribute attribute = JsonTypeReflector.GetAttribute<JsonContainerAttribute>((object) objectType);
      if (attribute?.NamingStrategyType != null) {
        NamingStrategy namingStrategy = JsonTypeReflector.GetContainerNamingStrategy(attribute);
        dictionaryContract.DictionaryKeyResolver = (Func<string, string>) (s => namingStrategy.GetDictionaryKey(s));
      } else
        dictionaryContract.DictionaryKeyResolver = new Func<string, string>(this.ResolveDictionaryKey);

      ConstructorInfo attributeConstructor = this.GetAttributeConstructor(dictionaryContract.NonNullableUnderlyingType);
      if (attributeConstructor != null) {
        ParameterInfo[] parameters = attributeConstructor.GetParameters();
        Type type1;
        if (dictionaryContract.DictionaryKeyType == null || dictionaryContract.DictionaryValueType == null)
          type1 = typeof(IDictionary);
        else
          type1 = typeof(IEnumerable<>).MakeGenericType(
            typeof(KeyValuePair<,>).MakeGenericType(dictionaryContract.DictionaryKeyType,
              dictionaryContract.DictionaryValueType));
        Type type2 = type1;
        if (parameters.Length == 0) {
          dictionaryContract.HasParameterizedCreator = false;
        } else {
          if (parameters.Length != 1 || !type2.IsAssignableFrom(parameters[0].ParameterType))
            throw new JsonException(
              "Constructor for '{0}' must have no parameters or a single parameter that implements '{1}'.".FormatWith(
                (IFormatProvider) CultureInfo.InvariantCulture, (object) dictionaryContract.UnderlyingType,
                (object) type2));
          dictionaryContract.HasParameterizedCreator = true;
        }

        dictionaryContract.OverrideCreator =
          JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor((MethodBase) attributeConstructor);
      }

      return dictionaryContract;
    }

    protected virtual JsonArrayContract CreateArrayContract(Type objectType) {
      JsonArrayContract jsonArrayContract = new JsonArrayContract(objectType);
      this.InitializeContract((JsonContract) jsonArrayContract);
      ConstructorInfo attributeConstructor = this.GetAttributeConstructor(jsonArrayContract.NonNullableUnderlyingType);
      if (attributeConstructor != null) {
        ParameterInfo[] parameters = attributeConstructor.GetParameters();
        Type type1;
        if (jsonArrayContract.CollectionItemType == null)
          type1 = typeof(IEnumerable);
        else
          type1 = typeof(IEnumerable<>).MakeGenericType(jsonArrayContract.CollectionItemType);
        Type type2 = type1;
        if (parameters.Length == 0) {
          jsonArrayContract.HasParameterizedCreator = false;
        } else {
          if (parameters.Length != 1 || !type2.IsAssignableFrom(parameters[0].ParameterType))
            throw new JsonException(
              "Constructor for '{0}' must have no parameters or a single parameter that implements '{1}'.".FormatWith(
                (IFormatProvider) CultureInfo.InvariantCulture, (object) jsonArrayContract.UnderlyingType,
                (object) type2));
          jsonArrayContract.HasParameterizedCreator = true;
        }

        jsonArrayContract.OverrideCreator =
          JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor((MethodBase) attributeConstructor);
      }

      return jsonArrayContract;
    }

    protected virtual JsonPrimitiveContract CreatePrimitiveContract(
      Type objectType) {
      JsonPrimitiveContract primitiveContract = new JsonPrimitiveContract(objectType);
      this.InitializeContract((JsonContract) primitiveContract);
      return primitiveContract;
    }

    protected virtual JsonLinqContract CreateLinqContract(Type objectType) {
      JsonLinqContract jsonLinqContract = new JsonLinqContract(objectType);
      this.InitializeContract((JsonContract) jsonLinqContract);
      return jsonLinqContract;
    }

    protected virtual JsonISerializableContract CreateISerializableContract(
      Type objectType) {
      JsonISerializableContract iserializableContract = new JsonISerializableContract(objectType);
      this.InitializeContract((JsonContract) iserializableContract);
      ConstructorInfo constructor = iserializableContract.NonNullableUnderlyingType.GetConstructor(
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, (Binder) null, new Type[2] {
          typeof(SerializationInfo),
          typeof(StreamingContext)
        }, (ParameterModifier[]) null);
      if (constructor != null) {
        ObjectConstructor<object> parameterizedConstructor =
          JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor((MethodBase) constructor);
        iserializableContract.ISerializableCreator = parameterizedConstructor;
      }

      return iserializableContract;
    }

    protected virtual JsonStringContract CreateStringContract(Type objectType) {
      JsonStringContract jsonStringContract = new JsonStringContract(objectType);
      this.InitializeContract((JsonContract) jsonStringContract);
      return jsonStringContract;
    }

    protected virtual JsonContract CreateContract(Type objectType) {
      if (DefaultContractResolver.IsJsonPrimitiveType(objectType))
        return (JsonContract) this.CreatePrimitiveContract(objectType);
      Type type = ReflectionUtils.EnsureNotNullableType(objectType);
      JsonContainerAttribute cachedAttribute =
        JsonTypeReflector.GetCachedAttribute<JsonContainerAttribute>((object) type);
      if (cachedAttribute is JsonObjectAttribute)
        return (JsonContract) this.CreateObjectContract(objectType);
      if (cachedAttribute is JsonArrayAttribute)
        return (JsonContract) this.CreateArrayContract(objectType);
      if (cachedAttribute is JsonDictionaryAttribute)
        return (JsonContract) this.CreateDictionaryContract(objectType);
      if (type == typeof(JToken) || type.IsSubclassOf(typeof(JToken)))
        return (JsonContract) this.CreateLinqContract(objectType);
      if (CollectionUtils.IsDictionaryType(type))
        return (JsonContract) this.CreateDictionaryContract(objectType);
      if (typeof(IEnumerable).IsAssignableFrom(type))
        return (JsonContract) this.CreateArrayContract(objectType);
      if (DefaultContractResolver.CanConvertToString(type))
        return (JsonContract) this.CreateStringContract(objectType);
      if (!this.IgnoreSerializableInterface && typeof(ISerializable).IsAssignableFrom(type))
        return (JsonContract) this.CreateISerializableContract(objectType);
      if (DefaultContractResolver.IsIConvertible(type))
        return (JsonContract) this.CreatePrimitiveContract(type);
      return (JsonContract) this.CreateObjectContract(objectType);
    }

    internal static bool IsJsonPrimitiveType(Type t) {
      PrimitiveTypeCode typeCode = ConvertUtils.GetTypeCode(t);
      if (typeCode != PrimitiveTypeCode.Empty)
        return typeCode != PrimitiveTypeCode.Object;
      return false;
    }

    internal static bool IsIConvertible(Type t) {
      if (typeof(IConvertible).IsAssignableFrom(t) || ReflectionUtils.IsNullableType(t) &&
          typeof(IConvertible).IsAssignableFrom(Nullable.GetUnderlyingType(t)))
        return !typeof(JToken).IsAssignableFrom(t);
      return false;
    }

    internal static bool CanConvertToString(Type type) {
      TypeConverter typeConverter;
      return JsonTypeReflector.CanTypeDescriptorConvertString(type, out typeConverter) || type == typeof(Type) ||
             type.IsSubclassOf(typeof(Type));
    }

    private static bool IsValidCallback(
      MethodInfo method,
      ParameterInfo[] parameters,
      Type attributeType,
      MethodInfo currentCallback,
      ref Type prevAttributeType) {
      if (!method.IsDefined(attributeType, false))
        return false;
      if (currentCallback != null)
        throw new JsonException("Invalid attribute. Both '{0}' and '{1}' in type '{2}' have '{3}'.".FormatWith(
          (IFormatProvider) CultureInfo.InvariantCulture, (object) method, (object) currentCallback,
          (object) DefaultContractResolver.GetClrTypeFullName(method.DeclaringType), (object) attributeType));
      if (prevAttributeType != null)
        throw new JsonException("Invalid Callback. Method '{3}' in type '{2}' has both '{0}' and '{1}'.".FormatWith(
          (IFormatProvider) CultureInfo.InvariantCulture, (object) prevAttributeType, (object) attributeType,
          (object) DefaultContractResolver.GetClrTypeFullName(method.DeclaringType), (object) method));
      if (method.IsVirtual)
        throw new JsonException("Virtual Method '{0}' of type '{1}' cannot be marked with '{2}' attribute.".FormatWith(
          (IFormatProvider) CultureInfo.InvariantCulture, (object) method,
          (object) DefaultContractResolver.GetClrTypeFullName(method.DeclaringType), (object) attributeType));
      if (method.ReturnType != typeof(void))
        throw new JsonException("Serialization Callback '{1}' in type '{0}' must return void.".FormatWith(
          (IFormatProvider) CultureInfo.InvariantCulture,
          (object) DefaultContractResolver.GetClrTypeFullName(method.DeclaringType), (object) method));
      if (attributeType == typeof(OnErrorAttribute)) {
        if (parameters == null || parameters.Length != 2 || (parameters[0].ParameterType != typeof(StreamingContext) ||
                                                             parameters[1].ParameterType != typeof(ErrorContext)))
          throw new JsonException(
            "Serialization Error Callback '{1}' in type '{0}' must have two parameters of type '{2}' and '{3}'."
              .FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                (object) DefaultContractResolver.GetClrTypeFullName(method.DeclaringType), (object) method,
                (object) typeof(StreamingContext), (object) typeof(ErrorContext)));
      } else if (parameters == null || parameters.Length != 1 ||
                 parameters[0].ParameterType != typeof(StreamingContext))
        throw new JsonException(
          "Serialization Callback '{1}' in type '{0}' must have a single parameter of type '{2}'.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture,
            (object) DefaultContractResolver.GetClrTypeFullName(method.DeclaringType), (object) method,
            (object) typeof(StreamingContext)));

      prevAttributeType = attributeType;
      return true;
    }

    internal static string GetClrTypeFullName(Type type) {
      if (type.IsGenericTypeDefinition() || !type.ContainsGenericParameters())
        return type.FullName;
      return "{0}.{1}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) type.Namespace,
        (object) type.Name);
    }

    protected virtual IList<JsonProperty> CreateProperties(
      Type type,
      MemberSerialization memberSerialization) {
      List<MemberInfo> serializableMembers = this.GetSerializableMembers(type);
      if (serializableMembers == null)
        throw new JsonSerializationException("Null collection of serializable members returned.");
      PropertyNameTable nameTable = this.GetNameTable();
      JsonPropertyCollection source = new JsonPropertyCollection(type);
      foreach (MemberInfo member in serializableMembers) {
        JsonProperty property = this.CreateProperty(member, memberSerialization);
        if (property != null) {
          lock (nameTable)
            property.PropertyName = nameTable.Add(property.PropertyName);
          source.AddProperty(property);
        }
      }

      return (IList<JsonProperty>) source.OrderBy<JsonProperty, int>((Func<JsonProperty, int>) (p => p.Order ?? -1))
        .ToList<JsonProperty>();
    }

    internal virtual PropertyNameTable GetNameTable() {
      return this._nameTable;
    }

    protected virtual IValueProvider CreateMemberValueProvider(MemberInfo member) {
      return !this.DynamicCodeGeneration
        ? (IValueProvider) new ReflectionValueProvider(member)
        : (IValueProvider) new DynamicValueProvider(member);
    }

    protected virtual JsonProperty CreateProperty(
      MemberInfo member,
      MemberSerialization memberSerialization) {
      JsonProperty property = new JsonProperty();
      property.PropertyType = ReflectionUtils.GetMemberUnderlyingType(member);
      property.DeclaringType = member.DeclaringType;
      property.ValueProvider = this.CreateMemberValueProvider(member);
      property.AttributeProvider = (IAttributeProvider) new ReflectionAttributeProvider((object) member);
      bool allowNonPublicAccess;
      this.SetPropertySettingsFromAttributes(property, (object) member, member.Name, member.DeclaringType,
        memberSerialization, out allowNonPublicAccess);
      if (memberSerialization != MemberSerialization.Fields) {
        property.Readable = ReflectionUtils.CanReadMemberValue(member, allowNonPublicAccess);
        property.Writable =
          ReflectionUtils.CanSetMemberValue(member, allowNonPublicAccess, property.HasMemberAttribute);
      } else {
        property.Readable = true;
        property.Writable = true;
      }

      property.ShouldSerialize = this.CreateShouldSerializeTest(member);
      this.SetIsSpecifiedActions(property, member, allowNonPublicAccess);
      return property;
    }

    private void SetPropertySettingsFromAttributes(
      JsonProperty property,
      object attributeProvider,
      string name,
      Type declaringType,
      MemberSerialization memberSerialization,
      out bool allowNonPublicAccess) {
      JsonPropertyAttribute attribute1 = JsonTypeReflector.GetAttribute<JsonPropertyAttribute>(attributeProvider);
      JsonRequiredAttribute attribute2 = JsonTypeReflector.GetAttribute<JsonRequiredAttribute>(attributeProvider);
      string str;
      bool hasSpecifiedName;
      if (attribute1 != null && attribute1.PropertyName != null) {
        str = attribute1.PropertyName;
        hasSpecifiedName = true;
      } else {
        str = name;
        hasSpecifiedName = false;
      }

      JsonContainerAttribute attribute3 =
        JsonTypeReflector.GetAttribute<JsonContainerAttribute>((object) declaringType);
      NamingStrategy namingStrategy = attribute1?.NamingStrategyType == null
        ? (attribute3?.NamingStrategyType == null
          ? this.NamingStrategy
          : JsonTypeReflector.GetContainerNamingStrategy(attribute3))
        : JsonTypeReflector.CreateNamingStrategyInstance(attribute1.NamingStrategyType,
          attribute1.NamingStrategyParameters);
      property.PropertyName = namingStrategy == null
        ? this.ResolvePropertyName(str)
        : namingStrategy.GetPropertyName(str, hasSpecifiedName);
      property.UnderlyingName = name;
      bool flag1 = false;
      if (attribute1 != null) {
        property._required = attribute1._required;
        property.Order = attribute1._order;
        property.DefaultValueHandling = attribute1._defaultValueHandling;
        flag1 = true;
        property.NullValueHandling = attribute1._nullValueHandling;
        property.ReferenceLoopHandling = attribute1._referenceLoopHandling;
        property.ObjectCreationHandling = attribute1._objectCreationHandling;
        property.TypeNameHandling = attribute1._typeNameHandling;
        property.IsReference = attribute1._isReference;
        property.ItemIsReference = attribute1._itemIsReference;
        property.ItemConverter = attribute1.ItemConverterType != null
          ? JsonTypeReflector.CreateJsonConverterInstance(attribute1.ItemConverterType,
            attribute1.ItemConverterParameters)
          : (JsonConverter) null;
        property.ItemReferenceLoopHandling = attribute1._itemReferenceLoopHandling;
        property.ItemTypeNameHandling = attribute1._itemTypeNameHandling;
      } else {
        property.NullValueHandling = new NullValueHandling?();
        property.ReferenceLoopHandling = new ReferenceLoopHandling?();
        property.ObjectCreationHandling = new ObjectCreationHandling?();
        property.TypeNameHandling = new TypeNameHandling?();
        property.IsReference = new bool?();
        property.ItemIsReference = new bool?();
        property.ItemConverter = (JsonConverter) null;
        property.ItemReferenceLoopHandling = new ReferenceLoopHandling?();
        property.ItemTypeNameHandling = new TypeNameHandling?();
      }

      if (attribute2 != null) {
        property._required = new Required?(Required.Always);
        flag1 = true;
      }

      property.HasMemberAttribute = flag1;
      bool flag2 = (object) JsonTypeReflector.GetAttribute<JsonIgnoreAttribute>(attributeProvider) != null ||
                   JsonTypeReflector.GetAttribute<JsonExtensionDataAttribute>(attributeProvider) != null ||
                   JsonTypeReflector.IsNonSerializable(attributeProvider);
      if (memberSerialization != MemberSerialization.OptIn) {
        bool flag3 = false;
        property.Ignored = flag2 | flag3;
      } else
        property.Ignored = flag2 || !flag1;

      property.Converter = JsonTypeReflector.GetJsonConverter(attributeProvider);
      property.MemberConverter = JsonTypeReflector.GetJsonConverter(attributeProvider);
      DefaultValueAttribute attribute4 = JsonTypeReflector.GetAttribute<DefaultValueAttribute>(attributeProvider);
      if (attribute4 != null)
        property.DefaultValue = attribute4.Value;
      allowNonPublicAccess = false;
      if ((this.DefaultMembersSearchFlags & BindingFlags.NonPublic) == BindingFlags.NonPublic)
        allowNonPublicAccess = true;
      if (flag1)
        allowNonPublicAccess = true;
      if (memberSerialization != MemberSerialization.Fields)
        return;
      allowNonPublicAccess = true;
    }

    private Predicate<object> CreateShouldSerializeTest(MemberInfo member) {
      MethodInfo method = member.DeclaringType.GetMethod("ShouldSerialize" + member.Name, ReflectionUtils.EmptyTypes);
      if (method == null || method.ReturnType != typeof(bool))
        return (Predicate<object>) null;
      MethodCall<object, object> shouldSerializeCall =
        JsonTypeReflector.ReflectionDelegateFactory.CreateMethodCall<object>((MethodBase) method);
      return (Predicate<object>) (o => (bool) shouldSerializeCall(o, new object[0]));
    }

    private void SetIsSpecifiedActions(
      JsonProperty property,
      MemberInfo member,
      bool allowNonPublicAccess) {
      MemberInfo memberInfo = (MemberInfo) member.DeclaringType.GetProperty(member.Name + "Specified") ??
                              (MemberInfo) member.DeclaringType.GetField(member.Name + "Specified");
      if (memberInfo == null || ReflectionUtils.GetMemberUnderlyingType(memberInfo) != typeof(bool))
        return;
      Func<object, object> specifiedPropertyGet =
        JsonTypeReflector.ReflectionDelegateFactory.CreateGet<object>(memberInfo);
      property.GetIsSpecified = (Predicate<object>) (o => (bool) specifiedPropertyGet(o));
      if (!ReflectionUtils.CanSetMemberValue(memberInfo, allowNonPublicAccess, false))
        return;
      property.SetIsSpecified = JsonTypeReflector.ReflectionDelegateFactory.CreateSet<object>(memberInfo);
    }

    protected virtual string ResolvePropertyName(string propertyName) {
      if (this.NamingStrategy != null)
        return this.NamingStrategy.GetPropertyName(propertyName, false);
      return propertyName;
    }

    protected virtual string ResolveExtensionDataName(string extensionDataName) {
      if (this.NamingStrategy != null)
        return this.NamingStrategy.GetExtensionDataName(extensionDataName);
      return extensionDataName;
    }

    protected virtual string ResolveDictionaryKey(string dictionaryKey) {
      if (this.NamingStrategy != null)
        return this.NamingStrategy.GetDictionaryKey(dictionaryKey);
      return this.ResolvePropertyName(dictionaryKey);
    }

    public string GetResolvedPropertyName(string propertyName) {
      return this.ResolvePropertyName(propertyName);
    }

    internal class
      EnumerableDictionaryWrapper<TEnumeratorKey, TEnumeratorValue> : IEnumerable<KeyValuePair<object, object>>,
        IEnumerable {
      private readonly IEnumerable<KeyValuePair<TEnumeratorKey, TEnumeratorValue>> _e;

      public EnumerableDictionaryWrapper(
        IEnumerable<KeyValuePair<TEnumeratorKey, TEnumeratorValue>> e) {
        ValidationUtils.ArgumentNotNull((object) e, nameof(e));
        this._e = e;
      }

      public IEnumerator<KeyValuePair<object, object>> GetEnumerator() {
        foreach (KeyValuePair<TEnumeratorKey, TEnumeratorValue> keyValuePair in this._e)
          yield return new KeyValuePair<object, object>((object) keyValuePair.Key, (object) keyValuePair.Value);
      }

      IEnumerator IEnumerable.GetEnumerator() {
        return (IEnumerator) this.GetEnumerator();
      }
    }
  }
}