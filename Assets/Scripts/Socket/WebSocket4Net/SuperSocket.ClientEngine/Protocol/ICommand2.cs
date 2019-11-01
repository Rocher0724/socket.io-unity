namespace Socket.WebSocket4Net.SuperSocket.ClientEngine.Protocol {
  public interface ICommand<TSession, TCommandInfo> : ICommand where TCommandInfo : ICommandInfo {
    void ExecuteCommand(TSession session, TCommandInfo commandInfo);
  }
}