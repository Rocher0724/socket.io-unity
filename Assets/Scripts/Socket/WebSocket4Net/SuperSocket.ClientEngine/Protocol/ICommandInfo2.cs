namespace Socket.WebSocket4Net.SuperSocket.ClientEngine.Protocol {
  public interface ICommandInfo<TCommandData> : ICommandInfo {
    TCommandData Data { get; }
  }
}