using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using Socket.Newtonsoft.Json.Serialization;

namespace Socket.Newtonsoft.Json.Utilities {
  internal static class ReflectionUtils {
    public static readonly Type[] EmptyTypes = Type.EmptyTypes;

    public static bool IsVirtual(this PropertyInfo propertyInfo) {
      ValidationUtils.ArgumentNotNull((object) propertyInfo, nameof(propertyInfo));
      MethodInfo getMethod = propertyInfo.GetGetMethod(true);
      if (getMethod != null && getMethod.IsVirtual)
        return true;
      MethodInfo setMethod = propertyInfo.GetSetMethod(true);
      return setMethod != null && setMethod.IsVirtual;
    }

    public static MethodInfo GetBaseDefinition(this PropertyInfo propertyInfo) {
      ValidationUtils.ArgumentNotNull((object) propertyInfo, nameof(propertyInfo));
      MethodInfo getMethod = propertyInfo.GetGetMethod(true);
      if (getMethod != null)
        return getMethod.GetBaseDefinition();
      return propertyInfo.GetSetMethod(true)?.GetBaseDefinition();
    }

    public static bool IsPublic(PropertyInfo property) {
      return property.GetGetMethod() != null && property.GetGetMethod().IsPublic ||
             property.GetSetMethod() != null && property.GetSetMethod().IsPublic;
    }

    public static Type GetObjectType(object v) {
      return v?.GetType();
    }

    public static string GetTypeName(
      Type t,
      TypeNameAssemblyFormatHandling assemblyFormat,
      ISerializationBinder binder) {
      string qualifiedTypeName = ReflectionUtils.GetFullyQualifiedTypeName(t, binder);
      if (assemblyFormat == TypeNameAssemblyFormatHandling.Simple)
        return ReflectionUtils.RemoveAssemblyDetails(qualifiedTypeName);
      if (assemblyFormat == TypeNameAssemblyFormatHandling.Full)
        return qualifiedTypeName;
      throw new ArgumentOutOfRangeException();
    }

    private static string GetFullyQualifiedTypeName(Type t, ISerializationBinder binder) {
      if (binder == null)
        return t.AssemblyQualifiedName;
      string assemblyName;
      string typeName;
      binder.BindToName(t, out assemblyName, out typeName);
      if (assemblyName == null & typeName == null)
        return t.AssemblyQualifiedName;
      return typeName + (assemblyName == null ? "" : ", " + assemblyName);
    }

    private static string RemoveAssemblyDetails(string fullyQualifiedTypeName) {
      StringBuilder stringBuilder = new StringBuilder();
      bool flag1 = false;
      bool flag2 = false;
      for (int index = 0; index < fullyQualifiedTypeName.Length; ++index) {
        char ch = fullyQualifiedTypeName[index];
        switch (ch) {
          case ',':
            if (!flag1) {
              flag1 = true;
              stringBuilder.Append(ch);
              break;
            }

            flag2 = true;
            break;
          case '[':
            flag1 = false;
            flag2 = false;
            stringBuilder.Append(ch);
            break;
          case ']':
            flag1 = false;
            flag2 = false;
            stringBuilder.Append(ch);
            break;
          default:
            if (!flag2) {
              stringBuilder.Append(ch);
              break;
            }

            break;
        }
      }

      return stringBuilder.ToString();
    }

    public static bool HasDefaultConstructor(Type t, bool nonPublic) {
      ValidationUtils.ArgumentNotNull((object) t, nameof(t));
      if (t.IsValueType())
        return true;
      return ReflectionUtils.GetDefaultConstructor(t, nonPublic) != null;
    }

    public static ConstructorInfo GetDefaultConstructor(Type t) {
      return ReflectionUtils.GetDefaultConstructor(t, false);
    }

