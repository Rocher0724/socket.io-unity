using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Socket.Newtonsoft.Json.Serialization {
  public class MemoryTraceWriter : ITraceWriter
  {
    private readonly Queue<string> _traceMessages;

    public TraceLevel LevelFilter { get; set; }

    public MemoryTraceWriter()
    {
      this.LevelFilter = TraceLevel.Verbose;
      this._traceMessages = new Queue<string>();
    }

    public void Trace_(TraceLevel level, string message, Exception ex)
    {
      if (this._traceMessages.Count >= 1000)
        this._traceMessages.Dequeue();
      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append(DateTime.Now.ToString("yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fff", (IFormatProvider) CultureInfo.InvariantCulture));
      stringBuilder.Append(" ");
      stringBuilder.Append(level.ToString("g"));
      stringBuilder.Append(" ");
      stringBuilder.Append(message);
      this._traceMessages.Enqueue(stringBuilder.ToString());
    }

    public IEnumerable<string> GetTraceMessages()
    {
      return (IEnumerable<string>) this._traceMessages;
    }

    public override string ToString()
    {
      StringBuilder stringBuilder = new StringBuilder();
      foreach (string traceMessage in this._traceMessages)
      {
        if (stringBuilder.Length > 0)
          stringBuilder.AppendLine();
        stringBuilder.Append(traceMessage);
      }
      return stringBuilder.ToString();
    }
  }
}
