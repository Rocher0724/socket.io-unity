

using System.Xml;

namespace Socket.Newtonsoft.Json.Converters {
  internal class XmlDocumentTypeWrapper : XmlNodeWrapper, IXmlDocumentType, IXmlNode {
    private readonly XmlDocumentType _documentType;

    public XmlDocumentTypeWrapper(XmlDocumentType documentType)
      : base((XmlNode) documentType) {
      this._documentType = documentType;
    }

    public string Name {
      get { return this._documentType.Name; }
    }

    public string System {
      get { return this._documentType.SystemId; }
    }

    public string Public {
      get { return this._documentType.PublicId; }
    }

    public string InternalSubset {
      get { return this._documentType.InternalSubset; }
    }

    public override string LocalName {
      get { return "DOCTYPE"; }
    }
  }
}