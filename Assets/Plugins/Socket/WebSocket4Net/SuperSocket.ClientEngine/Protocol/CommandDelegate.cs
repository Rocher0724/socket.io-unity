namespace Socket.WebSocket4Net.SuperSocket.ClientEngine.Protocol {
  public delegate void CommandDelegate<TClientSession, TCommandInfo>(
    TClientSession session,
    TCommandInfo commandInfo);
}