namespace Socket.Newtonsoft.Json.Converters {
  internal interface IXmlDocumentType : IXmlNode {
    string Name { get; }

    string System { get; }

    string Public { get; }

    string InternalSubset { get; }
  }
}