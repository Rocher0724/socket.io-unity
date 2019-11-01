using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Xml;
using Socket.Newtonsoft.Json.Utilities;

namespace Socket.Newtonsoft.Json.Converters {
  public class XmlNodeConverter : JsonConverter {
    internal static readonly List<IXmlNode> EmptyChildNodes = new List<IXmlNode>();
    private const string TextName = "#text";
    private const string CommentName = "#comment";
    private const string CDataName = "#cdata-section";
    private const string WhitespaceName = "#whitespace";
    private const string SignificantWhitespaceName = "#significant-whitespace";
    private const string DeclarationName = "?xml";
    private const string JsonNamespaceUri = "http://james.newtonking.com/projects/json";

    public string DeserializeRootElementName { get; set; }

    public bool WriteArrayAttribute { get; set; }

    public bool OmitRootObject { get; set; }

    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) {
      IXmlNode node = this.WrapXml(value);
      XmlNamespaceManager manager = new XmlNamespaceManager((XmlNameTable) new NameTable());
      this.PushParentNamespaces(node, manager);
      if (!this.OmitRootObject)
        writer.WriteStartObject();
      this.SerializeNode(writer, node, manager, !this.OmitRootObject);
      if (this.OmitRootObject)
        return;
      writer.WriteEndObject();
    }

    private IXmlNode WrapXml(object value) {
      XmlNode node = value as XmlNode;
      if (node != null)
        return XmlNodeWrapper.WrapNode(node);
      throw new ArgumentException("Value must be an XML object.", nameof(value));
    }

    private void PushParentNamespaces(IXmlNode node, XmlNamespaceManager manager) {
      List<IXmlNode> xmlNodeList = (List<IXmlNode>) null;
      IXmlNode xmlNode1 = node;
      while ((xmlNode1 = xmlNode1.ParentNode) != null) {
        if (xmlNode1.NodeType == XmlNodeType.Element) {
          if (xmlNodeList == null)
            xmlNodeList = new List<IXmlNode>();
          xmlNodeList.Add(xmlNode1);
        }
      }

      if (xmlNodeList == null)
        return;
      xmlNodeList.Reverse();
      foreach (IXmlNode xmlNode2 in xmlNodeList) {
        manager.PushScope();
        foreach (IXmlNode attribute in xmlNode2.Attributes) {
          if (attribute.NamespaceUri == "http://www.w3.org/2000/xmlns/" && attribute.LocalName != "xmlns")
            manager.AddNamespace(attribute.LocalName, attribute.Value);
        }
      }
    }

    private string ResolveFullName(IXmlNode node, XmlNamespaceManager manager) {
      string str =
        node.NamespaceUri == null || node.LocalName == "xmlns" && node.NamespaceUri == "http://www.w3.org/2000/xmlns/"
          ? (string) null
          : manager.LookupPrefix(node.NamespaceUri);
      if (!string.IsNullOrEmpty(str))
        return str + ":" + XmlConvert.DecodeName(node.LocalName);
      return XmlConvert.DecodeName(node.LocalName);
    }

    private string GetPropertyName(IXmlNode node, XmlNamespaceManager manager) {
      switch (node.NodeType) {
        case XmlNodeType.Element:
          if (node.NamespaceUri == "http://james.newtonking.com/projects/json")
            return "$" + node.LocalName;
          return this.ResolveFullName(node, manager);
        case XmlNodeType.Attribute:
          if (node.NamespaceUri == "http://james.newtonking.com/projects/json")
            return "$" + node.LocalName;
          return "@" + this.ResolveFullName(node, manager);
        case XmlNodeType.Text:
          return "#text";
        case XmlNodeType.CDATA:
          return "#cdata-section";
        case XmlNodeType.ProcessingInstruction:
          return "?" + this.ResolveFullName(node, manager);
        case XmlNodeType.Comment:
          return "#comment";
        case XmlNodeType.DocumentType:
          return "!" + this.ResolveFullName(node, manager);
        case XmlNodeType.Whitespace:
          return "#whitespace";
        case XmlNodeType.SignificantWhitespace:
          return "#significant-whitespace";
        case XmlNodeType.XmlDeclaration:
          return "?xml";
        default:
          throw new JsonSerializationException("Unexpected XmlNodeType when getting node name: " +
                                               (object) node.NodeType);
      }
    }

