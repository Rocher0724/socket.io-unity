using System.Collections.Generic;
using Socket.Newtonsoft.Json.Linq;

namespace Socket.Quobject.SocketIoClientDotNet.Parser {
  public class Packet {
    public int Type = -1;
    public int Id = -1;
    public string Nsp;
    public object Data;
    public int Attachments;

    public Packet() {
    }

    public Packet(int type)
      : this(type, (object) JToken.Parse("{}")) {
    }

    public Packet(int type, object data) {
      this.Type = type;
      this.Data = data;
    }

    public override string ToString() {
      return string.Format("Type:{0} Id:{1} Nsp:{2} Data:{3} Attachments:{4}", (object) this.Type, (object) this.Id,
        (object) this.Nsp, this.Data, (object) this.Attachments);
    }

    public List<object> GetDataAsList() {
      JArray jarray = this.Data is JArray ? (JArray) this.Data : JArray.Parse((string) ((JValue) this.Data).Value);
      List<object> objectList = new List<object>();
      foreach (JToken jtoken1 in jarray) {
        if (jtoken1 is JValue) {
          JValue jvalue = (JValue) jtoken1;
          if (jvalue != null)
            objectList.Add(jvalue.Value);
        } else if (jtoken1 != null) {
          JToken jtoken2 = jtoken1;
          if (jtoken2 != null)
            objectList.Add((object) jtoken2);
        }
      }

      return objectList;
    }

    public static JArray Args2JArray(IEnumerable<object> _args) {
      JArray jarray = new JArray();
      foreach (object content in _args)
        jarray.Add(content);
      return jarray;
    }

    public static JArray Remove(JArray a, int pos) {
      JArray jarray = new JArray();
      for (int index = 0; index < a.Count; ++index) {
        if (index != pos) {
          JToken jtoken = a[index];
          jarray.Add(jtoken);
        }
      }

      return jarray;
    }
  }
}