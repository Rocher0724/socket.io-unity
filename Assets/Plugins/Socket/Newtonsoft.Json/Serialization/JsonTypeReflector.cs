using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Security.Permissions;
using Socket.Newtonsoft.Json.Utilities;
using Socket.Newtonsoft.Json.Utilities.LinqBridge;

namespace Socket.Newtonsoft.Json.Serialization {
  internal static class JsonTypeReflector {
    private static readonly ThreadSafeStore<Type, Func<object[], object>> CreatorCache =
      new ThreadSafeStore<Type, Func<object[], object>>(
        new Func<Type, Func<object[], object>>(JsonTypeReflector.GetCreator));

    private static bool? _dynamicCodeGeneration;
    private static bool? _fullyTrusted;
    public const string IdPropertyName = "$id";
    public const string RefPropertyName = "$ref";
    public const string TypePropertyName = "$type";
    public const string ValuePropertyName = "$value";
    public const string ArrayValuesPropertyName = "$values";
    public const string ShouldSerializePrefix = "ShouldSerialize";
    public const string SpecifiedPostfix = "Specified";

    public static T GetCachedAttribute<T>(object attributeProvider) where T : Attribute {
      return CachedAttributeGetter<T>.GetAttribute(attributeProvider);
    }

    public static bool CanTypeDescriptorConvertString(Type type, out TypeConverter typeConverter) {
      typeConverter = TypeDescriptor.GetConverter(type);
      if (typeConverter != null) {
        Type type1 = typeConverter.GetType();
        if (!string.Equals(type1.FullName, "System.ComponentModel.ComponentConverter", StringComparison.Ordinal) &&
            !string.Equals(type1.FullName, "System.ComponentModel.ReferenceConverter", StringComparison.Ordinal) &&
            (!string.Equals(type1.FullName, "System.Windows.Forms.Design.DataSourceConverter",
               StringComparison.Ordinal) && type1 != typeof(TypeConverter)))
          return typeConverter.CanConvertTo(typeof(string));
      }

      return false;
    }

    public static MemberSerialization GetObjectMemberSerialization(
      Type objectType,
      bool ignoreSerializableAttribute) {
      JsonObjectAttribute cachedAttribute =
        JsonTypeReflector.GetCachedAttribute<JsonObjectAttribute>((object) objectType);
      if (cachedAttribute != null)
        return cachedAttribute.MemberSerialization;
      return !ignoreSerializableAttribute && JsonTypeReflector.IsSerializable((object) objectType)
        ? MemberSerialization.Fields
        : MemberSerialization.OptOut;
    }

    public static JsonConverter GetJsonConverter(object attributeProvider) {
      JsonConverterAttribute cachedAttribute =
        JsonTypeReflector.GetCachedAttribute<JsonConverterAttribute>(attributeProvider);
      if (cachedAttribute != null) {
        Func<object[], object> func = JsonTypeReflector.CreatorCache.Get(cachedAttribute.ConverterType);
        if (func != null)
          return (JsonConverter) func(cachedAttribute.ConverterParameters);
      }

      return (JsonConverter) null;
    }

    public static JsonConverter CreateJsonConverterInstance(
      Type converterType,
      object[] converterArgs) {
      return (JsonConverter) JsonTypeReflector.CreatorCache.Get(converterType)(converterArgs);
    }

    public static NamingStrategy CreateNamingStrategyInstance(
      Type namingStrategyType,
      object[] converterArgs) {
      return (NamingStrategy) JsonTypeReflector.CreatorCache.Get(namingStrategyType)(converterArgs);
    }

    public static NamingStrategy GetContainerNamingStrategy(
      JsonContainerAttribute containerAttribute) {
      if (containerAttribute.NamingStrategyInstance == null) {
        if (containerAttribute.NamingStrategyType == null)
          return (NamingStrategy) null;
        containerAttribute.NamingStrategyInstance =
          JsonTypeReflector.CreateNamingStrategyInstance(containerAttribute.NamingStrategyType,
            containerAttribute.NamingStrategyParameters);
      }

      return containerAttribute.NamingStrategyInstance;
    }

