namespace Socket.WebSocket4Net.Protocol {
  public class CloseStatusCodeHybi10 : ICloseStatusCode
  {
    public CloseStatusCodeHybi10()
    {
      this.NormalClosure = (short) 1000;
      this.GoingAway = (short) 1001;
      this.ProtocolError = (short) 1002;
      this.NotAcceptableData = (short) 1003;
      this.TooLargeFrame = (short) 1004;
      this.InvalidUTF8 = (short) 1007;
      this.ViolatePolicy = (short) 1000;
      this.ExtensionNotMatch = (short) 1000;
      this.UnexpectedCondition = (short) 1000;
      this.TLSHandshakeFailure = (short) 1000;
      this.NoStatusCode = (short) 1005;
    }

    public short NormalClosure { get; private set; }

    public short GoingAway { get; private set; }

    public short ProtocolError { get; private set; }

    public short NotAcceptableData { get; private set; }

    public short TooLargeFrame { get; private set; }

    public short InvalidUTF8 { get; private set; }

    public short ViolatePolicy { get; private set; }

    public short ExtensionNotMatch { get; private set; }

    public short UnexpectedCondition { get; private set; }

    public short TLSHandshakeFailure { get; private set; }

    public short NoStatusCode { get; private set; }
  }
}
