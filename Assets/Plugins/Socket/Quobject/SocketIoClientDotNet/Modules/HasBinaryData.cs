using Socket.Newtonsoft.Json.Linq;

namespace Socket.Quobject.SocketIoClientDotNet.Modules {
  public static class HasBinaryData
  {
    public static bool HasBinary(object data)
    {
      return HasBinaryData.RecursiveCheckForBinary(data);
    }

    private static bool RecursiveCheckForBinary(object obj)
    {
      if (obj == null || obj is string)
        return false;
      if (obj is byte[])
        return true;
      JArray jarray = obj as JArray;
      if (jarray != null)
      {
        foreach (object obj1 in jarray)
        {
          if (HasBinaryData.RecursiveCheckForBinary(obj1))
            return true;
        }
      }
      JObject jobject = obj as JObject;
      if (jobject != null)
      {
        foreach (object child in jobject.Children())
        {
          if (HasBinaryData.RecursiveCheckForBinary(child))
            return true;
        }
      }
      JValue jvalue = obj as JValue;
      if (jvalue != null)
        return HasBinaryData.RecursiveCheckForBinary(jvalue.Value);
      JProperty jproperty = obj as JProperty;
      if (jproperty != null)
        return HasBinaryData.RecursiveCheckForBinary((object) jproperty.Value);
      return false;
    }
  }
}