    private static Func<object[], object> GetCreator(Type type) {
      Func<object> defaultConstructor = ReflectionUtils.HasDefaultConstructor(type, false)
        ? JsonTypeReflector.ReflectionDelegateFactory.CreateDefaultConstructor<object>(type)
        : (Func<object>) null;
      return (Func<object[], object>) (parameters => {
        try {
          if (parameters != null) {
            ConstructorInfo constructor = type.GetConstructor(((IEnumerable<object>) parameters)
              .Select<object, Type>((Func<object, Type>) (param => param.GetType())).ToArray<Type>());
            if (constructor != null)
              return JsonTypeReflector.ReflectionDelegateFactory.CreateParameterizedConstructor(
                (MethodBase) constructor)(parameters);
            throw new JsonException(
              "No matching parameterized constructor found for '{0}'.".FormatWith(
                (IFormatProvider) CultureInfo.InvariantCulture, (object) type));
          }

          if (defaultConstructor == null)
            throw new JsonException(
              "No parameterless constructor defined for '{0}'.".FormatWith(
                (IFormatProvider) CultureInfo.InvariantCulture, (object) type));
          return defaultConstructor();
        } catch (Exception ex) {
          throw new JsonException(
            "Error creating '{0}'.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) type), ex);
        }
      });
    }

    private static T GetAttribute<T>(Type type) where T : Attribute {
      T attribute1 = ReflectionUtils.GetAttribute<T>((object) type, true);
      if ((object) attribute1 != null)
        return attribute1;
      foreach (object attributeProvider in type.GetInterfaces()) {
        T attribute2 = ReflectionUtils.GetAttribute<T>(attributeProvider, true);
        if ((object) attribute2 != null)
          return attribute2;
      }

      return default(T);
    }

    private static T GetAttribute<T>(MemberInfo memberInfo) where T : Attribute {
      T attribute1 = ReflectionUtils.GetAttribute<T>((object) memberInfo, true);
      if ((object) attribute1 != null)
        return attribute1;
      if (memberInfo.DeclaringType != null) {
        foreach (Type targetType in memberInfo.DeclaringType.GetInterfaces()) {
          MemberInfo memberInfoFromType = ReflectionUtils.GetMemberInfoFromType(targetType, memberInfo);
          if (memberInfoFromType != null) {
            T attribute2 = ReflectionUtils.GetAttribute<T>((object) memberInfoFromType, true);
            if ((object) attribute2 != null)
              return attribute2;
          }
        }
      }

      return default(T);
    }

    public static bool IsNonSerializable(object provider) {
      return JsonTypeReflector.GetCachedAttribute<NonSerializedAttribute>(provider) != null;
    }

    public static bool IsSerializable(object provider) {
      return JsonTypeReflector.GetCachedAttribute<SerializableAttribute>(provider) != null;
    }

    public static T GetAttribute<T>(object provider) where T : Attribute {
      Type type = provider as Type;
      if (type != null)
        return JsonTypeReflector.GetAttribute<T>(type);
      MemberInfo memberInfo = provider as MemberInfo;
      if (memberInfo != null)
        return JsonTypeReflector.GetAttribute<T>(memberInfo);
      return ReflectionUtils.GetAttribute<T>(provider, true);
    }

    public static bool DynamicCodeGeneration {
      get {
        if (!JsonTypeReflector._dynamicCodeGeneration.HasValue) {
          try {
            new ReflectionPermission(ReflectionPermissionFlag.MemberAccess).Demand();
            new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess).Demand();
            new SecurityPermission(SecurityPermissionFlag.SkipVerification).Demand();
            new SecurityPermission(SecurityPermissionFlag.UnmanagedCode).Demand();
            new SecurityPermission(PermissionState.Unrestricted).Demand();
            JsonTypeReflector._dynamicCodeGeneration = new bool?(true);
          } catch (Exception ex) {
            JsonTypeReflector._dynamicCodeGeneration = new bool?(false);
          }
        }

        return JsonTypeReflector._dynamicCodeGeneration.GetValueOrDefault();
      }
    }

    public static bool FullyTrusted {
      get {
        if (!JsonTypeReflector._fullyTrusted.HasValue) {
          try {
            new SecurityPermission(PermissionState.Unrestricted).Demand();
            JsonTypeReflector._fullyTrusted = new bool?(true);
          } catch (Exception ex) {
            JsonTypeReflector._fullyTrusted = new bool?(false);
          }
        }

        return JsonTypeReflector._fullyTrusted.GetValueOrDefault();
      }
    }

    public static ReflectionDelegateFactory ReflectionDelegateFactory {
      get {
        if (JsonTypeReflector.DynamicCodeGeneration)
          return (ReflectionDelegateFactory) DynamicReflectionDelegateFactory.Instance;
        return LateBoundReflectionDelegateFactory.Instance;
      }
    }
  }
}