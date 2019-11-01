using System;
using System.Collections.Generic;
using System.Text;
using Socket.WebSocket4Net.Default;
using Socket.WebSocket4Net.SuperSocket.ClientEngine;

namespace Socket.WebSocket4Net.Protocol {
  internal class HandshakeReader : ReaderBase {
    protected static readonly string BadRequestCode = 400.ToString ();
    protected static readonly byte[] HeaderTerminator = Encoding.UTF8.GetBytes ("\r\n\r\n");
    private const string m_BadRequestPrefix = "HTTP/1.1 400 ";
    private SearchMarkState<byte> m_HeadSeachState;

    public HandshakeReader (WebSocket websocket)
      : base (websocket) {
      this.m_HeadSeachState = new SearchMarkState<byte> (HandshakeReader.HeaderTerminator);
    }

    protected static WebSocketCommandInfo DefaultHandshakeCommandInfo { get; private set; }

    public override WebSocketCommandInfo GetCommandInfo (
      byte[] readBuffer,
      int offset,
      int length,
      out int left) {
      left = 0;
      int matched = this.m_HeadSeachState.Matched;
      int num1 = SuperSocket.ClientEngine.Extensions.SearchMark<byte> ((IList<byte>) readBuffer, offset, length,
        this.m_HeadSeachState);
      if (num1 < 0) {
        this.AddArraySegment (readBuffer, offset, length);
        return (WebSocketCommandInfo) null;
      }

      int num2 = num1 - offset;
      string empty = string.Empty;
      string str;
      if (this.BufferSegments.Count > 0) {
        if (num2 > 0) {
          this.AddArraySegment (readBuffer, offset, num2);
          str = this.BufferSegments.Decode (Encoding.UTF8);
        } else
          str = this.BufferSegments.Decode (Encoding.UTF8, 0, this.BufferSegments.Count - matched);
      } else
        str = Encoding.UTF8.GetString (readBuffer, offset, num2);

      left = length - num2 - (HandshakeReader.HeaderTerminator.Length - matched);
      this.BufferSegments.ClearSegements ();
      if (!str.StartsWith ("HTTP/1.1 400 ", StringComparison.OrdinalIgnoreCase))
        return new WebSocketCommandInfo () {
          Key = (-1).ToString (),
          Text = str
        };
      return new WebSocketCommandInfo () {
        Key = 400.ToString (),
        Text = str
      };
    }
  }
}