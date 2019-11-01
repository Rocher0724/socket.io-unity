using System;

namespace Socket.Newtonsoft.Json.Linq {
  public class JsonMergeSettings
  {
    private MergeArrayHandling _mergeArrayHandling;
    private MergeNullValueHandling _mergeNullValueHandling;

    public MergeArrayHandling MergeArrayHandling
    {
      get
      {
        return this._mergeArrayHandling;
      }
      set
      {
        switch (value)
        {
          case MergeArrayHandling.Concat:
          case MergeArrayHandling.Union:
          case MergeArrayHandling.Replace:
          case MergeArrayHandling.Merge:
            this._mergeArrayHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof (value));
        }
      }
    }

    public MergeNullValueHandling MergeNullValueHandling
    {
      get
      {
        return this._mergeNullValueHandling;
      }
      set
      {
        switch (value)
        {
          case MergeNullValueHandling.Ignore:
          case MergeNullValueHandling.Merge:
            this._mergeNullValueHandling = value;
            break;
          default:
            throw new ArgumentOutOfRangeException(nameof (value));
        }
      }
    }
  }
}
