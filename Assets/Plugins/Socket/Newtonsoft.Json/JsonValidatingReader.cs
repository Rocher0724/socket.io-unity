using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Socket.Newtonsoft.Json.Linq;
using Socket.Newtonsoft.Json.Schema;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json {
  [Obsolete(
    "JSON Schema validation has been moved to its own package. See http://www.newtonsoft.com/jsonschema for more details.")]
  public class JsonValidatingReader : JsonReader, IJsonLineInfo {
    private static readonly IList<JsonSchemaModel> EmptySchemaList =
      (IList<JsonSchemaModel>) new List<JsonSchemaModel>();

    private readonly JsonReader _reader;
    private readonly Stack<JsonValidatingReader.SchemaScope> _stack;
    private JsonSchema _schema;
    private JsonSchemaModel _model;
    private JsonValidatingReader.SchemaScope _currentScope;

    public event Newtonsoft.Json.Schema.ValidationEventHandler ValidationEventHandler;

    public override object Value {
      get { return this._reader.Value; }
    }

    public override int Depth {
      get { return this._reader.Depth; }
    }

    public override string Path {
      get { return this._reader.Path; }
    }

    public override char QuoteChar {
      get { return this._reader.QuoteChar; }
      protected internal set { }
    }

    public override JsonToken TokenType {
      get { return this._reader.TokenType; }
    }

    public override Type ValueType {
      get { return this._reader.ValueType; }
    }

    private void Push(JsonValidatingReader.SchemaScope scope) {
      this._stack.Push(scope);
      this._currentScope = scope;
    }

    private JsonValidatingReader.SchemaScope Pop() {
      JsonValidatingReader.SchemaScope schemaScope = this._stack.Pop();
      this._currentScope = this._stack.Count != 0 ? this._stack.Peek() : (JsonValidatingReader.SchemaScope) null;
      return schemaScope;
    }

    private IList<JsonSchemaModel> CurrentSchemas {
      get { return this._currentScope.Schemas; }
    }

    private IList<JsonSchemaModel> CurrentMemberSchemas {
      get {
        if (this._currentScope == null)
          return (IList<JsonSchemaModel>) new List<JsonSchemaModel>(
            (IEnumerable<JsonSchemaModel>) new JsonSchemaModel[1] {
              this._model
            });
        if (this._currentScope.Schemas == null || this._currentScope.Schemas.Count == 0)
          return JsonValidatingReader.EmptySchemaList;
        switch (this._currentScope.TokenType) {
          case JTokenType.None:
            return this._currentScope.Schemas;
          case JTokenType.Object:
            if (this._currentScope.CurrentPropertyName == null)
              throw new JsonReaderException("CurrentPropertyName has not been set on scope.");
            IList<JsonSchemaModel> jsonSchemaModelList1 = (IList<JsonSchemaModel>) new List<JsonSchemaModel>();
            foreach (JsonSchemaModel currentSchema in (IEnumerable<JsonSchemaModel>) this.CurrentSchemas) {
              JsonSchemaModel jsonSchemaModel;
              if (currentSchema.Properties != null &&
                  currentSchema.Properties.TryGetValue(this._currentScope.CurrentPropertyName, out jsonSchemaModel))
                jsonSchemaModelList1.Add(jsonSchemaModel);
              if (currentSchema.PatternProperties != null) {
                foreach (KeyValuePair<string, JsonSchemaModel> patternProperty in
                  (IEnumerable<KeyValuePair<string, JsonSchemaModel>>) currentSchema.PatternProperties) {
                  if (Regex.IsMatch(this._currentScope.CurrentPropertyName, patternProperty.Key))
                    jsonSchemaModelList1.Add(patternProperty.Value);
                }
              }

              if (jsonSchemaModelList1.Count == 0 && currentSchema.AllowAdditionalProperties &&
                  currentSchema.AdditionalProperties != null)
                jsonSchemaModelList1.Add(currentSchema.AdditionalProperties);
            }

            return jsonSchemaModelList1;
          case JTokenType.Array:
            IList<JsonSchemaModel> jsonSchemaModelList2 = (IList<JsonSchemaModel>) new List<JsonSchemaModel>();
            foreach (JsonSchemaModel currentSchema in (IEnumerable<JsonSchemaModel>) this.CurrentSchemas) {
              if (!currentSchema.PositionalItemsValidation) {
                if (currentSchema.Items != null && currentSchema.Items.Count > 0)
                  jsonSchemaModelList2.Add(currentSchema.Items[0]);
              } else {
                if (currentSchema.Items != null && currentSchema.Items.Count > 0 &&
                    currentSchema.Items.Count > this._currentScope.ArrayItemCount - 1)
                  jsonSchemaModelList2.Add(currentSchema.Items[this._currentScope.ArrayItemCount - 1]);
                if (currentSchema.AllowAdditionalItems && currentSchema.AdditionalItems != null)
                  jsonSchemaModelList2.Add(currentSchema.AdditionalItems);
              }
            }

            return jsonSchemaModelList2;
          case JTokenType.Constructor:
            return JsonValidatingReader.EmptySchemaList;
          default:
            throw new ArgumentOutOfRangeException("TokenType",
              "Unexpected token type: {0}".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                (object) this._currentScope.TokenType));
        }
      }
    }

    private void RaiseError(string message, JsonSchemaModel schema) {
      IJsonLineInfo jsonLineInfo = (IJsonLineInfo) this;
      this.OnValidationEvent(new JsonSchemaException(
        jsonLineInfo.HasLineInfo()
          ? message + " Line {0}, position {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) jsonLineInfo.LineNumber, (object) jsonLineInfo.LinePosition)
          : message, (Exception) null, this.Path, jsonLineInfo.LineNumber, jsonLineInfo.LinePosition));
    }

    private void OnValidationEvent(JsonSchemaException exception) {
      Newtonsoft.Json.Schema.ValidationEventHandler validationEventHandler = this.ValidationEventHandler;
      if (validationEventHandler == null)
        throw exception;
      validationEventHandler((object) this, new ValidationEventArgs(exception));
    }

    public JsonValidatingReader(JsonReader reader) {
      ValidationUtils.ArgumentNotNull((object) reader, nameof(reader));
      this._reader = reader;
      this._stack = new Stack<JsonValidatingReader.SchemaScope>();
    }

    public JsonSchema Schema {
      get { return this._schema; }
      set {
        if (this.TokenType != JsonToken.None)
          throw new InvalidOperationException("Cannot change schema while validating JSON.");
        this._schema = value;
        this._model = (JsonSchemaModel) null;
      }
    }

    public JsonReader Reader {
      get { return this._reader; }
    }

    public override void Close() {
      base.Close();
      if (!this.CloseInput)
        return;
      this._reader?.Close();
    }

    private void ValidateNotDisallowed(JsonSchemaModel schema) {
      if (schema == null)
        return;
      JsonSchemaType? currentNodeSchemaType = this.GetCurrentNodeSchemaType();
      if (!currentNodeSchemaType.HasValue || !JsonSchemaGenerator.HasFlag(new JsonSchemaType?(schema.Disallow),
            currentNodeSchemaType.GetValueOrDefault()))
        return;
      this.RaiseError(
        "Type {0} is disallowed.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) currentNodeSchemaType), schema);
    }

    private JsonSchemaType? GetCurrentNodeSchemaType() {
      switch (this._reader.TokenType) {
        case JsonToken.StartObject:
          return new JsonSchemaType?(JsonSchemaType.Object);
        case JsonToken.StartArray:
          return new JsonSchemaType?(JsonSchemaType.Array);
        case JsonToken.Integer:
          return new JsonSchemaType?(JsonSchemaType.Integer);
        case JsonToken.Float:
          return new JsonSchemaType?(JsonSchemaType.Float);
        case JsonToken.String:
          return new JsonSchemaType?(JsonSchemaType.String);
        case JsonToken.Boolean:
          return new JsonSchemaType?(JsonSchemaType.Boolean);
        case JsonToken.Null:
          return new JsonSchemaType?(JsonSchemaType.Null);
        default:
          return new JsonSchemaType?();
      }
    }

    public override int? ReadAsInt32() {
      int? nullable = this._reader.ReadAsInt32();
      this.ValidateCurrentToken();
      return nullable;
    }

    public override byte[] ReadAsBytes() {
      byte[] numArray = this._reader.ReadAsBytes();
      this.ValidateCurrentToken();
      return numArray;
    }

    public override Decimal? ReadAsDecimal() {
      Decimal? nullable = this._reader.ReadAsDecimal();
      this.ValidateCurrentToken();
      return nullable;
    }

    public override double? ReadAsDouble() {
      double? nullable = this._reader.ReadAsDouble();
      this.ValidateCurrentToken();
      return nullable;
    }

    public override bool? ReadAsBoolean() {
      bool? nullable = this._reader.ReadAsBoolean();
      this.ValidateCurrentToken();
      return nullable;
    }

    public override string ReadAsString() {
      string str = this._reader.ReadAsString();
      this.ValidateCurrentToken();
      return str;
    }

    public override DateTime? ReadAsDateTime() {
      DateTime? nullable = this._reader.ReadAsDateTime();
      this.ValidateCurrentToken();
      return nullable;
    }

    public override bool Read() {
      if (!this._reader.Read())
        return false;
      if (this._reader.TokenType == JsonToken.Comment)
        return true;
      this.ValidateCurrentToken();
      return true;
    }

    private void ValidateCurrentToken() {
      if (this._model == null) {
        this._model = new JsonSchemaModelBuilder().Build(this._schema);
        if (!JsonTokenUtils.IsStartToken(this._reader.TokenType))
          this.Push(new JsonValidatingReader.SchemaScope(JTokenType.None, this.CurrentMemberSchemas));
      }

      switch (this._reader.TokenType) {
        case JsonToken.None:
          break;
        case JsonToken.StartObject:
          this.ProcessValue();
          this.Push(new JsonValidatingReader.SchemaScope(JTokenType.Object,
            (IList<JsonSchemaModel>) this.CurrentMemberSchemas
              .Where<JsonSchemaModel>(new Func<JsonSchemaModel, bool>(this.ValidateObject)).ToList<JsonSchemaModel>()));
          this.WriteToken(this.CurrentSchemas);
          break;
        case JsonToken.StartArray:
          this.ProcessValue();
          this.Push(new JsonValidatingReader.SchemaScope(JTokenType.Array,
            (IList<JsonSchemaModel>) this.CurrentMemberSchemas
              .Where<JsonSchemaModel>(new Func<JsonSchemaModel, bool>(this.ValidateArray)).ToList<JsonSchemaModel>()));
          this.WriteToken(this.CurrentSchemas);
          break;
        case JsonToken.StartConstructor:
          this.ProcessValue();
          this.Push(new JsonValidatingReader.SchemaScope(JTokenType.Constructor, (IList<JsonSchemaModel>) null));
          this.WriteToken(this.CurrentSchemas);
          break;
        case JsonToken.PropertyName:
          this.WriteToken(this.CurrentSchemas);
          using (IEnumerator<JsonSchemaModel> enumerator = this.CurrentSchemas.GetEnumerator()) {
            while (enumerator.MoveNext())
              this.ValidatePropertyName(enumerator.Current);
            break;
          }
        case JsonToken.Raw:
          this.ProcessValue();
          break;
        case JsonToken.Integer:
          this.ProcessValue();
          this.WriteToken(this.CurrentMemberSchemas);
          using (IEnumerator<JsonSchemaModel> enumerator = this.CurrentMemberSchemas.GetEnumerator()) {
            while (enumerator.MoveNext())
              this.ValidateInteger(enumerator.Current);
            break;
          }
        case JsonToken.Float:
          this.ProcessValue();
          this.WriteToken(this.CurrentMemberSchemas);
          using (IEnumerator<JsonSchemaModel> enumerator = this.CurrentMemberSchemas.GetEnumerator()) {
            while (enumerator.MoveNext())
              this.ValidateFloat(enumerator.Current);
            break;
          }
        case JsonToken.String:
          this.ProcessValue();
          this.WriteToken(this.CurrentMemberSchemas);
          using (IEnumerator<JsonSchemaModel> enumerator = this.CurrentMemberSchemas.GetEnumerator()) {
            while (enumerator.MoveNext())
              this.ValidateString(enumerator.Current);
            break;
          }
        case JsonToken.Boolean:
          this.ProcessValue();
          this.WriteToken(this.CurrentMemberSchemas);
          using (IEnumerator<JsonSchemaModel> enumerator = this.CurrentMemberSchemas.GetEnumerator()) {
            while (enumerator.MoveNext())
              this.ValidateBoolean(enumerator.Current);
            break;
          }
        case JsonToken.Null:
          this.ProcessValue();
          this.WriteToken(this.CurrentMemberSchemas);
          using (IEnumerator<JsonSchemaModel> enumerator = this.CurrentMemberSchemas.GetEnumerator()) {
            while (enumerator.MoveNext())
              this.ValidateNull(enumerator.Current);
            break;
          }
        case JsonToken.Undefined:
        case JsonToken.Date:
        case JsonToken.Bytes:
          this.WriteToken(this.CurrentMemberSchemas);
          break;
        case JsonToken.EndObject:
          this.WriteToken(this.CurrentSchemas);
          foreach (JsonSchemaModel currentSchema in (IEnumerable<JsonSchemaModel>) this.CurrentSchemas)
            this.ValidateEndObject(currentSchema);
          this.Pop();
          break;
        case JsonToken.EndArray:
          this.WriteToken(this.CurrentSchemas);
          foreach (JsonSchemaModel currentSchema in (IEnumerable<JsonSchemaModel>) this.CurrentSchemas)
            this.ValidateEndArray(currentSchema);
          this.Pop();
          break;
        case JsonToken.EndConstructor:
          this.WriteToken(this.CurrentSchemas);
          this.Pop();
          break;
        default:
          throw new ArgumentOutOfRangeException();
      }
    }

    private void WriteToken(IList<JsonSchemaModel> schemas) {
      foreach (JsonValidatingReader.SchemaScope schemaScope in this._stack) {
        bool flag = schemaScope.TokenType == JTokenType.Array && schemaScope.IsUniqueArray &&
                    schemaScope.ArrayItemCount > 0;
        if (flag || schemas.Any<JsonSchemaModel>((Func<JsonSchemaModel, bool>) (s => s.Enum != null))) {
          if (schemaScope.CurrentItemWriter == null) {
            if (!JsonTokenUtils.IsEndToken(this._reader.TokenType))
              schemaScope.CurrentItemWriter = new JTokenWriter();
            else
              continue;
          }

          schemaScope.CurrentItemWriter.WriteToken(this._reader, false);
          if (schemaScope.CurrentItemWriter.Top == 0 && this._reader.TokenType != JsonToken.PropertyName) {
            JToken token = schemaScope.CurrentItemWriter.Token;
            schemaScope.CurrentItemWriter = (JTokenWriter) null;
            if (flag) {
              if (schemaScope.UniqueArrayItems.Contains<JToken>(token,
                (IEqualityComparer<JToken>) JToken.EqualityComparer))
                this.RaiseError(
                  "Non-unique array item at index {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                    (object) (schemaScope.ArrayItemCount - 1)),
                  schemaScope.Schemas.First<JsonSchemaModel>((Func<JsonSchemaModel, bool>) (s => s.UniqueItems)));
              schemaScope.UniqueArrayItems.Add(token);
            } else if (schemas.Any<JsonSchemaModel>((Func<JsonSchemaModel, bool>) (s => s.Enum != null))) {
              foreach (JsonSchemaModel schema in (IEnumerable<JsonSchemaModel>) schemas) {
                if (schema.Enum != null &&
                    !schema.Enum.ContainsValue<JToken>(token, (IEqualityComparer<JToken>) JToken.EqualityComparer)) {
                  StringWriter stringWriter = new StringWriter((IFormatProvider) CultureInfo.InvariantCulture);
                  token.WriteTo((JsonWriter) new JsonTextWriter((TextWriter) stringWriter));
                  this.RaiseError(
                    "Value {0} is not defined in enum.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
                      (object) stringWriter.ToString()), schema);
                }
              }
            }
          }
        }
      }
    }

    private void ValidateEndObject(JsonSchemaModel schema) {
      if (schema == null)
        return;
      Dictionary<string, bool> requiredProperties = this._currentScope.RequiredProperties;
      if (requiredProperties == null || !requiredProperties.Values.Any<bool>((Func<bool, bool>) (v => !v)))
        return;
      this.RaiseError(
        "Required properties are missing from object: {0}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) string.Join(", ",
            requiredProperties
              .Where<KeyValuePair<string, bool>>((Func<KeyValuePair<string, bool>, bool>) (kv => !kv.Value))
              .Select<KeyValuePair<string, bool>, string>((Func<KeyValuePair<string, bool>, string>) (kv => kv.Key))
              .ToArray<string>())), schema);
    }

    private void ValidateEndArray(JsonSchemaModel schema) {
      if (schema == null)
        return;
      int arrayItemCount = this._currentScope.ArrayItemCount;
      if (schema.MaximumItems.HasValue) {
        int num = arrayItemCount;
        int? maximumItems = schema.MaximumItems;
        int valueOrDefault = maximumItems.GetValueOrDefault();
        if ((num > valueOrDefault ? (maximumItems.HasValue ? 1 : 0) : 0) != 0)
          this.RaiseError(
            "Array item count {0} exceeds maximum count of {1}.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) arrayItemCount, (object) schema.MaximumItems),
            schema);
      }

      int? minimumItems = schema.MinimumItems;
      if (!minimumItems.HasValue)
        return;
      int num1 = arrayItemCount;
      minimumItems = schema.MinimumItems;
      int valueOrDefault1 = minimumItems.GetValueOrDefault();
      if ((num1 < valueOrDefault1 ? (minimumItems.HasValue ? 1 : 0) : 0) == 0)
        return;
      this.RaiseError(
        "Array item count {0} is less than minimum count of {1}.".FormatWith(
          (IFormatProvider) CultureInfo.InvariantCulture, (object) arrayItemCount, (object) schema.MinimumItems),
        schema);
    }

    private void ValidateNull(JsonSchemaModel schema) {
      if (schema == null || !this.TestType(schema, JsonSchemaType.Null))
        return;
      this.ValidateNotDisallowed(schema);
    }

    private void ValidateBoolean(JsonSchemaModel schema) {
      if (schema == null || !this.TestType(schema, JsonSchemaType.Boolean))
        return;
      this.ValidateNotDisallowed(schema);
    }

    private void ValidateString(JsonSchemaModel schema) {
      if (schema == null || !this.TestType(schema, JsonSchemaType.String))
        return;
      this.ValidateNotDisallowed(schema);
      string input = this._reader.Value.ToString();
      int? nullable = schema.MaximumLength;
      if (nullable.HasValue) {
        int length = input.Length;
        nullable = schema.MaximumLength;
        int valueOrDefault = nullable.GetValueOrDefault();
        if ((length > valueOrDefault ? (nullable.HasValue ? 1 : 0) : 0) != 0)
          this.RaiseError(
            "String '{0}' exceeds maximum length of {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) input, (object) schema.MaximumLength), schema);
      }

      nullable = schema.MinimumLength;
      if (nullable.HasValue) {
        int length = input.Length;
        nullable = schema.MinimumLength;
        int valueOrDefault = nullable.GetValueOrDefault();
        if ((length < valueOrDefault ? (nullable.HasValue ? 1 : 0) : 0) != 0)
          this.RaiseError(
            "String '{0}' is less than minimum length of {1}.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) input, (object) schema.MinimumLength), schema);
      }

      if (schema.Patterns == null)
        return;
      foreach (string pattern in (IEnumerable<string>) schema.Patterns) {
        if (!Regex.IsMatch(input, pattern))
          this.RaiseError(
            "String '{0}' does not match regex pattern '{1}'.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) input, (object) pattern), schema);
      }
    }

    private void ValidateInteger(JsonSchemaModel schema) {
      if (schema == null || !this.TestType(schema, JsonSchemaType.Integer))
        return;
      this.ValidateNotDisallowed(schema);
      object objA = this._reader.Value;
      if (schema.Maximum.HasValue) {
        if (JValue.Compare(JTokenType.Integer, objA, (object) schema.Maximum) > 0)
          this.RaiseError(
            "Integer {0} exceeds maximum value of {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture, objA,
              (object) schema.Maximum), schema);
        if (schema.ExclusiveMaximum && JValue.Compare(JTokenType.Integer, objA, (object) schema.Maximum) == 0)
          this.RaiseError(
            "Integer {0} equals maximum value of {1} and exclusive maximum is true.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, objA, (object) schema.Maximum), schema);
      }

      double? nullable = schema.Minimum;
      if (nullable.HasValue) {
        if (JValue.Compare(JTokenType.Integer, objA, (object) schema.Minimum) < 0)
          this.RaiseError(
            "Integer {0} is less than minimum value of {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              objA, (object) schema.Minimum), schema);
        if (schema.ExclusiveMinimum && JValue.Compare(JTokenType.Integer, objA, (object) schema.Minimum) == 0)
          this.RaiseError(
            "Integer {0} equals minimum value of {1} and exclusive minimum is true.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, objA, (object) schema.Minimum), schema);
      }

      nullable = schema.DivisibleBy;
      if (!nullable.HasValue)
        return;
      double int64 = (double) Convert.ToInt64(objA, (IFormatProvider) CultureInfo.InvariantCulture);
      nullable = schema.DivisibleBy;
      double valueOrDefault = nullable.GetValueOrDefault();
      if (JsonValidatingReader.IsZero(int64 % valueOrDefault))
        return;
      this.RaiseError(
        "Integer {0} is not evenly divisible by {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) JsonConvert.ToString(objA), (object) schema.DivisibleBy), schema);
    }

    private void ProcessValue() {
      if (this._currentScope == null || this._currentScope.TokenType != JTokenType.Array)
        return;
      ++this._currentScope.ArrayItemCount;
      foreach (JsonSchemaModel currentSchema in (IEnumerable<JsonSchemaModel>) this.CurrentSchemas) {
        if (currentSchema != null && currentSchema.PositionalItemsValidation && !currentSchema.AllowAdditionalItems &&
            (currentSchema.Items == null || this._currentScope.ArrayItemCount - 1 >= currentSchema.Items.Count))
          this.RaiseError(
            "Index {0} has not been defined and the schema does not allow additional items.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) this._currentScope.ArrayItemCount),
            currentSchema);
      }
    }

    private void ValidateFloat(JsonSchemaModel schema) {
      if (schema == null || !this.TestType(schema, JsonSchemaType.Float))
        return;
      this.ValidateNotDisallowed(schema);
      double num1 = Convert.ToDouble(this._reader.Value, (IFormatProvider) CultureInfo.InvariantCulture);
      double? nullable = schema.Maximum;
      if (nullable.HasValue) {
        double num2 = num1;
        nullable = schema.Maximum;
        double valueOrDefault1 = nullable.GetValueOrDefault();
        if ((num2 > valueOrDefault1 ? (nullable.HasValue ? 1 : 0) : 0) != 0)
          this.RaiseError(
            "Float {0} exceeds maximum value of {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) JsonConvert.ToString(num1), (object) schema.Maximum), schema);
        if (schema.ExclusiveMaximum) {
          double num3 = num1;
          nullable = schema.Maximum;
          double valueOrDefault2 = nullable.GetValueOrDefault();
          if ((num3 == valueOrDefault2 ? (nullable.HasValue ? 1 : 0) : 0) != 0)
            this.RaiseError(
              "Float {0} equals maximum value of {1} and exclusive maximum is true.".FormatWith(
                (IFormatProvider) CultureInfo.InvariantCulture, (object) JsonConvert.ToString(num1),
                (object) schema.Maximum), schema);
        }
      }

      nullable = schema.Minimum;
      if (nullable.HasValue) {
        double num2 = num1;
        nullable = schema.Minimum;
        double valueOrDefault1 = nullable.GetValueOrDefault();
        if ((num2 < valueOrDefault1 ? (nullable.HasValue ? 1 : 0) : 0) != 0)
          this.RaiseError(
            "Float {0} is less than minimum value of {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
              (object) JsonConvert.ToString(num1), (object) schema.Minimum), schema);
        if (schema.ExclusiveMinimum) {
          double num3 = num1;
          nullable = schema.Minimum;
          double valueOrDefault2 = nullable.GetValueOrDefault();
          if ((num3 == valueOrDefault2 ? (nullable.HasValue ? 1 : 0) : 0) != 0)
            this.RaiseError(
              "Float {0} equals minimum value of {1} and exclusive minimum is true.".FormatWith(
                (IFormatProvider) CultureInfo.InvariantCulture, (object) JsonConvert.ToString(num1),
                (object) schema.Minimum), schema);
        }
      }

      nullable = schema.DivisibleBy;
      if (!nullable.HasValue)
        return;
      double dividend = num1;
      nullable = schema.DivisibleBy;
      double valueOrDefault = nullable.GetValueOrDefault();
      if (JsonValidatingReader.IsZero(JsonValidatingReader.FloatingPointRemainder(dividend, valueOrDefault)))
        return;
      this.RaiseError(
        "Float {0} is not evenly divisible by {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) JsonConvert.ToString(num1), (object) schema.DivisibleBy), schema);
    }

    private static double FloatingPointRemainder(double dividend, double divisor) {
      return dividend - Math.Floor(dividend / divisor) * divisor;
    }

    private static bool IsZero(double value) {
      return Math.Abs(value) < 4.44089209850063E-15;
    }

    private void ValidatePropertyName(JsonSchemaModel schema) {
      if (schema == null)
        return;
      string index = Convert.ToString(this._reader.Value, (IFormatProvider) CultureInfo.InvariantCulture);
      if (this._currentScope.RequiredProperties.ContainsKey(index))
        this._currentScope.RequiredProperties[index] = true;
      if (!schema.AllowAdditionalProperties && !this.IsPropertyDefinied(schema, index))
        this.RaiseError(
          "Property '{0}' has not been defined and the schema does not allow additional properties.".FormatWith(
            (IFormatProvider) CultureInfo.InvariantCulture, (object) index), schema);
      this._currentScope.CurrentPropertyName = index;
    }

    private bool IsPropertyDefinied(JsonSchemaModel schema, string propertyName) {
      if (schema.Properties != null && schema.Properties.ContainsKey(propertyName))
        return true;
      if (schema.PatternProperties != null) {
        foreach (string key in (IEnumerable<string>) schema.PatternProperties.Keys) {
          if (Regex.IsMatch(propertyName, key))
            return true;
        }
      }

      return false;
    }

    private bool ValidateArray(JsonSchemaModel schema) {
      if (schema == null)
        return true;
      return this.TestType(schema, JsonSchemaType.Array);
    }

    private bool ValidateObject(JsonSchemaModel schema) {
      if (schema == null)
        return true;
      return this.TestType(schema, JsonSchemaType.Object);
    }

    private bool TestType(JsonSchemaModel currentSchema, JsonSchemaType currentType) {
      if (JsonSchemaGenerator.HasFlag(new JsonSchemaType?(currentSchema.Type), currentType))
        return true;
      this.RaiseError(
        "Invalid type. Expected {0} but got {1}.".FormatWith((IFormatProvider) CultureInfo.InvariantCulture,
          (object) currentSchema.Type, (object) currentType), currentSchema);
      return false;
    }

    bool IJsonLineInfo.HasLineInfo() {
      IJsonLineInfo reader = this._reader as IJsonLineInfo;
      if (reader != null)
        return reader.HasLineInfo();
      return false;
    }

    int IJsonLineInfo.LineNumber {
      get {
        IJsonLineInfo reader = this._reader as IJsonLineInfo;
        if (reader == null)
          return 0;
        return reader.LineNumber;
      }
    }

    int IJsonLineInfo.LinePosition {
      get {
        IJsonLineInfo reader = this._reader as IJsonLineInfo;
        if (reader == null)
          return 0;
        return reader.LinePosition;
      }
    }

    private class SchemaScope {
      private readonly JTokenType _tokenType;
      private readonly IList<JsonSchemaModel> _schemas;
      private readonly Dictionary<string, bool> _requiredProperties;

      public string CurrentPropertyName { get; set; }

      public int ArrayItemCount { get; set; }

      public bool IsUniqueArray { get; }

      public IList<JToken> UniqueArrayItems { get; }

      public JTokenWriter CurrentItemWriter { get; set; }

      public IList<JsonSchemaModel> Schemas {
        get { return this._schemas; }
      }

      public Dictionary<string, bool> RequiredProperties {
        get { return this._requiredProperties; }
      }

      public JTokenType TokenType {
        get { return this._tokenType; }
      }

      public SchemaScope(JTokenType tokenType, IList<JsonSchemaModel> schemas) {
        this._tokenType = tokenType;
        this._schemas = schemas;
        this._requiredProperties = schemas
          .SelectMany<JsonSchemaModel, string
          >(new Func<JsonSchemaModel, IEnumerable<string>>(this.GetRequiredProperties)).Distinct<string>()
          .ToDictionary<string, string, bool>((Func<string, string>) (p => p), (Func<string, bool>) (p => false));
        if (tokenType != JTokenType.Array ||
            !schemas.Any<JsonSchemaModel>((Func<JsonSchemaModel, bool>) (s => s.UniqueItems)))
          return;
        this.IsUniqueArray = true;
        this.UniqueArrayItems = (IList<JToken>) new List<JToken>();
      }

      private IEnumerable<string> GetRequiredProperties(JsonSchemaModel schema) {
        if (schema?.Properties == null)
          return Enumerable.Empty<string>();
        return schema.Properties
          .Where<KeyValuePair<string, JsonSchemaModel>
          >((Func<KeyValuePair<string, JsonSchemaModel>, bool>) (p => p.Value.Required))
          .Select<KeyValuePair<string, JsonSchemaModel>, string>(
            (Func<KeyValuePair<string, JsonSchemaModel>, string>) (p => p.Key));
      }
    }
  }
}