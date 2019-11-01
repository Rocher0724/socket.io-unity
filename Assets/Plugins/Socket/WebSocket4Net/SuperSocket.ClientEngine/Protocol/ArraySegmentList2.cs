using System;
using System.Collections.Generic;
using System.Collections;

namespace Socket.WebSocket4Net.SuperSocket.ClientEngine.Protocol {
  public class ArraySegmentList<T> : IList<T>, ICollection<T>, IEnumerable<T>, IEnumerable
    where T : IEquatable<T> {
    private IList<ArraySegmentEx<T>> m_Segments;
    private ArraySegmentEx<T> m_PrevSegment;
    private int m_PrevSegmentIndex;
    private int m_Count;

    internal IList<ArraySegmentEx<T>> Segments {
      get { return this.m_Segments; }
    }

    public ArraySegmentList() {
      this.m_Segments = (IList<ArraySegmentEx<T>>) new List<ArraySegmentEx<T>>();
    }

    private void CalculateSegmentsInfo(IList<ArraySegmentEx<T>> segments) {
      int num = 0;
      foreach (ArraySegmentEx<T> segment in (IEnumerable<ArraySegmentEx<T>>) segments) {
        if (segment.Count > 0) {
          segment.From = num;
          segment.To = num + segment.Count - 1;
          this.m_Segments.Add(segment);
          num += segment.Count;
        }
      }

      this.m_Count = num;
    }

    public int IndexOf(T item) {
      int num = 0;
      for (int index1 = 0; index1 < this.m_Segments.Count; ++index1) {
        ArraySegmentEx<T> segment = this.m_Segments[index1];
        int offset = segment.Offset;
        for (int index2 = 0; index2 < segment.Count; ++index2) {
          if (segment.Array[index2 + offset].Equals(item))
            return num;
          ++num;
        }
      }

      return -1;
    }

    public void Insert(int index, T item) {
      throw new NotSupportedException();
    }

    public void RemoveAt(int index) {
      throw new NotSupportedException();
    }

    public T this[int index] {
      get {
        ArraySegmentEx<T> segment;
        int elementInternalIndex = this.GetElementInternalIndex(index, out segment);
        if (elementInternalIndex < 0)
          throw new IndexOutOfRangeException();
        return segment.Array[elementInternalIndex];
      }
      set {
        ArraySegmentEx<T> segment;
        int elementInternalIndex = this.GetElementInternalIndex(index, out segment);
        if (elementInternalIndex < 0)
          throw new IndexOutOfRangeException();
        segment.Array[elementInternalIndex] = value;
      }
    }

    private int GetElementInternalIndex(int index, out ArraySegmentEx<T> segment) {
      segment = (ArraySegmentEx<T>) null;
      if (index < 0 || index > this.Count - 1)
        return -1;
      if (index == 0) {
        this.m_PrevSegment = this.m_Segments[0];
        this.m_PrevSegmentIndex = 0;
        segment = this.m_PrevSegment;
        return this.m_PrevSegment.Offset;
      }

      int num1 = 0;
      if (this.m_PrevSegment != null) {
        if (index >= this.m_PrevSegment.From) {
          if (index <= this.m_PrevSegment.To) {
            segment = this.m_PrevSegment;
            return this.m_PrevSegment.Offset + index - this.m_PrevSegment.From;
          }

          num1 = 1;
        } else
          num1 = -1;
      }

      int from;
      int to;
      if (num1 != 0) {
        int index1 = this.m_PrevSegmentIndex + num1;
        ArraySegmentEx<T> segment1 = this.m_Segments[index1];
        if (index >= segment1.From && index <= segment1.To) {
          segment = segment1;
          return segment1.Offset + index - segment1.From;
        }

        int index2 = index1 + num1;
        ArraySegmentEx<T> segment2 = this.m_Segments[index2];
        if (index >= segment2.From && index <= segment2.To) {
          this.m_PrevSegment = segment2;
          this.m_PrevSegmentIndex = index2;
          segment = segment2;
          return segment2.Offset + index - segment2.From;
        }

        if (num1 > 0) {
          from = index2 + 1;
          to = this.m_Segments.Count - 1;
        } else {
          int num2 = index2 - 1;
          from = 0;
          to = num2;
        }
      } else {
        from = 0;
        to = this.m_Segments.Count - 1;
      }

      int segmentIndex = -1;
      ArraySegmentEx<T> arraySegmentEx = this.QuickSearchSegment(from, to, index, out segmentIndex);
      if (arraySegmentEx != null) {
        this.m_PrevSegment = arraySegmentEx;
        this.m_PrevSegmentIndex = segmentIndex;
        segment = this.m_PrevSegment;
        return arraySegmentEx.Offset + index - arraySegmentEx.From;
      }

      this.m_PrevSegment = (ArraySegmentEx<T>) null;
      return -1;
    }

    internal ArraySegmentEx<T> QuickSearchSegment(
      int from,
      int to,
      int index,
      out int segmentIndex) {
      segmentIndex = -1;
      int num = to - from;
      switch (num) {
        case 0:
          ArraySegmentEx<T> segment1 = this.m_Segments[from];
          if (index < segment1.From || index > segment1.To)
            return (ArraySegmentEx<T>) null;
          segmentIndex = from;
          return segment1;
        case 1:
          ArraySegmentEx<T> segment2 = this.m_Segments[from];
          if (index >= segment2.From && index <= segment2.To) {
            segmentIndex = from;
            return segment2;
          }

          ArraySegmentEx<T> segment3 = this.m_Segments[to];
          if (index < segment3.From || index > segment3.To)
            return (ArraySegmentEx<T>) null;
          segmentIndex = to;
          return segment3;
        default:
          int index1 = from + num / 2;
          ArraySegmentEx<T> segment4 = this.m_Segments[index1];
          if (index < segment4.From)
            return this.QuickSearchSegment(from, index1 - 1, index, out segmentIndex);
          if (index > segment4.To)
            return this.QuickSearchSegment(index1 + 1, to, index, out segmentIndex);
          segmentIndex = index1;
          return segment4;
      }
    }

    public void Add(T item) {
      throw new NotSupportedException();
    }

    public void Clear() {
      throw new NotSupportedException();
    }

    public bool Contains(T item) {
      throw new NotSupportedException();
    }

    public void CopyTo(T[] array, int arrayIndex) {
      this.CopyTo(array, 0, arrayIndex, Math.Min(array.Length, this.Count - arrayIndex));
    }

    public int Count {
      get { return this.m_Count; }
    }

    public bool IsReadOnly {
      get { return true; }
    }

    public bool Remove(T item) {
      throw new NotSupportedException();
    }

    public IEnumerator<T> GetEnumerator() {
      throw new NotSupportedException();
    }

    IEnumerator IEnumerable.GetEnumerator() {
      throw new NotSupportedException();
    }

    public void RemoveSegmentAt(int index) {
      ArraySegmentEx<T> segment = this.m_Segments[index];
      int num = segment.To - segment.From + 1;
      this.m_Segments.RemoveAt(index);
      this.m_PrevSegment = (ArraySegmentEx<T>) null;
      if (index != this.m_Segments.Count) {
        for (int index1 = index; index1 < this.m_Segments.Count; ++index1) {
          this.m_Segments[index1].From -= num;
          this.m_Segments[index1].To -= num;
        }
      }

      this.m_Count -= num;
    }

    public void AddSegment(T[] array, int offset, int length) {
      this.AddSegment(array, offset, length, false);
    }

    public void AddSegment(T[] array, int offset, int length, bool toBeCopied) {
      if (length <= 0)
        return;
      int count = this.m_Count;
      ArraySegmentEx<T> arraySegmentEx = toBeCopied
        ? new ArraySegmentEx<T>(Extensions.CloneRange<T>(array, offset, length), 0, length)
        : new ArraySegmentEx<T>(array, offset, length);
      arraySegmentEx.From = count;
      this.m_Count = count + arraySegmentEx.Count;
      arraySegmentEx.To = this.m_Count - 1;
      this.m_Segments.Add(arraySegmentEx);
    }

    public int SegmentCount {
      get { return this.m_Segments.Count; }
    }

    public void ClearSegements() {
      this.m_Segments.Clear();
      this.m_PrevSegment = (ArraySegmentEx<T>) null;
      this.m_Count = 0;
    }

    public T[] ToArrayData() {
      return this.ToArrayData(0, this.m_Count);
    }

    public T[] ToArrayData(int startIndex, int length) {
      T[] objArray = new T[length];
      int num = 0;
      int destinationIndex = 0;
      int segmentIndex = 0;
      if (startIndex != 0) {
        ArraySegmentEx<T> arraySegmentEx =
          this.QuickSearchSegment(0, this.m_Segments.Count - 1, startIndex, out segmentIndex);
        num = startIndex - arraySegmentEx.From;
        if (arraySegmentEx == null)
          throw new IndexOutOfRangeException();
      }

      for (int index = segmentIndex; index < this.m_Segments.Count; ++index) {
        ArraySegmentEx<T> segment = this.m_Segments[index];
        int length1 = Math.Min(segment.Count - num, length - destinationIndex);
        Array.Copy((Array) segment.Array, segment.Offset + num, (Array) objArray, destinationIndex, length1);
        destinationIndex += length1;
        if (destinationIndex < length)
          num = 0;
        else
          break;
      }

      return objArray;
    }

    public void TrimEnd(int trimSize) {
      if (trimSize <= 0)
        return;
      int num = this.Count - trimSize - 1;
      for (int index = this.m_Segments.Count - 1; index >= 0; --index) {
        ArraySegmentEx<T> segment = this.m_Segments[index];
        if (segment.From <= num && num < segment.To) {
          segment.To = num;
          this.m_Count -= trimSize;
          break;
        }

        this.RemoveSegmentAt(index);
      }
    }

    public int SearchLastSegment(SearchMarkState<T> state) {
      if (this.m_Segments.Count <= 0)
        return -1;
      ArraySegmentEx<T> segment = this.m_Segments[this.m_Segments.Count - 1];
      if (segment == null)
        return -1;
      int? nullable = Extensions.SearchMark<T>((IList<T>) segment.Array, segment.Offset, segment.Count, state.Mark);
      if (!nullable.HasValue)
        return -1;
      if (nullable.Value > 0) {
        state.Matched = 0;
        return nullable.Value - segment.Offset + segment.From;
      }

      state.Matched = -nullable.Value;
      return -1;
    }

    public int CopyTo(T[] to) {
      return this.CopyTo(to, 0, 0, Math.Min(this.m_Count, to.Length));
    }

    public int CopyTo(T[] to, int srcIndex, int toIndex, int length) {
      int num1 = 0;
      int segmentIndex;
      ArraySegmentEx<T> arraySegmentEx;
      if (srcIndex > 0) {
        arraySegmentEx = this.QuickSearchSegment(0, this.m_Segments.Count - 1, srcIndex, out segmentIndex);
      } else {
        arraySegmentEx = this.m_Segments[0];
        segmentIndex = 0;
      }

      int sourceIndex = srcIndex - arraySegmentEx.From + arraySegmentEx.Offset;
      int length1 = Math.Min(arraySegmentEx.Count - sourceIndex + arraySegmentEx.Offset, length - num1);
      Array.Copy((Array) arraySegmentEx.Array, sourceIndex, (Array) to, num1 + toIndex, length1);
      int num2 = num1 + length1;
      if (num2 >= length)
        return num2;
      for (int index = segmentIndex + 1; index < this.m_Segments.Count; ++index) {
        ArraySegmentEx<T> segment = this.m_Segments[index];
        int length2 = Math.Min(segment.Count, length - num2);
        Array.Copy((Array) segment.Array, segment.Offset, (Array) to, num2 + toIndex, length2);
        num2 += length2;
        if (num2 >= length)
          break;
      }

      return num2;
    }
  }
}