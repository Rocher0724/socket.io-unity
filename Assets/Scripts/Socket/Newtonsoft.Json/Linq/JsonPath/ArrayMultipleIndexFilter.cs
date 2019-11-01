using System.Collections.Generic;

namespace Socket.Newtonsoft.Json.Linq.JsonPath {
  internal class ArrayMultipleIndexFilter : PathFilter {
    public List<int> Indexes { get; set; }

    public override IEnumerable<JToken> ExecuteFilter(
      JToken root,
      IEnumerable<JToken> current,
      bool errorWhenNoMatch) {
      foreach (JToken jtoken in current) {
        JToken t = jtoken;
        foreach (int index in this.Indexes) {
          JToken tokenIndex = PathFilter.GetTokenIndex(t, errorWhenNoMatch, index);
          if (tokenIndex != null)
            yield return tokenIndex;
        }

        t = (JToken) null;
      }
    }
  }
}