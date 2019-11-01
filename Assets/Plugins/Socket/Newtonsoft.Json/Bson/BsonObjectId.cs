using System;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Bson {
  [Obsolete("BSON reading and writing has been moved to its own package. See https://www.nuget.org/packages/Newtonsoft.Json.Bson for more details.")]
  public class BsonObjectId
  {
    public byte[] Value { get; }

    public BsonObjectId(byte[] value)
    {
      ValidationUtils.ArgumentNotNull((object) value, nameof (value));
      if (value.Length != 12)
        throw new ArgumentException("An ObjectId must be 12 bytes", nameof (value));
      this.Value = value;
    }
  }
}
