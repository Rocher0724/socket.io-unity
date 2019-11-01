using System;

namespace Socket.Newtonsoft.Json.Linq {
  public class JsonLoadSettings
  {
    private CommentHandling _commentHandling;
    private LineInfoHandling _lineInfoHandling;

    public JsonLoadSettings()
    {
      this._lineInfoHandling = LineInfoHandling.Load;
      this._commentHandling = CommentHandling.Ignore;
    }

    public CommentHandling CommentHandling
    {
      get
      {
        return this._commentHandling;
      }
      set
      {
        switch (value)
        {
          case CommentHandling.Ignore:
          case CommentHandling.Load:
            this._commentHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof (value));
        }
      }
    }

    public LineInfoHandling LineInfoHandling
    {
      get
      {
        return this._lineInfoHandling;
      }
      set
      {
        switch (value)
        {
          case LineInfoHandling.Ignore:
          case LineInfoHandling.Load:
            this._lineInfoHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof (value));
        }
      }
    }
  }
}