    public static ConstructorInfo GetDefaultConstructor(Type t, bool nonPublic) {
      BindingFlags bindingAttr = BindingFlags.Instance | BindingFlags.Public;
      if (nonPublic)
        bindingAttr |= BindingFlags.NonPublic;
      return ((IEnumerable<ConstructorInfo>) t.GetConstructors(bindingAttr)).SingleOrDefault<ConstructorInfo>(
        (Func<ConstructorInfo, bool>) (c => !((IEnumerable<ParameterInfo>) c.GetParameters()).Any<ParameterInfo>()));
    }

    public static bool IsNullable(Type t) {
      ValidationUtils.ArgumentNotNull((object) t, nameof(t));
      if (t.IsValueType())
        return ReflectionUtils.IsNullableType(t);
      return true;
    }

    public static bool IsNullableType(Type t) {
      ValidationUtils.ArgumentNotNull((object) t, nameof(t));
      if (t.IsGenericType())
        return t.GetGenericTypeDefinition() == typeof(Nullable<>);
      return false;
    }

    public static Type EnsureNotNullableType(Type t) {
      if (!ReflectionUtils.IsNullableType(t))
        return t;
      return Nullable.GetUnderlyingType(t);
    }

    public static bool IsGenericDefinition(Type type, Type genericInterfaceDefinition) {
      if (!type.IsGenericType())
        return false;
      return type.GetGenericTypeDefinition() == genericInterfaceDefinition;
    }

    public static bool ImplementsGenericDefinition(Type type, Type genericInterfaceDefinition) {
      Type implementingType;
      return ReflectionUtils.ImplementsGenericDefinition(type, genericInterfaceDefinition, out implementingType);
    }

    public static bool ImplementsGenericDefinition(
      Type type,
      Type genericInterfaceDefinition,
      out Type implementingType) {
      ValidationUtils.ArgumentNotNull((object) type, nameof(type));
      ValidationUtils.ArgumentNotNull((object) genericInterfaceDefinition, nameof(genericInterfaceDefinition));
      if (!genericInterfaceDefinition.IsInterface() || !genericInterfaceDefinition.IsGenericTypeDefinition())
        throw new ArgumentNullException("'{0}' is not a generic interface definition.".FormatWith(
          (IFormatProvider) CultureInfo.InvariantCulture, (object) genericInterfaceDefinition));
      if (type.IsInterface() && type.IsGenericType()) {
        Type genericTypeDefinition = type.GetGenericTypeDefinition();
        if (genericInterfaceDefinition == genericTypeDefinition) {
          implementingType = type;
          return true;
        }
      }

      foreach (Type type1 in type.GetInterfaces()) {
        if (type1.IsGenericType()) {
          Type genericTypeDefinition = type1.GetGenericTypeDefinition();
          if (genericInterfaceDefinition == genericTypeDefinition) {
            implementingType = type1;
            return true;
          }
        }
      }

      implementingType = (Type) null;
      return false;
    }

    public static bool InheritsGenericDefinition(Type type, Type genericClassDefinition) {
      Type implementingType;
      return ReflectionUtils.InheritsGenericDefinition(type, genericClassDefinition, out implementingType);
    }

