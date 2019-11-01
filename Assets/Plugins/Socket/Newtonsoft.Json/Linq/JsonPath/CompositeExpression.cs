using System;
using System.Collections.Generic;

namespace Socket.Newtonsoft.Json.Linq.JsonPath {
  internal class CompositeExpression : QueryExpression {
    public List<QueryExpression> Expressions { get; set; }

    public CompositeExpression() {
      this.Expressions = new List<QueryExpression>();
    }

    public override bool IsMatch(JToken root, JToken t) {
      switch (this.Operator) {
        case QueryOperator.And:
          foreach (QueryExpression expression in this.Expressions) {
            if (!expression.IsMatch(root, t))
              return false;
          }

          return true;
        case QueryOperator.Or:
          foreach (QueryExpression expression in this.Expressions) {
            if (expression.IsMatch(root, t))
              return true;
          }

          return false;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }
  }
}