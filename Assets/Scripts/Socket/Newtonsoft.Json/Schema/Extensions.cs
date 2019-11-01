using System;
using System.Collections.Generic;
using Socket.Newtonsoft.Json.Linq;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Schema {
  [Obsolete(
    "JSON Schema validation has been moved to its own package. See http://www.newtonsoft.com/jsonschema for more details.")]
  public static class Extensions {
    [Obsolete(
      "JSON Schema validation has been moved to its own package. See http://www.newtonsoft.com/jsonschema for more details.")]
    public static bool IsValid(this JToken source, JsonSchema schema) {
      bool valid = true;
      source.Validate(schema, (ValidationEventHandler) ((sender, args) => valid = false));
      return valid;
    }

    [Obsolete(
      "JSON Schema validation has been moved to its own package. See http://www.newtonsoft.com/jsonschema for more details.")]
    public static bool IsValid(
      this JToken source,
      JsonSchema schema,
      out IList<string> errorMessages) {
      IList<string> errors = (IList<string>) new List<string>();
      source.Validate(schema, (ValidationEventHandler) ((sender, args) => errors.Add(args.Message)));
      errorMessages = errors;
      return errorMessages.Count == 0;
    }

    [Obsolete(
      "JSON Schema validation has been moved to its own package. See http://www.newtonsoft.com/jsonschema for more details.")]
    public static void Validate(this JToken source, JsonSchema schema) {
      source.Validate(schema, (ValidationEventHandler) null);
    }

    [Obsolete(
      "JSON Schema validation has been moved to its own package. See http://www.newtonsoft.com/jsonschema for more details.")]
    public static void Validate(
      this JToken source,
      JsonSchema schema,
      ValidationEventHandler validationEventHandler) {
      ValidationUtils.ArgumentNotNull((object) source, nameof(source));
      ValidationUtils.ArgumentNotNull((object) schema, nameof(schema));
      using (JsonValidatingReader validatingReader = new JsonValidatingReader(source.CreateReader())) {
        validatingReader.Schema = schema;
        if (validationEventHandler != null)
          validatingReader.ValidationEventHandler += validationEventHandler;
        do
          ;
        while (validatingReader.Read());
      }
    }
  }
}