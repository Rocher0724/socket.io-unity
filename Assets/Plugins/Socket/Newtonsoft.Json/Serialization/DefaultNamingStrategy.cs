namespace Socket.Newtonsoft.Json.Serialization {
  public class DefaultNamingStrategy : NamingStrategy {
    protected override string ResolvePropertyName(string name) {
      return name;
    }
  }
}