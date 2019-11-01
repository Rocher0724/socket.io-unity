namespace Socket.WebSocket4Net.Protocol.FramePartReader {
  internal abstract class DataFramePartReader : IDataFramePartReader {
    static DataFramePartReader () {
      DataFramePartReader.FixPartReader =
        (IDataFramePartReader) new WebSocket4Net.Protocol.FramePartReader.FixPartReader ();
      DataFramePartReader.ExtendedLenghtReader =
        (IDataFramePartReader) new WebSocket4Net.Protocol.FramePartReader.ExtendedLenghtReader ();
      DataFramePartReader.MaskKeyReader =
        (IDataFramePartReader) new WebSocket4Net.Protocol.FramePartReader.MaskKeyReader ();
      DataFramePartReader.PayloadDataReader =
        (IDataFramePartReader) new WebSocket4Net.Protocol.FramePartReader.PayloadDataReader ();
    }

    public abstract int Process (
      int lastLength,
      WebSocketDataFrame frame,
      out IDataFramePartReader nextPartReader);

    public static IDataFramePartReader NewReader {
      get { return DataFramePartReader.FixPartReader; }
    }

    protected static IDataFramePartReader FixPartReader { get; private set; }

    protected static IDataFramePartReader ExtendedLenghtReader { get; private set; }

    protected static IDataFramePartReader MaskKeyReader { get; private set; }

    protected static IDataFramePartReader PayloadDataReader { get; private set; }
  }
}