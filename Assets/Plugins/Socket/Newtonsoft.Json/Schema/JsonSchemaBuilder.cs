using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Socket.Newtonsoft.Json.Linq;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Schema {
  [Obsolete(
    "JSON Schema validation has been moved to its own package. See http://www.newtonsoft.com/jsonschema for more details.")]
  internal class JsonSchemaBuilder {
    private readonly IList<JsonSchema> _stack;
    private readonly JsonSchemaResolver _resolver;
    private readonly IDictionary<string, JsonSchema> _documentSchemas;
    private JsonSchema _currentSchema;
    private JObject _rootSchema;

    public JsonSchemaBuilder(JsonSchemaResolver resolver) {
      this._stack = (IList<JsonSchema>) new List<JsonSchema>();
      this._documentSchemas = (IDictionary<string, JsonSchema>) new Dictionary<string, JsonSchema>();
      this._resolver = resolver;
    }

    private void Push(JsonSchema value) {
      this._currentSchema = value;
      this._stack.Add(value);
      this._resolver.LoadedSchemas.Add(value);
      this._documentSchemas.Add(value.Location, value);
    }

    private JsonSchema Pop() {
      JsonSchema currentSchema = this._currentSchema;
      this._stack.RemoveAt(this._stack.Count - 1);
      this._currentSchema = this._stack.LastOrDefault<JsonSchema>();
      return currentSchema;
    }

    private JsonSchema CurrentSchema {
      get { return this._currentSchema; }
    }

    internal JsonSchema Read(JsonReader reader) {
      JToken token = JToken.ReadFrom(reader);
      this._rootSchema = token as JObject;
      JsonSchema schema = this.BuildSchema(token);
      this.ResolveReferences(schema);
      return schema;
    }

    private string UnescapeReference(string reference) {
      return Uri.UnescapeDataString(reference).Replace("~1", "/").Replace("~0", "~");
    }

    private JsonSchema ResolveReferences(JsonSchema schema) {
      if (schema.DeferredReference != null) {
        string reference1 = schema.DeferredReference;
        bool flag = reference1.StartsWith("#", StringComparison.Ordinal);
        if (flag)
          reference1 = this.UnescapeReference(reference1);
        JsonSchema jsonSchema = this._resolver.GetSchema(reference1);
        if (jsonSchema == null) {
          if (flag) {
            string[] strArray = schema.DeferredReference.TrimStart('#').Split(new char[1] {
              '/'
            }, StringSplitOptions.RemoveEmptyEntries);
            JToken jtoken = (JToken) this._rootSchema;
            foreach (string reference2 in strArray) {
              string s = this.UnescapeReference(reference2);
              if (jtoken.Type == JTokenType.Object)
                jtoken = jtoken[(object) s];
              else if (jtoken.Type == JTokenType.Array || jtoken.Type == JTokenType.Constructor) {
                int result;
                jtoken = !int.TryParse(s, out result) || result < 0 || result >= jtoken.Count<JToken>()
                  ? (JToken) null
                  : jtoken[(object) result];
              }

              if (jtoken == null)
                break;
            }

            if (jtoken != null)
              jsonSchema = this.BuildSchema(jtoken);
          }

          if (jsonSchema == null)
            throw new JsonException("Could not resolve schema reference '{0}'.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) schema.DeferredReference));
        }

        schema = jsonSchema;
      }

      if (schema.ReferencesResolved)
        return schema;
      schema.ReferencesResolved = true;
      if (schema.Extends != null) {
        for (int index = 0; index < schema.Extends.Count; ++index)
          schema.Extends[index] = this.ResolveReferences(schema.Extends[index]);
      }

      if (schema.Items != null) {
        for (int index = 0; index < schema.Items.Count; ++index)
          schema.Items[index] = this.ResolveReferences(schema.Items[index]);
      }

      if (schema.AdditionalItems != null)
        schema.AdditionalItems = this.ResolveReferences(schema.AdditionalItems);
      if (schema.PatternProperties != null) {
        foreach (KeyValuePair<string, JsonSchema> keyValuePair in schema.PatternProperties
          .ToList<KeyValuePair<string, JsonSchema>>())
          schema.PatternProperties[keyValuePair.Key] = this.ResolveReferences(keyValuePair.Value);
      }

      if (schema.Properties != null) {
        foreach (KeyValuePair<string, JsonSchema> keyValuePair in schema.Properties
          .ToList<KeyValuePair<string, JsonSchema>>())
          schema.Properties[keyValuePair.Key] = this.ResolveReferences(keyValuePair.Value);
      }

      if (schema.AdditionalProperties != null)
        schema.AdditionalProperties = this.ResolveReferences(schema.AdditionalProperties);
      return schema;
    }

    private JsonSchema BuildSchema(JToken token) {
      JObject schemaObject = token as JObject;
      if (schemaObject == null)
        throw JsonException.Create((IJsonLineInfo) token, token.Path,
          "Expected object while parsing schema object, got {0}.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) token.Type));
      JToken jtoken;
      if (schemaObject.TryGetValue("$ref", out jtoken))
        return new JsonSchema() {
          DeferredReference = (string) jtoken
        };
      string str = token.Path.Replace(".", "/").Replace("[", "/").Replace("]", string.Empty);
      if (!string.IsNullOrEmpty(str))
        str = "/" + str;
      string key = "#" + str;
      JsonSchema jsonSchema;
      if (this._documentSchemas.TryGetValue(key, out jsonSchema))
        return jsonSchema;
      this.Push(new JsonSchema() {Location = key});
      this.ProcessSchemaProperties(schemaObject);
      return this.Pop();
    }

    private void ProcessSchemaProperties(JObject schemaObject) {
      foreach (KeyValuePair<string, JToken> keyValuePair in schemaObject) {
        switch (keyValuePair.Key) {
          case "additionalItems":
            this.ProcessAdditionalItems(keyValuePair.Value);
            continue;
          case "additionalProperties":
            this.ProcessAdditionalProperties(keyValuePair.Value);
            continue;
          case "default":
            this.CurrentSchema.Default = keyValuePair.Value.DeepClone();
            continue;
          case "description":
            this.CurrentSchema.Description = (string) keyValuePair.Value;
            continue;
          case "disallow":
            this.CurrentSchema.Disallow = this.ProcessType(keyValuePair.Value);
            continue;
          case "divisibleBy":
            this.CurrentSchema.DivisibleBy = new double?((double) keyValuePair.Value);
            continue;
          case "enum":
            this.ProcessEnum(keyValuePair.Value);
            continue;
          case "exclusiveMaximum":
            this.CurrentSchema.ExclusiveMaximum = new bool?((bool) keyValuePair.Value);
            continue;
          case "exclusiveMinimum":
            this.CurrentSchema.ExclusiveMinimum = new bool?((bool) keyValuePair.Value);
            continue;
          case "extends":
            this.ProcessExtends(keyValuePair.Value);
            continue;
          case "format":
            this.CurrentSchema.Format = (string) keyValuePair.Value;
            continue;
          case "hidden":
            this.CurrentSchema.Hidden = new bool?((bool) keyValuePair.Value);
            continue;
          case "id":
            this.CurrentSchema.Id = (string) keyValuePair.Value;
            continue;
          case "items":
            this.ProcessItems(keyValuePair.Value);
            continue;
          case "maxItems":
            this.CurrentSchema.MaximumItems = new int?((int) keyValuePair.Value);
            continue;
          case "maxLength":
            this.CurrentSchema.MaximumLength = new int?((int) keyValuePair.Value);
            continue;
          case "maximum":
            this.CurrentSchema.Maximum = new double?((double) keyValuePair.Value);
            continue;
          case "minItems":
            this.CurrentSchema.MinimumItems = new int?((int) keyValuePair.Value);
            continue;
          case "minLength":
            this.CurrentSchema.MinimumLength = new int?((int) keyValuePair.Value);
            continue;
          case "minimum":
            this.CurrentSchema.Minimum = new double?((double) keyValuePair.Value);
            continue;
          case "pattern":
            this.CurrentSchema.Pattern = (string) keyValuePair.Value;
            continue;
          case "patternProperties":
            this.CurrentSchema.PatternProperties = this.ProcessProperties(keyValuePair.Value);
            continue;
          case "properties":
            this.CurrentSchema.Properties = this.ProcessProperties(keyValuePair.Value);
            continue;
          case "readonly":
            this.CurrentSchema.ReadOnly = new bool?((bool) keyValuePair.Value);
            continue;
          case "required":
            this.CurrentSchema.Required = new bool?((bool) keyValuePair.Value);
            continue;
          case "requires":
            this.CurrentSchema.Requires = (string) keyValuePair.Value;
            continue;
          case "title":
            this.CurrentSchema.Title = (string) keyValuePair.Value;
            continue;
          case "type":
            this.CurrentSchema.Type = this.ProcessType(keyValuePair.Value);
            continue;
          case "uniqueItems":
            this.CurrentSchema.UniqueItems = (bool) keyValuePair.Value;
            continue;
          default:
            continue;
        }
      }
    }

    private void ProcessExtends(JToken token) {
      IList<JsonSchema> jsonSchemaList = (IList<JsonSchema>) new List<JsonSchema>();
      if (token.Type == JTokenType.Array) {
        foreach (JToken token1 in (IEnumerable<JToken>) token)
          jsonSchemaList.Add(this.BuildSchema(token1));
      } else {
        JsonSchema jsonSchema = this.BuildSchema(token);
        if (jsonSchema != null)
          jsonSchemaList.Add(jsonSchema);
      }

      if (jsonSchemaList.Count <= 0)
        return;
      this.CurrentSchema.Extends = jsonSchemaList;
    }

    private void ProcessEnum(JToken token) {
      if (token.Type != JTokenType.Array)
        throw JsonException.Create((IJsonLineInfo) token, token.Path,
          "Expected Array token while parsing enum values, got {0}.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) token.Type));
      this.CurrentSchema.Enum = (IList<JToken>) new List<JToken>();
      foreach (JToken jtoken in (IEnumerable<JToken>) token)
        this.CurrentSchema.Enum.Add(jtoken.DeepClone());
    }

    private void ProcessAdditionalProperties(JToken token) {
      if (token.Type == JTokenType.Boolean)
        this.CurrentSchema.AllowAdditionalProperties = (bool) token;
      else
        this.CurrentSchema.AdditionalProperties = this.BuildSchema(token);
    }

    private void ProcessAdditionalItems(JToken token) {
      if (token.Type == JTokenType.Boolean)
        this.CurrentSchema.AllowAdditionalItems = (bool) token;
      else
        this.CurrentSchema.AdditionalItems = this.BuildSchema(token);
    }

    private IDictionary<string, JsonSchema> ProcessProperties(JToken token) {
      IDictionary<string, JsonSchema> dictionary =
        (IDictionary<string, JsonSchema>) new Dictionary<string, JsonSchema>();
      if (token.Type != JTokenType.Object)
        throw JsonException.Create((IJsonLineInfo) token, token.Path,
          "Expected Object token while parsing schema properties, got {0}.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) token.Type));
      foreach (JProperty jproperty in (IEnumerable<JToken>) token) {
        if (dictionary.ContainsKey(jproperty.Name))
          throw new JsonException(
            "Property {0} has already been defined in schema.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) jproperty.Name));
        dictionary.Add(jproperty.Name, this.BuildSchema(jproperty.Value));
      }

      return dictionary;
    }

    private void ProcessItems(JToken token) {
      this.CurrentSchema.Items = (IList<JsonSchema>) new List<JsonSchema>();
      switch (token.Type) {
        case JTokenType.Object:
          this.CurrentSchema.Items.Add(this.BuildSchema(token));
          this.CurrentSchema.PositionalItemsValidation = false;
          break;
        case JTokenType.Array:
          this.CurrentSchema.PositionalItemsValidation = true;
          using (IEnumerator<JToken> enumerator = ((IEnumerable<JToken>) token).GetEnumerator()) {
            while (enumerator.MoveNext())
              this.CurrentSchema.Items.Add(this.BuildSchema(enumerator.Current));
            break;
          }
        default:
          throw JsonException.Create((IJsonLineInfo) token, token.Path,
            "Expected array or JSON schema object, got {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) token.Type));
      }
    }

    private JsonSchemaType? ProcessType(JToken token) {
      switch (token.Type) {
        case JTokenType.Array:
          JsonSchemaType? nullable1 = new JsonSchemaType?(JsonSchemaType.None);
          foreach (JToken jtoken in (IEnumerable<JToken>) token) {
            if (jtoken.Type != JTokenType.String)
              throw JsonException.Create((IJsonLineInfo) jtoken, jtoken.Path,
                "Expected JSON schema type string token, got {0}.".FormatWith(
                  (IFormatProvider) CultureInfo.InvariantCulture, (object) token.Type));
            JsonSchemaType? nullable2 = nullable1;
            JsonSchemaType jsonSchemaType = JsonSchemaBuilder.MapType((string) jtoken);
            nullable1 = nullable2.HasValue
              ? new JsonSchemaType?(nullable2.GetValueOrDefault() | jsonSchemaType)
              : new JsonSchemaType?();
          }

          return nullable1;
        case JTokenType.String:
          return new JsonSchemaType?(JsonSchemaBuilder.MapType((string) token));
        default:
          throw JsonException.Create((IJsonLineInfo) token, token.Path,
            "Expected array or JSON schema type string token, got {0}.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) token.Type));
      }
    }

    internal static JsonSchemaType MapType(string type) {
      JsonSchemaType jsonSchemaType;
      if (!JsonSchemaConstants.JsonSchemaTypeMapping.TryGetValue(type, out jsonSchemaType))
        throw new JsonException(
          "Invalid JSON schema type: {0}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) type));
      return jsonSchemaType;
    }

    internal static string MapType(JsonSchemaType type) {
      return JsonSchemaConstants.JsonSchemaTypeMapping
        .Single<KeyValuePair<string, JsonSchemaType>>(
          (Func<KeyValuePair<string, JsonSchemaType>, bool>) (kv => kv.Value == type)).Key;
    }
  }
}