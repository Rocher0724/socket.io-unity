using System;
using System.Collections.Generic;

namespace Socket.Quobject.Collections.Immutable {
  internal class AvlNode<T>
  {
    public static readonly AvlNode<T> Empty = (AvlNode<T>) new AvlNode<T>.NullNode();
    public T Value;
    private AvlNode<T> left;
    private AvlNode<T> right;
    private int _count;
    private int _depth;

    public virtual bool IsEmpty
    {
      get
      {
        return false;
      }
    }

    public AvlNode<T> Left
    {
      get
      {
        return this.left;
      }
    }

    public AvlNode<T> Right
    {
      get
      {
        return this.right;
      }
    }

    public int Count
    {
      get
      {
        return this._count;
      }
    }

    private int Balance
    {
      get
      {
        if (this.IsEmpty)
          return 0;
        return this.left._depth - this.right._depth;
      }
    }

    private int Depth
    {
      get
      {
        return this._depth;
      }
    }

    public AvlNode()
    {
      this.right = AvlNode<T>.Empty;
      this.left = AvlNode<T>.Empty;
    }

    public AvlNode(T val)
      : this(val, AvlNode<T>.Empty, AvlNode<T>.Empty)
    {
    }

    private AvlNode(T val, AvlNode<T> lt, AvlNode<T> gt)
    {
      this.Value = val;
      this.left = lt;
      this.right = gt;
      this._count = 1 + this.left._count + this.right._count;
      this._depth = 1 + Math.Max(this.left._depth, this.right._depth);
    }

    private AvlNode<T> Min
    {
      get
      {
        if (this.IsEmpty)
          return AvlNode<T>.Empty;
        AvlNode<T> avlNode = this;
        for (AvlNode<T> left = avlNode.left; left != AvlNode<T>.Empty; left = avlNode.left)
          avlNode = left;
        return avlNode;
      }
    }

    private AvlNode<T> FixRootBalance()
    {
      int balance = this.Balance;
      if (Math.Abs(balance) < 2)
        return this;
      if (balance == 2)
      {
        if (this.left.Balance == 1 || this.left.Balance == 0)
          return this.RotateToGT();
        if (this.left.Balance == -1)
          return this.NewOrMutate(this.Value, this.ToMutableIfNecessary(this.left).RotateToLT(), this.right).RotateToGT();
        throw new Exception(string.Format("LTDict too unbalanced: {0}", (object) this.left.Balance));
      }
      if (balance != -2)
        throw new Exception(string.Format("Tree too out of balance: {0}", (object) this.Balance));
      if (this.right.Balance == -1 || this.right.Balance == 0)
        return this.RotateToLT();
      if (this.right.Balance == 1)
        return this.NewOrMutate(this.Value, this.left, this.ToMutableIfNecessary(this.right).RotateToGT()).RotateToLT();
      throw new Exception(string.Format("LTDict too unbalanced: {0}", (object) this.left.Balance));
    }

    public AvlNode<T> SearchNode(T value, Comparison<T> comparer)
    {
      AvlNode<T> avlNode = this;
      while (avlNode != AvlNode<T>.Empty)
      {
        int num = comparer(avlNode.Value, value);
        if (num < 0)
        {
          avlNode = avlNode.right;
        }
        else
        {
          if (num <= 0)
            return avlNode;
          avlNode = avlNode.left;
        }
      }
      return AvlNode<T>.Empty;
    }

    public AvlNode<T> InsertIntoNew(int index, T val)
    {
      if (this.IsEmpty)
        return new AvlNode<T>(val);
      AvlNode<T> newLeft = this.left;
      AvlNode<T> newRight = this.right;
      if (index <= this.left._count)
        newLeft = this.ToMutableIfNecessary(this.left).InsertIntoNew(index, val);
      else
        newRight = this.ToMutableIfNecessary(this.right).InsertIntoNew(index - this.left._count - 1, val);
      return this.NewOrMutate(this.Value, newLeft, newRight).FixRootBalance();
    }

    public AvlNode<T> InsertIntoNew(T val, Comparison<T> comparer)
    {
      if (this.IsEmpty)
        return new AvlNode<T>(val);
      AvlNode<T> newLeft = this.left;
      AvlNode<T> newRight = this.right;
      int num = comparer(this.Value, val);
      T newValue = this.Value;
      if (num < 0)
        newRight = this.ToMutableIfNecessary(this.right).InsertIntoNew(val, comparer);
      else if (num > 0)
        newLeft = this.ToMutableIfNecessary(this.left).InsertIntoNew(val, comparer);
      else
        newValue = val;
      return this.NewOrMutate(newValue, newLeft, newRight).FixRootBalance();
    }

