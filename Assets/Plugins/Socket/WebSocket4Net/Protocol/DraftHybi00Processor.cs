using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Socket.WebSocket4Net.Default;
using Socket.WebSocket4Net.System.Linq;

namespace Socket.WebSocket4Net.Protocol {
  internal class DraftHybi00Processor : ProtocolProcessorBase {
    private static List<char> m_CharLib = new List<char> ();
    private static List<char> m_DigLib = new List<char> ();
    private static Random m_Random = new Random ();

    public static byte[] CloseHandshake = new byte[2] {
      byte.MaxValue,
      (byte) 0
    };

    public const byte StartByte = 0;
    public const byte EndByte = 255;
    private const string m_Error_ChallengeLengthNotMatch = "challenge length doesn't match";
    private const string m_Error_ChallengeNotMatch = "challenge doesn't match";
    private const string m_Error_InvalidHandshake = "invalid handshake";
    private byte[] m_ExpectedChallenge;

    public DraftHybi00Processor ()
      : base (WebSocketVersion.DraftHybi00, (ICloseStatusCode) new CloseStatusCodeHybi10 ()) { }

    static DraftHybi00Processor () {
      for (int index = 33; index <= 126; ++index) {
        char c = (char) index;
        if (char.IsLetter (c))
          DraftHybi00Processor.m_CharLib.Add (c);
        else if (char.IsDigit (c))
          DraftHybi00Processor.m_DigLib.Add (c);
      }
    }

    public override ReaderBase CreateHandshakeReader (WebSocket websocket) {
      return (ReaderBase) new DraftHybi00HandshakeReader (websocket);
    }

