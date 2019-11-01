namespace Socket.Newtonsoft.Json.Bson {
  internal class BsonEmpty : BsonToken
  {
    public static readonly BsonToken Null = (BsonToken) new BsonEmpty(BsonType.Null);
    public static readonly BsonToken Undefined = (BsonToken) new BsonEmpty(BsonType.Undefined);

    private BsonEmpty(BsonType type)
    {
      this.Type = type;
    }

    public override BsonType Type { get; }
  }
}