    public AvlNode<T> SetItem(int index, T val)
    {
      AvlNode<T> newLeft = this.left;
      AvlNode<T> newRight = this.right;
      if (index < this.left._count)
      {
        newLeft = this.ToMutableIfNecessary(this.left).SetItem(index, val);
      }
      else
      {
        if (index <= this.left._count)
          return this.NewOrMutate(val, newLeft, newRight);
        newRight = this.ToMutableIfNecessary(this.right).SetItem(index - this.left._count - 1, val);
      }
      return this.NewOrMutate(this.Value, newLeft, newRight);
    }

    public AvlNode<T> GetNodeAt(int index)
    {
      if (index < this.left._count)
        return this.left.GetNodeAt(index);
      if (index > this.left._count)
        return this.right.GetNodeAt(index - this.left._count - 1);
      return this;
    }

    public AvlNode<T> RemoveFromNew(int index, out bool found)
    {
      if (this.IsEmpty)
      {
        found = false;
        return AvlNode<T>.Empty;
      }
      if (index < this.left._count)
      {
        AvlNode<T> newLeft = this.ToMutableIfNecessary(this.left).RemoveFromNew(index, out found);
        if (!found)
          return this;
        return this.NewOrMutate(this.Value, newLeft, this.right).FixRootBalance();
      }
      if (index > this.left._count)
      {
        AvlNode<T> newRight = this.ToMutableIfNecessary(this.right).RemoveFromNew(index - this.left._count - 1, out found);
        if (!found)
          return this;
        return this.NewOrMutate(this.Value, this.left, newRight).FixRootBalance();
      }
      found = true;
      return this.RemoveRoot();
    }

    public AvlNode<T> RemoveFromNew(T val, Comparison<T> comparer, out bool found)
    {
      if (this.IsEmpty)
      {
        found = false;
        return AvlNode<T>.Empty;
      }
      int num = comparer(this.Value, val);
      if (num < 0)
      {
        AvlNode<T> newRight = this.ToMutableIfNecessary(this.right).RemoveFromNew(val, comparer, out found);
        if (!found)
          return this;
        return this.NewOrMutate(this.Value, this.left, newRight).FixRootBalance();
      }
      if (num > 0)
      {
        AvlNode<T> newLeft = this.ToMutableIfNecessary(this.left).RemoveFromNew(val, comparer, out found);
        if (!found)
          return this;
        return this.NewOrMutate(this.Value, newLeft, this.right).FixRootBalance();
      }
      found = true;
      return this.RemoveRoot();
    }

    private AvlNode<T> RemoveMax(out AvlNode<T> max)
    {
      if (this.IsEmpty)
      {
        max = AvlNode<T>.Empty;
        return AvlNode<T>.Empty;
      }
      if (!this.right.IsEmpty)
        return this.NewOrMutate(this.Value, this.left, this.ToMutableIfNecessary(this.right).RemoveMax(out max)).FixRootBalance();
      max = this;
      return this.left;
    }

    private AvlNode<T> RemoveMin(out AvlNode<T> min)
    {
      if (this.IsEmpty)
      {
        min = AvlNode<T>.Empty;
        return AvlNode<T>.Empty;
      }
      if (!this.left.IsEmpty)
        return this.NewOrMutate(this.Value, this.ToMutableIfNecessary(this.left).RemoveMin(out min), this.right).FixRootBalance();
      min = this;
      return this.right;
    }

    private AvlNode<T> RemoveRoot()
    {
      if (this.IsEmpty)
        return this;
      if (this.left.IsEmpty)
        return this.right;
      if (this.right.IsEmpty)
        return this.left;
      if (this.left._count < this.right._count)
      {
        AvlNode<T> min;
        AvlNode<T> newRight = this.ToMutableIfNecessary(this.right).RemoveMin(out min);
        return this.NewOrMutate(min.Value, this.left, newRight).FixRootBalance();
      }
      AvlNode<T> max;
      AvlNode<T> newLeft = this.ToMutableIfNecessary(this.left).RemoveMax(out max);
      return this.NewOrMutate(max.Value, newLeft, this.right).FixRootBalance();
    }

    private AvlNode<T> RotateToGT()
    {
      if (this.left.IsEmpty || this.IsEmpty)
        return this;
      AvlNode<T> mutableIfNecessary = this.ToMutableIfNecessary(this.left);
      AvlNode<T> left = mutableIfNecessary.left;
      AvlNode<T> newRight = this.NewOrMutate(this.Value, mutableIfNecessary.right, this.right);
      return mutableIfNecessary.NewOrMutate(mutableIfNecessary.Value, left, newRight);
    }