    public static bool InheritsGenericDefinition(
      Type type,
      Type genericClassDefinition,
      out Type implementingType) {
      ValidationUtils.ArgumentNotNull((object) type, nameof(type));
      ValidationUtils.ArgumentNotNull((object) genericClassDefinition, nameof(genericClassDefinition));
      if (!genericClassDefinition.IsClass() || !genericClassDefinition.IsGenericTypeDefinition())
        throw new ArgumentNullException(
          "'{0}' is not a generic class definition.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) genericClassDefinition));
      return ReflectionUtils.InheritsGenericDefinitionInternal(type, genericClassDefinition, out implementingType);
    }

    private static bool InheritsGenericDefinitionInternal(
      Type currentType,
      Type genericClassDefinition,
      out Type implementingType) {
      if (currentType.IsGenericType()) {
        Type genericTypeDefinition = currentType.GetGenericTypeDefinition();
        if (genericClassDefinition == genericTypeDefinition) {
          implementingType = currentType;
          return true;
        }
      }

      if (currentType.BaseType() != null)
        return ReflectionUtils.InheritsGenericDefinitionInternal(currentType.BaseType(), genericClassDefinition,
          out implementingType);
      implementingType = (Type) null;
      return false;
    }

    public static Type GetCollectionItemType(Type type) {
      ValidationUtils.ArgumentNotNull((object) type, nameof(type));
      if (type.IsArray)
        return type.GetElementType();
      Type implementingType;
      if (ReflectionUtils.ImplementsGenericDefinition(type, typeof(IEnumerable<>), out implementingType)) {
        if (implementingType.IsGenericTypeDefinition())
          throw new Exception("Type {0} is not a collection.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) type));
        return implementingType.GetGenericArguments()[0];
      }

      if (typeof(IEnumerable).IsAssignableFrom(type))
        return (Type) null;
      throw new Exception(
        "Type {0} is not a collection.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) type));
    }

    public static void GetDictionaryKeyValueTypes(
      Type dictionaryType,
      out Type keyType,
      out Type valueType) {
      ValidationUtils.ArgumentNotNull((object) dictionaryType, nameof(dictionaryType));
      Type implementingType;
      if (ReflectionUtils.ImplementsGenericDefinition(dictionaryType, typeof(IDictionary<,>), out implementingType)) {
        if (implementingType.IsGenericTypeDefinition())
          throw new Exception("Type {0} is not a dictionary.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) dictionaryType));
        Type[] genericArguments = implementingType.GetGenericArguments();
        keyType = genericArguments[0];
        valueType = genericArguments[1];
      } else {
        if (!typeof(IDictionary).IsAssignableFrom(dictionaryType))
          throw new Exception("Type {0} is not a dictionary.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) dictionaryType));
        keyType = (Type) null;
        valueType = (Type) null;
      }
    }

    public static Type GetMemberUnderlyingType(MemberInfo member) {
      ValidationUtils.ArgumentNotNull((object) member, nameof(member));
      switch (member.MemberType()) {
        case MemberTypes.Event:
          return ((EventInfo) member).EventHandlerType;
        case MemberTypes.Field:
          return ((FieldInfo) member).FieldType;
        case MemberTypes.Method:
          return ((MethodInfo) member).ReturnType;
        case MemberTypes.Property:
          return ((PropertyInfo) member).PropertyType;
        default:
          throw new ArgumentException("MemberInfo must be of type FieldInfo, PropertyInfo, EventInfo or MethodInfo",
            nameof(member));
      }
    }

    public static bool IsIndexedProperty(MemberInfo member) {
      ValidationUtils.ArgumentNotNull((object) member, nameof(member));
      PropertyInfo property = member as PropertyInfo;
      if (property != null)
        return ReflectionUtils.IsIndexedProperty(property);
      return false;
    }

    public static bool IsIndexedProperty(PropertyInfo property) {
      ValidationUtils.ArgumentNotNull((object) property, nameof(property));
      return (uint) property.GetIndexParameters().Length > 0U;
    }

    public static object GetMemberValue(MemberInfo member, object target) {
      ValidationUtils.ArgumentNotNull((object) member, nameof(member));
      ValidationUtils.ArgumentNotNull(target, nameof(target));
      switch (member.MemberType()) {
        case MemberTypes.Field:
          return ((FieldInfo) member).GetValue(target);
        case MemberTypes.Property:
          try {
            return ((PropertyInfo) member).GetValue(target, (object[]) null);
          } catch (TargetParameterCountException ex) {
            throw new ArgumentException(
              "MemberInfo '{0}' has index parameters".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                (object) member.Name), (Exception) ex);
          }
        default:
          throw new ArgumentException(
            "MemberInfo '{0}' is not of type FieldInfo or PropertyInfo".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) member.Name), nameof(member));
      }
    }

    public static void SetMemberValue(MemberInfo member, object target, object value) {
      ValidationUtils.ArgumentNotNull((object) member, nameof(member));
      ValidationUtils.ArgumentNotNull(target, nameof(target));
      switch (member.MemberType()) {
        case MemberTypes.Field:
          ((FieldInfo) member).SetValue(target, value);
          break;
        case MemberTypes.Property:
          ((PropertyInfo) member).SetValue(target, value, (object[]) null);
          break;
        default:
          throw new ArgumentException(
            "MemberInfo '{0}' must be of type FieldInfo or PropertyInfo".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) member.Name), nameof(member));
      }
    }

