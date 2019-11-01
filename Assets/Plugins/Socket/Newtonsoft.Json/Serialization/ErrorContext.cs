using System;

namespace Socket.Newtonsoft.Json.Serialization {
  public class ErrorContext {
    internal ErrorContext(object originalObject, object member, string path, Exception error) {
      this.OriginalObject = originalObject;
      this.Member = member;
      this.Error = error;
      this.Path = path;
    }

    internal bool Traced { get; set; }

    public Exception Error { get; }

    public object OriginalObject { get; }

    public object Member { get; }

    public string Path { get; }

    public bool Handled { get; set; }
  }
}