using System;
using System.Collections.Generic;
using System.Text;
using Socket.WebSocket4Net.Default;

namespace Socket.WebSocket4Net.Protocol {
  internal class DraftHybi10Processor : ProtocolProcessorBase {
    private static Random m_Random = new Random ();
    private string m_ExpectedAcceptKey = "ExpectedAccept";
    private readonly string m_OriginHeaderName = "Sec-WebSocket-Origin";
    private const string m_Magic = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";
    private const string m_Error_InvalidHandshake = "invalid handshake";
    private const string m_Error_SubProtocolNotMatch = "subprotocol doesn't match";
    private const string m_Error_AcceptKeyNotMatch = "accept key doesn't match";

    public DraftHybi10Processor ()
      : base (WebSocketVersion.DraftHybi10, (ICloseStatusCode) new CloseStatusCodeHybi10 ()) { }

    protected DraftHybi10Processor (
      WebSocketVersion version,
      ICloseStatusCode closeStatusCode,
      string originHeaderName)
      : base (version, closeStatusCode) {
      this.m_OriginHeaderName = originHeaderName;
    }

    public override void SendHandshake (WebSocket websocket) {
      string base64String =
        Convert.ToBase64String (Encoding.ASCII.GetBytes (Guid.NewGuid ().ToString ().Substring (0, 16)));
      string challenge = (base64String + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11").CalculateChallenge ();
      websocket.Items[this.m_ExpectedAcceptKey] = (object) challenge;
      StringBuilder builder = new StringBuilder ();
      if (websocket.HttpConnectProxy == null)
        builder.AppendFormatWithCrCf ("GET {0} HTTP/1.1", (object) websocket.TargetUri.PathAndQuery);
      else
        builder.AppendFormatWithCrCf ("GET {0} HTTP/1.1", (object) websocket.TargetUri.ToString ());
      builder.AppendWithCrCf ("Upgrade: WebSocket");
      builder.AppendWithCrCf ("Connection: Upgrade");
      builder.Append ("Sec-WebSocket-Version: ");
      builder.AppendWithCrCf (this.VersionTag);
      builder.Append ("Sec-WebSocket-Key: ");
      builder.AppendWithCrCf (base64String);
      builder.Append ("Host: ");
      builder.AppendWithCrCf (websocket.HandshakeHost);
      builder.Append (string.Format ("{0}: ", (object) this.m_OriginHeaderName));
      builder.AppendWithCrCf (websocket.Origin);
      if (!string.IsNullOrEmpty (websocket.SubProtocol)) {
        builder.Append ("Sec-WebSocket-Protocol: ");
        builder.AppendWithCrCf (websocket.SubProtocol);
      }

      List<KeyValuePair<string, string>> cookies = websocket.Cookies;
      if (cookies != null && cookies.Count > 0) {
        string[] strArray = new string[cookies.Count];
        for (int index = 0; index < cookies.Count; ++index) {
          KeyValuePair<string, string> keyValuePair = cookies[index];
          strArray[index] = keyValuePair.Key + "=" + Uri.EscapeUriString (keyValuePair.Value);
        }

        builder.Append ("Cookie: ");
        builder.AppendWithCrCf (string.Join (";", strArray));
      }

      if (websocket.CustomHeaderItems != null) {
        for (int index = 0; index < websocket.CustomHeaderItems.Count; ++index) {
          KeyValuePair<string, string> customHeaderItem = websocket.CustomHeaderItems[index];
          builder.AppendFormatWithCrCf ("{0}: {1}", (object) customHeaderItem.Key, (object) customHeaderItem.Value);
        }
      }

      builder.AppendWithCrCf ();
      byte[] bytes = Encoding.UTF8.GetBytes (builder.ToString ());
      websocket.Client.Send (bytes, 0, bytes.Length);
    }

    public override ReaderBase CreateHandshakeReader (WebSocket websocket) {
      return (ReaderBase) new DraftHybi10HandshakeReader (websocket);
    }

    private void SendMessage (WebSocket websocket, int opCode, string message) {
      byte[] bytes = Encoding.UTF8.GetBytes (message);
      this.SendDataFragment (websocket, opCode, bytes, 0, bytes.Length);
    }

    private byte[] EncodeDataFrame (int opCode, byte[] playloadData, int offset, int length) {
      return this.EncodeDataFrame (opCode, true, playloadData, offset, length);
    }

    private byte[] EncodeDataFrame (
      int opCode,
      bool isFinal,
      byte[] playloadData,
      int offset,
      int length) {
      int num1 = 4;
      byte[] numArray;
      if (length < 126) {
        numArray = new byte[2 + num1 + length];
        numArray[1] = (byte) length;
      } else if (length < 65536) {
        numArray = new byte[4 + num1 + length];
        numArray[1] = (byte) 126;
        numArray[2] = (byte) (length / 256);
        numArray[3] = (byte) (length % 256);
      } else {
        numArray = new byte[10 + num1 + length];
        numArray[1] = (byte) 127;
        int num2 = length;
        int num3 = 256;
        for (int index = 9; index > 1; --index) {
          numArray[index] = (byte) (num2 % num3);
          num2 /= num3;
          if (num2 == 0)
            break;
        }
      }

      numArray[0] = !isFinal ? (byte) opCode : (byte) (opCode | 128);
      numArray[1] = (byte) ((uint) numArray[1] | 128U);
      this.GenerateMask (numArray, numArray.Length - num1 - length);
      if (length > 0)
        this.MaskData (playloadData, offset, length, numArray, numArray.Length - length, numArray,
          numArray.Length - num1 - length);
      return numArray;
    }

    private void SendDataFragment (
      WebSocket websocket,
      int opCode,
      byte[] playloadData,
      int offset,
      int length) {
      byte[] data = this.EncodeDataFrame (opCode, playloadData, offset, length);
      websocket.Client.Send (data, 0, data.Length);
    }

    public override void SendData (WebSocket websocket, byte[] data, int offset, int length) {
      this.SendDataFragment (websocket, 2, data, offset, length);
    }

    public override void SendData (WebSocket websocket, IList<ArraySegment<byte>> segments) {
      List<ArraySegment<byte>> arraySegmentList = new List<ArraySegment<byte>> (segments.Count);
      int num = segments.Count - 1;
      for (int index = 0; index < segments.Count; ++index) {
        ArraySegment<byte> segment = segments[index];
        arraySegmentList.Add (new ArraySegment<byte> (this.EncodeDataFrame (index == 0 ? 2 : 0, index == num,
          segment.Array, segment.Offset, segment.Count)));
      }

      websocket.Client.Send ((IList<ArraySegment<byte>>) arraySegmentList);
    }

    public override void SendMessage (WebSocket websocket, string message) {
      this.SendMessage (websocket, 1, message);
    }

    public override void SendCloseHandshake (
      WebSocket websocket,
      int statusCode,
      string closeReason) {
      byte[] numArray =
        new byte[(string.IsNullOrEmpty (closeReason) ? 0 : Encoding.UTF8.GetMaxByteCount (closeReason.Length)) + 2];
      int num1 = statusCode / 256;
      int num2 = statusCode % 256;
      numArray[0] = (byte) num1;
      numArray[1] = (byte) num2;
      if (websocket.State == WebSocketState.Closed)
        return;
      if (!string.IsNullOrEmpty (closeReason)) {
        int bytes = Encoding.UTF8.GetBytes (closeReason, 0, closeReason.Length, numArray, 2);
        this.SendDataFragment (websocket, 8, numArray, 0, bytes + 2);
      } else
        this.SendDataFragment (websocket, 8, numArray, 0, numArray.Length);
    }

    public override void SendPing (WebSocket websocket, string ping) {
      this.SendMessage (websocket, 9, ping);
    }

    public override void SendPong (WebSocket websocket, string pong) {
      this.SendMessage (websocket, 10, pong);
    }

    public override bool VerifyHandshake (
      WebSocket websocket,
      WebSocketCommandInfo handshakeInfo,
      out string description) {
      if (string.IsNullOrEmpty (handshakeInfo.Text)) {
        description = "invalid handshake";
        return false;
      }

      string verbLine = string.Empty;
      if (!handshakeInfo.Text.ParseMimeHeader (websocket.Items, out verbLine)) {
        description = "invalid handshake";
        return false;
      }

      if (!this.ValidateVerbLine (verbLine)) {
        description = verbLine;
        return false;
      }

      if (!string.IsNullOrEmpty (websocket.SubProtocol)) {
        string str = websocket.Items.GetValue<string> ("Sec-WebSocket-Protocol", string.Empty);
        if (!websocket.SubProtocol.Equals (str, StringComparison.OrdinalIgnoreCase)) {
          description = "subprotocol doesn't match";
          return false;
        }
      }

      string str1 = websocket.Items.GetValue<string> ("Sec-WebSocket-Accept", string.Empty);
      if (!websocket.Items.GetValue<string> (this.m_ExpectedAcceptKey, string.Empty)
        .Equals (str1, StringComparison.OrdinalIgnoreCase)) {
        description = "accept key doesn't match";
        return false;
      }

      description = string.Empty;
      return true;
    }

    public override bool SupportBinary {
      get { return true; }
    }

    public override bool SupportPingPong {
      get { return true; }
    }

    private void GenerateMask (byte[] mask, int offset) {
      int num = Math.Min (offset + 4, mask.Length);
      for (int index = offset; index < num; ++index)
        mask[index] = (byte) DraftHybi10Processor.m_Random.Next (0, (int) byte.MaxValue);
    }

    private void MaskData (
      byte[] rawData,
      int offset,
      int length,
      byte[] outputData,
      int outputOffset,
      byte[] mask,
      int maskOffset) {
      for (int index1 = 0; index1 < length; ++index1) {
        int index2 = offset + index1;
        outputData[outputOffset++] = (byte) ((uint) rawData[index2] ^ (uint) mask[maskOffset + index1 % 4]);
      }
    }
  }
}