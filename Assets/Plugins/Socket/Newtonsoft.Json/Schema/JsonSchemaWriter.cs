using System;
using System.Collections.Generic;
using Socket.Newtonsoft.Json.Linq;
using Socket.Newtonsoft.Json.Utilities;
using Socket.Newtonsoft.Json.Utilities.LinqBridge;

namespace Socket.Newtonsoft.Json.Schema {
  [Obsolete(
    "JSON Schema validation has been moved to its own package. See http://www.newtonsoft.com/jsonschema for more details.")]
  internal class JsonSchemaWriter {
    private readonly JsonWriter _writer;
    private readonly JsonSchemaResolver _resolver;

    public JsonSchemaWriter(JsonWriter writer, JsonSchemaResolver resolver) {
      ValidationUtils.ArgumentNotNull((object) writer, nameof(writer));
      this._writer = writer;
      this._resolver = resolver;
    }

    private void ReferenceOrWriteSchema(JsonSchema schema) {
      if (schema.Id != null && this._resolver.GetSchema(schema.Id) != null) {
        this._writer.WriteStartObject();
        this._writer.WritePropertyName("$ref");
        this._writer.WriteValue(schema.Id);
        this._writer.WriteEndObject();
      } else
        this.WriteSchema(schema);
    }

    public void WriteSchema(JsonSchema schema) {
      ValidationUtils.ArgumentNotNull((object) schema, nameof(schema));
      if (!this._resolver.LoadedSchemas.Contains(schema))
        this._resolver.LoadedSchemas.Add(schema);
      this._writer.WriteStartObject();
      this.WritePropertyIfNotNull(this._writer, "id", (object) schema.Id);
      this.WritePropertyIfNotNull(this._writer, "title", (object) schema.Title);
      this.WritePropertyIfNotNull(this._writer, "description", (object) schema.Description);
      this.WritePropertyIfNotNull(this._writer, "required", (object) schema.Required);
      this.WritePropertyIfNotNull(this._writer, "readonly", (object) schema.ReadOnly);
      this.WritePropertyIfNotNull(this._writer, "hidden", (object) schema.Hidden);
      this.WritePropertyIfNotNull(this._writer, "transient", (object) schema.Transient);
      if (schema.Type.HasValue)
        this.WriteType("type", this._writer, schema.Type.GetValueOrDefault());
      if (!schema.AllowAdditionalProperties) {
        this._writer.WritePropertyName("additionalProperties");
        this._writer.WriteValue(schema.AllowAdditionalProperties);
      } else if (schema.AdditionalProperties != null) {
        this._writer.WritePropertyName("additionalProperties");
        this.ReferenceOrWriteSchema(schema.AdditionalProperties);
      }

      if (!schema.AllowAdditionalItems) {
        this._writer.WritePropertyName("additionalItems");
        this._writer.WriteValue(schema.AllowAdditionalItems);
      } else if (schema.AdditionalItems != null) {
        this._writer.WritePropertyName("additionalItems");
        this.ReferenceOrWriteSchema(schema.AdditionalItems);
      }

      this.WriteSchemaDictionaryIfNotNull(this._writer, "properties", schema.Properties);
      this.WriteSchemaDictionaryIfNotNull(this._writer, "patternProperties", schema.PatternProperties);
      this.WriteItems(schema);
      this.WritePropertyIfNotNull(this._writer, "minimum", (object) schema.Minimum);
      this.WritePropertyIfNotNull(this._writer, "maximum", (object) schema.Maximum);
      this.WritePropertyIfNotNull(this._writer, "exclusiveMinimum", (object) schema.ExclusiveMinimum);
      this.WritePropertyIfNotNull(this._writer, "exclusiveMaximum", (object) schema.ExclusiveMaximum);
      this.WritePropertyIfNotNull(this._writer, "minLength", (object) schema.MinimumLength);
      this.WritePropertyIfNotNull(this._writer, "maxLength", (object) schema.MaximumLength);
      this.WritePropertyIfNotNull(this._writer, "minItems", (object) schema.MinimumItems);
      this.WritePropertyIfNotNull(this._writer, "maxItems", (object) schema.MaximumItems);
      this.WritePropertyIfNotNull(this._writer, "divisibleBy", (object) schema.DivisibleBy);
      this.WritePropertyIfNotNull(this._writer, "format", (object) schema.Format);
      this.WritePropertyIfNotNull(this._writer, "pattern", (object) schema.Pattern);
      if (schema.Enum != null) {
        this._writer.WritePropertyName("enum");
        this._writer.WriteStartArray();
        foreach (JToken jtoken in (IEnumerable<JToken>) schema.Enum)
          jtoken.WriteTo(this._writer);
        this._writer.WriteEndArray();
      }

      if (schema.Default != null) {
        this._writer.WritePropertyName("default");
        schema.Default.WriteTo(this._writer);
      }

      JsonSchemaType? disallow = schema.Disallow;
      if (disallow.HasValue) {
        JsonWriter writer = this._writer;
        disallow = schema.Disallow;
        int valueOrDefault = (int) disallow.GetValueOrDefault();
        this.WriteType("disallow", writer, (JsonSchemaType) valueOrDefault);
      }

      if (schema.Extends != null && schema.Extends.Count > 0) {
        this._writer.WritePropertyName("extends");
        if (schema.Extends.Count == 1) {
          this.ReferenceOrWriteSchema(schema.Extends[0]);
        } else {
          this._writer.WriteStartArray();
          foreach (JsonSchema extend in (IEnumerable<JsonSchema>) schema.Extends)
            this.ReferenceOrWriteSchema(extend);
          this._writer.WriteEndArray();
        }
      }

      this._writer.WriteEndObject();
    }