    private bool IsArray(IXmlNode node) {
      foreach (IXmlNode attribute in node.Attributes) {
        if (attribute.LocalName == "Array" && attribute.NamespaceUri == "http://james.newtonking.com/projects/json")
          return XmlConvert.ToBoolean(attribute.Value);
      }

      return false;
    }

    private void SerializeGroupedNodes(
      JsonWriter writer,
      IXmlNode node,
      XmlNamespaceManager manager,
      bool writePropertyName) {
      switch (node.ChildNodes.Count) {
        case 0:
          break;
        case 1:
          string propertyName1 = this.GetPropertyName(node.ChildNodes[0], manager);
          this.WriteGroupedNodes(writer, manager, writePropertyName, node.ChildNodes, propertyName1);
          break;
        default:
          Dictionary<string, object> dictionary = (Dictionary<string, object>) null;
          string str = (string) null;
          for (int capacity = 0; capacity < node.ChildNodes.Count; ++capacity) {
            IXmlNode childNode = node.ChildNodes[capacity];
            string propertyName2 = this.GetPropertyName(childNode, manager);
            if (dictionary == null) {
              if (str == null)
                str = propertyName2;
              else if (!(propertyName2 == str)) {
                dictionary = new Dictionary<string, object>();
                if (capacity > 1) {
                  List<IXmlNode> xmlNodeList = new List<IXmlNode>(capacity);
                  for (int index = 0; index < capacity; ++index)
                    xmlNodeList.Add(node.ChildNodes[index]);
                  dictionary.Add(str, (object) xmlNodeList);
                } else
                  dictionary.Add(str, (object) node.ChildNodes[0]);

                dictionary.Add(propertyName2, (object) childNode);
              }
            } else {
              object obj;
              if (!dictionary.TryGetValue(propertyName2, out obj)) {
                dictionary.Add(propertyName2, (object) childNode);
              } else {
                List<IXmlNode> xmlNodeList = obj as List<IXmlNode>;
                if (xmlNodeList == null) {
                  xmlNodeList = new List<IXmlNode>() {
                    (IXmlNode) obj
                  };
                  dictionary[propertyName2] = (object) xmlNodeList;
                }

                xmlNodeList.Add(childNode);
              }
            }
          }

          if (dictionary == null) {
            this.WriteGroupedNodes(writer, manager, writePropertyName, node.ChildNodes, str);
            break;
          }

          using (Dictionary<string, object>.Enumerator enumerator = dictionary.GetEnumerator()) {
            while (enumerator.MoveNext()) {
              KeyValuePair<string, object> current = enumerator.Current;
              List<IXmlNode> groupedNodes = current.Value as List<IXmlNode>;
              if (groupedNodes != null)
                this.WriteGroupedNodes(writer, manager, writePropertyName, groupedNodes, current.Key);
              else
                this.WriteGroupedNodes(writer, manager, writePropertyName, (IXmlNode) current.Value, current.Key);
            }

            break;
          }
      }
    }

    private void WriteGroupedNodes(
      JsonWriter writer,
      XmlNamespaceManager manager,
      bool writePropertyName,
      List<IXmlNode> groupedNodes,
      string elementNames) {
      if (groupedNodes.Count == 1 && !this.IsArray(groupedNodes[0])) {
        this.SerializeNode(writer, groupedNodes[0], manager, writePropertyName);
      } else {
        if (writePropertyName)
          writer.WritePropertyName(elementNames);
        writer.WriteStartArray();
        for (int index = 0; index < groupedNodes.Count; ++index)
          this.SerializeNode(writer, groupedNodes[index], manager, false);
        writer.WriteEndArray();
      }
    }

