using Socket.WebSocket4Net.Default;
using Socket.WebSocket4Net.SuperSocket.ClientEngine.Protocol;

namespace Socket.WebSocket4Net.Command {
  public abstract class WebSocketCommandBase : ICommand<WebSocket, WebSocketCommandInfo>, ICommand {
    public abstract void ExecuteCommand (WebSocket session, WebSocketCommandInfo commandInfo);

    public abstract string Name { get; }
  }
}