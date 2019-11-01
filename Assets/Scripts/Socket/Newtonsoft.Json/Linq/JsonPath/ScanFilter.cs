using System.Collections.Generic;

namespace Socket.Newtonsoft.Json.Linq.JsonPath {
  internal class ScanFilter : PathFilter
  {
    public string Name { get; set; }

    public override IEnumerable<JToken> ExecuteFilter(
      JToken root,
      IEnumerable<JToken> current,
      bool errorWhenNoMatch)
    {
      foreach (JToken jtoken1 in current)
      {
        JToken c = jtoken1;
        if (this.Name == null)
          yield return c;
        JToken value = c;
        JToken jtoken2 = c;
        while (true)
        {
          if (jtoken2 != null && jtoken2.HasValues)
          {
            value = jtoken2.First;
          }
          else
          {
            while (value != null && value != c && value == value.Parent.Last)
              value = (JToken) value.Parent;
            if (value != null && value != c)
              value = value.Next;
            else
              break;
          }
          JProperty jproperty = value as JProperty;
          if (jproperty != null)
          {
            if (jproperty.Name == this.Name)
              yield return jproperty.Value;
          }
          else if (this.Name == null)
            yield return value;
          jtoken2 = (JToken) (value as JContainer);
        }
        value = (JToken) null;
        c = (JToken) null;
      }
    }
  }
}
