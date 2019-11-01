namespace Socket.WebSocket4Net.SuperSocket.ClientEngine.Protocol {
  internal class DelegateCommand<TClientSession, TCommandInfo> : ICommand<TClientSession, TCommandInfo>, ICommand
    where TClientSession : IClientSession
    where TCommandInfo : ICommandInfo {
    private CommandDelegate<TClientSession, TCommandInfo> m_Execution;

    public DelegateCommand(
      string name,
      CommandDelegate<TClientSession, TCommandInfo> execution) {
      this.Name = name;
      this.m_Execution = execution;
    }

    public void ExecuteCommand(TClientSession session, TCommandInfo commandInfo) {
      this.m_Execution(session, commandInfo);
    }

    public string Name { get; private set; }
  }
}