    public override bool VerifyHandshake (
      WebSocket websocket,
      WebSocketCommandInfo handshakeInfo,
      out string description) {
      byte[] data = handshakeInfo.Data;
      if (data.Length != data.Length) {
        description = "challenge length doesn't match";
        return false;
      }

      for (int index = 0; index < this.m_ExpectedChallenge.Length; ++index) {
        if ((int) data[index] != (int) this.m_ExpectedChallenge[index]) {
          description = "challenge doesn't match";
          return false;
        }
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

      description = string.Empty;
      return true;
    }

    public override void SendMessage (WebSocket websocket, string message) {
      byte[] numArray = new byte[Encoding.UTF8.GetMaxByteCount (message.Length) + 2];
      numArray[0] = (byte) 0;
      int bytes = Encoding.UTF8.GetBytes (message, 0, message.Length, numArray, 1);
      numArray[1 + bytes] = byte.MaxValue;
      websocket.Client.Send (numArray, 0, bytes + 2);
    }

    public override void SendData (WebSocket websocket, byte[] data, int offset, int length) {
      throw new NotSupportedException ();
    }

    public override void SendData (WebSocket websocket, IList<ArraySegment<byte>> segments) {
      throw new NotSupportedException ();
    }

    public override void SendCloseHandshake (
      WebSocket websocket,
      int statusCode,
      string closeReason) {
      if (websocket.State == WebSocketState.Closed)
        return;
      websocket.Client.Send (DraftHybi00Processor.CloseHandshake, 0, DraftHybi00Processor.CloseHandshake.Length);
    }

    public override void SendPing (WebSocket websocket, string ping) {
      throw new NotSupportedException ();
    }

    public override void SendPong (WebSocket websocket, string pong) {
      throw new NotSupportedException ();
    }

    public override void SendHandshake (WebSocket websocket) {
      string str1 = Encoding.UTF8.GetString (this.GenerateSecKey ());
      string str2 = Encoding.UTF8.GetString (this.GenerateSecKey ());
      byte[] secKey = this.GenerateSecKey (8);
      this.m_ExpectedChallenge = this.GetResponseSecurityKey (str1, str2, secKey);
      StringBuilder builder = new StringBuilder ();
      if (websocket.HttpConnectProxy == null)
        builder.AppendFormatWithCrCf ("GET {0} HTTP/1.1", (object) websocket.TargetUri.PathAndQuery);
      else
        builder.AppendFormatWithCrCf ("GET {0} HTTP/1.1", (object) websocket.TargetUri.ToString ());
      builder.AppendWithCrCf ("Upgrade: WebSocket");
      builder.AppendWithCrCf ("Connection: Upgrade");
      builder.Append ("Sec-WebSocket-Key1: ");
      builder.AppendWithCrCf (str1);
      builder.Append ("Sec-WebSocket-Key2: ");
      builder.AppendWithCrCf (str2);
      builder.Append ("Host: ");
      builder.AppendWithCrCf (websocket.TargetUri.Host);
      builder.Append ("Origin: ");
      builder.AppendWithCrCf (string.IsNullOrEmpty (websocket.Origin) ? websocket.TargetUri.Host : websocket.Origin);
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
      builder.Append (Encoding.UTF8.GetString (secKey, 0, secKey.Length));
      byte[] bytes = Encoding.UTF8.GetBytes (builder.ToString ());
      websocket.Client.Send (bytes, 0, bytes.Length);
    }

    private byte[] GetResponseSecurityKey (string secKey1, string secKey2, byte[] secKey3) {
      string s1 = Regex.Replace (secKey1, "[^0-9]", string.Empty);
      string s2 = Regex.Replace (secKey2, "[^0-9]", string.Empty);
      long num1 = long.Parse (s1);
      long num2 = long.Parse (s2);
      int num3 = secKey1.Count<char> ((Predicate<char>) (c => c == ' '));
      int num4 = secKey2.Count<char> ((Predicate<char>) (c => c == ' '));
      int num5 = (int) (num1 / (long) num3);
      int num6 = (int) (num2 / (long) num4);
      byte[] bytes1 = BitConverter.GetBytes (num5);
      Array.Reverse ((Array) bytes1);
      byte[] bytes2 = BitConverter.GetBytes (num6);
      Array.Reverse ((Array) bytes2);
      byte[] numArray = secKey3;
      byte[] source = new byte[bytes1.Length + bytes2.Length + numArray.Length];
      Array.Copy ((Array) bytes1, 0, (Array) source, 0, bytes1.Length);
      Array.Copy ((Array) bytes2, 0, (Array) source, bytes1.Length, bytes2.Length);
      Array.Copy ((Array) numArray, 0, (Array) source, bytes1.Length + bytes2.Length, numArray.Length);
      return source.ComputeMD5Hash ();
    }

    private byte[] GenerateSecKey () {
      return this.GenerateSecKey (DraftHybi00Processor.m_Random.Next (10, 20));
    }

    private byte[] GenerateSecKey (int totalLen) {
      int num1 = DraftHybi00Processor.m_Random.Next (1, totalLen / 2 + 1);
      int num2 = DraftHybi00Processor.m_Random.Next (3, totalLen - 1 - num1);
      int num3 = totalLen - num1 - num2;
      byte[] source = new byte[totalLen];
      int num4 = 0;
      for (int index = 0; index < num1; ++index)
        source[num4++] = (byte) 32;
      for (int index = 0; index < num2; ++index)
        source[num4++] =
          (byte) DraftHybi00Processor.m_CharLib[
            DraftHybi00Processor.m_Random.Next (0, DraftHybi00Processor.m_CharLib.Count - 1)];
      for (int index = 0; index < num3; ++index)
        source[num4++] =
          (byte) DraftHybi00Processor.m_DigLib[
            DraftHybi00Processor.m_Random.Next (0, DraftHybi00Processor.m_DigLib.Count - 1)];
      return SuperSocket.ClientEngine.Extensions.RandomOrder<byte> (source);
    }

    public override bool SupportBinary {
      get { return false; }
    }

    public override bool SupportPingPong {
      get { return false; }
    }
  }
}