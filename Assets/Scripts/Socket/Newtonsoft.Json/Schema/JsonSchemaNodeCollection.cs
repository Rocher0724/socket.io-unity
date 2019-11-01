using System;
using System.Collections.ObjectModel;

namespace Socket.Newtonsoft.Json.Schema {
  [Obsolete("JSON Schema validation has been moved to its own package. See http://www.newtonsoft.com/jsonschema for more details.")]
  internal class JsonSchemaNodeCollection : KeyedCollection<string, JsonSchemaNode>
  {
    protected override string GetKeyForItem(JsonSchemaNode item)
    {
      return item.Id;
    }
  }
}
