using System;
using System.Diagnostics;

namespace Socket.Newtonsoft.Json.Serialization {
  public interface ITraceWriter {
    TraceLevel LevelFilter { get; }

    void Trace_(TraceLevel level, string message, Exception ex);
  }
}