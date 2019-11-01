namespace Socket.WebSocket4Net.Protocol.FramePartReader {
  internal class PayloadDataReader : DataFramePartReader {
    public override int Process (
      int lastLength,
      WebSocketDataFrame frame,
      out IDataFramePartReader nextPartReader) {
      long num = (long) lastLength + frame.ActualPayloadLength;
      if ((long) frame.Length < num) {
        nextPartReader = (IDataFramePartReader) this;
        return -1;
      }

      nextPartReader = (IDataFramePartReader) null;
      return (int) ((long) frame.Length - num);
    }
  }
}