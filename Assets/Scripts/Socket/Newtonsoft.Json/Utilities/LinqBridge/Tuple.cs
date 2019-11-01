//using System;
//using System.Collections.Generic;
//using System.Globalization;
//
//namespace Socket.Newtonsoft.Json.Utilities.LinqBridge {
//  [Serializable]
////  public class Tuple<T1, T2> : IStructuralEquatable, IStructuralComparable, IComparable, ITupleInternal, ITuple
//
//  internal struct Tuple<TFirst, TSecond> : IEquatable<Tuple<TFirst, TSecond>> {
//    public TFirst First { get; private set; }
//
//    public TSecond Second { get; private set; }
//
//    public unsafe Tuple(TFirst first, TSecond second) {
//      *(Tuple<TFirst, TSecond>*) ref this = new Tuple<TFirst, TSecond>();
//      this.First = first;
//      this.Second = second;
//    }
//
//    public override bool Equals(object obj) {
//      if (obj != null && obj is Tuple<TFirst, TSecond>)
//        return base.Equals((object) (Tuple<TFirst, TSecond>) obj);
//      return false;
//    }
//
//    public bool Equals(Tuple<TFirst, TSecond> other) {
//      if (EqualityComparer<TFirst>.Default.Equals(other.First, this.First))
//        return EqualityComparer<TSecond>.Default.Equals(other.Second, this.Second);
//      return false;
//    }
//
//    public override int GetHashCode() {
//      return -1521134295 * (-1521134295 * 2049903426 + EqualityComparer<TFirst>.Default.GetHashCode(this.First)) +
//             EqualityComparer<TSecond>.Default.GetHashCode(this.Second);
//    }
//
//    public override string ToString() {
//      return string.Format((IFormatProvider) CultureInfo.InvariantCulture, "{{ First = {0}, Second = {1} }}",
//        (object) this.First, (object) this.Second);
//    }
//  }
//}