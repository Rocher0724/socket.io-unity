using System.Collections.Generic;
using System.Xml;

namespace Socket.Newtonsoft.Json.Converters {
  internal class XmlNodeWrapper : IXmlNode {
    private readonly XmlNode _node;
    private List<IXmlNode> _childNodes;
    private List<IXmlNode> _attributes;

    public XmlNodeWrapper(XmlNode node) {
      this._node = node;
    }

    public object WrappedNode {
      get { return (object) this._node; }
    }

    public XmlNodeType NodeType {
      get { return this._node.NodeType; }
    }

    public virtual string LocalName {
      get { return this._node.LocalName; }
    }

    public List<IXmlNode> ChildNodes {
      get {
        if (this._childNodes == null) {
          if (!this._node.HasChildNodes) {
            this._childNodes = XmlNodeConverter.EmptyChildNodes;
          } else {
            this._childNodes = new List<IXmlNode>(this._node.ChildNodes.Count);
            foreach (XmlNode childNode in this._node.ChildNodes)
              this._childNodes.Add(XmlNodeWrapper.WrapNode(childNode));
          }
        }

        return this._childNodes;
      }
    }

    protected virtual bool HasChildNodes {
      get { return this._node.HasChildNodes; }
    }

    internal static IXmlNode WrapNode(XmlNode node) {
      switch (node.NodeType) {
        case XmlNodeType.Element:
          return (IXmlNode) new XmlElementWrapper((XmlElement) node);
        case XmlNodeType.DocumentType:
          return (IXmlNode) new XmlDocumentTypeWrapper((XmlDocumentType) node);
        case XmlNodeType.XmlDeclaration:
          return (IXmlNode) new XmlDeclarationWrapper((XmlDeclaration) node);
        default:
          return (IXmlNode) new XmlNodeWrapper(node);
      }
    }

    public List<IXmlNode> Attributes {
      get {
        if (this._attributes == null) {
          if (!this.HasAttributes) {
            this._attributes = XmlNodeConverter.EmptyChildNodes;
          } else {
            this._attributes = new List<IXmlNode>(this._node.Attributes.Count);
            foreach (XmlNode attribute in (XmlNamedNodeMap) this._node.Attributes)
              this._attributes.Add(XmlNodeWrapper.WrapNode(attribute));
          }
        }

        return this._attributes;
      }
    }

    private bool HasAttributes {
      get {
        XmlElement node = this._node as XmlElement;
        if (node != null)
          return node.HasAttributes;
        XmlAttributeCollection attributes = this._node.Attributes;
        if (attributes == null)
          return false;
        return attributes.Count > 0;
      }
    }

    public IXmlNode ParentNode {
      get {
        XmlAttribute node1 = this._node as XmlAttribute;
        XmlNode node2 = node1 != null ? (XmlNode) node1.OwnerElement : this._node.ParentNode;
        if (node2 == null)
          return (IXmlNode) null;
        return XmlNodeWrapper.WrapNode(node2);
      }
    }

    public string Value {
      get { return this._node.Value; }
      set { this._node.Value = value; }
    }

    public IXmlNode AppendChild(IXmlNode newChild) {
      this._node.AppendChild(((XmlNodeWrapper) newChild)._node);
      this._childNodes = (List<IXmlNode>) null;
      this._attributes = (List<IXmlNode>) null;
      return newChild;
    }

    public string NamespaceUri {
      get { return this._node.NamespaceURI; }
    }
  }
}