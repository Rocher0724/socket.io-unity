namespace Socket.Newtonsoft.Json.Bson {
  internal class BsonString : BsonValue {
    public int ByteCount { get; set; }

    public bool IncludeLength { get; }

    public BsonString(object value, bool includeLength)
      : base(value, BsonType.String) {
      this.IncludeLength = includeLength;
    }
  }
}