    private AvlNode<T> RotateToLT()
    {
      if (this.right.IsEmpty || this.IsEmpty)
        return this;
      AvlNode<T> mutableIfNecessary = this.ToMutableIfNecessary(this.right);
      AvlNode<T> left = mutableIfNecessary.left;
      AvlNode<T> right = mutableIfNecessary.right;
      AvlNode<T> newLeft = this.NewOrMutate(this.Value, this.left, left);
      return mutableIfNecessary.NewOrMutate(mutableIfNecessary.Value, newLeft, right);
    }

    public IEnumerator<T> GetEnumerator(bool reverse)
    {
      Stack<AvlNode<T>> to_visit = new Stack<AvlNode<T>>();
      to_visit.Push(this);
      while (to_visit.Count > 0)
      {
        AvlNode<T> this_d = to_visit.Pop();
        while (!this_d.IsEmpty)
        {
          if (reverse)
          {
            if (this_d.right.IsEmpty)
            {
              yield return this_d.Value;
              this_d = this_d.left;
            }
            else
            {
              to_visit.Push(this_d.left);
              to_visit.Push(new AvlNode<T>(this_d.Value));
              this_d = this_d.right;
            }
          }
          else if (this_d.left.IsEmpty)
          {
            yield return this_d.Value;
            this_d = this_d.right;
          }
          else
          {
            if (!this_d.right.IsEmpty)
              to_visit.Push(this_d.right);
            to_visit.Push(new AvlNode<T>(this_d.Value));
            this_d = this_d.left;
          }
        }
      }
    }

    public IEnumerable<T> Enumerate(int index, int count, bool reverse)
    {
      IEnumerator<T> e = this.GetEnumerator(reverse);
      int i;
      if (!reverse)
      {
        i = 0;
        while (e.MoveNext())
        {
          if (index <= i)
            yield return e.Current;
          ++i;
          if (i >= index + count)
            break;
        }
      }
      else
      {
        i = this.Count - 1;
        while (e.MoveNext())
        {
          if (i <= index)
            yield return e.Current;
          --i;
          if (i <= index - count)
            break;
        }
      }
    }

    public virtual AvlNode<T> ToImmutable()
    {
      return this;
    }

    public virtual AvlNode<T> NewOrMutate(
      T newValue,
      AvlNode<T> newLeft,
      AvlNode<T> newRight)
    {
      return new AvlNode<T>(newValue, newLeft, newRight);
    }

    public virtual AvlNode<T> ToMutable()
    {
      return (AvlNode<T>) new AvlNode<T>.MutableAvlNode(this.Value, this.left, this.right);
    }

    public virtual AvlNode<T> ToMutableIfNecessary(AvlNode<T> node)
    {
      return node;
    }

    public virtual bool IsMutable
    {
      get
      {
        return false;
      }
    }

    private sealed class NullNode : AvlNode<T>
    {
      public override bool IsEmpty
      {
        get
        {
          return true;
        }
      }

      public override AvlNode<T> NewOrMutate(
        T newValue,
        AvlNode<T> newLeft,
        AvlNode<T> newRight)
      {
        throw new NotSupportedException();
      }

      public override AvlNode<T> ToMutable()
      {
        return (AvlNode<T>) this;
      }
    }

    private sealed class MutableAvlNode : AvlNode<T>
    {
      public MutableAvlNode(T val, AvlNode<T> lt, AvlNode<T> gt)
        : base(val, lt, gt)
      {
      }

      public override AvlNode<T> ToImmutable()
      {
        return new AvlNode<T>(this.Value, this.left.ToImmutable(), this.right.ToImmutable());
      }

      public override AvlNode<T> NewOrMutate(
        T newValue,
        AvlNode<T> newLeft,
        AvlNode<T> newRight)
      {
        this.Value = newValue;
        this.left = newLeft;
        this.right = newRight;
        this._count = 1 + this.left._count + this.right._count;
        this._depth = 1 + Math.Max(this.left._depth, this.right._depth);
        return (AvlNode<T>) this;
      }

      public override AvlNode<T> ToMutable()
      {
        return (AvlNode<T>) this;
      }

      public override AvlNode<T> ToMutableIfNecessary(AvlNode<T> node)
      {
        return node.ToMutable();
      }

      public override bool IsMutable
      {
        get
        {
          return true;
        }
      }
    }
  }
}
