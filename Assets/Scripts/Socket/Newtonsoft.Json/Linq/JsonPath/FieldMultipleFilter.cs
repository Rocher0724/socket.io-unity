using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Linq.JsonPath {
  internal class FieldMultipleFilter : PathFilter {
    public List<string> Names { get; set; }

    public override IEnumerable<JToken> ExecuteFilter(
      JToken root,
      IEnumerable<JToken> current,
      bool errorWhenNoMatch) {
      foreach (JToken jtoken1 in current) {
        JToken t = jtoken1;
        JObject o = t as JObject;
        if (o != null) {
          foreach (string name1 in this.Names) {
            string name = name1;
            JToken jtoken2 = o[name];
            if (jtoken2 != null)
              yield return jtoken2;
            if (errorWhenNoMatch)
              throw new JsonException(
                "Property '{0}' does not exist on JObject.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                  (object) name));
            name = (string) null;
          }
        } else if (errorWhenNoMatch)
          throw new JsonException("Properties {0} not valid on {1}.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture,
            (object) string.Join(", ",
              this.Names.Select<string, string>((Func<string, string>) (n => "'" + n + "'")).ToArray<string>()),
            (object) t.GetType().Name));

        o = (JObject) null;
        t = (JToken) null;
      }
    }
  }
}