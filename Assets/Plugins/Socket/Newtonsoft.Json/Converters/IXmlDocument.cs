namespace Socket.Newtonsoft.Json.Converters {
  internal interface IXmlDocument : IXmlNode {
    IXmlNode CreateComment(string text);

    IXmlNode CreateTextNode(string text);

    IXmlNode CreateCDataSection(string data);

    IXmlNode CreateWhitespace(string text);

    IXmlNode CreateSignificantWhitespace(string text);

    IXmlNode CreateXmlDeclaration(string version, string encoding, string standalone);

    IXmlNode CreateXmlDocumentType(
      string name,
      string publicId,
      string systemId,
      string internalSubset);

    IXmlNode CreateProcessingInstruction(string target, string data);

    IXmlElement CreateElement(string elementName);

    IXmlElement CreateElement(string qualifiedName, string namespaceUri);

    IXmlNode CreateAttribute(string name, string value);

    IXmlNode CreateAttribute(string qualifiedName, string namespaceUri, string value);

    IXmlElement DocumentElement { get; }
  }
}