using System.Collections.Generic;

namespace Socket.Newtonsoft.Json.Linq.JsonPath {
  internal class QueryFilter : PathFilter {
    public QueryExpression Expression { get; set; }

    public override IEnumerable<JToken> ExecuteFilter(
      JToken root,
      IEnumerable<JToken> current,
      bool errorWhenNoMatch) {
      foreach (IEnumerable<JToken> jtokens in current) {
        foreach (JToken t in jtokens) {
          if (this.Expression.IsMatch(root, t))
            yield return t;
        }
      }
    }
  }
}