    public static bool CanReadMemberValue(MemberInfo member, bool nonPublic) {
      switch (member.MemberType()) {
        case MemberTypes.Field:
          FieldInfo fieldInfo = (FieldInfo) member;
          return nonPublic || fieldInfo.IsPublic;
        case MemberTypes.Property:
          PropertyInfo propertyInfo = (PropertyInfo) member;
          if (!propertyInfo.CanRead)
            return false;
          if (nonPublic)
            return true;
          return propertyInfo.GetGetMethod(nonPublic) != null;
        default:
          return false;
      }
    }

    public static bool CanSetMemberValue(MemberInfo member, bool nonPublic, bool canSetReadOnly) {
      switch (member.MemberType()) {
        case MemberTypes.Field:
          FieldInfo fieldInfo = (FieldInfo) member;
          return !fieldInfo.IsLiteral && (!fieldInfo.IsInitOnly || canSetReadOnly) && (nonPublic || fieldInfo.IsPublic);
        case MemberTypes.Property:
          PropertyInfo propertyInfo = (PropertyInfo) member;
          if (!propertyInfo.CanWrite)
            return false;
          if (nonPublic)
            return true;
          return propertyInfo.GetSetMethod(nonPublic) != null;
        default:
          return false;
      }
    }

    public static List<MemberInfo> GetFieldsAndProperties(
      Type type,
      BindingFlags bindingAttr) {
      List<MemberInfo> memberInfoList1 = new List<MemberInfo>();
      memberInfoList1.AddRange<MemberInfo>((IEnumerable) ReflectionUtils.GetFields(type, bindingAttr));
      memberInfoList1.AddRange<MemberInfo>((IEnumerable) ReflectionUtils.GetProperties(type, bindingAttr));
      List<MemberInfo> memberInfoList2 = new List<MemberInfo>(memberInfoList1.Count);
      foreach (IGrouping<string, MemberInfo> source in memberInfoList1.GroupBy<MemberInfo, string>(
        (Func<MemberInfo, string>) (m => m.Name))) {
        if (source.Count<MemberInfo>() == 1) {
          memberInfoList2.Add(source.First<MemberInfo>());
        } else {
          IList<MemberInfo> memberInfoList3 = (IList<MemberInfo>) new List<MemberInfo>();
          foreach (MemberInfo memberInfo in (IEnumerable<MemberInfo>) source) {
            if (memberInfoList3.Count == 0)
              memberInfoList3.Add(memberInfo);
            else if (!ReflectionUtils.IsOverridenGenericMember(memberInfo, bindingAttr) || memberInfo.Name == "Item")
              memberInfoList3.Add(memberInfo);
          }

          memberInfoList2.AddRange((IEnumerable<MemberInfo>) memberInfoList3);
        }
      }

      return memberInfoList2;
    }

    private static bool IsOverridenGenericMember(MemberInfo memberInfo, BindingFlags bindingAttr) {
      if (memberInfo.MemberType() != MemberTypes.Property)
        return false;
      PropertyInfo propertyInfo = (PropertyInfo) memberInfo;
      if (!propertyInfo.IsVirtual())
        return false;
      Type declaringType = propertyInfo.DeclaringType;
      if (!declaringType.IsGenericType())
        return false;
      Type genericTypeDefinition = declaringType.GetGenericTypeDefinition();
      if (genericTypeDefinition == null)
        return false;
      MemberInfo[] member = genericTypeDefinition.GetMember(propertyInfo.Name, bindingAttr);
      return member.Length != 0 && ReflectionUtils.GetMemberUnderlyingType(member[0]).IsGenericParameter;
    }