    private void WriteGroupedNodes(
      JsonWriter writer,
      XmlNamespaceManager manager,
      bool writePropertyName,
      IXmlNode node,
      string elementNames) {
      if (!this.IsArray(node)) {
        this.SerializeNode(writer, node, manager, writePropertyName);
      } else {
        if (writePropertyName)
          writer.WritePropertyName(elementNames);
        writer.WriteStartArray();
        this.SerializeNode(writer, node, manager, false);
        writer.WriteEndArray();
      }
    }

    private void SerializeNode(
      JsonWriter writer,
      IXmlNode node,
      XmlNamespaceManager manager,
      bool writePropertyName) {
      switch (node.NodeType) {
        case XmlNodeType.Element:
          if (this.IsArray(node) && XmlNodeConverter.AllSameName(node) && node.ChildNodes.Count > 0) {
            this.SerializeGroupedNodes(writer, node, manager, false);
            break;
          }

          manager.PushScope();
          foreach (IXmlNode attribute in node.Attributes) {
            if (attribute.NamespaceUri == "http://www.w3.org/2000/xmlns/") {
              string prefix = attribute.LocalName != "xmlns"
                ? XmlConvert.DecodeName(attribute.LocalName)
                : string.Empty;
              string uri = attribute.Value;
              manager.AddNamespace(prefix, uri);
            }
          }

          if (writePropertyName)
            writer.WritePropertyName(this.GetPropertyName(node, manager));
          if (!this.ValueAttributes(node.Attributes) && node.ChildNodes.Count == 1 &&
              node.ChildNodes[0].NodeType == XmlNodeType.Text)
            writer.WriteValue(node.ChildNodes[0].Value);
          else if (node.ChildNodes.Count == 0 && node.Attributes.Count == 0) {
            if (((IXmlElement) node).IsEmpty)
              writer.WriteNull();
            else
              writer.WriteValue(string.Empty);
          } else {
            writer.WriteStartObject();
            for (int index = 0; index < node.Attributes.Count; ++index)
              this.SerializeNode(writer, node.Attributes[index], manager, true);
            this.SerializeGroupedNodes(writer, node, manager, true);
            writer.WriteEndObject();
          }

          manager.PopScope();
          break;
        case XmlNodeType.Attribute:
        case XmlNodeType.Text:
        case XmlNodeType.CDATA:
        case XmlNodeType.ProcessingInstruction:
        case XmlNodeType.Whitespace:
        case XmlNodeType.SignificantWhitespace:
          if (node.NamespaceUri == "http://www.w3.org/2000/xmlns/" &&
              node.Value == "http://james.newtonking.com/projects/json" ||
              node.NamespaceUri == "http://james.newtonking.com/projects/json" && node.LocalName == "Array")
            break;
          if (writePropertyName)
            writer.WritePropertyName(this.GetPropertyName(node, manager));
          writer.WriteValue(node.Value);
          break;
        case XmlNodeType.Comment:
          if (!writePropertyName)
            break;
          writer.WriteComment(node.Value);
          break;
        case XmlNodeType.Document:
        case XmlNodeType.DocumentFragment:
          this.SerializeGroupedNodes(writer, node, manager, writePropertyName);
          break;
        case XmlNodeType.DocumentType:
          IXmlDocumentType xmlDocumentType = (IXmlDocumentType) node;
          writer.WritePropertyName(this.GetPropertyName(node, manager));
          writer.WriteStartObject();
          if (!string.IsNullOrEmpty(xmlDocumentType.Name)) {
            writer.WritePropertyName("@name");
            writer.WriteValue(xmlDocumentType.Name);
          }

          if (!string.IsNullOrEmpty(xmlDocumentType.Public)) {
            writer.WritePropertyName("@public");
            writer.WriteValue(xmlDocumentType.Public);
          }

          if (!string.IsNullOrEmpty(xmlDocumentType.System)) {
            writer.WritePropertyName("@system");
            writer.WriteValue(xmlDocumentType.System);
          }

          if (!string.IsNullOrEmpty(xmlDocumentType.InternalSubset)) {
            writer.WritePropertyName("@internalSubset");
            writer.WriteValue(xmlDocumentType.InternalSubset);
          }

          writer.WriteEndObject();
          break;
        case XmlNodeType.XmlDeclaration:
          IXmlDeclaration xmlDeclaration = (IXmlDeclaration) node;
          writer.WritePropertyName(this.GetPropertyName(node, manager));
          writer.WriteStartObject();
          if (!string.IsNullOrEmpty(xmlDeclaration.Version)) {
            writer.WritePropertyName("@version");
            writer.WriteValue(xmlDeclaration.Version);
          }

          if (!string.IsNullOrEmpty(xmlDeclaration.Encoding)) {
            writer.WritePropertyName("@encoding");
            writer.WriteValue(xmlDeclaration.Encoding);
          }

          if (!string.IsNullOrEmpty(xmlDeclaration.Standalone)) {
            writer.WritePropertyName("@standalone");
            writer.WriteValue(xmlDeclaration.Standalone);
          }

          writer.WriteEndObject();
          break;
        default:
          throw new JsonSerializationException("Unexpected XmlNodeType when serializing nodes: " +
                                               (object) node.NodeType);
      }
    }

