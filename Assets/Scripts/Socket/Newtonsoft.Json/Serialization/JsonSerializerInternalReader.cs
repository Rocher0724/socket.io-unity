using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Socket.Newtonsoft.Json.Linq;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Serialization {
  internal class JsonSerializerInternalReader : JsonSerializerInternalBase {
    public JsonSerializerInternalReader(JsonSerializer serializer)
      : base(serializer) {
    }

    public void Populate(JsonReader reader, object target) {
      ValidationUtils.ArgumentNotNull(target, nameof(target));
      Type type = target.GetType();
      JsonContract jsonContract = this.Serializer._contractResolver.ResolveContract(type);
      if (!reader.MoveToContent())
        throw JsonSerializationException.Create(reader, "No JSON content found.");
      if (reader.TokenType == JsonToken.StartArray) {
        if (jsonContract.ContractType != JsonContractType.Array)
          throw JsonSerializationException.Create(reader,
            "Cannot populate JSON array onto type '{0}'.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) type));
        JsonArrayContract contract = (JsonArrayContract) jsonContract;
        this.PopulateList(contract.ShouldCreateWrapper ? (IList) contract.CreateWrapper(target) : (IList) target,
          reader, contract, (JsonProperty) null, (string) null);
      } else {
        if (reader.TokenType != JsonToken.StartObject)
          throw JsonSerializationException.Create(reader,
            "Unexpected initial token '{0}' when populating object. Expected JSON object or array.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) reader.TokenType));
        reader.ReadAndAssert();
        string id = (string) null;
        if (this.Serializer.MetadataPropertyHandling != MetadataPropertyHandling.Ignore &&
            reader.TokenType == JsonToken.PropertyName &&
            string.Equals(reader.Value.ToString(), "$id", StringComparison.Ordinal)) {
          reader.ReadAndAssert();
          id = reader.Value?.ToString();
          reader.ReadAndAssert();
        }

        if (jsonContract.ContractType == JsonContractType.Dictionary) {
          JsonDictionaryContract contract = (JsonDictionaryContract) jsonContract;
          this.PopulateDictionary(
            contract.ShouldCreateWrapper ? (IDictionary) contract.CreateWrapper(target) : (IDictionary) target, reader,
            contract, (JsonProperty) null, id);
        } else {
          if (jsonContract.ContractType != JsonContractType.Object)
            throw JsonSerializationException.Create(reader,
              "Cannot populate JSON object onto type '{0}'.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                (object) type));
          this.PopulateObject(target, reader, (JsonObjectContract) jsonContract, (JsonProperty) null, id);
        }
      }
    }

    private JsonContract GetContractSafe(Type type) {
      if (type == null)
        return (JsonContract) null;
      return this.Serializer._contractResolver.ResolveContract(type);
    }

    public object Deserialize(JsonReader reader, Type objectType, bool checkAdditionalContent) {
      if (reader == null)
        throw new ArgumentNullException(nameof(reader));
      JsonContract contractSafe = this.GetContractSafe(objectType);
      try {
        JsonConverter converter = this.GetConverter(contractSafe, (JsonConverter) null, (JsonContainerContract) null,
          (JsonProperty) null);
        if (reader.TokenType == JsonToken.None && !this.ReadForType(reader, contractSafe, converter != null)) {
          if (contractSafe != null && !contractSafe.IsNullable)
            throw JsonSerializationException.Create(reader,
              "No JSON content found and type '{0}' is not nullable.".FormatWith(
                (IFormatProvider) CultureInfo.InvariantCulture, (object) contractSafe.UnderlyingType));
          return (object) null;
        }

        object obj = converter == null || !converter.CanRead
          ? this.CreateValueInternal(reader, objectType, contractSafe, (JsonProperty) null,
            (JsonContainerContract) null, (JsonProperty) null, (object) null)
          : this.DeserializeConvertable(converter, reader, objectType, (object) null);
        if (checkAdditionalContent) {
          while (reader.Read()) {
            if (reader.TokenType != JsonToken.Comment)
              throw JsonSerializationException.Create(reader,
                "Additional text found in JSON string after finishing deserializing object.");
          }
        }

        return obj;
      } catch (Exception ex) {
        if (this.IsErrorHandled((object) null, contractSafe, (object) null, reader as IJsonLineInfo, reader.Path, ex)) {
          this.HandleError(reader, false, 0);
          return (object) null;
        }

        this.ClearErrorContext();
        throw;
      }
    }

    private JsonSerializerProxy GetInternalSerializer() {
      if (this.InternalSerializer == null)
        this.InternalSerializer = new JsonSerializerProxy(this);
      return this.InternalSerializer;
    }

    private JToken CreateJToken(JsonReader reader, JsonContract contract) {
      ValidationUtils.ArgumentNotNull((object) reader, nameof(reader));
      if (contract != null) {
        if (contract.UnderlyingType == typeof(JRaw))
          return (JToken) JRaw.Create(reader);
        if (reader.TokenType == JsonToken.Null && contract.UnderlyingType != typeof(JValue) &&
            contract.UnderlyingType != typeof(JToken))
          return (JToken) null;
      }

      using (JTokenWriter jtokenWriter = new JTokenWriter()) {
        jtokenWriter.WriteToken(reader);
        return jtokenWriter.Token;
      }
    }

    private JToken CreateJObject(JsonReader reader) {
      ValidationUtils.ArgumentNotNull((object) reader, nameof(reader));
      using (JTokenWriter jtokenWriter = new JTokenWriter()) {
        jtokenWriter.WriteStartObject();
        do {
          if (reader.TokenType == JsonToken.PropertyName) {
            string str = (string) reader.Value;
            if (reader.ReadAndMoveToContent()) {
              if (!this.CheckPropertyName(reader, str)) {
                jtokenWriter.WritePropertyName(str);
                jtokenWriter.WriteToken(reader, true, true, false);
              }
            } else
              break;
          } else if (reader.TokenType != JsonToken.Comment) {
            jtokenWriter.WriteEndObject();
            return jtokenWriter.Token;
          }
        } while (reader.Read());

        throw JsonSerializationException.Create(reader, "Unexpected end when deserializing object.");
      }
    }

    private object CreateValueInternal(
      JsonReader reader,
      Type objectType,
      JsonContract contract,
      JsonProperty member,
      JsonContainerContract containerContract,
      JsonProperty containerMember,
      object existingValue) {
      if (contract != null && contract.ContractType == JsonContractType.Linq)
        return (object) this.CreateJToken(reader, contract);
      do {
        switch (reader.TokenType) {
          case JsonToken.StartObject:
            return this.CreateObject(reader, objectType, contract, member, containerContract, containerMember,
              existingValue);
          case JsonToken.StartArray:
            return this.CreateList(reader, objectType, contract, member, existingValue, (string) null);
          case JsonToken.StartConstructor:
            string str = reader.Value.ToString();
            return this.EnsureType(reader, (object) str, CultureInfo.InvariantCulture, contract, objectType);
          case JsonToken.Comment:
            continue;
          case JsonToken.Raw:
            return (object) new JRaw((object) (string) reader.Value);
          case JsonToken.Integer:
          case JsonToken.Float:
          case JsonToken.Boolean:
          case JsonToken.Date:
          case JsonToken.Bytes:
            return this.EnsureType(reader, reader.Value, CultureInfo.InvariantCulture, contract, objectType);
          case JsonToken.String:
            string s = (string) reader.Value;
            if (JsonSerializerInternalReader.CoerceEmptyStringToNull(objectType, contract, s))
              return (object) null;
            if (objectType == typeof(byte[]))
              return (object) Convert.FromBase64String(s);
            return this.EnsureType(reader, (object) s, CultureInfo.InvariantCulture, contract, objectType);
          case JsonToken.Null:
          case JsonToken.Undefined:
            if (objectType == typeof(DBNull))
              return (object) DBNull.Value;
            return this.EnsureType(reader, reader.Value, CultureInfo.InvariantCulture, contract, objectType);
          default:
            throw JsonSerializationException.Create(reader,
              "Unexpected token while deserializing object: " + (object) reader.TokenType);
        }
      } while (reader.Read());

      throw JsonSerializationException.Create(reader, "Unexpected end when deserializing object.");
    }

    private static bool CoerceEmptyStringToNull(Type objectType, JsonContract contract, string s) {
      if (string.IsNullOrEmpty(s) && objectType != null &&
          (objectType != typeof(string) && objectType != typeof(object)) && contract != null)
        return contract.IsNullable;
      return false;
    }

    internal string GetExpectedDescription(JsonContract contract) {
      switch (contract.ContractType) {
        case JsonContractType.Object:
        case JsonContractType.Dictionary:
        case JsonContractType.Serializable:
          return "JSON object (e.g. {\"name\":\"value\"})";
        case JsonContractType.Array:
          return "JSON array (e.g. [1,2,3])";
        case JsonContractType.Primitive:
          return "JSON primitive value (e.g. string, number, boolean, null)";
        case JsonContractType.String:
          return "JSON string value";
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private JsonConverter GetConverter(
      JsonContract contract,
      JsonConverter memberConverter,
      JsonContainerContract containerContract,
      JsonProperty containerProperty) {
      JsonConverter jsonConverter = (JsonConverter) null;
      if (memberConverter != null)
        jsonConverter = memberConverter;
      else if (containerProperty?.ItemConverter != null)
        jsonConverter = containerProperty.ItemConverter;
      else if (containerContract?.ItemConverter != null)
        jsonConverter = containerContract.ItemConverter;
      else if (contract != null) {
        if (contract.Converter != null) {
          jsonConverter = contract.Converter;
        } else {
          JsonConverter matchingConverter;
          if ((matchingConverter = this.Serializer.GetMatchingConverter(contract.UnderlyingType)) != null)
            jsonConverter = matchingConverter;
          else if (contract.InternalConverter != null)
            jsonConverter = contract.InternalConverter;
        }
      }

      return jsonConverter;
    }

    private object CreateObject(
      JsonReader reader,
      Type objectType,
      JsonContract contract,
      JsonProperty member,
      JsonContainerContract containerContract,
      JsonProperty containerMember,
      object existingValue) {
      Type objectType1 = objectType;
      string id;
      if (this.Serializer.MetadataPropertyHandling == MetadataPropertyHandling.Ignore) {
        reader.ReadAndAssert();
        id = (string) null;
      } else if (this.Serializer.MetadataPropertyHandling == MetadataPropertyHandling.ReadAhead) {
        JTokenReader reader1 = reader as JTokenReader;
        if (reader1 == null) {
          reader1 = (JTokenReader) JToken.ReadFrom(reader).CreateReader();
          reader1.Culture = reader.Culture;
          reader1.DateFormatString = reader.DateFormatString;
          reader1.DateParseHandling = reader.DateParseHandling;
          reader1.DateTimeZoneHandling = reader.DateTimeZoneHandling;
          reader1.FloatParseHandling = reader.FloatParseHandling;
          reader1.SupportMultipleContent = reader.SupportMultipleContent;
          reader1.ReadAndAssert();
          reader = (JsonReader) reader1;
        }

        object newValue;
        if (this.ReadMetadataPropertiesToken(reader1, ref objectType1, ref contract, member, containerContract,
          containerMember, existingValue, out newValue, out id))
          return newValue;
      } else {
        reader.ReadAndAssert();
        object newValue;
        if (this.ReadMetadataProperties(reader, ref objectType1, ref contract, member, containerContract,
          containerMember, existingValue, out newValue, out id))
          return newValue;
      }

      if (this.HasNoDefinedType(contract))
        return (object) this.CreateJObject(reader);
      switch (contract.ContractType) {
        case JsonContractType.Object:
          bool createdFromNonDefaultCreator1 = false;
          JsonObjectContract jsonObjectContract = (JsonObjectContract) contract;
          object newObject =
            existingValue == null || objectType1 != objectType && !objectType1.IsAssignableFrom(existingValue.GetType())
              ? this.CreateNewObject(reader, jsonObjectContract, member, containerMember, id,
                out createdFromNonDefaultCreator1)
              : existingValue;
          if (createdFromNonDefaultCreator1)
            return newObject;
          return this.PopulateObject(newObject, reader, jsonObjectContract, member, id);
        case JsonContractType.Primitive:
          JsonPrimitiveContract primitiveContract = (JsonPrimitiveContract) contract;
          if (this.Serializer.MetadataPropertyHandling != MetadataPropertyHandling.Ignore &&
              reader.TokenType == JsonToken.PropertyName &&
              string.Equals(reader.Value.ToString(), "$value", StringComparison.Ordinal)) {
            reader.ReadAndAssert();
            if (reader.TokenType == JsonToken.StartObject)
              throw JsonSerializationException.Create(reader,
                "Unexpected token when deserializing primitive value: " + (object) reader.TokenType);
            object valueInternal = this.CreateValueInternal(reader, objectType1, (JsonContract) primitiveContract,
              member, (JsonContainerContract) null, (JsonProperty) null, existingValue);
            reader.ReadAndAssert();
            return valueInternal;
          }

          break;
        case JsonContractType.Dictionary:
          JsonDictionaryContract contract1 = (JsonDictionaryContract) contract;
          object obj;
          if (existingValue == null) {
            bool createdFromNonDefaultCreator2;
            IDictionary newDictionary = this.CreateNewDictionary(reader, contract1, out createdFromNonDefaultCreator2);
            if (createdFromNonDefaultCreator2) {
              if (id != null)
                throw JsonSerializationException.Create(reader,
                  "Cannot preserve reference to readonly dictionary, or dictionary created from a non-default constructor: {0}."
                    .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) contract.UnderlyingType));
              if (contract.OnSerializingCallbacks.Count > 0)
                throw JsonSerializationException.Create(reader,
                  "Cannot call OnSerializing on readonly dictionary, or dictionary created from a non-default constructor: {0}."
                    .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) contract.UnderlyingType));
              if (contract.OnErrorCallbacks.Count > 0)
                throw JsonSerializationException.Create(reader,
                  "Cannot call OnError on readonly list, or dictionary created from a non-default constructor: {0}."
                    .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) contract.UnderlyingType));
              if (!contract1.HasParameterizedCreatorInternal)
                throw JsonSerializationException.Create(reader,
                  "Cannot deserialize readonly or fixed size dictionary: {0}.".FormatWith(
                    (IFormatProvider) CultureInfo.InvariantCulture, (object) contract.UnderlyingType));
            }

            this.PopulateDictionary(newDictionary, reader, contract1, member, id);
            if (createdFromNonDefaultCreator2)
              return (contract1.OverrideCreator ?? contract1.ParameterizedCreator)(new object[1] {
                (object) newDictionary
              });
            if (newDictionary is IWrappedDictionary)
              return ((IWrappedDictionary) newDictionary).UnderlyingDictionary;
            obj = (object) newDictionary;
          } else
            obj = this.PopulateDictionary(
              contract1.ShouldCreateWrapper
                ? (IDictionary) contract1.CreateWrapper(existingValue)
                : (IDictionary) existingValue, reader, contract1, member, id);

          return obj;
        case JsonContractType.Serializable:
          JsonISerializableContract contract2 = (JsonISerializableContract) contract;
          return this.CreateISerializable(reader, contract2, member, id);
      }

      string message =
        ("Cannot deserialize the current JSON object (e.g. {{\"name\":\"value\"}}) into type '{0}' because the type requires a {1} to deserialize correctly." +
         Environment.NewLine +
         "To fix this error either change the JSON to a {1} or change the deserialized type so that it is a normal .NET type (e.g. not a primitive type like integer, not a collection type like an array or List<T>) that can be deserialized from a JSON object. JsonObjectAttribute can also be added to the type to force it to deserialize from a JSON object." +
         Environment.NewLine).FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) objectType1,
          (object) this.GetExpectedDescription(contract));
      throw JsonSerializationException.Create(reader, message);
    }

    private bool ReadMetadataPropertiesToken(
      JTokenReader reader,
      ref Type objectType,
      ref JsonContract contract,
      JsonProperty member,
      JsonContainerContract containerContract,
      JsonProperty containerMember,
      object existingValue,
      out object newValue,
      out string id) {
      id = (string) null;
      newValue = (object) null;
      if (reader.TokenType == JsonToken.StartObject) {
        JObject currentToken = (JObject) reader.CurrentToken;
        JToken jtoken1 = currentToken["$ref"];
        if (jtoken1 != null) {
          if (jtoken1.Type != JTokenType.String && jtoken1.Type != JTokenType.Null)
            throw JsonSerializationException.Create((IJsonLineInfo) jtoken1, jtoken1.Path,
              "JSON reference {0} property must have a string or null value.".FormatWith(
                (IFormatProvider) CultureInfo.InvariantCulture, (object) "$ref"), (Exception) null);
          JToken parent = (JToken) jtoken1.Parent;
          JToken jtoken2 = (JToken) null;
          if (parent.Next != null)
            jtoken2 = parent.Next;
          else if (parent.Previous != null)
            jtoken2 = parent.Previous;
          string reference = (string) jtoken1;
          if (reference != null) {
            if (jtoken2 != null)
              throw JsonSerializationException.Create((IJsonLineInfo) jtoken2, jtoken2.Path,
                "Additional content found in JSON reference object. A JSON reference object should only have a {0} property."
                  .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) "$ref"), (Exception) null);
            newValue = this.Serializer.GetReferenceResolver().ResolveReference((object) this, reference);
            if (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Info)
              this.TraceWriter.Trace_(TraceLevel.Info,
                JsonPosition.FormatMessage((IJsonLineInfo) reader, reader.Path,
                  "Resolved object reference '{0}' to {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                    (object) reference, (object) newValue.GetType())), (Exception) null);
            reader.Skip();
            return true;
          }
        }

        JToken jtoken3 = currentToken["$type"];
        if (jtoken3 != null) {
          string qualifiedTypeName = (string) jtoken3;
          JsonReader reader1 = jtoken3.CreateReader();
          reader1.ReadAndAssert();
          this.ResolveTypeName(reader1, ref objectType, ref contract, member, containerContract, containerMember,
            qualifiedTypeName);
          if (currentToken["$value"] != null) {
            while (true) {
              reader.ReadAndAssert();
              if (reader.TokenType != JsonToken.PropertyName || !((string) reader.Value == "$value")) {
                reader.ReadAndAssert();
                reader.Skip();
              } else
                break;
            }

            return false;
          }
        }

        JToken jtoken4 = currentToken["$id"];
        if (jtoken4 != null)
          id = (string) jtoken4;
        JToken jtoken5 = currentToken["$values"];
        if (jtoken5 != null) {
          JsonReader reader1 = jtoken5.CreateReader();
          reader1.ReadAndAssert();
          newValue = this.CreateList(reader1, objectType, contract, member, existingValue, id);
          reader.Skip();
          return true;
        }
      }

      reader.ReadAndAssert();
      return false;
    }

    private bool ReadMetadataProperties(
      JsonReader reader,
      ref Type objectType,
      ref JsonContract contract,
      JsonProperty member,
      JsonContainerContract containerContract,
      JsonProperty containerMember,
      object existingValue,
      out object newValue,
      out string id) {
      id = (string) null;
      newValue = (object) null;
      if (reader.TokenType == JsonToken.PropertyName) {
        string str = reader.Value.ToString();
        if (str.Length > 0 && str[0] == '$') {
          bool flag;
          do {
            string a = reader.Value.ToString();
            if (string.Equals(a, "$ref", StringComparison.Ordinal)) {
              reader.ReadAndAssert();
              if (reader.TokenType != JsonToken.String && reader.TokenType != JsonToken.Null)
                throw JsonSerializationException.Create(reader,
                  "JSON reference {0} property must have a string or null value.".FormatWith(
                    (IFormatProvider) CultureInfo.InvariantCulture, (object) "$ref"));
              string reference = reader.Value?.ToString();
              reader.ReadAndAssert();
              if (reference != null) {
                if (reader.TokenType == JsonToken.PropertyName)
                  throw JsonSerializationException.Create(reader,
                    "Additional content found in JSON reference object. A JSON reference object should only have a {0} property."
                      .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) "$ref"));
                newValue = this.Serializer.GetReferenceResolver().ResolveReference((object) this, reference);
                if (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Info)
                  this.TraceWriter.Trace_(TraceLevel.Info,
                    JsonPosition.FormatMessage(reader as IJsonLineInfo, reader.Path,
                      "Resolved object reference '{0}' to {1}.".FormatWith(
                        (IFormatProvider) CultureInfo.InvariantCulture, (object) reference,
                        (object) newValue.GetType())), (Exception) null);
                return true;
              }

              flag = true;
            } else if (string.Equals(a, "$type", StringComparison.Ordinal)) {
              reader.ReadAndAssert();
              string qualifiedTypeName = reader.Value.ToString();
              this.ResolveTypeName(reader, ref objectType, ref contract, member, containerContract, containerMember,
                qualifiedTypeName);
              reader.ReadAndAssert();
              flag = true;
            } else if (string.Equals(a, "$id", StringComparison.Ordinal)) {
              reader.ReadAndAssert();
              id = reader.Value?.ToString();
              reader.ReadAndAssert();
              flag = true;
            } else {
              if (string.Equals(a, "$values", StringComparison.Ordinal)) {
                reader.ReadAndAssert();
                object list = this.CreateList(reader, objectType, contract, member, existingValue, id);
                reader.ReadAndAssert();
                newValue = list;
                return true;
              }

              flag = false;
            }
          } while (flag && reader.TokenType == JsonToken.PropertyName);
        }
      }

      return false;
    }

    private void ResolveTypeName(
      JsonReader reader,
      ref Type objectType,
      ref JsonContract contract,
      JsonProperty member,
      JsonContainerContract containerContract,
      JsonProperty containerMember,
      string qualifiedTypeName) {
      TypeNameHandling? typeNameHandling1 = (TypeNameHandling?) member?.TypeNameHandling;
      int num;
      if (!typeNameHandling1.HasValue) {
        TypeNameHandling? typeNameHandling2 = (TypeNameHandling?) containerContract?.ItemTypeNameHandling;
        if (!typeNameHandling2.HasValue) {
          TypeNameHandling? typeNameHandling3 = (TypeNameHandling?) containerMember?.ItemTypeNameHandling;
          num = typeNameHandling3.HasValue
            ? (int) typeNameHandling3.GetValueOrDefault()
            : (int) this.Serializer._typeNameHandling;
        } else
          num = (int) typeNameHandling2.GetValueOrDefault();
      } else
        num = (int) typeNameHandling1.GetValueOrDefault();

      if (num == 0)
        return;
      TypeNameKey typeNameKey = ReflectionUtils.SplitFullyQualifiedTypeName(qualifiedTypeName);
      Type type;
      try {
        type = this.Serializer._serializationBinder.BindToType(typeNameKey.AssemblyName, typeNameKey.TypeName);
      } catch (Exception ex) {
        throw JsonSerializationException.Create(reader,
          "Error resolving type specified in JSON '{0}'.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) qualifiedTypeName), ex);
      }

      if (type == null)
        throw JsonSerializationException.Create(reader,
          "Type specified in JSON '{0}' was not resolved.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) qualifiedTypeName));
      if (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Verbose)
        this.TraceWriter.Trace_(TraceLevel.Verbose,
          JsonPosition.FormatMessage(reader as IJsonLineInfo, reader.Path,
            "Resolved type '{0}' to {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) qualifiedTypeName, (object) type)), (Exception) null);
      if (objectType != null && !objectType.IsAssignableFrom(type))
        throw JsonSerializationException.Create(reader,
          "Type specified in JSON '{0}' is not compatible with '{1}'.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) type.AssemblyQualifiedName,
            (object) objectType.AssemblyQualifiedName));
      objectType = type;
      contract = this.GetContractSafe(type);
    }

    private JsonArrayContract EnsureArrayContract(
      JsonReader reader,
      Type objectType,
      JsonContract contract) {
      if (contract == null)
        throw JsonSerializationException.Create(reader,
          "Could not resolve type '{0}' to a JsonContract.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) objectType));
      JsonArrayContract jsonArrayContract = contract as JsonArrayContract;
      if (jsonArrayContract != null)
        return jsonArrayContract;
      string message =
        ("Cannot deserialize the current JSON array (e.g. [1,2,3]) into type '{0}' because the type requires a {1} to deserialize correctly." +
         Environment.NewLine +
         "To fix this error either change the JSON to a {1} or change the deserialized type to an array or a type that implements a collection interface (e.g. ICollection, IList) like List<T> that can be deserialized from a JSON array. JsonArrayAttribute can also be added to the type to force it to deserialize from a JSON array." +
         Environment.NewLine).FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) objectType,
          (object) this.GetExpectedDescription(contract));
      throw JsonSerializationException.Create(reader, message);
    }

    private object CreateList(
      JsonReader reader,
      Type objectType,
      JsonContract contract,
      JsonProperty member,
      object existingValue,
      string id) {
      if (this.HasNoDefinedType(contract))
        return (object) this.CreateJToken(reader, contract);
      JsonArrayContract contract1 = this.EnsureArrayContract(reader, objectType, contract);
      object obj;
      if (existingValue == null) {
        bool createdFromNonDefaultCreator;
        IList list = this.CreateNewList(reader, contract1, out createdFromNonDefaultCreator);
        if (createdFromNonDefaultCreator) {
          if (id != null)
            throw JsonSerializationException.Create(reader,
              "Cannot preserve reference to array or readonly list, or list created from a non-default constructor: {0}."
                .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) contract.UnderlyingType));
          if (contract.OnSerializingCallbacks.Count > 0)
            throw JsonSerializationException.Create(reader,
              "Cannot call OnSerializing on an array or readonly list, or list created from a non-default constructor: {0}."
                .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) contract.UnderlyingType));
          if (contract.OnErrorCallbacks.Count > 0)
            throw JsonSerializationException.Create(reader,
              "Cannot call OnError on an array or readonly list, or list created from a non-default constructor: {0}."
                .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) contract.UnderlyingType));
          if (!contract1.HasParameterizedCreatorInternal && !contract1.IsArray)
            throw JsonSerializationException.Create(reader,
              "Cannot deserialize readonly or fixed size list: {0}.".FormatWith(
                (IFormatProvider) CultureInfo.InvariantCulture, (object) contract.UnderlyingType));
        }

        if (!contract1.IsMultidimensionalArray)
          this.PopulateList(list, reader, contract1, member, id);
        else
          this.PopulateMultidimensionalArray(list, reader, contract1, member, id);
        if (createdFromNonDefaultCreator) {
          if (contract1.IsMultidimensionalArray)
            list = (IList) CollectionUtils.ToMultidimensionalArray(list, contract1.CollectionItemType,
              contract.CreatedType.GetArrayRank());
          else if (contract1.IsArray) {
            Array instance = Array.CreateInstance(contract1.CollectionItemType, list.Count);
            list.CopyTo(instance, 0);
            list = (IList) instance;
          } else
            return (contract1.OverrideCreator ?? contract1.ParameterizedCreator)(new object[1] {
              (object) list
            });
        } else if (list is IWrappedCollection)
          return ((IWrappedCollection) list).UnderlyingCollection;

        obj = (object) list;
      } else {
        if (!contract1.CanDeserialize)
          throw JsonSerializationException.Create(reader,
            "Cannot populate list type {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) contract.CreatedType));
        obj = this.PopulateList(
          contract1.ShouldCreateWrapper ? (IList) contract1.CreateWrapper(existingValue) : (IList) existingValue,
          reader, contract1, member, id);
      }

      return obj;
    }

    private bool HasNoDefinedType(JsonContract contract) {
      if (contract != null && contract.UnderlyingType != typeof(object))
        return contract.ContractType == JsonContractType.Linq;
      return true;
    }

    private object EnsureType(
      JsonReader reader,
      object value,
      CultureInfo culture,
      JsonContract contract,
      Type targetType) {
      if (targetType == null || ReflectionUtils.GetObjectType(value) == targetType)
        return value;
      if (value == null && contract.IsNullable)
        return (object) null;
      try {
        if (!contract.IsConvertable)
          return ConvertUtils.ConvertOrCast(value, culture, contract.NonNullableUnderlyingType);
        JsonPrimitiveContract primitiveContract = (JsonPrimitiveContract) contract;
        if (contract.IsEnum) {
          if (value is string)
            return Enum.Parse(contract.NonNullableUnderlyingType, value.ToString(), true);
          if (ConvertUtils.IsInteger((object) primitiveContract.TypeCode))
            return Enum.ToObject(contract.NonNullableUnderlyingType, value);
        }

        return Convert.ChangeType(value, contract.NonNullableUnderlyingType, (IFormatProvider) culture);
      } catch (Exception ex) {
        throw JsonSerializationException.Create(reader,
          "Error converting value {0} to type '{1}'.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) MiscellaneousUtils.FormatValueForPrint(value), (object) targetType), ex);
      }
    }

    private bool SetPropertyValue(
      JsonProperty property,
      JsonConverter propertyConverter,
      JsonContainerContract containerContract,
      JsonProperty containerProperty,
      JsonReader reader,
      object target) {
      bool useExistingValue;
      object currentValue;
      JsonContract propertyContract;
      bool gottenCurrentValue;
      if (this.CalculatePropertyDetails(property, ref propertyConverter, containerContract, containerProperty, reader,
        target, out useExistingValue, out currentValue, out propertyContract, out gottenCurrentValue))
        return false;
      object obj;
      if (propertyConverter != null && propertyConverter.CanRead) {
        if (!gottenCurrentValue && target != null && property.Readable)
          currentValue = property.ValueProvider.GetValue(target);
        obj = this.DeserializeConvertable(propertyConverter, reader, property.PropertyType, currentValue);
      } else
        obj = this.CreateValueInternal(reader, property.PropertyType, propertyContract, property, containerContract,
          containerProperty, useExistingValue ? currentValue : (object) null);

      if (useExistingValue && obj == currentValue || !this.ShouldSetPropertyValue(property, obj))
        return useExistingValue;
      property.ValueProvider.SetValue(target, obj);
      if (property.SetIsSpecified != null) {
        if (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Verbose)
          this.TraceWriter.Trace_(TraceLevel.Verbose,
            JsonPosition.FormatMessage(reader as IJsonLineInfo, reader.Path,
              "IsSpecified for property '{0}' on {1} set to true.".FormatWith(
                (IFormatProvider) CultureInfo.InvariantCulture, (object) property.PropertyName,
                (object) property.DeclaringType)), (Exception) null);
        property.SetIsSpecified(target, (object) true);
      }

      return true;
    }

    private bool CalculatePropertyDetails(
      JsonProperty property,
      ref JsonConverter propertyConverter,
      JsonContainerContract containerContract,
      JsonProperty containerProperty,
      JsonReader reader,
      object target,
      out bool useExistingValue,
      out object currentValue,
      out JsonContract propertyContract,
      out bool gottenCurrentValue) {
      currentValue = (object) null;
      useExistingValue = false;
      propertyContract = (JsonContract) null;
      gottenCurrentValue = false;
      if (property.Ignored)
        return true;
      JsonToken tokenType = reader.TokenType;
      if (property.PropertyContract == null)
        property.PropertyContract = this.GetContractSafe(property.PropertyType);
      if (property.ObjectCreationHandling.GetValueOrDefault(this.Serializer._objectCreationHandling) !=
          ObjectCreationHandling.Replace && (tokenType == JsonToken.StartArray || tokenType == JsonToken.StartObject) &&
          property.Readable) {
        currentValue = property.ValueProvider.GetValue(target);
        gottenCurrentValue = true;
        if (currentValue != null) {
          propertyContract = this.GetContractSafe(currentValue.GetType());
          useExistingValue = !propertyContract.IsReadOnlyOrFixedSize && !propertyContract.UnderlyingType.IsValueType();
        }
      }

      if (!property.Writable && !useExistingValue ||
          property.NullValueHandling.GetValueOrDefault(this.Serializer._nullValueHandling) ==
          NullValueHandling.Ignore && tokenType == JsonToken.Null ||
          this.HasFlag(property.DefaultValueHandling.GetValueOrDefault(this.Serializer._defaultValueHandling),
            DefaultValueHandling.Ignore) &&
          (!this.HasFlag(property.DefaultValueHandling.GetValueOrDefault(this.Serializer._defaultValueHandling),
             DefaultValueHandling.Populate) && JsonTokenUtils.IsPrimitiveToken(tokenType) &&
           MiscellaneousUtils.ValueEquals(reader.Value, property.GetResolvedDefaultValue())))
        return true;
      if (currentValue == null) {
        propertyContract = property.PropertyContract;
      } else {
        propertyContract = this.GetContractSafe(currentValue.GetType());
        if (propertyContract != property.PropertyContract)
          propertyConverter = this.GetConverter(propertyContract, property.MemberConverter, containerContract,
            containerProperty);
      }

      return false;
    }

    private void AddReference(JsonReader reader, string id, object value) {
      try {
        if (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Verbose)
          this.TraceWriter.Trace_(TraceLevel.Verbose,
            JsonPosition.FormatMessage(reader as IJsonLineInfo, reader.Path,
              "Read object reference Id '{0}' for {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                (object) id, (object) value.GetType())), (Exception) null);
        this.Serializer.GetReferenceResolver().AddReference((object) this, id, value);
      } catch (Exception ex) {
        throw JsonSerializationException.Create(reader,
          "Error reading object reference '{0}'.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) id), ex);
      }
    }

    private bool HasFlag(DefaultValueHandling value, DefaultValueHandling flag) {
      return (value & flag) == flag;
    }

    private bool ShouldSetPropertyValue(JsonProperty property, object value) {
      return (property.NullValueHandling.GetValueOrDefault(this.Serializer._nullValueHandling) !=
              NullValueHandling.Ignore || value != null) &&
             (!this.HasFlag(property.DefaultValueHandling.GetValueOrDefault(this.Serializer._defaultValueHandling),
                DefaultValueHandling.Ignore) ||
              (this.HasFlag(property.DefaultValueHandling.GetValueOrDefault(this.Serializer._defaultValueHandling),
                 DefaultValueHandling.Populate) ||
               !MiscellaneousUtils.ValueEquals(value, property.GetResolvedDefaultValue()))) && property.Writable;
    }

    private IList CreateNewList(
      JsonReader reader,
      JsonArrayContract contract,
      out bool createdFromNonDefaultCreator) {
      if (!contract.CanDeserialize)
        throw JsonSerializationException.Create(reader,
          "Cannot create and populate list type {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) contract.CreatedType));
      if (contract.OverrideCreator != null) {
        if (contract.HasParameterizedCreator) {
          createdFromNonDefaultCreator = true;
          return contract.CreateTemporaryCollection();
        }

        object list = contract.OverrideCreator(new object[0]);
        if (contract.ShouldCreateWrapper)
          list = (object) contract.CreateWrapper(list);
        createdFromNonDefaultCreator = false;
        return (IList) list;
      }

      if (contract.IsReadOnlyOrFixedSize) {
        createdFromNonDefaultCreator = true;
        IList list = contract.CreateTemporaryCollection();
        if (contract.ShouldCreateWrapper)
          list = (IList) contract.CreateWrapper((object) list);
        return list;
      }

      if (contract.DefaultCreator != null && (!contract.DefaultCreatorNonPublic ||
                                              this.Serializer._constructorHandling ==
                                              ConstructorHandling.AllowNonPublicDefaultConstructor)) {
        object list = contract.DefaultCreator();
        if (contract.ShouldCreateWrapper)
          list = (object) contract.CreateWrapper(list);
        createdFromNonDefaultCreator = false;
        return (IList) list;
      }

      if (contract.HasParameterizedCreatorInternal) {
        createdFromNonDefaultCreator = true;
        return contract.CreateTemporaryCollection();
      }

      if (!contract.IsInstantiable)
        throw JsonSerializationException.Create(reader,
          "Could not create an instance of type {0}. Type is an interface or abstract class and cannot be instantiated."
            .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) contract.UnderlyingType));
      throw JsonSerializationException.Create(reader,
        "Unable to find a constructor to use for type {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) contract.UnderlyingType));
    }

    private IDictionary CreateNewDictionary(
      JsonReader reader,
      JsonDictionaryContract contract,
      out bool createdFromNonDefaultCreator) {
      if (contract.OverrideCreator != null) {
        if (contract.HasParameterizedCreator) {
          createdFromNonDefaultCreator = true;
          return contract.CreateTemporaryDictionary();
        }

        createdFromNonDefaultCreator = false;
        return (IDictionary) contract.OverrideCreator(new object[0]);
      }

      if (contract.IsReadOnlyOrFixedSize) {
        createdFromNonDefaultCreator = true;
        return contract.CreateTemporaryDictionary();
      }

      if (contract.DefaultCreator != null && (!contract.DefaultCreatorNonPublic ||
                                              this.Serializer._constructorHandling ==
                                              ConstructorHandling.AllowNonPublicDefaultConstructor)) {
        object dictionary = contract.DefaultCreator();
        if (contract.ShouldCreateWrapper)
          dictionary = (object) contract.CreateWrapper(dictionary);
        createdFromNonDefaultCreator = false;
        return (IDictionary) dictionary;
      }

      if (contract.HasParameterizedCreatorInternal) {
        createdFromNonDefaultCreator = true;
        return contract.CreateTemporaryDictionary();
      }

      if (!contract.IsInstantiable)
        throw JsonSerializationException.Create(reader,
          "Could not create an instance of type {0}. Type is an interface or abstract class and cannot be instantiated."
            .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) contract.UnderlyingType));
      throw JsonSerializationException.Create(reader,
        "Unable to find a default constructor to use for type {0}.".FormatWith(
          (IFormatProvider) CultureInfo.InvariantCulture, (object) contract.UnderlyingType));
    }

    private void OnDeserializing(JsonReader reader, JsonContract contract, object value) {
      if (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Info)
        this.TraceWriter.Trace_(TraceLevel.Info,
          JsonPosition.FormatMessage(reader as IJsonLineInfo, reader.Path,
            "Started deserializing {0}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) contract.UnderlyingType)), (Exception) null);
      contract.InvokeOnDeserializing(value, this.Serializer._context);
    }

    private void OnDeserialized(JsonReader reader, JsonContract contract, object value) {
      if (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Info)
        this.TraceWriter.Trace_(TraceLevel.Info,
          JsonPosition.FormatMessage(reader as IJsonLineInfo, reader.Path,
            "Finished deserializing {0}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) contract.UnderlyingType)), (Exception) null);
      contract.InvokeOnDeserialized(value, this.Serializer._context);
    }

    private object PopulateDictionary(
      IDictionary dictionary,
      JsonReader reader,
      JsonDictionaryContract contract,
      JsonProperty containerProperty,
      string id) {
      IWrappedDictionary wrappedDictionary = dictionary as IWrappedDictionary;
      object currentObject = wrappedDictionary != null ? wrappedDictionary.UnderlyingDictionary : (object) dictionary;
      if (id != null)
        this.AddReference(reader, id, currentObject);
      this.OnDeserializing(reader, (JsonContract) contract, currentObject);
      int depth = reader.Depth;
      if (contract.KeyContract == null)
        contract.KeyContract = this.GetContractSafe(contract.DictionaryKeyType);
      if (contract.ItemContract == null)
        contract.ItemContract = this.GetContractSafe(contract.DictionaryValueType);
      JsonConverter converter = contract.ItemConverter ?? this.GetConverter(contract.ItemContract, (JsonConverter) null,
                                  (JsonContainerContract) contract, containerProperty);
      JsonPrimitiveContract keyContract = contract.KeyContract as JsonPrimitiveContract;
      PrimitiveTypeCode primitiveTypeCode = keyContract != null ? keyContract.TypeCode : PrimitiveTypeCode.Empty;
      bool flag = false;
      do {
        switch (reader.TokenType) {
          case JsonToken.PropertyName:
            object keyValue = reader.Value;
            if (!this.CheckPropertyName(reader, keyValue.ToString())) {
              try {
                try {
                  DateTime dt;
                  keyValue =
                    primitiveTypeCode == PrimitiveTypeCode.DateTime ||
                    primitiveTypeCode == PrimitiveTypeCode.DateTimeNullable
                      ? (!DateTimeUtils.TryParseDateTime(keyValue.ToString(), reader.DateTimeZoneHandling,
                        reader.DateFormatString, reader.Culture, out dt)
                        ? this.EnsureType(reader, keyValue, CultureInfo.InvariantCulture, contract.KeyContract,
                          contract.DictionaryKeyType)
                        : (object) dt)
                      : this.EnsureType(reader, keyValue, CultureInfo.InvariantCulture, contract.KeyContract,
                        contract.DictionaryKeyType);
                } catch (Exception ex) {
                  throw JsonSerializationException.Create(reader,
                    "Could not convert string '{0}' to dictionary key type '{1}'. Create a TypeConverter to convert from the string to the key type object."
                      .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, reader.Value,
                        (object) contract.DictionaryKeyType), ex);
                }

                if (!this.ReadForType(reader, contract.ItemContract, converter != null))
                  throw JsonSerializationException.Create(reader, "Unexpected end when deserializing object.");
                object obj = converter == null || !converter.CanRead
                  ? this.CreateValueInternal(reader, contract.DictionaryValueType, contract.ItemContract,
                    (JsonProperty) null, (JsonContainerContract) contract, containerProperty, (object) null)
                  : this.DeserializeConvertable(converter, reader, contract.DictionaryValueType, (object) null);
                dictionary[keyValue] = obj;
                goto case JsonToken.Comment;
              } catch (Exception ex) {
                if (this.IsErrorHandled(currentObject, (JsonContract) contract, keyValue, reader as IJsonLineInfo,
                  reader.Path, ex)) {
                  this.HandleError(reader, true, depth);
                  goto case JsonToken.Comment;
                } else
                  throw;
              }
            } else
              goto case JsonToken.Comment;
          case JsonToken.Comment:
            continue;
          case JsonToken.EndObject:
            flag = true;
            goto case JsonToken.Comment;
          default:
            throw JsonSerializationException.Create(reader,
              "Unexpected token when deserializing object: " + (object) reader.TokenType);
        }
      } while (!flag && reader.Read());

      if (!flag)
        this.ThrowUnexpectedEndException(reader, (JsonContract) contract, currentObject,
          "Unexpected end when deserializing object.");
      this.OnDeserialized(reader, (JsonContract) contract, currentObject);
      return currentObject;
    }

    private object PopulateMultidimensionalArray(
      IList list,
      JsonReader reader,
      JsonArrayContract contract,
      JsonProperty containerProperty,
      string id) {
      int arrayRank = contract.UnderlyingType.GetArrayRank();
      if (id != null)
        this.AddReference(reader, id, (object) list);
      this.OnDeserializing(reader, (JsonContract) contract, (object) list);
      JsonContract contractSafe = this.GetContractSafe(contract.CollectionItemType);
      JsonConverter converter = this.GetConverter(contractSafe, (JsonConverter) null, (JsonContainerContract) contract,
        containerProperty);
      int? nullable1 = new int?();
      Stack<IList> listStack = new Stack<IList>();
      listStack.Push(list);
      IList list1 = list;
      bool flag = false;
      do {
        int depth = reader.Depth;
        if (listStack.Count == arrayRank) {
          try {
            if (this.ReadForType(reader, contractSafe, converter != null)) {
              if (reader.TokenType == JsonToken.EndArray) {
                listStack.Pop();
                list1 = listStack.Peek();
                nullable1 = new int?();
              } else {
                object obj = converter == null || !converter.CanRead
                  ? this.CreateValueInternal(reader, contract.CollectionItemType, contractSafe, (JsonProperty) null,
                    (JsonContainerContract) contract, containerProperty, (object) null)
                  : this.DeserializeConvertable(converter, reader, contract.CollectionItemType, (object) null);
                list1.Add(obj);
              }
            } else
              break;
          } catch (Exception ex) {
            JsonPosition position1 = reader.GetPosition(depth);
            if (this.IsErrorHandled((object) list, (JsonContract) contract, (object) position1.Position,
              reader as IJsonLineInfo, reader.Path, ex)) {
              this.HandleError(reader, true, depth);
              if (nullable1.HasValue) {
                int? nullable2 = nullable1;
                int position2 = position1.Position;
                if ((nullable2.GetValueOrDefault() == position2 ? (nullable2.HasValue ? 1 : 0) : 0) != 0)
                  throw JsonSerializationException.Create(reader, "Infinite loop detected from error handling.", ex);
              }

              nullable1 = new int?(position1.Position);
            } else
              throw;
          }
        } else if (reader.Read()) {
          switch (reader.TokenType) {
            case JsonToken.StartArray:
              IList list2 = (IList) new List<object>();
              list1.Add((object) list2);
              listStack.Push(list2);
              list1 = list2;
              break;
            case JsonToken.Comment:
              break;
            case JsonToken.EndArray:
              listStack.Pop();
              if (listStack.Count > 0) {
                list1 = listStack.Peek();
                break;
              }

              flag = true;
              break;
            default:
              throw JsonSerializationException.Create(reader,
                "Unexpected token when deserializing multidimensional array: " + (object) reader.TokenType);
          }
        } else
          break;
      } while (!flag);

      if (!flag)
        this.ThrowUnexpectedEndException(reader, (JsonContract) contract, (object) list,
          "Unexpected end when deserializing array.");
      this.OnDeserialized(reader, (JsonContract) contract, (object) list);
      return (object) list;
    }

    private void ThrowUnexpectedEndException(
      JsonReader reader,
      JsonContract contract,
      object currentObject,
      string message) {
      try {
        throw JsonSerializationException.Create(reader, message);
      } catch (Exception ex) {
        if (this.IsErrorHandled(currentObject, contract, (object) null, reader as IJsonLineInfo, reader.Path, ex))
          this.HandleError(reader, false, 0);
        else
          throw;
      }
    }

    private object PopulateList(
      IList list,
      JsonReader reader,
      JsonArrayContract contract,
      JsonProperty containerProperty,
      string id) {
      IWrappedCollection wrappedCollection = list as IWrappedCollection;
      object currentObject = wrappedCollection != null ? wrappedCollection.UnderlyingCollection : (object) list;
      if (id != null)
        this.AddReference(reader, id, currentObject);
      if (list.IsFixedSize) {
        reader.Skip();
        return currentObject;
      }

      this.OnDeserializing(reader, (JsonContract) contract, currentObject);
      int depth = reader.Depth;
      if (contract.ItemContract == null)
        contract.ItemContract = this.GetContractSafe(contract.CollectionItemType);
      JsonConverter converter = this.GetConverter(contract.ItemContract, (JsonConverter) null,
        (JsonContainerContract) contract, containerProperty);
      int? nullable1 = new int?();
      bool flag = false;
      do {
        try {
          if (this.ReadForType(reader, contract.ItemContract, converter != null)) {
            if (reader.TokenType == JsonToken.EndArray) {
              flag = true;
            } else {
              object obj = converter == null || !converter.CanRead
                ? this.CreateValueInternal(reader, contract.CollectionItemType, contract.ItemContract,
                  (JsonProperty) null, (JsonContainerContract) contract, containerProperty, (object) null)
                : this.DeserializeConvertable(converter, reader, contract.CollectionItemType, (object) null);
              list.Add(obj);
            }
          } else
            break;
        } catch (Exception ex) {
          JsonPosition position1 = reader.GetPosition(depth);
          if (this.IsErrorHandled(currentObject, (JsonContract) contract, (object) position1.Position,
            reader as IJsonLineInfo, reader.Path, ex)) {
            this.HandleError(reader, true, depth);
            if (nullable1.HasValue) {
              int? nullable2 = nullable1;
              int position2 = position1.Position;
              if ((nullable2.GetValueOrDefault() == position2 ? (nullable2.HasValue ? 1 : 0) : 0) != 0)
                throw JsonSerializationException.Create(reader, "Infinite loop detected from error handling.", ex);
            }

            nullable1 = new int?(position1.Position);
          } else
            throw;
        }
      } while (!flag);

      if (!flag)
        this.ThrowUnexpectedEndException(reader, (JsonContract) contract, currentObject,
          "Unexpected end when deserializing array.");
      this.OnDeserialized(reader, (JsonContract) contract, currentObject);
      return currentObject;
    }

    private object CreateISerializable(
      JsonReader reader,
      JsonISerializableContract contract,
      JsonProperty member,
      string id) {
      Type underlyingType = contract.UnderlyingType;
      if (!JsonTypeReflector.FullyTrusted) {
        string message =
          ("Type '{0}' implements ISerializable but cannot be deserialized using the ISerializable interface because the current application is not fully trusted and ISerializable can expose secure data." +
           Environment.NewLine +
           "To fix this error either change the environment to be fully trusted, change the application to not deserialize the type, add JsonObjectAttribute to the type or change the JsonSerializer setting ContractResolver to use a new DefaultContractResolver with IgnoreSerializableInterface set to true." +
           Environment.NewLine).FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) underlyingType);
        throw JsonSerializationException.Create(reader, message);
      }

      if (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Info)
        this.TraceWriter.Trace_(TraceLevel.Info,
          JsonPosition.FormatMessage(reader as IJsonLineInfo, reader.Path,
            "Deserializing {0} using ISerializable constructor.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) contract.UnderlyingType)), (Exception) null);
      SerializationInfo serializationInfo = new SerializationInfo(contract.UnderlyingType,
        (IFormatterConverter) new JsonFormatterConverter(this, contract, member));
      bool flag = false;
      do {
        switch (reader.TokenType) {
          case JsonToken.PropertyName:
            string name = reader.Value.ToString();
            if (!reader.Read())
              throw JsonSerializationException.Create(reader,
                "Unexpected end when setting {0}'s value.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                  (object) name));
            serializationInfo.AddValue(name, (object) JToken.ReadFrom(reader));
            goto case JsonToken.Comment;
          case JsonToken.Comment:
            continue;
          case JsonToken.EndObject:
            flag = true;
            goto case JsonToken.Comment;
          default:
            throw JsonSerializationException.Create(reader,
              "Unexpected token when deserializing object: " + (object) reader.TokenType);
        }
      } while (!flag && reader.Read());

      if (!flag)
        this.ThrowUnexpectedEndException(reader, (JsonContract) contract, (object) serializationInfo,
          "Unexpected end when deserializing object.");
      if (contract.ISerializableCreator == null)
        throw JsonSerializationException.Create(reader,
          "ISerializable type '{0}' does not have a valid constructor. To correctly implement ISerializable a constructor that takes SerializationInfo and StreamingContext parameters should be present."
            .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) underlyingType));
      object obj = contract.ISerializableCreator(new object[2] {
        (object) serializationInfo,
        (object) this.Serializer._context
      });
      if (id != null)
        this.AddReference(reader, id, obj);
      this.OnDeserializing(reader, (JsonContract) contract, obj);
      this.OnDeserialized(reader, (JsonContract) contract, obj);
      return obj;
    }

    internal object CreateISerializableItem(
      JToken token,
      Type type,
      JsonISerializableContract contract,
      JsonProperty member) {
      JsonContract contractSafe = this.GetContractSafe(type);
      JsonConverter converter =
        this.GetConverter(contractSafe, (JsonConverter) null, (JsonContainerContract) contract, member);
      JsonReader reader = token.CreateReader();
      reader.ReadAndAssert();
      return converter == null || !converter.CanRead
        ? this.CreateValueInternal(reader, type, contractSafe, (JsonProperty) null, (JsonContainerContract) contract,
          member, (object) null)
        : this.DeserializeConvertable(converter, reader, type, (object) null);
    }

    private object CreateObjectUsingCreatorWithParameters(
      JsonReader reader,
      JsonObjectContract contract,
      JsonProperty containerProperty,
      ObjectConstructor<object> creator,
      string id) {
      ValidationUtils.ArgumentNotNull((object) creator, nameof(creator));
      bool flag = contract.HasRequiredOrDefaultValueProperties ||
                  this.HasFlag(this.Serializer._defaultValueHandling, DefaultValueHandling.Populate);
      Type underlyingType = contract.UnderlyingType;
      if (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Info) {
        string str = string.Join(", ",
          contract.CreatorParameters.Select<JsonProperty, string>((Func<JsonProperty, string>) (p => p.PropertyName))
            .ToArray<string>());
        this.TraceWriter.Trace_(TraceLevel.Info,
          JsonPosition.FormatMessage(reader as IJsonLineInfo, reader.Path,
            "Deserializing {0} using creator with parameters: {1}.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) contract.UnderlyingType, (object) str)),
          (Exception) null);
      }

      List<JsonSerializerInternalReader.CreatorPropertyContext> source =
        this.ResolvePropertyAndCreatorValues(contract, containerProperty, reader, underlyingType);
      if (flag) {
        foreach (JsonProperty property1 in (Collection<JsonProperty>) contract.Properties) {
          JsonProperty property = property1;
          if (source.All<JsonSerializerInternalReader.CreatorPropertyContext>(
            (Func<JsonSerializerInternalReader.CreatorPropertyContext, bool>) (p => p.Property != property)))
            source.Add(new JsonSerializerInternalReader.CreatorPropertyContext() {
              Property = property,
              Name = property.PropertyName,
              Presence = new JsonSerializerInternalReader.PropertyPresence?(JsonSerializerInternalReader
                .PropertyPresence.None)
            });
        }
      }

      object[] objArray = new object[contract.CreatorParameters.Count];
      foreach (JsonSerializerInternalReader.CreatorPropertyContext creatorPropertyContext in source) {
        if (flag && creatorPropertyContext.Property != null && !creatorPropertyContext.Presence.HasValue) {
          object obj = creatorPropertyContext.Value;
          JsonSerializerInternalReader.PropertyPresence propertyPresence = obj != null
            ? (!(obj is string)
              ? JsonSerializerInternalReader.PropertyPresence.Value
              : (JsonSerializerInternalReader.CoerceEmptyStringToNull(creatorPropertyContext.Property.PropertyType,
                creatorPropertyContext.Property.PropertyContract, (string) obj)
                ? JsonSerializerInternalReader.PropertyPresence.Null
                : JsonSerializerInternalReader.PropertyPresence.Value))
            : JsonSerializerInternalReader.PropertyPresence.Null;
          creatorPropertyContext.Presence = new JsonSerializerInternalReader.PropertyPresence?(propertyPresence);
        }

        JsonProperty constructorProperty = creatorPropertyContext.ConstructorProperty;
        if (constructorProperty == null && creatorPropertyContext.Property != null)
          constructorProperty = contract.CreatorParameters.ForgivingCaseSensitiveFind<JsonProperty>(
            (Func<JsonProperty, string>) (p => p.PropertyName), creatorPropertyContext.Property.UnderlyingName);
        if (constructorProperty != null && !constructorProperty.Ignored) {
          if (flag) {
            JsonSerializerInternalReader.PropertyPresence? presence = creatorPropertyContext.Presence;
            JsonSerializerInternalReader.PropertyPresence propertyPresence1 =
              JsonSerializerInternalReader.PropertyPresence.None;
            if ((presence.GetValueOrDefault() == propertyPresence1 ? (presence.HasValue ? 1 : 0) : 0) == 0) {
              presence = creatorPropertyContext.Presence;
              JsonSerializerInternalReader.PropertyPresence propertyPresence2 =
                JsonSerializerInternalReader.PropertyPresence.Null;
              if ((presence.GetValueOrDefault() == propertyPresence2 ? (presence.HasValue ? 1 : 0) : 0) == 0)
                goto label_25;
            }

            if (constructorProperty.PropertyContract == null)
              constructorProperty.PropertyContract = this.GetContractSafe(constructorProperty.PropertyType);
            if (this.HasFlag(
              constructorProperty.DefaultValueHandling.GetValueOrDefault(this.Serializer._defaultValueHandling),
              DefaultValueHandling.Populate))
              creatorPropertyContext.Value = this.EnsureType(reader, constructorProperty.GetResolvedDefaultValue(),
                CultureInfo.InvariantCulture, constructorProperty.PropertyContract, constructorProperty.PropertyType);
          }

          label_25:
          int index = contract.CreatorParameters.IndexOf(constructorProperty);
          objArray[index] = creatorPropertyContext.Value;
          creatorPropertyContext.Used = true;
        }
      }

      object obj1 = creator(objArray);
      if (id != null)
        this.AddReference(reader, id, obj1);
      this.OnDeserializing(reader, (JsonContract) contract, obj1);
      foreach (JsonSerializerInternalReader.CreatorPropertyContext creatorPropertyContext in source) {
        if (!creatorPropertyContext.Used && creatorPropertyContext.Property != null &&
            !creatorPropertyContext.Property.Ignored) {
          JsonSerializerInternalReader.PropertyPresence? presence = creatorPropertyContext.Presence;
          JsonSerializerInternalReader.PropertyPresence propertyPresence =
            JsonSerializerInternalReader.PropertyPresence.None;
          if ((presence.GetValueOrDefault() == propertyPresence ? (presence.HasValue ? 1 : 0) : 0) == 0) {
            JsonProperty property = creatorPropertyContext.Property;
            object obj2 = creatorPropertyContext.Value;
            if (this.ShouldSetPropertyValue(property, obj2)) {
              property.ValueProvider.SetValue(obj1, obj2);
              creatorPropertyContext.Used = true;
            } else if (!property.Writable && obj2 != null) {
              JsonContract jsonContract = this.Serializer._contractResolver.ResolveContract(property.PropertyType);
              if (jsonContract.ContractType == JsonContractType.Array) {
                JsonArrayContract jsonArrayContract = (JsonArrayContract) jsonContract;
                if (jsonArrayContract.CanDeserialize) {
                  object list1 = property.ValueProvider.GetValue(obj1);
                  if (list1 != null) {
                    IList list2 = jsonArrayContract.ShouldCreateWrapper
                      ? (IList) jsonArrayContract.CreateWrapper(list1)
                      : (IList) list1;
                    foreach (object obj3 in jsonArrayContract.ShouldCreateWrapper
                      ? (IEnumerable) jsonArrayContract.CreateWrapper(obj2)
                      : (IEnumerable) obj2)
                      list2.Add(obj3);
                  }
                }
              } else if (jsonContract.ContractType == JsonContractType.Dictionary) {
                JsonDictionaryContract dictionaryContract = (JsonDictionaryContract) jsonContract;
                if (!dictionaryContract.IsReadOnlyOrFixedSize) {
                  object dictionary1 = property.ValueProvider.GetValue(obj1);
                  if (dictionary1 != null) {
                    IDictionary dictionary2 = dictionaryContract.ShouldCreateWrapper
                      ? (IDictionary) dictionaryContract.CreateWrapper(dictionary1)
                      : (IDictionary) dictionary1;
                    IDictionaryEnumerator enumerator =
                      (dictionaryContract.ShouldCreateWrapper
                        ? (IDictionary) dictionaryContract.CreateWrapper(obj2)
                        : (IDictionary) obj2).GetEnumerator();
                    try {
                      while (enumerator.MoveNext()) {
                        DictionaryEntry entry = enumerator.Entry;
                        dictionary2[entry.Key] = entry.Value;
                      }
                    } finally {
                      (enumerator as IDisposable)?.Dispose();
                    }
                  }
                }
              }

              creatorPropertyContext.Used = true;
            }
          }
        }
      }

      if (contract.ExtensionDataSetter != null) {
        foreach (JsonSerializerInternalReader.CreatorPropertyContext creatorPropertyContext in source) {
          if (!creatorPropertyContext.Used)
            contract.ExtensionDataSetter(obj1, creatorPropertyContext.Name, creatorPropertyContext.Value);
        }
      }

      if (flag) {
        foreach (JsonSerializerInternalReader.CreatorPropertyContext creatorPropertyContext in source) {
          if (creatorPropertyContext.Property != null)
            this.EndProcessProperty(obj1, reader, contract, reader.Depth, creatorPropertyContext.Property,
              creatorPropertyContext.Presence.GetValueOrDefault(), !creatorPropertyContext.Used);
        }
      }

      this.OnDeserialized(reader, (JsonContract) contract, obj1);
      return obj1;
    }

    private object DeserializeConvertable(
      JsonConverter converter,
      JsonReader reader,
      Type objectType,
      object existingValue) {
      if (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Info)
        this.TraceWriter.Trace_(TraceLevel.Info,
          JsonPosition.FormatMessage(reader as IJsonLineInfo, reader.Path,
            "Started deserializing {0} with converter {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) objectType, (object) converter.GetType())), (Exception) null);
      object obj = converter.ReadJson(reader, objectType, existingValue, (JsonSerializer) this.GetInternalSerializer());
      if (this.TraceWriter == null || this.TraceWriter.LevelFilter < TraceLevel.Info)
        return obj;
      this.TraceWriter.Trace_(TraceLevel.Info,
        JsonPosition.FormatMessage(reader as IJsonLineInfo, reader.Path,
          "Finished deserializing {0} with converter {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) objectType, (object) converter.GetType())), (Exception) null);
      return obj;
    }

    private List<JsonSerializerInternalReader.CreatorPropertyContext> ResolvePropertyAndCreatorValues(
      JsonObjectContract contract,
      JsonProperty containerProperty,
      JsonReader reader,
      Type objectType) {
      List<JsonSerializerInternalReader.CreatorPropertyContext> creatorPropertyContextList =
        new List<JsonSerializerInternalReader.CreatorPropertyContext>();
      bool flag = false;
      do {
        switch (reader.TokenType) {
          case JsonToken.PropertyName:
            string propertyName = reader.Value.ToString();
            JsonSerializerInternalReader.CreatorPropertyContext creatorPropertyContext =
              new JsonSerializerInternalReader.CreatorPropertyContext() {
                Name = reader.Value.ToString(),
                ConstructorProperty = contract.CreatorParameters.GetClosestMatchProperty(propertyName),
                Property = contract.Properties.GetClosestMatchProperty(propertyName)
              };
            creatorPropertyContextList.Add(creatorPropertyContext);
            JsonProperty member = creatorPropertyContext.ConstructorProperty ?? creatorPropertyContext.Property;
            if (member != null && !member.Ignored) {
              if (member.PropertyContract == null)
                member.PropertyContract = this.GetContractSafe(member.PropertyType);
              JsonConverter converter = this.GetConverter(member.PropertyContract, member.MemberConverter,
                (JsonContainerContract) contract, containerProperty);
              if (!this.ReadForType(reader, member.PropertyContract, converter != null))
                throw JsonSerializationException.Create(reader,
                  "Unexpected end when setting {0}'s value.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                    (object) propertyName));
              creatorPropertyContext.Value = converter == null || !converter.CanRead
                ? this.CreateValueInternal(reader, member.PropertyType, member.PropertyContract, member,
                  (JsonContainerContract) contract, containerProperty, (object) null)
                : this.DeserializeConvertable(converter, reader, member.PropertyType, (object) null);
              goto case JsonToken.Comment;
            } else {
              if (!reader.Read())
                throw JsonSerializationException.Create(reader,
                  "Unexpected end when setting {0}'s value.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                    (object) propertyName));
              if (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Verbose)
                this.TraceWriter.Trace_(TraceLevel.Verbose,
                  JsonPosition.FormatMessage(reader as IJsonLineInfo, reader.Path,
                    "Could not find member '{0}' on {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                      (object) propertyName, (object) contract.UnderlyingType)), (Exception) null);
              if (this.Serializer._missingMemberHandling == MissingMemberHandling.Error)
                throw JsonSerializationException.Create(reader,
                  "Could not find member '{0}' on object of type '{1}'".FormatWith(
                    (IFormatProvider) CultureInfo.InvariantCulture, (object) propertyName, (object) objectType.Name));
              if (contract.ExtensionDataSetter != null) {
                creatorPropertyContext.Value = this.ReadExtensionDataValue(contract, containerProperty, reader);
                goto case JsonToken.Comment;
              } else {
                reader.Skip();
                goto case JsonToken.Comment;
              }
            }
          case JsonToken.Comment:
            continue;
          case JsonToken.EndObject:
            flag = true;
            goto case JsonToken.Comment;
          default:
            throw JsonSerializationException.Create(reader,
              "Unexpected token when deserializing object: " + (object) reader.TokenType);
        }
      } while (!flag && reader.Read());

      if (!flag)
        this.ThrowUnexpectedEndException(reader, (JsonContract) contract, (object) null,
          "Unexpected end when deserializing object.");
      return creatorPropertyContextList;
    }

    private bool ReadForType(JsonReader reader, JsonContract contract, bool hasConverter) {
      if (hasConverter)
        return reader.Read();
      switch (contract != null ? (int) contract.InternalReadType : 0) {
        case 0:
          return reader.ReadAndMoveToContent();
        case 1:
          reader.ReadAsInt32();
          break;
        case 2:
          reader.ReadAsBytes();
          break;
        case 3:
          reader.ReadAsString();
          break;
        case 4:
          reader.ReadAsDecimal();
          break;
        case 5:
          reader.ReadAsDateTime();
          break;
        case 6:
          reader.ReadAsDouble();
          break;
        case 7:
          reader.ReadAsBoolean();
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }

      return (uint) reader.TokenType > 0U;
    }

    public object CreateNewObject(
      JsonReader reader,
      JsonObjectContract objectContract,
      JsonProperty containerMember,
      JsonProperty containerProperty,
      string id,
      out bool createdFromNonDefaultCreator) {
      object obj = (object) null;
      if (objectContract.OverrideCreator != null) {
        if (objectContract.CreatorParameters.Count > 0) {
          createdFromNonDefaultCreator = true;
          return this.CreateObjectUsingCreatorWithParameters(reader, objectContract, containerMember,
            objectContract.OverrideCreator, id);
        }

        obj = objectContract.OverrideCreator(CollectionUtils.ArrayEmpty<object>());
      } else if (objectContract.DefaultCreator != null && (!objectContract.DefaultCreatorNonPublic ||
                                                           this.Serializer._constructorHandling == ConstructorHandling
                                                             .AllowNonPublicDefaultConstructor ||
                                                           objectContract.ParameterizedCreator == null))
        obj = objectContract.DefaultCreator();
      else if (objectContract.ParameterizedCreator != null) {
        createdFromNonDefaultCreator = true;
        return this.CreateObjectUsingCreatorWithParameters(reader, objectContract, containerMember,
          objectContract.ParameterizedCreator, id);
      }

      if (obj == null) {
        if (!objectContract.IsInstantiable)
          throw JsonSerializationException.Create(reader,
            "Could not create an instance of type {0}. Type is an interface or abstract class and cannot be instantiated."
              .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) objectContract.UnderlyingType));
        throw JsonSerializationException.Create(reader,
          "Unable to find a constructor to use for type {0}. A class should either have a default constructor, one constructor with arguments or a constructor marked with the JsonConstructor attribute."
            .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) objectContract.UnderlyingType));
      }

      createdFromNonDefaultCreator = false;
      return obj;
    }

    private object PopulateObject(
      object newObject,
      JsonReader reader,
      JsonObjectContract contract,
      JsonProperty member,
      string id) {
      this.OnDeserializing(reader, (JsonContract) contract, newObject);
      Dictionary<JsonProperty, JsonSerializerInternalReader.PropertyPresence> requiredProperties =
        contract.HasRequiredOrDefaultValueProperties ||
        this.HasFlag(this.Serializer._defaultValueHandling, DefaultValueHandling.Populate)
          ? contract.Properties.ToDictionary<JsonProperty, JsonProperty, JsonSerializerInternalReader.PropertyPresence>(
            (Func<JsonProperty, JsonProperty>) (m => m),
            (Func<JsonProperty, JsonSerializerInternalReader.PropertyPresence>) (m =>
              JsonSerializerInternalReader.PropertyPresence.None))
          : (Dictionary<JsonProperty, JsonSerializerInternalReader.PropertyPresence>) null;
      if (id != null)
        this.AddReference(reader, id, newObject);
      int depth = reader.Depth;
      bool flag = false;
      do {
        switch (reader.TokenType) {
          case JsonToken.PropertyName:
            string str = reader.Value.ToString();
            if (!this.CheckPropertyName(reader, str)) {
              try {
                JsonProperty closestMatchProperty = contract.Properties.GetClosestMatchProperty(str);
                if (closestMatchProperty == null) {
                  if (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Verbose)
                    this.TraceWriter.Trace_(TraceLevel.Verbose,
                      JsonPosition.FormatMessage(reader as IJsonLineInfo, reader.Path,
                        "Could not find member '{0}' on {1}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                          (object) str, (object) contract.UnderlyingType)), (Exception) null);
                  if (this.Serializer._missingMemberHandling == MissingMemberHandling.Error)
                    throw JsonSerializationException.Create(reader,
                      "Could not find member '{0}' on object of type '{1}'".FormatWith(
                        (IFormatProvider) CultureInfo.InvariantCulture, (object) str,
                        (object) contract.UnderlyingType.Name));
                  if (reader.Read()) {
                    this.SetExtensionData(contract, member, reader, str, newObject);
                    goto case JsonToken.Comment;
                  } else
                    goto case JsonToken.Comment;
                } else if (closestMatchProperty.Ignored ||
                           !this.ShouldDeserialize(reader, closestMatchProperty, newObject)) {
                  if (reader.Read()) {
                    this.SetPropertyPresence(reader, closestMatchProperty, requiredProperties);
                    this.SetExtensionData(contract, member, reader, str, newObject);
                    goto case JsonToken.Comment;
                  } else
                    goto case JsonToken.Comment;
                } else {
                  if (closestMatchProperty.PropertyContract == null)
                    closestMatchProperty.PropertyContract = this.GetContractSafe(closestMatchProperty.PropertyType);
                  JsonConverter converter = this.GetConverter(closestMatchProperty.PropertyContract,
                    closestMatchProperty.MemberConverter, (JsonContainerContract) contract, member);
                  if (!this.ReadForType(reader, closestMatchProperty.PropertyContract, converter != null))
                    throw JsonSerializationException.Create(reader,
                      "Unexpected end when setting {0}'s value.".FormatWith(
                        (IFormatProvider) CultureInfo.InvariantCulture, (object) str));
                  this.SetPropertyPresence(reader, closestMatchProperty, requiredProperties);
                  if (!this.SetPropertyValue(closestMatchProperty, converter, (JsonContainerContract) contract, member,
                    reader, newObject)) {
                    this.SetExtensionData(contract, member, reader, str, newObject);
                    goto case JsonToken.Comment;
                  } else
                    goto case JsonToken.Comment;
                }
              } catch (Exception ex) {
                if (this.IsErrorHandled(newObject, (JsonContract) contract, (object) str, reader as IJsonLineInfo,
                  reader.Path, ex)) {
                  this.HandleError(reader, true, depth - 1);
                  goto case JsonToken.Comment;
                } else
                  throw;
              }
            } else
              goto case JsonToken.Comment;
          case JsonToken.Comment:
            continue;
          case JsonToken.EndObject:
            flag = true;
            goto case JsonToken.Comment;
          default:
            throw JsonSerializationException.Create(reader,
              "Unexpected token when deserializing object: " + (object) reader.TokenType);
        }
      } while (!flag && reader.Read());

      if (!flag)
        this.ThrowUnexpectedEndException(reader, (JsonContract) contract, newObject,
          "Unexpected end when deserializing object.");
      if (requiredProperties != null) {
        foreach (KeyValuePair<JsonProperty, JsonSerializerInternalReader.PropertyPresence> keyValuePair in
          requiredProperties) {
          JsonProperty key = keyValuePair.Key;
          JsonSerializerInternalReader.PropertyPresence presence = keyValuePair.Value;
          this.EndProcessProperty(newObject, reader, contract, depth, key, presence, true);
        }
      }

      this.OnDeserialized(reader, (JsonContract) contract, newObject);
      return newObject;
    }

    private bool ShouldDeserialize(JsonReader reader, JsonProperty property, object target) {
      if (property.ShouldDeserialize == null)
        return true;
      bool flag = property.ShouldDeserialize(target);
      if (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Verbose)
        this.TraceWriter.Trace_(TraceLevel.Verbose,
          JsonPosition.FormatMessage((IJsonLineInfo) null, reader.Path,
            "ShouldDeserialize result for property '{0}' on {1}: {2}".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) property.PropertyName,
              (object) property.DeclaringType, (object) flag)), (Exception) null);
      return flag;
    }

    private bool CheckPropertyName(JsonReader reader, string memberName) {
      if (this.Serializer.MetadataPropertyHandling != MetadataPropertyHandling.ReadAhead || !(memberName == "$id") &&
          !(memberName == "$ref") && (!(memberName == "$type") && !(memberName == "$values")))
        return false;
      reader.Skip();
      return true;
    }

    private void SetExtensionData(
      JsonObjectContract contract,
      JsonProperty member,
      JsonReader reader,
      string memberName,
      object o) {
      if (contract.ExtensionDataSetter != null) {
        try {
          object obj = this.ReadExtensionDataValue(contract, member, reader);
          contract.ExtensionDataSetter(o, memberName, obj);
        } catch (Exception ex) {
          throw JsonSerializationException.Create(reader,
            "Error setting value in extension data for type '{0}'.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) contract.UnderlyingType), ex);
        }
      } else
        reader.Skip();
    }

    private object ReadExtensionDataValue(
      JsonObjectContract contract,
      JsonProperty member,
      JsonReader reader) {
      return !contract.ExtensionDataIsJToken
        ? this.CreateValueInternal(reader, (Type) null, (JsonContract) null, (JsonProperty) null,
          (JsonContainerContract) contract, member, (object) null)
        : (object) JToken.ReadFrom(reader);
    }

    private void EndProcessProperty(
      object newObject,
      JsonReader reader,
      JsonObjectContract contract,
      int initialDepth,
      JsonProperty property,
      JsonSerializerInternalReader.PropertyPresence presence,
      bool setDefaultValue) {
      if (presence != JsonSerializerInternalReader.PropertyPresence.None &&
          presence != JsonSerializerInternalReader.PropertyPresence.Null)
        return;
      try {
        Required? required1 = property._required;
        int num;
        if (!required1.HasValue) {
          Required? itemRequired = contract.ItemRequired;
          num = itemRequired.HasValue ? (int) itemRequired.GetValueOrDefault() : 0;
        } else
          num = (int) required1.GetValueOrDefault();

        Required required2 = (Required) num;
        switch (presence) {
          case JsonSerializerInternalReader.PropertyPresence.None:
            if (required2 == Required.AllowNull || required2 == Required.Always)
              throw JsonSerializationException.Create(reader,
                "Required property '{0}' not found in JSON.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                  (object) property.PropertyName));
            if (!setDefaultValue || property.Ignored)
              break;
            if (property.PropertyContract == null)
              property.PropertyContract = this.GetContractSafe(property.PropertyType);
            if (!this.HasFlag(property.DefaultValueHandling.GetValueOrDefault(this.Serializer._defaultValueHandling),
                  DefaultValueHandling.Populate) || !property.Writable)
              break;
            property.ValueProvider.SetValue(newObject,
              this.EnsureType(reader, property.GetResolvedDefaultValue(), CultureInfo.InvariantCulture,
                property.PropertyContract, property.PropertyType));
            break;
          case JsonSerializerInternalReader.PropertyPresence.Null:
            if (required2 == Required.Always)
              throw JsonSerializationException.Create(reader,
                "Required property '{0}' expects a value but got null.".FormatWith(
                  (IFormatProvider) CultureInfo.InvariantCulture, (object) property.PropertyName));
            if (required2 != Required.DisallowNull)
              break;
            throw JsonSerializationException.Create(reader,
              "Required property '{0}' expects a non-null value.".FormatWith(
                (IFormatProvider) CultureInfo.InvariantCulture, (object) property.PropertyName));
        }
      } catch (Exception ex) {
        if (this.IsErrorHandled(newObject, (JsonContract) contract, (object) property.PropertyName,
          reader as IJsonLineInfo, reader.Path, ex))
          this.HandleError(reader, true, initialDepth);
        else
          throw;
      }
    }

    private void SetPropertyPresence(
      JsonReader reader,
      JsonProperty property,
      Dictionary<JsonProperty, JsonSerializerInternalReader.PropertyPresence> requiredProperties) {
      if (property == null || requiredProperties == null)
        return;
      JsonSerializerInternalReader.PropertyPresence propertyPresence;
      switch (reader.TokenType) {
        case JsonToken.String:
          propertyPresence =
            JsonSerializerInternalReader.CoerceEmptyStringToNull(property.PropertyType, property.PropertyContract,
              (string) reader.Value)
              ? JsonSerializerInternalReader.PropertyPresence.Null
              : JsonSerializerInternalReader.PropertyPresence.Value;
          break;
        case JsonToken.Null:
        case JsonToken.Undefined:
          propertyPresence = JsonSerializerInternalReader.PropertyPresence.Null;
          break;
        default:
          propertyPresence = JsonSerializerInternalReader.PropertyPresence.Value;
          break;
      }

      requiredProperties[property] = propertyPresence;
    }

    private void HandleError(JsonReader reader, bool readPastError, int initialDepth) {
      this.ClearErrorContext();
      if (!readPastError)
        return;
      reader.Skip();
      do
        ;
      while (reader.Depth > initialDepth + 1 && reader.Read());
    }

    internal enum PropertyPresence {
      None,
      Null,
      Value,
    }

    internal class CreatorPropertyContext {
      public string Name;
      public JsonProperty Property;
      public JsonProperty ConstructorProperty;
      public JsonSerializerInternalReader.PropertyPresence? Presence;
      public object Value;
      public bool Used;
    }
  }
}