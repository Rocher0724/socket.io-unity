using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Runtime.Serialization;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Serialization {
  public class DefaultSerializationBinder : SerializationBinder, ISerializationBinder {
    internal static readonly DefaultSerializationBinder Instance = new DefaultSerializationBinder();
    private readonly ThreadSafeStore<TypeNameKey, Type> _typeCache;

    public DefaultSerializationBinder() {
      this._typeCache =
        new ThreadSafeStore<TypeNameKey, Type>(new Func<TypeNameKey, Type>(this.GetTypeFromTypeNameKey));
    }

    private Type GetTypeFromTypeNameKey(TypeNameKey typeNameKey) {
      string assemblyName = typeNameKey.AssemblyName;
      string typeName = typeNameKey.TypeName;
      if (assemblyName == null)
        return Type.GetType(typeName);
      Assembly assembly1 = Assembly.LoadWithPartialName(assemblyName);
      if (assembly1 == null) {
        foreach (Assembly assembly2 in AppDomain.CurrentDomain.GetAssemblies()) {
          if (assembly2.FullName == assemblyName || assembly2.GetName().Name == assemblyName) {
            assembly1 = assembly2;
            break;
          }
        }
      }

      if (assembly1 == null)
        throw new JsonSerializationException(
          "Could not load assembly '{0}'.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) assemblyName));
      Type type = assembly1.GetType(typeName);
      if (type == null) {
        if (typeName.IndexOf('`') >= 0) {
          try {
            type = this.GetGenericTypeFromTypeName(typeName, assembly1);
          } catch (Exception ex) {
            throw new JsonSerializationException(
              "Could not find type '{0}' in assembly '{1}'.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                (object) typeName, (object) assembly1.FullName), ex);
          }
        }

        if (type == null)
          throw new JsonSerializationException("Could not find type '{0}' in assembly '{1}'.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) typeName, (object) assembly1.FullName));
      }

      return type;
    }

    private Type GetGenericTypeFromTypeName(string typeName, Assembly assembly) {
      Type type1 = (Type) null;
      int length = typeName.IndexOf('[');
      if (length >= 0) {
        string name = typeName.Substring(0, length);
        Type type2 = assembly.GetType(name);
        if (type2 != null) {
          List<Type> typeList = new List<Type>();
          int num1 = 0;
          int startIndex = 0;
          int num2 = typeName.Length - 1;
          for (int index = length + 1; index < num2; ++index) {
            switch (typeName[index]) {
              case '[':
                if (num1 == 0)
                  startIndex = index + 1;
                ++num1;
                break;
              case ']':
                --num1;
                if (num1 == 0) {
                  TypeNameKey typeNameKey =
                    ReflectionUtils.SplitFullyQualifiedTypeName(typeName.Substring(startIndex, index - startIndex));
                  typeList.Add(this.GetTypeByName(typeNameKey));
                  break;
                }

                break;
            }
          }

          type1 = type2.MakeGenericType(typeList.ToArray());
        }
      }

      return type1;
    }

    private Type GetTypeByName(TypeNameKey typeNameKey) {
      return this._typeCache.Get(typeNameKey);
    }

    public override Type BindToType(string assemblyName, string typeName) {
      return this.GetTypeByName(new TypeNameKey(assemblyName, typeName));
    }

    public void BindToName(Type serializedType, out string assemblyName, out string typeName) {
      assemblyName = serializedType.Assembly.FullName;
      typeName = serializedType.FullName;
    }
  }
}