    private static bool AllSameName(IXmlNode node) {
      foreach (IXmlNode childNode in node.ChildNodes) {
        if (childNode.LocalName != node.LocalName)
          return false;
      }

      return true;
    }

    public override object ReadJson(
      JsonReader reader,
      Type objectType,
      object existingValue,
      JsonSerializer serializer) {
      switch (reader.TokenType) {
        case JsonToken.StartObject:
          XmlNamespaceManager manager = new XmlNamespaceManager((XmlNameTable) new NameTable());
          IXmlDocument document = (IXmlDocument) null;
          IXmlNode currentNode = (IXmlNode) null;
          if (typeof(XmlNode).IsAssignableFrom(objectType)) {
            if (objectType != typeof(XmlDocument))
              throw JsonSerializationException.Create(reader,
                "XmlNodeConverter only supports deserializing XmlDocuments");
            document = (IXmlDocument) new XmlDocumentWrapper(new XmlDocument() {
              XmlResolver = (XmlResolver) null
            });
            currentNode = (IXmlNode) document;
          }

          if (document == null || currentNode == null)
            throw JsonSerializationException.Create(reader,
              "Unexpected type when converting XML: " + (object) objectType);
          if (!string.IsNullOrEmpty(this.DeserializeRootElementName)) {
            this.ReadElement(reader, document, currentNode, this.DeserializeRootElementName, manager);
          } else {
            reader.Read();
            this.DeserializeNode(reader, document, manager, currentNode);
          }

          return document.WrappedNode;
        case JsonToken.Null:
          return (object) null;
        default:
          throw JsonSerializationException.Create(reader,
            "XmlNodeConverter can only convert JSON that begins with an object.");
      }
    }

    private void DeserializeValue(
      JsonReader reader,
      IXmlDocument document,
      XmlNamespaceManager manager,
      string propertyName,
      IXmlNode currentNode) {
      if (!(propertyName == "#text")) {
        if (!(propertyName == "#cdata-section")) {
          if (!(propertyName == "#whitespace")) {
            if (propertyName == "#significant-whitespace")
              currentNode.AppendChild(document.CreateSignificantWhitespace(this.ConvertTokenToXmlValue(reader)));
            else if (!string.IsNullOrEmpty(propertyName) && propertyName[0] == '?')
              this.CreateInstruction(reader, document, currentNode, propertyName);
            else if (string.Equals(propertyName, "!DOCTYPE", StringComparison.OrdinalIgnoreCase))
              this.CreateDocumentType(reader, document, currentNode);
            else if (reader.TokenType == JsonToken.StartArray)
              this.ReadArrayElements(reader, document, propertyName, currentNode, manager);
            else
              this.ReadElement(reader, document, currentNode, propertyName, manager);
          } else
            currentNode.AppendChild(document.CreateWhitespace(this.ConvertTokenToXmlValue(reader)));
        } else
          currentNode.AppendChild(document.CreateCDataSection(this.ConvertTokenToXmlValue(reader)));
      } else
        currentNode.AppendChild(document.CreateTextNode(this.ConvertTokenToXmlValue(reader)));
    }

