namespace Socket.Quobject.SocketIoClientDotNet.Parser {
  public class Packet<T> {
    public int Type = -1;
    public int Id = -1;
    public string Nsp;
    public T Data;
    public int Attachments;

    public Packet() {
    }

    public Packet(int type) {
      this.Type = type;
    }

    public Packet(int type, T data) {
      this.Type = type;
      this.Data = data;
    }
  }
}