using System;
using System.Diagnostics;

namespace Socket.Newtonsoft.Json.Serialization {
  public class DiagnosticsTraceWriter : ITraceWriter {
    public TraceLevel LevelFilter { get; set; }

    private TraceEventType GetTraceEventType(TraceLevel level) {
      switch (level) {
        case TraceLevel.Error:
          return TraceEventType.Error;
        case TraceLevel.Warning:
          return TraceEventType.Warning;
        case TraceLevel.Info:
          return TraceEventType.Information;
        case TraceLevel.Verbose:
          return TraceEventType.Verbose;
        default:
          throw new ArgumentOutOfRangeException(nameof(level));
      }
    }

    public void Trace_(TraceLevel level, string message, Exception ex) {
      if (level == TraceLevel.Off)
        return;
      TraceEventCache eventCache = new TraceEventCache();
      TraceEventType traceEventType = this.GetTraceEventType(level);
      foreach (TraceListener listener in Trace.Listeners) {
        if (!listener.IsThreadSafe) {
          lock (listener)
            listener.TraceEvent(eventCache, "Newtonsoft.Json", traceEventType, 0, message);
        } else
          listener.TraceEvent(eventCache, "Newtonsoft.Json", traceEventType, 0, message);

        if (Trace.AutoFlush)
          listener.Flush();
      }
    }
  }
}