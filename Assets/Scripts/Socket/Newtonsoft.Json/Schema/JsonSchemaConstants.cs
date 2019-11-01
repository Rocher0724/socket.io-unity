using System;
using System.Collections.Generic;

namespace Socket.Newtonsoft.Json.Schema {
  [Obsolete(
    "JSON Schema validation has been moved to its own package. See http://www.newtonsoft.com/jsonschema for more details.")]
  internal static class JsonSchemaConstants {
    public static readonly IDictionary<string, JsonSchemaType> JsonSchemaTypeMapping =
      (IDictionary<string, JsonSchemaType>) new Dictionary<string, JsonSchemaType>() {
        {
          "string",
          JsonSchemaType.String
        }, {
          "object",
          JsonSchemaType.Object
        }, {
          "integer",
          JsonSchemaType.Integer
        }, {
          "number",
          JsonSchemaType.Float
        }, {
          "null",
          JsonSchemaType.Null
        }, {
          "boolean",
          JsonSchemaType.Boolean
        }, {
          "array",
          JsonSchemaType.Array
        }, {
          "any",
          JsonSchemaType.Any
        }
      };

    public const string TypePropertyName = "type";
    public const string PropertiesPropertyName = "properties";
    public const string ItemsPropertyName = "items";
    public const string AdditionalItemsPropertyName = "additionalItems";
    public const string RequiredPropertyName = "required";
    public const string PatternPropertiesPropertyName = "patternProperties";
    public const string AdditionalPropertiesPropertyName = "additionalProperties";
    public const string RequiresPropertyName = "requires";
    public const string MinimumPropertyName = "minimum";
    public const string MaximumPropertyName = "maximum";
    public const string ExclusiveMinimumPropertyName = "exclusiveMinimum";
    public const string ExclusiveMaximumPropertyName = "exclusiveMaximum";
    public const string MinimumItemsPropertyName = "minItems";
    public const string MaximumItemsPropertyName = "maxItems";
    public const string PatternPropertyName = "pattern";
    public const string MaximumLengthPropertyName = "maxLength";
    public const string MinimumLengthPropertyName = "minLength";
    public const string EnumPropertyName = "enum";
    public const string ReadOnlyPropertyName = "readonly";
    public const string TitlePropertyName = "title";
    public const string DescriptionPropertyName = "description";
    public const string FormatPropertyName = "format";
    public const string DefaultPropertyName = "default";
    public const string TransientPropertyName = "transient";
    public const string DivisibleByPropertyName = "divisibleBy";
    public const string HiddenPropertyName = "hidden";
    public const string DisallowPropertyName = "disallow";
    public const string ExtendsPropertyName = "extends";
    public const string IdPropertyName = "id";
    public const string UniqueItemsPropertyName = "uniqueItems";
    public const string OptionValuePropertyName = "value";
    public const string OptionLabelPropertyName = "label";
  }
}