namespace Socket.Newtonsoft.Json.Utilities {
  internal delegate T Creator<T>();
  internal delegate TResult MethodCall<T, TResult>(T target, params object[] args);

  
}