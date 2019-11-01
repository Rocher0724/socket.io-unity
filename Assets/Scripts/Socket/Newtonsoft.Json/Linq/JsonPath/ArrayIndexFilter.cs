using System;
using System.Collections.Generic;
using System.Globalization;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Linq.JsonPath {
  internal class ArrayIndexFilter : PathFilter {
    public int? Index { get; set; }

    public override IEnumerable<JToken> ExecuteFilter(
      JToken root,
      IEnumerable<JToken> current,
      bool errorWhenNoMatch) {
      foreach (JToken jtoken1 in current) {
        JToken t = jtoken1;
        if (this.Index.HasValue) {
          JToken tokenIndex = PathFilter.GetTokenIndex(t, errorWhenNoMatch, this.Index.GetValueOrDefault());
          if (tokenIndex != null)
            yield return tokenIndex;
        } else if (t is JArray || t is JConstructor) {
          foreach (JToken jtoken2 in (IEnumerable<JToken>) t)
            yield return jtoken2;
        } else if (errorWhenNoMatch)
          throw new JsonException("Index * not valid on {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
            (object) t.GetType().Name));

        t = (JToken) null;
      }
    }
  }
}