    public static T GetAttribute<T>(object attributeProvider) where T : Attribute {
      return ReflectionUtils.GetAttribute<T>(attributeProvider, true);
    }

    public static T GetAttribute<T>(object attributeProvider, bool inherit) where T : Attribute {
      T[] attributes = ReflectionUtils.GetAttributes<T>(attributeProvider, inherit);
      if (attributes == null)
        return default(T);
      return ((IEnumerable<T>) attributes).FirstOrDefault<T>();
    }

    public static T[] GetAttributes<T>(object attributeProvider, bool inherit) where T : Attribute {
      Attribute[] attributes = ReflectionUtils.GetAttributes(attributeProvider, typeof(T), inherit);
      return attributes as T[] ?? attributes.Cast<T>().ToArray<T>();
    }

    public static Attribute[] GetAttributes(
      object attributeProvider,
      Type attributeType,
      bool inherit) {
      ValidationUtils.ArgumentNotNull(attributeProvider, nameof(attributeProvider));
      object obj = attributeProvider;
      Type type = obj as Type;
      if (type != null) {
        Attribute[] array =
          (attributeType != null
            ? (IEnumerable) type.GetCustomAttributes(attributeType, inherit)
            : (IEnumerable) type.GetCustomAttributes(inherit)).Cast<Attribute>().ToArray<Attribute>();
        if (inherit && type.BaseType != null)
          array = ((IEnumerable<Attribute>) array)
            .Union<Attribute>(
              (IEnumerable<Attribute>) ReflectionUtils.GetAttributes((object) type.BaseType, attributeType, inherit))
            .ToArray<Attribute>();
        return array;
      }

      Assembly element1 = obj as Assembly;
      if (element1 != null) {
        if (attributeType == null)
          return Attribute.GetCustomAttributes(element1);
        return Attribute.GetCustomAttributes(element1, attributeType);
      }

      MemberInfo element2 = obj as MemberInfo;
      if (element2 != null) {
        if (attributeType == null)
          return Attribute.GetCustomAttributes(element2, inherit);
        return Attribute.GetCustomAttributes(element2, attributeType, inherit);
      }

      Module element3 = obj as Module;
      if (element3 != null) {
        if (attributeType == null)
          return Attribute.GetCustomAttributes(element3, inherit);
        return Attribute.GetCustomAttributes(element3, attributeType, inherit);
      }

      ParameterInfo element4 = obj as ParameterInfo;
      if (element4 != null) {
        if (attributeType == null)
          return Attribute.GetCustomAttributes(element4, inherit);
        return Attribute.GetCustomAttributes(element4, attributeType, inherit);
      }

      ICustomAttributeProvider attributeProvider1 = (ICustomAttributeProvider) attributeProvider;
      return attributeType != null
        ? (Attribute[]) attributeProvider1.GetCustomAttributes(attributeType, inherit)
        : (Attribute[]) attributeProvider1.GetCustomAttributes(inherit);
    }

    public static TypeNameKey SplitFullyQualifiedTypeName(string fullyQualifiedTypeName) {
      int? assemblyDelimiterIndex = ReflectionUtils.GetAssemblyDelimiterIndex(fullyQualifiedTypeName);
      string typeName;
      string assemblyName;
      if (assemblyDelimiterIndex.HasValue) {
        typeName = fullyQualifiedTypeName.Trim(0, assemblyDelimiterIndex.GetValueOrDefault());
        assemblyName = fullyQualifiedTypeName.Trim(assemblyDelimiterIndex.GetValueOrDefault() + 1,
          fullyQualifiedTypeName.Length - assemblyDelimiterIndex.GetValueOrDefault() - 1);
      } else {
        typeName = fullyQualifiedTypeName;
        assemblyName = (string) null;
      }

      return new TypeNameKey(assemblyName, typeName);
    }