    private void ReadElement(
      JsonReader reader,
      IXmlDocument document,
      IXmlNode currentNode,
      string propertyName,
      XmlNamespaceManager manager) {
      if (string.IsNullOrEmpty(propertyName))
        throw JsonSerializationException.Create(reader,
          "XmlNodeConverter cannot convert JSON with an empty property name to XML.");
      Dictionary<string, string> attributeNameValues = this.ReadAttributeElements(reader, manager);
      string prefix1 = MiscellaneousUtils.GetPrefix(propertyName);
      if (propertyName.StartsWith('@')) {
        string str = propertyName.Substring(1);
        string prefix2 = MiscellaneousUtils.GetPrefix(str);
        XmlNodeConverter.AddAttribute(reader, document, currentNode, propertyName, str, manager, prefix2);
      } else {
        if (propertyName.StartsWith('$')) {
          if (!(propertyName == "$values")) {
            if (propertyName == "$id" || propertyName == "$ref" ||
                (propertyName == "$type" || propertyName == "$value")) {
              string attributeName = propertyName.Substring(1);
              string attributePrefix = manager.LookupPrefix("http://james.newtonking.com/projects/json");
              XmlNodeConverter.AddAttribute(reader, document, currentNode, propertyName, attributeName, manager,
                attributePrefix);
              return;
            }
          } else {
            propertyName = propertyName.Substring(1);
            string elementPrefix = manager.LookupPrefix("http://james.newtonking.com/projects/json");
            this.CreateElement(reader, document, currentNode, propertyName, manager, elementPrefix,
              attributeNameValues);
            return;
          }
        }

        this.CreateElement(reader, document, currentNode, propertyName, manager, prefix1, attributeNameValues);
      }
    }

    private void CreateElement(
      JsonReader reader,
      IXmlDocument document,
      IXmlNode currentNode,
      string elementName,
      XmlNamespaceManager manager,
      string elementPrefix,
      Dictionary<string, string> attributeNameValues) {
      IXmlElement element = this.CreateElement(elementName, document, elementPrefix, manager);
      currentNode.AppendChild((IXmlNode) element);
      if (attributeNameValues != null) {
        foreach (KeyValuePair<string, string> attributeNameValue in attributeNameValues) {
          string str = XmlConvert.EncodeName(attributeNameValue.Key);
          string prefix = MiscellaneousUtils.GetPrefix(attributeNameValue.Key);
          IXmlNode attribute = !string.IsNullOrEmpty(prefix)
            ? document.CreateAttribute(str, manager.LookupNamespace(prefix) ?? string.Empty, attributeNameValue.Value)
            : document.CreateAttribute(str, attributeNameValue.Value);
          element.SetAttributeNode(attribute);
        }
      }

      switch (reader.TokenType) {
        case JsonToken.Integer:
        case JsonToken.Float:
        case JsonToken.String:
        case JsonToken.Boolean:
        case JsonToken.Date:
          string xmlValue = this.ConvertTokenToXmlValue(reader);
          if (xmlValue == null)
            break;
          element.AppendChild(document.CreateTextNode(xmlValue));
          break;
        case JsonToken.Null:
          break;
        case JsonToken.EndObject:
          manager.RemoveNamespace(string.Empty, manager.DefaultNamespace);
          break;
        default:
          manager.PushScope();
          this.DeserializeNode(reader, document, manager, (IXmlNode) element);
          manager.PopScope();
          manager.RemoveNamespace(string.Empty, manager.DefaultNamespace);
          break;
      }
    }

    private static void AddAttribute(
      JsonReader reader,
      IXmlDocument document,
      IXmlNode currentNode,
      string propertyName,
      string attributeName,
      XmlNamespaceManager manager,
      string attributePrefix) {
      if (currentNode.NodeType == XmlNodeType.Document)
        throw JsonSerializationException.Create(reader,
          "JSON root object has property '{0}' that will be converted to an attribute. A root object cannot have any attribute properties. Consider specifying a DeserializeRootElementName."
            .FormatWith((IFormatProvider) CultureInfo.InvariantCulture, (object) propertyName));
      string str1 = XmlConvert.EncodeName(attributeName);
      string str2 = reader.Value.ToString();
      IXmlNode attribute = !string.IsNullOrEmpty(attributePrefix)
        ? document.CreateAttribute(str1, manager.LookupNamespace(attributePrefix), str2)
        : document.CreateAttribute(str1, str2);
      ((IXmlElement) currentNode).SetAttributeNode(attribute);
    }

