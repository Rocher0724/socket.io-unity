using System;
using System.Collections.Generic;
using System.Linq;

namespace Socket.Newtonsoft.Json.Schema {
[Obsolete("JSON Schema validation has been moved to its own package. See http://www.newtonsoft.com/jsonschema for more details.")]
  internal class JsonSchemaModelBuilder
  {
    private JsonSchemaNodeCollection _nodes = new JsonSchemaNodeCollection();
    private Dictionary<JsonSchemaNode, JsonSchemaModel> _nodeModels = new Dictionary<JsonSchemaNode, JsonSchemaModel>();
    private JsonSchemaNode _node;

    public JsonSchemaModel Build(JsonSchema schema)
    {
      this._nodes = new JsonSchemaNodeCollection();
      this._node = this.AddSchema((JsonSchemaNode) null, schema);
      this._nodeModels = new Dictionary<JsonSchemaNode, JsonSchemaModel>();
      return this.BuildNodeModel(this._node);
    }

    public JsonSchemaNode AddSchema(JsonSchemaNode existingNode, JsonSchema schema)
    {
      string id;
      if (existingNode != null)
      {
        if (existingNode.Schemas.Contains(schema))
          return existingNode;
        id = JsonSchemaNode.GetId(existingNode.Schemas.Union<JsonSchema>((IEnumerable<JsonSchema>) new JsonSchema[1]
        {
          schema
        }));
      }
      else
        id = JsonSchemaNode.GetId((IEnumerable<JsonSchema>) new JsonSchema[1]
        {
          schema
        });
      if (this._nodes.Contains(id))
        return this._nodes[id];
      JsonSchemaNode jsonSchemaNode = existingNode != null ? existingNode.Combine(schema) : new JsonSchemaNode(schema);
      this._nodes.Add(jsonSchemaNode);
      this.AddProperties(schema.Properties, (IDictionary<string, JsonSchemaNode>) jsonSchemaNode.Properties);
      this.AddProperties(schema.PatternProperties, (IDictionary<string, JsonSchemaNode>) jsonSchemaNode.PatternProperties);
      if (schema.Items != null)
      {
        for (int index = 0; index < schema.Items.Count; ++index)
          this.AddItem(jsonSchemaNode, index, schema.Items[index]);
      }
      if (schema.AdditionalItems != null)
        this.AddAdditionalItems(jsonSchemaNode, schema.AdditionalItems);
      if (schema.AdditionalProperties != null)
        this.AddAdditionalProperties(jsonSchemaNode, schema.AdditionalProperties);
      if (schema.Extends != null)
      {
        foreach (JsonSchema extend in (IEnumerable<JsonSchema>) schema.Extends)
          jsonSchemaNode = this.AddSchema(jsonSchemaNode, extend);
      }
      return jsonSchemaNode;
    }

    public void AddProperties(
      IDictionary<string, JsonSchema> source,
      IDictionary<string, JsonSchemaNode> target)
    {
      if (source == null)
        return;
      foreach (KeyValuePair<string, JsonSchema> keyValuePair in (IEnumerable<KeyValuePair<string, JsonSchema>>) source)
        this.AddProperty(target, keyValuePair.Key, keyValuePair.Value);
    }

    public void AddProperty(
      IDictionary<string, JsonSchemaNode> target,
      string propertyName,
      JsonSchema schema)
    {
      JsonSchemaNode existingNode;
      target.TryGetValue(propertyName, out existingNode);
      target[propertyName] = this.AddSchema(existingNode, schema);
    }

    public void AddItem(JsonSchemaNode parentNode, int index, JsonSchema schema)
    {
      JsonSchemaNode jsonSchemaNode = this.AddSchema(parentNode.Items.Count > index ? parentNode.Items[index] : (JsonSchemaNode) null, schema);
      if (parentNode.Items.Count <= index)
        parentNode.Items.Add(jsonSchemaNode);
      else
        parentNode.Items[index] = jsonSchemaNode;
    }

    public void AddAdditionalProperties(JsonSchemaNode parentNode, JsonSchema schema)
    {
      parentNode.AdditionalProperties = this.AddSchema(parentNode.AdditionalProperties, schema);
    }

    public void AddAdditionalItems(JsonSchemaNode parentNode, JsonSchema schema)
    {
      parentNode.AdditionalItems = this.AddSchema(parentNode.AdditionalItems, schema);
    }

    private JsonSchemaModel BuildNodeModel(JsonSchemaNode node)
    {
      JsonSchemaModel jsonSchemaModel1;
      if (this._nodeModels.TryGetValue(node, out jsonSchemaModel1))
        return jsonSchemaModel1;
      JsonSchemaModel jsonSchemaModel2 = JsonSchemaModel.Create((IList<JsonSchema>) node.Schemas);
      this._nodeModels[node] = jsonSchemaModel2;
      foreach (KeyValuePair<string, JsonSchemaNode> property in node.Properties)
      {
        if (jsonSchemaModel2.Properties == null)
          jsonSchemaModel2.Properties = (IDictionary<string, JsonSchemaModel>) new Dictionary<string, JsonSchemaModel>();
        jsonSchemaModel2.Properties[property.Key] = this.BuildNodeModel(property.Value);
      }
      foreach (KeyValuePair<string, JsonSchemaNode> patternProperty in node.PatternProperties)
      {
        if (jsonSchemaModel2.PatternProperties == null)
          jsonSchemaModel2.PatternProperties = (IDictionary<string, JsonSchemaModel>) new Dictionary<string, JsonSchemaModel>();
        jsonSchemaModel2.PatternProperties[patternProperty.Key] = this.BuildNodeModel(patternProperty.Value);
      }
      foreach (JsonSchemaNode node1 in node.Items)
      {
        if (jsonSchemaModel2.Items == null)
          jsonSchemaModel2.Items = (IList<JsonSchemaModel>) new List<JsonSchemaModel>();
        jsonSchemaModel2.Items.Add(this.BuildNodeModel(node1));
      }
      if (node.AdditionalProperties != null)
        jsonSchemaModel2.AdditionalProperties = this.BuildNodeModel(node.AdditionalProperties);
      if (node.AdditionalItems != null)
        jsonSchemaModel2.AdditionalItems = this.BuildNodeModel(node.AdditionalItems);
      return jsonSchemaModel2;
    }
  }
}
