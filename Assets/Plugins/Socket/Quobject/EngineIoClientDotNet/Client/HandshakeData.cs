using System.Collections.Generic;
using Socket.Newtonsoft.Json.Linq;
using Socket.Quobject.Collections.Immutable;

namespace Socket.Quobject.EngineIoClientDotNet.Client {
  public class HandshakeData {
    public ImmutableList<string> Upgrades = ImmutableList<string>.Empty;
    public string Sid;
    public long PingInterval;
    public long PingTimeout;

    public HandshakeData(string data)
      : this(JObject.Parse(data)) {
    }

    public HandshakeData(JObject data) {
      foreach (object obj in (IEnumerable<JToken>) data.GetValue("upgrades"))
        this.Upgrades = this.Upgrades.Add(obj.ToString());
      this.Sid = data.GetValue("sid").Value<string>();
      this.PingInterval = data.GetValue("pingInterval").Value<long>();
      this.PingTimeout = data.GetValue("pingTimeout").Value<long>();
    }
  }
}