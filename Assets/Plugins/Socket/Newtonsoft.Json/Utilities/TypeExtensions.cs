using System;
using System.Collections.Generic;
using System.Reflection;

namespace Socket.Newtonsoft.Json.Utilities {
  internal static class TypeExtensions {
    public static MethodInfo Method(this Delegate d) {
      return d.Method;
    }

    public static MemberTypes MemberType(this MemberInfo memberInfo) {
      return memberInfo.MemberType;
    }

    public static bool ContainsGenericParameters(this Type type) {
      return type.ContainsGenericParameters;
    }

    public static bool IsInterface(this Type type) {
      return type.IsInterface;
    }

    public static bool IsGenericType(this Type type) {
      return type.IsGenericType;
    }

    public static bool IsGenericTypeDefinition(this Type type) {
      return type.IsGenericTypeDefinition;
    }

    public static Type BaseType(this Type type) {
      return type.BaseType;
    }

    public static Assembly Assembly(this Type type) {
      return type.Assembly;
    }

    public static bool IsEnum(this Type type) {
      return type.IsEnum;
    }

    public static bool IsClass(this Type type) {
      return type.IsClass;
    }

    public static bool IsSealed(this Type type) {
      return type.IsSealed;
    }

    public static bool IsAbstract(this Type type) {
      return type.IsAbstract;
    }

    public static bool IsVisible(this Type type) {
      return type.IsVisible;
    }

    public static bool IsValueType(this Type type) {
      return type.IsValueType;
    }

    public static bool IsPrimitive(this Type type) {
      return type.IsPrimitive;
    }

    public static bool AssignableToTypeName(
      this Type type,
      string fullTypeName,
      bool searchInterfaces,
      out Type match) {
      for (Type type1 = type; type1 != null; type1 = type1.BaseType()) {
        if (string.Equals(type1.FullName, fullTypeName, StringComparison.Ordinal)) {
          match = type1;
          return true;
        }
      }

      if (searchInterfaces) {
        foreach (MemberInfo memberInfo in type.GetInterfaces()) {
          if (string.Equals(memberInfo.Name, fullTypeName, StringComparison.Ordinal)) {
            match = type;
            return true;
          }
        }
      }

      match = (Type) null;
      return false;
    }

    public static bool AssignableToTypeName(
      this Type type,
      string fullTypeName,
      bool searchInterfaces) {
      Type match;
      return type.AssignableToTypeName(fullTypeName, searchInterfaces, out match);
    }

    public static bool ImplementInterface(this Type type, Type interfaceType) {
      for (Type type1 = type; type1 != null; type1 = type1.BaseType()) {
        foreach (Type type2 in (IEnumerable<Type>) type1.GetInterfaces()) {
          if (type2 == interfaceType || type2 != null && type2.ImplementInterface(interfaceType))
            return true;
        }
      }

      return false;
    }
  }
}