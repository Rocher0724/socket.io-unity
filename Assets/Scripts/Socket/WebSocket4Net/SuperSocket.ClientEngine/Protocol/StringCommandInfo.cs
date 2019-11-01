namespace Socket.WebSocket4Net.SuperSocket.ClientEngine.Protocol {
  public class StringCommandInfo : CommandInfo<string> {
    public StringCommandInfo(string key, string data, string[] parameters)
      : base(key, data) {
      this.Parameters = parameters;
    }

    public string[] Parameters { get; private set; }

    public string GetFirstParam() {
      if (this.Parameters.Length > 0)
        return this.Parameters[0];
      return string.Empty;
    }

    public string this[int index] {
      get { return this.Parameters[index]; }
    }
  }
}