    private string ConvertTokenToXmlValue(JsonReader reader) {
      switch (reader.TokenType) {
        case JsonToken.Integer:
          return XmlConvert.ToString(Convert.ToInt64(reader.Value, (IFormatProvider) CultureInfo.InvariantCulture));
        case JsonToken.Float:
          if (reader.Value is Decimal)
            return XmlConvert.ToString((Decimal) reader.Value);
          if (reader.Value is float)
            return XmlConvert.ToString((float) reader.Value);
          return XmlConvert.ToString(Convert.ToDouble(reader.Value, (IFormatProvider) CultureInfo.InvariantCulture));
        case JsonToken.String:
          return reader.Value?.ToString();
        case JsonToken.Boolean:
          return XmlConvert.ToString(Convert.ToBoolean(reader.Value, (IFormatProvider) CultureInfo.InvariantCulture));
        case JsonToken.Null:
          return (string) null;
        case JsonToken.Date:
          DateTime dateTime = Convert.ToDateTime(reader.Value, (IFormatProvider) CultureInfo.InvariantCulture);
          return XmlConvert.ToString(dateTime, DateTimeUtils.ToSerializationMode(dateTime.Kind));
        default:
          throw JsonSerializationException.Create(reader,
            "Cannot get an XML string value from token type '{0}'.".FormatWith(
              (IFormatProvider) CultureInfo.InvariantCulture, (object) reader.TokenType));
      }
    }

    private void ReadArrayElements(
      JsonReader reader,
      IXmlDocument document,
      string propertyName,
      IXmlNode currentNode,
      XmlNamespaceManager manager) {
      string prefix = MiscellaneousUtils.GetPrefix(propertyName);
      IXmlElement element1 = this.CreateElement(propertyName, document, prefix, manager);
      currentNode.AppendChild((IXmlNode) element1);
      int num = 0;
      while (reader.Read() && reader.TokenType != JsonToken.EndArray) {
        this.DeserializeValue(reader, document, manager, propertyName, (IXmlNode) element1);
        ++num;
      }

      if (this.WriteArrayAttribute)
        this.AddJsonArrayAttribute(element1, document);
      if (num != 1 || !this.WriteArrayAttribute)
        return;
      foreach (IXmlNode childNode in element1.ChildNodes) {
        IXmlElement element2 = childNode as IXmlElement;
        if (element2 != null && element2.LocalName == propertyName) {
          this.AddJsonArrayAttribute(element2, document);
          break;
        }
      }
    }

    private void AddJsonArrayAttribute(IXmlElement element, IXmlDocument document) {
      element.SetAttributeNode(document.CreateAttribute("json:Array", "http://james.newtonking.com/projects/json",
        "true"));
    }

