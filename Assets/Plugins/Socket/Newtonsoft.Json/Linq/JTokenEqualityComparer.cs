using System.Collections.Generic;

namespace Socket.Newtonsoft.Json.Linq {
  public class JTokenEqualityComparer : IEqualityComparer<JToken> {
    public bool Equals(JToken x, JToken y) {
      return JToken.DeepEquals(x, y);
    }

    public int GetHashCode(JToken obj) {
      if (obj == null)
        return 0;
      return obj.GetDeepHashCode();
    }
  }
}