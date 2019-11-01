namespace Socket.Newtonsoft.Json.Bson {
  internal class BsonRegex : BsonToken {
    public BsonString Pattern { get; set; }

    public BsonString Options { get; set; }

    public BsonRegex(string pattern, string options) {
      this.Pattern = new BsonString((object) pattern, false);
      this.Options = new BsonString((object) options, false);
    }

    public override BsonType Type {
      get { return BsonType.Regex; }
    }
  }
}