using System.Xml.Linq;

namespace Minio.Helpers;

internal static class XmlHelper
{
    /// <summary>
    /// Load an XML document and remove all the namespaces in the XML elements.
    /// </summary>
    /// <param name="stream">Stream that should be read.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>XML document that doesn't have any XML namespaces.</returns>
    public static async Task<XDocument> LoadXDocumentAsync(Stream stream, CancellationToken cancellationToken)
    {
        var xDoc = await XDocument.LoadAsync(stream, LoadOptions.None, cancellationToken).ConfigureAwait(false);
        RemoveNamespaces(xDoc.Root!);
        return xDoc;

        void RemoveNamespaces(XElement element)
        {
            if (element.HasElements)
            {
                foreach (var child in element.Elements())
                    RemoveNamespaces(child);
            }
            if (!string.IsNullOrEmpty(element.Name.NamespaceName))
                element.Name = element.Name.LocalName;
            var attr = element.FirstAttribute;
            while (attr != null)
            {
                var nextAttr = attr.NextAttribute;
                if (attr.IsNamespaceDeclaration)
                {
                    attr.Remove();
                } 
                else if (!string.IsNullOrEmpty(attr.Name.NamespaceName))
                {
                    // niche case where attributes also use namespaces 
                    var attrs = new List<XAttribute>();
                    foreach (var a in element.Attributes())
                    {
                        if (!a.IsNamespaceDeclaration)
                            attrs.Add(new XAttribute(a.Name.LocalName, a.Value));
                    }
                    element.ReplaceAttributes(attrs);
                    break;
                }
                attr = nextAttr;
            }
        }
    }

}