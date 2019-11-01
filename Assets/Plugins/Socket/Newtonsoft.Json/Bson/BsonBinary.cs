namespace Socket.Newtonsoft.Json.Bson {
  internal class BsonBinary : BsonValue
  {
    public BsonBinaryType BinaryType { get; set; }

    public BsonBinary(byte[] value, BsonBinaryType binaryType)
      : base((object) value, BsonType.Binary)
    {
      this.BinaryType = binaryType;
    }
  }
}
