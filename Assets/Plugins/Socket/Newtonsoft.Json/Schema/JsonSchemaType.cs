using System;

namespace Socket.Newtonsoft.Json.Schema {
  [Flags]
  [Obsolete("JSON Schema validation has been moved to its own package. See http://www.newtonsoft.com/jsonschema for more details.")]
  public enum JsonSchemaType
  {
    None = 0,
    String = 1,
    Float = 2,
    Integer = 4,
    Boolean = 8,
    Object = 16, // 0x00000010
    Array = 32, // 0x00000020
    Null = 64, // 0x00000040
    Any = Null | Array | Object | Boolean | Integer | Float | String, // 0x0000007F
  }
}
