using System.Xml;

namespace Socket.Newtonsoft.Json.Converters {
  internal class XmlElementWrapper : XmlNodeWrapper, IXmlElement, IXmlNode {
    private readonly XmlElement _element;

    public XmlElementWrapper(XmlElement element)
      : base((XmlNode) element) {
      this._element = element;
    }

    public void SetAttributeNode(IXmlNode attribute) {
      this._element.SetAttributeNode((XmlAttribute) ((XmlNodeWrapper) attribute).WrappedNode);
    }

    public string GetPrefixOfNamespace(string namespaceUri) {
      return this._element.GetPrefixOfNamespace(namespaceUri);
    }

    public bool IsEmpty {
      get { return this._element.IsEmpty; }
    }
  }
}