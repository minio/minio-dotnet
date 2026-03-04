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
        return new XDocument(Remove(xDoc.Root!));

        XElement Remove(XElement element)
        {
            var newElement = new XElement(element.Name.LocalName, element.HasElements ? element.Elements().Select(Remove) : (object?)element.Value);
            foreach (var attr in element.Attributes())
            {
                if (!attr.IsNamespaceDeclaration)
                    newElement.Add(new XAttribute(attr.Name.LocalName, attr.Value));
            }
            return newElement;
        }

    }

}