using System;

namespace Socket.Newtonsoft.Json.Serialization {

  public class ErrorEventArgs : EventArgs {
    public object CurrentObject { get; }

    public ErrorContext ErrorContext { get; }

    public ErrorEventArgs(object currentObject, ErrorContext errorContext) {
      this.CurrentObject = currentObject;
      this.ErrorContext = errorContext;
    }
  }
}