using System;
using System.Collections.Generic;
using Socket.Newtonsoft.Json.Linq;

namespace Socket.Quobject.SocketIoClientDotNet.Parser {

  public class Binary {
    private static readonly string KEY_PLACEHOLDER = "_placeholder";
    private static readonly string KEY_NUM = "num";

    public static Binary.DeconstructedPacket DeconstructPacket (Packet packet) {
      List<byte[]> buffers = new List<byte[]> ();
      packet.Data = (object) Binary._deconstructPacket (packet.Data, buffers);
      packet.Attachments = buffers.Count;
      return new Binary.DeconstructedPacket () {
        Packet = packet,
        Buffers = buffers.ToArray ()
      };
    }

    private static JToken _deconstructPacket (object data, List<byte[]> buffers) {
      if (data == null)
        return (JToken) null;
      if (data is byte[]) {
        byte[] byteArray = (byte[]) data;
        return Binary.AddPlaceholder (buffers, byteArray);
      }

      if (data is JArray) {
        JArray jarray1 = new JArray ();
        JArray jarray2 = (JArray) data;
        int count = jarray2.Count;
        for (int index = 0; index < count; ++index) {
          try {
            jarray1.Add (Binary._deconstructPacket ((object) jarray2[index], buffers));
          }
          catch (Exception ex) {
            return (JToken) null;
          }
        }

        return (JToken) jarray1;
      }

      if (!(data is JToken))
        throw new NotImplementedException ();
      JToken jtoken = (JToken) data;
      if (jtoken.Type == JTokenType.String)
        return (JToken) jtoken.Value<string> ();
      if (jtoken.Type == JTokenType.Bytes) {
        byte[] byteArray = jtoken.Value<byte[]> ();
        return Binary.AddPlaceholder (buffers, byteArray);
      }

      if (jtoken.Type != JTokenType.Object)
        throw new NotImplementedException ();
      JObject jobject = new JObject ();
      foreach (JProperty property in ((JObject) jtoken).Properties ()) {
        try {
          jobject[property.Name] = Binary._deconstructPacket ((object) property.Value, buffers);
        }
        catch (Exception ex) {
          return (JToken) null;
        }
      }

      return (JToken) jobject;
    }

    private static JToken AddPlaceholder (List<byte[]> buffers, byte[] byteArray) {
      JObject jobject = new JObject ();
      try {
        jobject.Add (Binary.KEY_PLACEHOLDER, (JToken) true);
        jobject.Add (Binary.KEY_NUM, (JToken) buffers.Count);
      }
      catch (Exception ex) {
        return (JToken) null;
      }

      buffers.Add (byteArray);
      return (JToken) jobject;
    }

    public static Packet ReconstructPacket (Packet packet, byte[][] buffers) {
      packet.Data = Binary._reconstructPacket (packet.Data, buffers);
      packet.Attachments = -1;
      return packet;
    }

    private static object _reconstructPacket (object data, byte[][] buffers) {
      if (data is JValue) {
        string str = data.ToString ();
        if (!str.StartsWith ("[") && !str.StartsWith ("{"))
          return (object) str;
        JToken jtoken1 = JToken.Parse (data.ToString ());
        if (jtoken1.SelectToken (Binary.KEY_PLACEHOLDER) == null)
          return Binary._reconstructPacket ((object) jtoken1, buffers);
        JToken jtoken2 = jtoken1[(object) Binary.KEY_PLACEHOLDER];
        JToken jtoken3 = jtoken1[(object) Binary.KEY_NUM];
        if (jtoken2 != null && jtoken3 != null && jtoken2.ToObject<bool> ()) {
          int index = jtoken3.ToObject<int> ();
          return (object) buffers[index];
        }
      } else if (data is JArray) {
        JArray jarray1 = (JArray) data;
        int count = jarray1.Count;
        JArray jarray2 = new JArray ();
        for (int index = 0; index < count; ++index) {
          try {
            object obj = Binary._reconstructPacket ((object) jarray1[index], buffers);
            if (obj is string)
              jarray2.Add ((JToken) ((string) obj));
            else if (obj is byte[])
              jarray2.Add ((JToken) ((byte[]) obj));
            else if (obj is JArray)
              jarray2.Add ((JToken) obj);
            else if (obj is JObject)
              jarray2.Add ((JToken) obj);
          }
          catch (Exception ex) {
            return (object) null;
          }
        }

        return (object) jarray2;
      }

      if (!(data is JObject))
        return data;
      JObject jobject1 = new JObject ();
      JObject jobject2 = (JObject) data;
      if (jobject2.SelectToken (Binary.KEY_PLACEHOLDER) != null && (bool) jobject2[Binary.KEY_PLACEHOLDER]) {
        int index = (int) jobject2[Binary.KEY_NUM];
        return index < 0 || index >= buffers.Length ? (object) (byte[]) null : (object) buffers[index];
      }

      foreach (JProperty property in jobject2.Properties ()) {
        try {
          object obj = Binary._reconstructPacket ((object) property.Value, buffers);
          if (obj is string)
            jobject1[property.Name] = (JToken) ((string) obj);
          else if (obj is byte[])
            jobject1[property.Name] = (JToken) ((byte[]) obj);
          else if (obj is JArray)
            jobject1[property.Name] = (JToken) obj;
          else if (obj is JObject)
            jobject1[property.Name] = (JToken) obj;
        }
        catch (Exception ex) {
          return (object) null;
        }
      }

      return (object) jobject1;
    }

    public class DeconstructedPacket {
      public Packet Packet;
      public byte[][] Buffers;
    }
  }
}