    private Dictionary<string, string> ReadAttributeElements(
      JsonReader reader,
      XmlNamespaceManager manager) {
      switch (reader.TokenType) {
        case JsonToken.StartConstructor:
        case JsonToken.Integer:
        case JsonToken.Float:
        case JsonToken.String:
        case JsonToken.Boolean:
        case JsonToken.Null:
        case JsonToken.Date:
          return (Dictionary<string, string>) null;
        default:
          Dictionary<string, string> dictionary = (Dictionary<string, string>) null;
          bool flag = false;
          while (!flag && reader.Read()) {
            switch (reader.TokenType) {
              case JsonToken.PropertyName:
                string str1 = reader.Value.ToString();
                if (!string.IsNullOrEmpty(str1)) {
                  switch (str1[0]) {
                    case '$':
                      if (str1 == "$values" || str1 == "$id" || (str1 == "$ref" || str1 == "$type") ||
                          str1 == "$value") {
                        string prefix = manager.LookupPrefix("http://james.newtonking.com/projects/json");
                        if (prefix == null) {
                          if (dictionary == null)
                            dictionary = new Dictionary<string, string>();
                          int? nullable = new int?();
                          while (manager.LookupNamespace("json" + (object) nullable) != null)
                            nullable = new int?(nullable.GetValueOrDefault() + 1);
                          prefix = "json" + (object) nullable;
                          dictionary.Add("xmlns:" + prefix, "http://james.newtonking.com/projects/json");
                          manager.AddNamespace(prefix, "http://james.newtonking.com/projects/json");
                        }

                        if (str1 == "$values") {
                          flag = true;
                          continue;
                        }

                        string str2 = str1.Substring(1);
                        reader.Read();
                        if (!JsonTokenUtils.IsPrimitiveToken(reader.TokenType))
                          throw JsonSerializationException.Create(reader,
                            "Unexpected JsonToken: " + (object) reader.TokenType);
                        if (dictionary == null)
                          dictionary = new Dictionary<string, string>();
                        string str3 = reader.Value?.ToString();
                        dictionary.Add(prefix + ":" + str2, str3);
                        continue;
                      }

                      flag = true;
                      continue;
                    case '@':
                      if (dictionary == null)
                        dictionary = new Dictionary<string, string>();
                      string str4 = str1.Substring(1);
                      reader.Read();
                      string xmlValue = this.ConvertTokenToXmlValue(reader);
                      dictionary.Add(str4, xmlValue);
                      string prefix1;
                      if (this.IsNamespaceAttribute(str4, out prefix1)) {
                        manager.AddNamespace(prefix1, xmlValue);
                        continue;
                      }

                      continue;
                    default:
                      flag = true;
                      continue;
                  }
                } else {
                  flag = true;
                  continue;
                }
              case JsonToken.Comment:
              case JsonToken.EndObject:
                flag = true;
                continue;
              default:
                throw JsonSerializationException.Create(reader, "Unexpected JsonToken: " + (object) reader.TokenType);
            }
          }

          return dictionary;
      }
    }

    private void CreateInstruction(
      JsonReader reader,
      IXmlDocument document,
      IXmlNode currentNode,
      string propertyName) {
      if (propertyName == "?xml") {
        string version = (string) null;
        string encoding = (string) null;
        string standalone = (string) null;
        while (reader.Read() && reader.TokenType != JsonToken.EndObject) {
          string str = reader.Value.ToString();
          if (!(str == "@version")) {
            if (!(str == "@encoding")) {
              if (!(str == "@standalone"))
                throw JsonSerializationException.Create(reader,
                  "Unexpected property name encountered while deserializing XmlDeclaration: " + reader.Value);
              reader.Read();
              standalone = this.ConvertTokenToXmlValue(reader);
            } else {
              reader.Read();
              encoding = this.ConvertTokenToXmlValue(reader);
            }
          } else {
            reader.Read();
            version = this.ConvertTokenToXmlValue(reader);
          }
        }

        IXmlNode xmlDeclaration = document.CreateXmlDeclaration(version, encoding, standalone);
        currentNode.AppendChild(xmlDeclaration);
      } else {
        IXmlNode processingInstruction =
          document.CreateProcessingInstruction(propertyName.Substring(1), this.ConvertTokenToXmlValue(reader));
        currentNode.AppendChild(processingInstruction);
      }
    }

    private void CreateDocumentType(JsonReader reader, IXmlDocument document, IXmlNode currentNode) {
      string name = (string) null;
      string publicId = (string) null;
      string systemId = (string) null;
      string internalSubset = (string) null;
      while (reader.Read() && reader.TokenType != JsonToken.EndObject) {
        string str = reader.Value.ToString();
        if (!(str == "@name")) {
          if (!(str == "@public")) {
            if (!(str == "@system")) {
              if (!(str == "@internalSubset"))
                throw JsonSerializationException.Create(reader,
                  "Unexpected property name encountered while deserializing XmlDeclaration: " + reader.Value);
              reader.Read();
              internalSubset = this.ConvertTokenToXmlValue(reader);
            } else {
              reader.Read();
              systemId = this.ConvertTokenToXmlValue(reader);
            }
          } else {
            reader.Read();
            publicId = this.ConvertTokenToXmlValue(reader);
          }
        } else {
          reader.Read();
          name = this.ConvertTokenToXmlValue(reader);
        }
      }

      IXmlNode xmlDocumentType = document.CreateXmlDocumentType(name, publicId, systemId, internalSubset);
      currentNode.AppendChild(xmlDocumentType);
    }

