namespace Socket.Newtonsoft.Json.Converters {

  internal interface IXmlDeclaration : IXmlNode {
    string Version { get; }

    string Encoding { get; set; }

    string Standalone { get; set; }
  }
}