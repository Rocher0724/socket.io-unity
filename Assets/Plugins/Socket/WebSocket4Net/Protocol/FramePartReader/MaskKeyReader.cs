namespace Socket.WebSocket4Net.Protocol.FramePartReader {
  internal class MaskKeyReader : DataFramePartReader {
    public override int Process (
      int lastLength,
      WebSocketDataFrame frame,
      out IDataFramePartReader nextPartReader) {
      int lastLength1 = lastLength + 4;
      if (frame.Length < lastLength1) {
        nextPartReader = (IDataFramePartReader) this;
        return -1;
      }

      frame.MaskKey = frame.InnerData.ToArrayData (lastLength, 4);
      if (frame.ActualPayloadLength == 0L) {
        nextPartReader = (IDataFramePartReader) null;
        return (int) ((long) frame.Length - (long) lastLength1);
      }

      nextPartReader = (IDataFramePartReader) new PayloadDataReader ();
      if (frame.Length > lastLength1)
        return nextPartReader.Process (lastLength1, frame, out nextPartReader);
      return 0;
    }
  }
}