    private IXmlElement CreateElement(
      string elementName,
      IXmlDocument document,
      string elementPrefix,
      XmlNamespaceManager manager) {
      string str = XmlConvert.EncodeName(elementName);
      string namespaceUri = string.IsNullOrEmpty(elementPrefix)
        ? manager.DefaultNamespace
        : manager.LookupNamespace(elementPrefix);
      if (string.IsNullOrEmpty(namespaceUri))
        return document.CreateElement(str);
      return document.CreateElement(str, namespaceUri);
    }

    private void DeserializeNode(
      JsonReader reader,
      IXmlDocument document,
      XmlNamespaceManager manager,
      IXmlNode currentNode) {
      do {
        switch (reader.TokenType) {
          case JsonToken.StartConstructor:
            string propertyName1 = reader.Value.ToString();
            while (reader.Read() && reader.TokenType != JsonToken.EndConstructor)
              this.DeserializeValue(reader, document, manager, propertyName1, currentNode);
            break;
          case JsonToken.PropertyName:
            if (currentNode.NodeType == XmlNodeType.Document && document.DocumentElement != null)
              throw JsonSerializationException.Create(reader,
                "JSON root object has multiple properties. The root object must have a single property in order to create a valid XML document. Consider specifying a DeserializeRootElementName.");
            string propertyName2 = reader.Value.ToString();
            reader.Read();
            if (reader.TokenType == JsonToken.StartArray) {
              int num = 0;
              while (reader.Read() && reader.TokenType != JsonToken.EndArray) {
                this.DeserializeValue(reader, document, manager, propertyName2, currentNode);
                ++num;
              }

              if (num == 1 && this.WriteArrayAttribute) {
                using (List<IXmlNode>.Enumerator enumerator = currentNode.ChildNodes.GetEnumerator()) {
                  while (enumerator.MoveNext()) {
                    IXmlElement current = enumerator.Current as IXmlElement;
                    if (current != null && current.LocalName == propertyName2) {
                      this.AddJsonArrayAttribute(current, document);
                      break;
                    }
                  }

                  break;
                }
              } else
                break;
            } else {
              this.DeserializeValue(reader, document, manager, propertyName2, currentNode);
              break;
            }
          case JsonToken.Comment:
            currentNode.AppendChild(document.CreateComment((string) reader.Value));
            break;
          case JsonToken.EndObject:
            return;
          case JsonToken.EndArray:
            return;
          default:
            throw JsonSerializationException.Create(reader,
              "Unexpected JsonToken when deserializing node: " + (object) reader.TokenType);
        }
      } while (reader.Read());
    }

    private bool IsNamespaceAttribute(string attributeName, out string prefix) {
      if (attributeName.StartsWith("xmlns", StringComparison.Ordinal)) {
        if (attributeName.Length == 5) {
          prefix = string.Empty;
          return true;
        }

        if (attributeName[5] == ':') {
          prefix = attributeName.Substring(6, attributeName.Length - 6);
          return true;
        }
      }

      prefix = (string) null;
      return false;
    }

    private bool ValueAttributes(List<IXmlNode> c) {
      foreach (IXmlNode xmlNode in c) {
        if (!(xmlNode.NamespaceUri == "http://james.newtonking.com/projects/json") &&
            (!(xmlNode.NamespaceUri == "http://www.w3.org/2000/xmlns/") ||
             !(xmlNode.Value == "http://james.newtonking.com/projects/json")))
          return true;
      }

      return false;
    }

    public override bool CanConvert(Type valueType) {
      if (valueType.AssignableToTypeName("System.Xml.XmlNode", false))
        return this.IsXmlNode(valueType);
      return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private bool IsXmlNode(Type valueType) {
      return typeof(XmlNode).IsAssignableFrom(valueType);
    }
  }
}