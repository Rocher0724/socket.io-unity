using System;
using System.Collections.Generic;
using Socket.Newtonsoft.Json.Linq;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Schema {
  [Obsolete(
    "JSON Schema validation has been moved to its own package. See http://www.newtonsoft.com/jsonschema for more details.")]
  internal class JsonSchemaModel {
    public bool Required { get; set; }

    public JsonSchemaType Type { get; set; }

    public int? MinimumLength { get; set; }

    public int? MaximumLength { get; set; }

    public double? DivisibleBy { get; set; }

    public double? Minimum { get; set; }

    public double? Maximum { get; set; }

    public bool ExclusiveMinimum { get; set; }

    public bool ExclusiveMaximum { get; set; }

    public int? MinimumItems { get; set; }

    public int? MaximumItems { get; set; }

    public IList<string> Patterns { get; set; }

    public IList<JsonSchemaModel> Items { get; set; }

    public IDictionary<string, JsonSchemaModel> Properties { get; set; }

    public IDictionary<string, JsonSchemaModel> PatternProperties { get; set; }

    public JsonSchemaModel AdditionalProperties { get; set; }

    public JsonSchemaModel AdditionalItems { get; set; }

    public bool PositionalItemsValidation { get; set; }

    public bool AllowAdditionalProperties { get; set; }

    public bool AllowAdditionalItems { get; set; }

    public bool UniqueItems { get; set; }

    public IList<JToken> Enum { get; set; }

    public JsonSchemaType Disallow { get; set; }

    public JsonSchemaModel() {
      this.Type = JsonSchemaType.Any;
      this.AllowAdditionalProperties = true;
      this.AllowAdditionalItems = true;
      this.Required = false;
    }

    public static JsonSchemaModel Create(IList<JsonSchema> schemata) {
      JsonSchemaModel model = new JsonSchemaModel();
      foreach (JsonSchema schema in (IEnumerable<JsonSchema>) schemata)
        JsonSchemaModel.Combine(model, schema);
      return model;
    }

    private static void Combine(JsonSchemaModel model, JsonSchema schema) {
      JsonSchemaModel jsonSchemaModel1 = model;
      bool? nullable1;
      int num1;
      if (!model.Required) {
        nullable1 = schema.Required;
        num1 = nullable1.HasValue ? (nullable1.GetValueOrDefault() ? 1 : 0) : 0;
      } else
        num1 = 1;

      jsonSchemaModel1.Required = num1 != 0;
      JsonSchemaModel jsonSchemaModel2 = model;
      int type = (int) model.Type;
      JsonSchemaType? nullable2 = schema.Type;
      int num2 = nullable2.HasValue ? (int) nullable2.GetValueOrDefault() : (int) sbyte.MaxValue;
      int num3 = type & num2;
      jsonSchemaModel2.Type = (JsonSchemaType) num3;
      model.MinimumLength = MathUtils.Max(model.MinimumLength, schema.MinimumLength);
      model.MaximumLength = MathUtils.Min(model.MaximumLength, schema.MaximumLength);
      model.DivisibleBy = MathUtils.Max(model.DivisibleBy, schema.DivisibleBy);
      model.Minimum = MathUtils.Max(model.Minimum, schema.Minimum);
      model.Maximum = MathUtils.Max(model.Maximum, schema.Maximum);
      JsonSchemaModel jsonSchemaModel3 = model;
      int num4;
      if (!model.ExclusiveMinimum) {
        nullable1 = schema.ExclusiveMinimum;
        num4 = nullable1.HasValue ? (nullable1.GetValueOrDefault() ? 1 : 0) : 0;
      } else
        num4 = 1;

      jsonSchemaModel3.ExclusiveMinimum = num4 != 0;
      JsonSchemaModel jsonSchemaModel4 = model;
      int num5;
      if (!model.ExclusiveMaximum) {
        nullable1 = schema.ExclusiveMaximum;
        num5 = nullable1.HasValue ? (nullable1.GetValueOrDefault() ? 1 : 0) : 0;
      } else
        num5 = 1;

      jsonSchemaModel4.ExclusiveMaximum = num5 != 0;
      model.MinimumItems = MathUtils.Max(model.MinimumItems, schema.MinimumItems);
      model.MaximumItems = MathUtils.Min(model.MaximumItems, schema.MaximumItems);
      model.PositionalItemsValidation = model.PositionalItemsValidation || schema.PositionalItemsValidation;
      model.AllowAdditionalProperties = model.AllowAdditionalProperties && schema.AllowAdditionalProperties;
      model.AllowAdditionalItems = model.AllowAdditionalItems && schema.AllowAdditionalItems;
      model.UniqueItems = model.UniqueItems || schema.UniqueItems;
      if (schema.Enum != null) {
        if (model.Enum == null)
          model.Enum = (IList<JToken>) new List<JToken>();
        model.Enum.AddRangeDistinct<JToken>((IEnumerable<JToken>) schema.Enum,
          (IEqualityComparer<JToken>) JToken.EqualityComparer);
      }

      JsonSchemaModel jsonSchemaModel5 = model;
      int disallow = (int) model.Disallow;
      nullable2 = schema.Disallow;
      int num6 = nullable2.HasValue ? (int) nullable2.GetValueOrDefault() : 0;
      int num7 = disallow | num6;
      jsonSchemaModel5.Disallow = (JsonSchemaType) num7;
      if (schema.Pattern == null)
        return;
      if (model.Patterns == null)
        model.Patterns = (IList<string>) new List<string>();
      model.Patterns.AddDistinct<string>(schema.Pattern);
    }
  }
}
