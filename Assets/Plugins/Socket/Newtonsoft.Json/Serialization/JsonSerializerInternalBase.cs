using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Serialization {
  internal abstract class JsonSerializerInternalBase {
    private ErrorContext _currentErrorContext;
    private BidirectionalDictionary<string, object> _mappings;
    internal readonly JsonSerializer Serializer;
    internal readonly ITraceWriter TraceWriter;
    protected JsonSerializerProxy InternalSerializer;

    protected JsonSerializerInternalBase(JsonSerializer serializer) {
      ValidationUtils.ArgumentNotNull((object) serializer, nameof(serializer));
      this.Serializer = serializer;
      this.TraceWriter = serializer.TraceWriter;
    }

    internal BidirectionalDictionary<string, object> DefaultReferenceMappings {
      get {
        if (this._mappings == null)
          this._mappings = new BidirectionalDictionary<string, object>(
            (IEqualityComparer<string>) EqualityComparer<string>.Default,
            (IEqualityComparer<object>) new JsonSerializerInternalBase.ReferenceEqualsEqualityComparer(),
            "A different value already has the Id '{0}'.", "A different Id has already been assigned for value '{0}'.");
        return this._mappings;
      }
    }

    private ErrorContext GetErrorContext(
      object currentObject,
      object member,
      string path,
      Exception error) {
      if (this._currentErrorContext == null)
        this._currentErrorContext = new ErrorContext(currentObject, member, path, error);
      if (this._currentErrorContext.Error != error)
        throw new InvalidOperationException("Current error context error is different to requested error.");
      return this._currentErrorContext;
    }

    protected void ClearErrorContext() {
      if (this._currentErrorContext == null)
        throw new InvalidOperationException("Could not clear error context. Error context is already null.");
      this._currentErrorContext = (ErrorContext) null;
    }

    protected bool IsErrorHandled(
      object currentObject,
      JsonContract contract,
      object keyValue,
      IJsonLineInfo lineInfo,
      string path,
      Exception ex) {
      ErrorContext errorContext = this.GetErrorContext(currentObject, keyValue, path, ex);
      if (this.TraceWriter != null && this.TraceWriter.LevelFilter >= TraceLevel.Error && !errorContext.Traced) {
        errorContext.Traced = true;
        string str = this.GetType() == typeof(JsonSerializerInternalWriter)
          ? "Error serializing"
          : "Error deserializing";
        if (contract != null)
          str = str + " " + (object) contract.UnderlyingType;
        string message = str + ". " + ex.Message;
        if (!(ex is JsonException))
          message = JsonPosition.FormatMessage(lineInfo, path, message);
        this.TraceWriter.Trace_(TraceLevel.Error, message, ex);
      }

      if (contract != null && currentObject != null)
        contract.InvokeOnError(currentObject, this.Serializer.Context, errorContext);
      if (!errorContext.Handled)
        this.Serializer.OnError(new ErrorEventArgs(currentObject, errorContext));
      return errorContext.Handled;
    }

    private class ReferenceEqualsEqualityComparer : IEqualityComparer<object> {
      bool IEqualityComparer<object>.Equals(object x, object y) {
        return x == y;
      }

      int IEqualityComparer<object>.GetHashCode(object obj) {
        return RuntimeHelpers.GetHashCode(obj);
      }
    }
  }
}