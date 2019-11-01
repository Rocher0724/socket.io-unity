using System;
using System.Collections.Generic;
using System.Reflection;
using Socket.Newtonsoft.Json.Serialization;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Converters {
  public class KeyValuePairConverter : JsonConverter {
    private static readonly ThreadSafeStore<Type, ReflectionObject> ReflectionObjectPerType =
      new ThreadSafeStore<Type, ReflectionObject>(
        new Func<Type, ReflectionObject>(KeyValuePairConverter.InitializeReflectionObject));

    private const string KeyName = "Key";
    private const string ValueName = "Value";

    private static ReflectionObject InitializeReflectionObject(Type t) {
      Type[] genericArguments = t.GetGenericArguments();
      Type type1 = ((IList<Type>) genericArguments)[0];
      Type type2 = ((IList<Type>) genericArguments)[1];
      return ReflectionObject.Create(t, (MethodBase) t.GetConstructor(new Type[2] {
        type1,
        type2
      }), "Key", "Value");
    }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      ReflectionObject reflectionObject = KeyValuePairConverter.ReflectionObjectPerType.Get(value.GetType());
      DefaultContractResolver contractResolver = serializer.ContractResolver as DefaultContractResolver;
      writer.WriteStartObject();
      writer.WritePropertyName(contractResolver != null ? contractResolver.GetResolvedPropertyName("Key") : "Key");
      serializer.Serialize(writer, reflectionObject.GetValue(value, "Key"), reflectionObject.GetType("Key"));
      writer.WritePropertyName(contractResolver != null ? contractResolver.GetResolvedPropertyName("Value") : "Value");
      serializer.Serialize(writer, reflectionObject.GetValue(value, "Value"), reflectionObject.GetType("Value"));
      writer.WriteEndObject();
    }

    public override object ReadJson(
      JsonReader reader,
      Type objectType,
      object existingValue,
      JsonSerializer serializer) {
      if (reader.TokenType == JsonToken.Null) {
        if (!ReflectionUtils.IsNullableType(objectType))
          throw JsonSerializationException.Create(reader, "Cannot convert null value to KeyValuePair.");
        return (object) null;
      }

      object obj1 = (object) null;
      object obj2 = (object) null;
      reader.ReadAndAssert();
      Type key = ReflectionUtils.IsNullableType(objectType) ? Nullable.GetUnderlyingType(objectType) : objectType;
      ReflectionObject reflectionObject = KeyValuePairConverter.ReflectionObjectPerType.Get(key);
      while (reader.TokenType == JsonToken.PropertyName) {
        string a = reader.Value.ToString();
        if (string.Equals(a, "Key", StringComparison.OrdinalIgnoreCase)) {
          reader.ReadAndAssert();
          obj1 = serializer.Deserialize(reader, reflectionObject.GetType("Key"));
        } else if (string.Equals(a, "Value", StringComparison.OrdinalIgnoreCase)) {
          reader.ReadAndAssert();
          obj2 = serializer.Deserialize(reader, reflectionObject.GetType("Value"));
        } else
          reader.Skip();

        reader.ReadAndAssert();
      }

      return reflectionObject.Creator(new object[2] {
        obj1,
        obj2
      });
    }

    public override bool CanConvert(Type objectType) {
      Type type = ReflectionUtils.IsNullableType(objectType) ? Nullable.GetUnderlyingType(objectType) : objectType;
      if (type.IsValueType() && type.IsGenericType())
        return type.GetGenericTypeDefinition() == typeof(KeyValuePair<,>);
      return false;
    }
  }
}