    private void WriteSchemaDictionaryIfNotNull(
      JsonWriter writer,
      string propertyName,
      IDictionary<string, JsonSchema> properties) {
      if (properties == null)
        return;
      writer.WritePropertyName(propertyName);
      writer.WriteStartObject();
      foreach (KeyValuePair<string, JsonSchema> property in (IEnumerable<KeyValuePair<string, JsonSchema>>) properties
      ) {
        writer.WritePropertyName(property.Key);
        this.ReferenceOrWriteSchema(property.Value);
      }

      writer.WriteEndObject();
    }

    private void WriteItems(JsonSchema schema) {
      if (schema.Items == null && !schema.PositionalItemsValidation)
        return;
      this._writer.WritePropertyName("items");
      if (!schema.PositionalItemsValidation) {
        if (schema.Items != null && schema.Items.Count > 0) {
          this.ReferenceOrWriteSchema(schema.Items[0]);
        } else {
          this._writer.WriteStartObject();
          this._writer.WriteEndObject();
        }
      } else {
        this._writer.WriteStartArray();
        if (schema.Items != null) {
          foreach (JsonSchema schema1 in (IEnumerable<JsonSchema>) schema.Items)
            this.ReferenceOrWriteSchema(schema1);
        }

        this._writer.WriteEndArray();
      }
    }

    private void WriteType(string propertyName, JsonWriter writer, JsonSchemaType type) {
      if (Enum.IsDefined(typeof(JsonSchemaType), (object) type)) {
        writer.WritePropertyName(propertyName);
        writer.WriteValue(JsonSchemaBuilder.MapType(type));
      } else {
        IEnumerator<JsonSchemaType> enumerator = EnumUtils.GetFlagsValues<JsonSchemaType>(type)
          .Where<JsonSchemaType>((Func<JsonSchemaType, bool>) (v => (uint) v > 0U)).GetEnumerator();
        if (!enumerator.MoveNext())
          return;
        writer.WritePropertyName(propertyName);
        JsonSchemaType current = enumerator.Current;
        if (enumerator.MoveNext()) {
          writer.WriteStartArray();
          writer.WriteValue(JsonSchemaBuilder.MapType(current));
          do {
            writer.WriteValue(JsonSchemaBuilder.MapType(enumerator.Current));
          } while (enumerator.MoveNext());

          writer.WriteEndArray();
        } else
          writer.WriteValue(JsonSchemaBuilder.MapType(current));
      }
    }

    private void WritePropertyIfNotNull(JsonWriter writer, string propertyName, object value) {
      if (value == null)
        return;
      writer.WritePropertyName(propertyName);
      writer.WriteValue(value);
    }
  }
}