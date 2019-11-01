using System;
using Socket.WebSocket4Net.Default;
using Socket.WebSocket4Net.SuperSocket.ClientEngine.Protocol;

namespace Socket.WebSocket4Net.Protocol {
  internal class DraftHybi00HandshakeReader : HandshakeReader {
    private int m_ReceivedChallengeLength = -1;
    private int m_ExpectedChallengeLength = 16;
    private byte[] m_Challenges = new byte[16];
    private WebSocketCommandInfo m_HandshakeCommand;

    public DraftHybi00HandshakeReader (WebSocket websocket)
      : base (websocket) { }

    private void SetDataReader () {
      this.NextCommandReader =
        (IClientCommandReader<WebSocketCommandInfo>) new DraftHybi00DataReader ((ReaderBase) this);
    }

    public override WebSocketCommandInfo GetCommandInfo (
      byte[] readBuffer,
      int offset,
      int length,
      out int left) {
      if (this.m_ReceivedChallengeLength < 0) {
        WebSocketCommandInfo commandInfo = base.GetCommandInfo (readBuffer, offset, length, out left);
        if (commandInfo == null)
          return (WebSocketCommandInfo) null;
        if (HandshakeReader.BadRequestCode.Equals (commandInfo.Key))
          return commandInfo;
        this.m_ReceivedChallengeLength = 0;
        this.m_HandshakeCommand = commandInfo;
        int srcOffset = offset + length - left;
        if (left < this.m_ExpectedChallengeLength) {
          if (left > 0) {
            Buffer.BlockCopy ((Array) readBuffer, srcOffset, (Array) this.m_Challenges, 0, left);
            this.m_ReceivedChallengeLength = left;
            left = 0;
          }

          return (WebSocketCommandInfo) null;
        }

        if (left == this.m_ExpectedChallengeLength) {
          Buffer.BlockCopy ((Array) readBuffer, srcOffset, (Array) this.m_Challenges, 0, left);
          this.SetDataReader ();
          this.m_HandshakeCommand.Data = this.m_Challenges;
          left = 0;
          return this.m_HandshakeCommand;
        }

        Buffer.BlockCopy ((Array) readBuffer, srcOffset, (Array) this.m_Challenges, 0, this.m_ExpectedChallengeLength);
        left -= this.m_ExpectedChallengeLength;
        this.SetDataReader ();
        this.m_HandshakeCommand.Data = this.m_Challenges;
        return this.m_HandshakeCommand;
      }

      int num = this.m_ReceivedChallengeLength + length;
      if (num < this.m_ExpectedChallengeLength) {
        Buffer.BlockCopy ((Array) readBuffer, offset, (Array) this.m_Challenges, this.m_ReceivedChallengeLength,
          length);
        left = 0;
        this.m_ReceivedChallengeLength = num;
        return (WebSocketCommandInfo) null;
      }

      if (num == this.m_ExpectedChallengeLength) {
        Buffer.BlockCopy ((Array) readBuffer, offset, (Array) this.m_Challenges, this.m_ReceivedChallengeLength,
          length);
        left = 0;
        this.SetDataReader ();
        this.m_HandshakeCommand.Data = this.m_Challenges;
        return this.m_HandshakeCommand;
      }

      int count = this.m_ExpectedChallengeLength - this.m_ReceivedChallengeLength;
      Buffer.BlockCopy ((Array) readBuffer, offset, (Array) this.m_Challenges, this.m_ReceivedChallengeLength, count);
      left = length - count;
      this.SetDataReader ();
      this.m_HandshakeCommand.Data = this.m_Challenges;
      return this.m_HandshakeCommand;
    }
  }
}