    private static int? GetAssemblyDelimiterIndex(string fullyQualifiedTypeName) {
      int num = 0;
      for (int index = 0; index < fullyQualifiedTypeName.Length; ++index) {
        switch (fullyQualifiedTypeName[index]) {
          case ',':
            if (num == 0)
              return new int?(index);
            break;
          case '[':
            ++num;
            break;
          case ']':
            --num;
            break;
        }
      }

      return new int?();
    }

    public static MemberInfo GetMemberInfoFromType(Type targetType, MemberInfo memberInfo) {
      if (memberInfo.MemberType() != MemberTypes.Property)
        return ((IEnumerable<MemberInfo>) targetType.GetMember(memberInfo.Name, memberInfo.MemberType(),
            BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
          .SingleOrDefault<MemberInfo>();
      PropertyInfo propertyInfo = (PropertyInfo) memberInfo;
      Type[] array = ((IEnumerable<ParameterInfo>) propertyInfo.GetIndexParameters())
        .Select<ParameterInfo, Type>((Func<ParameterInfo, Type>) (p => p.ParameterType)).ToArray<Type>();
      return (MemberInfo) targetType.GetProperty(propertyInfo.Name,
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, (Binder) null,
        propertyInfo.PropertyType, array, (ParameterModifier[]) null);
    }

    public static IEnumerable<FieldInfo> GetFields(
      Type targetType,
      BindingFlags bindingAttr) {
      ValidationUtils.ArgumentNotNull((object) targetType, nameof(targetType));
      List<MemberInfo> source = new List<MemberInfo>((IEnumerable<MemberInfo>) targetType.GetFields(bindingAttr));
      ReflectionUtils.GetChildPrivateFields((IList<MemberInfo>) source, targetType, bindingAttr);
      return source.Cast<FieldInfo>();
    }

    private static void GetChildPrivateFields(
      IList<MemberInfo> initialFields,
      Type targetType,
      BindingFlags bindingAttr) {
      if ((bindingAttr & BindingFlags.NonPublic) == BindingFlags.Default)
        return;
      BindingFlags bindingAttr1 = bindingAttr.RemoveFlag(BindingFlags.Public);
      while ((targetType = targetType.BaseType()) != null) {
        IEnumerable<FieldInfo> fieldInfos =
          ((IEnumerable<FieldInfo>) targetType.GetFields(bindingAttr1)).Where<FieldInfo>(
            (Func<FieldInfo, bool>) (f => f.IsPrivate));
        initialFields.AddRange<MemberInfo>((IEnumerable) fieldInfos);
      }
    }

    public static IEnumerable<PropertyInfo> GetProperties(
      Type targetType,
      BindingFlags bindingAttr) {
      ValidationUtils.ArgumentNotNull((object) targetType, nameof(targetType));
      List<PropertyInfo> propertyInfoList =
        new List<PropertyInfo>((IEnumerable<PropertyInfo>) targetType.GetProperties(bindingAttr));
      if (targetType.IsInterface()) {
        foreach (Type type in targetType.GetInterfaces())
          propertyInfoList.AddRange((IEnumerable<PropertyInfo>) type.GetProperties(bindingAttr));
      }

      ReflectionUtils.GetChildPrivateProperties((IList<PropertyInfo>) propertyInfoList, targetType, bindingAttr);
      for (int index = 0; index < propertyInfoList.Count; ++index) {
        PropertyInfo propertyInfo = propertyInfoList[index];
        if (propertyInfo.DeclaringType != targetType) {
          PropertyInfo memberInfoFromType =
            (PropertyInfo) ReflectionUtils.GetMemberInfoFromType(propertyInfo.DeclaringType, (MemberInfo) propertyInfo);
          propertyInfoList[index] = memberInfoFromType;
        }
      }

      return (IEnumerable<PropertyInfo>) propertyInfoList;
    }

    public static BindingFlags RemoveFlag(
      this BindingFlags bindingAttr,
      BindingFlags flag) {
      if ((bindingAttr & flag) != flag)
        return bindingAttr;
      return bindingAttr ^ flag;
    }

    private static void GetChildPrivateProperties(
      IList<PropertyInfo> initialProperties,
      Type targetType,
      BindingFlags bindingAttr) {
      while ((targetType = targetType.BaseType()) != null) {
        foreach (PropertyInfo property in targetType.GetProperties(bindingAttr)) {
          PropertyInfo subTypeProperty = property;
          if (!subTypeProperty.IsVirtual()) {
            if (!ReflectionUtils.IsPublic(subTypeProperty)) {
              int index = initialProperties.IndexOf<PropertyInfo>(
                (Func<PropertyInfo, bool>) (p => p.Name == subTypeProperty.Name));
              if (index == -1)
                initialProperties.Add(subTypeProperty);
              else if (!ReflectionUtils.IsPublic(initialProperties[index]))
                initialProperties[index] = subTypeProperty;
            } else if (initialProperties.IndexOf<PropertyInfo>((Func<PropertyInfo, bool>) (p => {
              if (p.Name == subTypeProperty.Name)
                return p.DeclaringType == subTypeProperty.DeclaringType;
              return false;
            })) == -1)
              initialProperties.Add(subTypeProperty);
          } else {
            Type subTypePropertyDeclaringType =
              subTypeProperty.GetBaseDefinition()?.DeclaringType ?? subTypeProperty.DeclaringType;
            if (initialProperties.IndexOf<PropertyInfo>((Func<PropertyInfo, bool>) (p => {
              if (p.Name == subTypeProperty.Name && p.IsVirtual())
                return (p.GetBaseDefinition()?.DeclaringType ?? p.DeclaringType).IsAssignableFrom(
                  subTypePropertyDeclaringType);
              return false;
            })) == -1)
              initialProperties.Add(subTypeProperty);
          }
        }
      }
    }

    public static bool IsMethodOverridden(
      Type currentType,
      Type methodDeclaringType,
      string method) {
      return ((IEnumerable<MethodInfo>) currentType.GetMethods(
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)).Any<MethodInfo>(
        (Func<MethodInfo, bool>) (info => {
          if (info.Name == method && info.DeclaringType != methodDeclaringType)
            return info.GetBaseDefinition().DeclaringType == methodDeclaringType;
          return false;
        }));
    }

    public static object GetDefaultValue(Type type) {
      if (!type.IsValueType())
        return (object) null;
      switch (ConvertUtils.GetTypeCode(type)) {
        case PrimitiveTypeCode.Char:
        case PrimitiveTypeCode.SByte:
        case PrimitiveTypeCode.Int16:
        case PrimitiveTypeCode.UInt16:
        case PrimitiveTypeCode.Int32:
        case PrimitiveTypeCode.Byte:
        case PrimitiveTypeCode.UInt32:
          return (object) 0;
        case PrimitiveTypeCode.Boolean:
          return (object) false;
        case PrimitiveTypeCode.Int64:
        case PrimitiveTypeCode.UInt64:
          return (object) 0L;
        case PrimitiveTypeCode.Single:
          return (object) 0.0f;
        case PrimitiveTypeCode.Double:
          return (object) 0.0;
        case PrimitiveTypeCode.DateTime:
          return (object) new DateTime();
        case PrimitiveTypeCode.Decimal:
          return (object) Decimal.Zero;
        case PrimitiveTypeCode.Guid:
          return (object) new Guid();
        default:
          if (ReflectionUtils.IsNullable(type))
            return (object) null;
          return Activator.CreateInstance(type);
      }
    }
  }
}