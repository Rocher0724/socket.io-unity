using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Linq.JsonPath {
  internal class JPath {
    private readonly string _expression;
    private int _currentIndex;

    public List<PathFilter> Filters { get; }

    public JPath(string expression) {
      ValidationUtils.ArgumentNotNull((object) expression, nameof(expression));
      this._expression = expression;
      this.Filters = new List<PathFilter>();
      this.ParseMain();
    }

    private void ParseMain() {
      int currentIndex1 = this._currentIndex;
      this.EatWhitespace();
      if (this._expression.Length == this._currentIndex)
        return;
      if (this._expression[this._currentIndex] == '$') {
        if (this._expression.Length == 1)
          return;
        switch (this._expression[this._currentIndex + 1]) {
          case '.':
          case '[':
            ++this._currentIndex;
            currentIndex1 = this._currentIndex;
            break;
        }
      }

      if (this.ParsePath(this.Filters, currentIndex1, false))
        return;
      int currentIndex2 = this._currentIndex;
      this.EatWhitespace();
      if (this._currentIndex < this._expression.Length)
        throw new JsonException(
          "Unexpected character while parsing path: " + this._expression[currentIndex2].ToString());
    }

    private bool ParsePath(List<PathFilter> filters, int currentPartStartIndex, bool query) {
      bool scan = false;
      bool flag1 = false;
      bool flag2 = false;
      bool flag3 = false;
      while (this._currentIndex < this._expression.Length && !flag3) {
        char indexerOpenChar = this._expression[this._currentIndex];
        switch (indexerOpenChar) {
          case ' ':
            if (this._currentIndex < this._expression.Length) {
              flag3 = true;
              continue;
            }

            continue;
          case '(':
          case '[':
            if (this._currentIndex > currentPartStartIndex) {
              string member =
                this._expression.Substring(currentPartStartIndex, this._currentIndex - currentPartStartIndex);
              if (member == "*")
                member = (string) null;
              filters.Add(JPath.CreatePathFilter(member, scan));
              scan = false;
            }

            filters.Add(this.ParseIndexer(indexerOpenChar, scan));
            ++this._currentIndex;
            currentPartStartIndex = this._currentIndex;
            flag1 = true;
            flag2 = false;
            continue;
          case ')':
          case ']':
            flag3 = true;
            continue;
          case '.':
            if (this._currentIndex > currentPartStartIndex) {
              string member =
                this._expression.Substring(currentPartStartIndex, this._currentIndex - currentPartStartIndex);
              if (member == "*")
                member = (string) null;
              filters.Add(JPath.CreatePathFilter(member, scan));
              scan = false;
            }

            if (this._currentIndex + 1 < this._expression.Length && this._expression[this._currentIndex + 1] == '.') {
              scan = true;
              ++this._currentIndex;
            }

            ++this._currentIndex;
            currentPartStartIndex = this._currentIndex;
            flag1 = false;
            flag2 = true;
            continue;
          default:
            if (query && (indexerOpenChar == '=' || indexerOpenChar == '<' ||
                          (indexerOpenChar == '!' || indexerOpenChar == '>') ||
                          (indexerOpenChar == '|' || indexerOpenChar == '&'))) {
              flag3 = true;
              continue;
            }

            if (flag1)
              throw new JsonException("Unexpected character following indexer: " + indexerOpenChar.ToString());
            ++this._currentIndex;
            continue;
        }
      }

      bool flag4 = this._currentIndex == this._expression.Length;
      if (this._currentIndex > currentPartStartIndex) {
        string member = this._expression.Substring(currentPartStartIndex, this._currentIndex - currentPartStartIndex)
          .TrimEnd();
        if (member == "*")
          member = (string) null;
        filters.Add(JPath.CreatePathFilter(member, scan));
      } else if (flag2 && flag4 | query)
        throw new JsonException("Unexpected end while parsing path.");

      return flag4;
    }

    private static PathFilter CreatePathFilter(string member, bool scan) {
      if (!scan)
        return (PathFilter) new FieldFilter() {
          Name = member
        };
      return (PathFilter) new ScanFilter() {
        Name = member
      };
    }

    private PathFilter ParseIndexer(char indexerOpenChar, bool scan) {
      ++this._currentIndex;
      char indexerCloseChar = indexerOpenChar == '[' ? ']' : ')';
      this.EnsureLength("Path ended with open indexer.");
      this.EatWhitespace();
      if (this._expression[this._currentIndex] == '\'')
        return this.ParseQuotedField(indexerCloseChar, scan);
      if (this._expression[this._currentIndex] == '?')
        return this.ParseQuery(indexerCloseChar);
      return this.ParseArrayIndexer(indexerCloseChar);
    }

    private PathFilter ParseArrayIndexer(char indexerCloseChar) {
      int currentIndex = this._currentIndex;
      int? nullable1 = new int?();
      List<int> intList = (List<int>) null;
      int num = 0;
      int? nullable2 = new int?();
      int? nullable3 = new int?();
      int? nullable4 = new int?();
      while (this._currentIndex < this._expression.Length) {
        char c = this._expression[this._currentIndex];
        if (c == ' ') {
          nullable1 = new int?(this._currentIndex);
          this.EatWhitespace();
        } else {
          int? nullable5;
          if ((int) c == (int) indexerCloseChar) {
            nullable5 = nullable1;
            int length = (nullable5 ?? this._currentIndex) - currentIndex;
            if (intList != null) {
              if (length == 0)
                throw new JsonException("Array index expected.");
              int int32 = Convert.ToInt32(this._expression.Substring(currentIndex, length),
                (IFormatProvider) CultureInfo.InvariantCulture);
              intList.Add(int32);
              return (PathFilter) new ArrayMultipleIndexFilter() {
                Indexes = intList
              };
            }

            if (num > 0) {
              if (length > 0) {
                int int32 = Convert.ToInt32(this._expression.Substring(currentIndex, length),
                  (IFormatProvider) CultureInfo.InvariantCulture);
                if (num == 1)
                  nullable3 = new int?(int32);
                else
                  nullable4 = new int?(int32);
              }

              return (PathFilter) new ArraySliceFilter() {
                Start = nullable2,
                End = nullable3,
                Step = nullable4
              };
            }

            if (length == 0)
              throw new JsonException("Array index expected.");
            int int32_1 = Convert.ToInt32(this._expression.Substring(currentIndex, length),
              (IFormatProvider) CultureInfo.InvariantCulture);
            return (PathFilter) new ArrayIndexFilter() {
              Index = new int?(int32_1)
            };
          }

          switch (c) {
            case '*':
              ++this._currentIndex;
              this.EnsureLength("Path ended with open indexer.");
              this.EatWhitespace();
              if ((int) this._expression[this._currentIndex] != (int) indexerCloseChar)
                throw new JsonException("Unexpected character while parsing path indexer: " + c.ToString());
              return (PathFilter) new ArrayIndexFilter();
            case ',':
              nullable5 = nullable1;
              int length1 = (nullable5 ?? this._currentIndex) - currentIndex;
              if (length1 == 0)
                throw new JsonException("Array index expected.");
              if (intList == null)
                intList = new List<int>();
              string str = this._expression.Substring(currentIndex, length1);
              intList.Add(Convert.ToInt32(str, (IFormatProvider) CultureInfo.InvariantCulture));
              ++this._currentIndex;
              this.EatWhitespace();
              currentIndex = this._currentIndex;
              nullable1 = new int?();
              continue;
            case ':':
              nullable5 = nullable1;
              int length2 = (nullable5 ?? this._currentIndex) - currentIndex;
              if (length2 > 0) {
                int int32 = Convert.ToInt32(this._expression.Substring(currentIndex, length2),
                  (IFormatProvider) CultureInfo.InvariantCulture);
                switch (num) {
                  case 0:
                    nullable2 = new int?(int32);
                    break;
                  case 1:
                    nullable3 = new int?(int32);
                    break;
                  default:
                    nullable4 = new int?(int32);
                    break;
                }
              }

              ++num;
              ++this._currentIndex;
              this.EatWhitespace();
              currentIndex = this._currentIndex;
              nullable1 = new int?();
              continue;
            default:
              if (!char.IsDigit(c) && c != '-')
                throw new JsonException("Unexpected character while parsing path indexer: " + c.ToString());
              if (nullable1.HasValue)
                throw new JsonException("Unexpected character while parsing path indexer: " + c.ToString());
              ++this._currentIndex;
              continue;
          }
        }
      }

      throw new JsonException("Path ended with open indexer.");
    }

    private void EatWhitespace() {
      while (this._currentIndex < this._expression.Length && this._expression[this._currentIndex] == ' ')
        ++this._currentIndex;
    }

    private PathFilter ParseQuery(char indexerCloseChar) {
      ++this._currentIndex;
      this.EnsureLength("Path ended with open indexer.");
      if (this._expression[this._currentIndex] != '(')
        throw new JsonException("Unexpected character while parsing path indexer: " +
                                this._expression[this._currentIndex].ToString());
      ++this._currentIndex;
      QueryExpression expression = this.ParseExpression();
      ++this._currentIndex;
      this.EnsureLength("Path ended with open indexer.");
      this.EatWhitespace();
      if ((int) this._expression[this._currentIndex] != (int) indexerCloseChar)
        throw new JsonException("Unexpected character while parsing path indexer: " +
                                this._expression[this._currentIndex].ToString());
      return (PathFilter) new QueryFilter() {
        Expression = expression
      };
    }

    private bool TryParseExpression(out List<PathFilter> expressionPath) {
      if (this._expression[this._currentIndex] == '$') {
        expressionPath = new List<PathFilter>();
        expressionPath.Add((PathFilter) RootFilter.Instance);
      } else if (this._expression[this._currentIndex] == '@') {
        expressionPath = new List<PathFilter>();
      } else {
        expressionPath = (List<PathFilter>) null;
        return false;
      }

      ++this._currentIndex;
      if (this.ParsePath(expressionPath, this._currentIndex, true))
        throw new JsonException("Path ended with open query.");
      return true;
    }

    private JsonException CreateUnexpectedCharacterException() {
      return new JsonException("Unexpected character while parsing path query: " +
                               this._expression[this._currentIndex].ToString());
    }

    private object ParseSide() {
      this.EatWhitespace();
      List<PathFilter> expressionPath;
      if (this.TryParseExpression(out expressionPath)) {
        this.EatWhitespace();
        this.EnsureLength("Path ended with open query.");
        return (object) expressionPath;
      }

      object obj;
      if (!this.TryParseValue(out obj))
        throw this.CreateUnexpectedCharacterException();
      this.EatWhitespace();
      this.EnsureLength("Path ended with open query.");
      return (object) new JValue(obj);
    }

    private QueryExpression ParseExpression() {
      QueryExpression queryExpression = (QueryExpression) null;
      CompositeExpression compositeExpression1 = (CompositeExpression) null;
      while (this._currentIndex < this._expression.Length) {
        object side = this.ParseSide();
        object obj = (object) null;
        QueryOperator queryOperator;
        if (this._expression[this._currentIndex] == ')' || this._expression[this._currentIndex] == '|' ||
            this._expression[this._currentIndex] == '&') {
          queryOperator = QueryOperator.Exists;
        } else {
          queryOperator = this.ParseOperator();
          obj = this.ParseSide();
        }

        BooleanQueryExpression booleanQueryExpression1 = new BooleanQueryExpression();
        booleanQueryExpression1.Left = side;
        booleanQueryExpression1.Operator = queryOperator;
        booleanQueryExpression1.Right = obj;
        BooleanQueryExpression booleanQueryExpression2 = booleanQueryExpression1;
        if (this._expression[this._currentIndex] == ')') {
          if (compositeExpression1 == null)
            return (QueryExpression) booleanQueryExpression2;
          compositeExpression1.Expressions.Add((QueryExpression) booleanQueryExpression2);
          return queryExpression;
        }

        if (this._expression[this._currentIndex] == '&') {
          if (!this.Match("&&"))
            throw this.CreateUnexpectedCharacterException();
          if (compositeExpression1 == null || compositeExpression1.Operator != QueryOperator.And) {
            CompositeExpression compositeExpression2 = new CompositeExpression();
            compositeExpression2.Operator = QueryOperator.And;
            CompositeExpression compositeExpression3 = compositeExpression2;
            compositeExpression1?.Expressions.Add((QueryExpression) compositeExpression3);
            compositeExpression1 = compositeExpression3;
            if (queryExpression == null)
              queryExpression = (QueryExpression) compositeExpression1;
          }

          compositeExpression1.Expressions.Add((QueryExpression) booleanQueryExpression2);
        }

        if (this._expression[this._currentIndex] == '|') {
          if (!this.Match("||"))
            throw this.CreateUnexpectedCharacterException();
          if (compositeExpression1 == null || compositeExpression1.Operator != QueryOperator.Or) {
            CompositeExpression compositeExpression2 = new CompositeExpression();
            compositeExpression2.Operator = QueryOperator.Or;
            CompositeExpression compositeExpression3 = compositeExpression2;
            compositeExpression1?.Expressions.Add((QueryExpression) compositeExpression3);
            compositeExpression1 = compositeExpression3;
            if (queryExpression == null)
              queryExpression = (QueryExpression) compositeExpression1;
          }

          compositeExpression1.Expressions.Add((QueryExpression) booleanQueryExpression2);
        }
      }

      throw new JsonException("Path ended with open query.");
    }

    private bool TryParseValue(out object value) {
      char c = this._expression[this._currentIndex];
      if (c == '\'') {
        value = (object) this.ReadQuotedString();
        return true;
      }

      if (!char.IsDigit(c)) {
        switch (c) {
          case '-':
            break;
          case 'f':
            if (this.Match("false")) {
              value = (object) false;
              return true;
            }

            goto label_17;
          case 'n':
            if (this.Match("null")) {
              value = (object) null;
              return true;
            }

            goto label_17;
          case 't':
            if (this.Match("true")) {
              value = (object) true;
              return true;
            }

            goto label_17;
          default:
            goto label_17;
        }
      }

      StringBuilder stringBuilder = new StringBuilder();
      stringBuilder.Append(c);
      for (++this._currentIndex; this._currentIndex < this._expression.Length; ++this._currentIndex) {
        char ch = this._expression[this._currentIndex];
        switch (ch) {
          case ' ':
          case ')':
            string s = stringBuilder.ToString();
            if (s.IndexOfAny(new char[3] {'.', 'E', 'e'}) != -1) {
              double result;
              int num = double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands,
                (IFormatProvider) CultureInfo.InvariantCulture, out result)
                ? 1
                : 0;
              value = (object) result;
              return num != 0;
            }

            long result1;
            int num1 = long.TryParse(s, NumberStyles.Integer, (IFormatProvider) CultureInfo.InvariantCulture,
              out result1)
              ? 1
              : 0;
            value = (object) result1;
            return num1 != 0;
          default:
            stringBuilder.Append(ch);
            continue;
        }
      }

      label_17:
      value = (object) null;
      return false;
    }

    private string ReadQuotedString() {
      StringBuilder stringBuilder = new StringBuilder();
      ++this._currentIndex;
      while (this._currentIndex < this._expression.Length) {
        char ch = this._expression[this._currentIndex];
        if (ch == '\\' && this._currentIndex + 1 < this._expression.Length) {
          ++this._currentIndex;
          if (this._expression[this._currentIndex] == '\'') {
            stringBuilder.Append('\'');
          } else {
            if (this._expression[this._currentIndex] != '\\')
              throw new JsonException("Unknown escape character: \\" + this._expression[this._currentIndex].ToString());
            stringBuilder.Append('\\');
          }

          ++this._currentIndex;
        } else {
          if (ch == '\'') {
            ++this._currentIndex;
            return stringBuilder.ToString();
          }

          ++this._currentIndex;
          stringBuilder.Append(ch);
        }
      }

      throw new JsonException("Path ended with an open string.");
    }

    private bool Match(string s) {
      int currentIndex = this._currentIndex;
      foreach (char ch in s) {
        if (currentIndex >= this._expression.Length || (int) this._expression[currentIndex] != (int) ch)
          return false;
        ++currentIndex;
      }

      this._currentIndex = currentIndex;
      return true;
    }

    private QueryOperator ParseOperator() {
      if (this._currentIndex + 1 >= this._expression.Length)
        throw new JsonException("Path ended with open query.");
      if (this.Match("=="))
        return QueryOperator.Equals;
      if (this.Match("!=") || this.Match("<>"))
        return QueryOperator.NotEquals;
      if (this.Match("<="))
        return QueryOperator.LessThanOrEquals;
      if (this.Match("<"))
        return QueryOperator.LessThan;
      if (this.Match(">="))
        return QueryOperator.GreaterThanOrEquals;
      if (this.Match(">"))
        return QueryOperator.GreaterThan;
      throw new JsonException("Could not read query operator.");
    }

    private PathFilter ParseQuotedField(char indexerCloseChar, bool scan) {
      List<string> stringList = (List<string>) null;
      while (this._currentIndex < this._expression.Length) {
        string member = this.ReadQuotedString();
        this.EatWhitespace();
        this.EnsureLength("Path ended with open indexer.");
        if ((int) this._expression[this._currentIndex] == (int) indexerCloseChar) {
          if (stringList == null)
            return JPath.CreatePathFilter(member, scan);
          stringList.Add(member);
          if (!scan)
            return (PathFilter) new FieldMultipleFilter() {
              Names = stringList
            };
          return (PathFilter) new ScanMultipleFilter() {
            Names = stringList
          };
        }

        if (this._expression[this._currentIndex] != ',')
          throw new JsonException("Unexpected character while parsing path indexer: " +
                                  this._expression[this._currentIndex].ToString());
        ++this._currentIndex;
        this.EatWhitespace();
        if (stringList == null)
          stringList = new List<string>();
        stringList.Add(member);
      }

      throw new JsonException("Path ended with open indexer.");
    }

    private void EnsureLength(string message) {
      if (this._currentIndex >= this._expression.Length)
        throw new JsonException(message);
    }

    internal IEnumerable<JToken> Evaluate(
      JToken root,
      JToken t,
      bool errorWhenNoMatch) {
      return JPath.Evaluate(this.Filters, root, t, errorWhenNoMatch);
    }

    internal static IEnumerable<JToken> Evaluate(
      List<PathFilter> filters,
      JToken root,
      JToken t,
      bool errorWhenNoMatch) {
      IEnumerable<JToken> current = (IEnumerable<JToken>) new JToken[1] {
        t
      };
      foreach (PathFilter filter in filters)
        current = filter.ExecuteFilter(root, current, errorWhenNoMatch);
      return current;
    }
  }
}