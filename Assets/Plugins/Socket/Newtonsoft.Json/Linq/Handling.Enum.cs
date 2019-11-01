using System;

namespace Socket.Newtonsoft.Json.Linq {
  public enum CommentHandling
  {
    Ignore,
    Load,
  }
  
  public enum LineInfoHandling
  {
    Ignore,
    Load,
  }
  
  public enum MergeArrayHandling
  {
    Concat,
    Union,
    Replace,
    Merge,
  }
  
  [Flags]
  public enum MergeNullValueHandling
  {
    Ignore = 0,
    Merge = 1,
  }
}