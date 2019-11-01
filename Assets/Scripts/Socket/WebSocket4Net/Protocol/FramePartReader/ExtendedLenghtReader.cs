namespace Socket.WebSocket4Net.Protocol.FramePartReader {
  internal class ExtendedLenghtReader : DataFramePartReader {
    public override int Process (
      int lastLength,
      WebSocketDataFrame frame,
      out IDataFramePartReader nextPartReader) {
      int num = 2;
      int lastLength1 = frame.PayloadLenght != (sbyte) 126 ? num + 8 : num + 2;
      if (frame.Length < lastLength1) {
        nextPartReader = (IDataFramePartReader) this;
        return -1;
      }

      if (frame.HasMask) {
        nextPartReader = DataFramePartReader.MaskKeyReader;
      } else {
        if (frame.ActualPayloadLength == 0L) {
          nextPartReader = (IDataFramePartReader) null;
          return (int) ((long) frame.Length - (long) lastLength1);
        }

        nextPartReader = DataFramePartReader.PayloadDataReader;
      }

      if (frame.Length > lastLength1)
        return nextPartReader.Process (lastLength1, frame, out nextPartReader);
      return 0;
    }
  }
}