namespace Socket.Newtonsoft.Json.Linq.JsonPath {
  internal abstract class QueryExpression
  {
    public QueryOperator Operator { get; set; }

    public abstract bool IsMatch(JToken root, JToken t);
  }
}
