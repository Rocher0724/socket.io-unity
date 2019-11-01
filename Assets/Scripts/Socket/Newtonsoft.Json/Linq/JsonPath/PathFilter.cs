using System;
using System.Collections.Generic;
using System.Globalization;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Linq.JsonPath {
  internal abstract class PathFilter {
    public abstract IEnumerable<JToken> ExecuteFilter(
      JToken root,
      IEnumerable<JToken> current,
      bool errorWhenNoMatch);

    protected static JToken GetTokenIndex(JToken t, bool errorWhenNoMatch, int index) {
      JArray jarray = t as JArray;
      JConstructor jconstructor = t as JConstructor;
      if (jarray != null) {
        if (jarray.Count > index)
          return jarray[index];
        if (errorWhenNoMatch)
          throw new JsonException(
            "Index {0} outside the bounds of JArray.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) index));
        return (JToken) null;
      }

      if (jconstructor != null) {
        if (jconstructor.Count > index)
          return jconstructor[(object) index];
        if (errorWhenNoMatch)
          throw new JsonException(
            "Index {0} outside the bounds of JConstructor.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) index));
        return (JToken) null;
      }

      if (errorWhenNoMatch)
        throw new JsonException("Index {0} not valid on {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) index, (object) t.GetType().Name));
      return (JToken) null;
    }
  }
}