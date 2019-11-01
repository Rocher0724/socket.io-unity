namespace Socket.Newtonsoft.Json.Bson {
  internal class BsonBoolean : BsonValue
  {
    public static readonly BsonBoolean False = new BsonBoolean(false);
    public static readonly BsonBoolean True = new BsonBoolean(true);

    private BsonBoolean(bool value)
      : base((object) value, BsonType.Boolean)
    {
    }
  }
}
