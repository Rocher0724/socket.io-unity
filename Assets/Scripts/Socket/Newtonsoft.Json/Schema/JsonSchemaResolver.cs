using System;
using System.Collections.Generic;
using System.Linq;

namespace Socket.Newtonsoft.Json.Schema {
  [Obsolete("JSON Schema validation has been moved to its own package. See http://www.newtonsoft.com/jsonschema for more details.")]
  public class JsonSchemaResolver
  {
    public IList<JsonSchema> LoadedSchemas { get; protected set; }

    public JsonSchemaResolver()
    {
      this.LoadedSchemas = (IList<JsonSchema>) new List<JsonSchema>();
    }

    public virtual JsonSchema GetSchema(string reference)
    {
      return this.LoadedSchemas.SingleOrDefault<JsonSchema>((Func<JsonSchema, bool>) (s => string.Equals(s.Id, reference, StringComparison.Ordinal))) ?? this.LoadedSchemas.SingleOrDefault<JsonSchema>((Func<JsonSchema, bool>) (s => string.Equals(s.Location, reference, StringComparison.Ordinal)));
    }
  }
}
