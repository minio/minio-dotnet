using System.Xml.Linq;

namespace Minio.IntegrationTests.Helpers;

public static class XmlHelpers
{
    private static readonly XNamespace XsiNs = "https://www.w3.org/2001/XMLSchema-instance";

    private static readonly XName SchemaLocation = XsiNs + "schemaLocation";
    private static readonly XName NoNamespaceSchemaLocation = XsiNs + "noNamespaceSchemaLocation";

    public static bool DeepEqualsWithNormalization(XElement elt1, XElement elt2)
    {
        ArgumentNullException.ThrowIfNull(elt1);
        ArgumentNullException.ThrowIfNull(elt2);

        var d1 = NormalizeElement(elt1);
        var d2 = NormalizeElement(elt2);
        return XNode.DeepEquals(d1, d2);
    }

    private static XNode? NormalizeNode(XNode node)
    {
        return node switch
        {
            // Trim comments and processing instructions from normalized tree
            XComment or XProcessingInstruction => null,
            XElement e => NormalizeElement(e),
            // Only thing left is XCData and XText, so clone them
            _ => node
        };
    }

    private static XElement NormalizeElement(XElement element)
        =>  new(element.Name, NormalizeAttributes(element), element.Nodes().Select(NormalizeNode));

    private static IEnumerable<XAttribute> NormalizeAttributes(XElement element)
    {
        return element
            .Attributes()
            .Where(a => !a.IsNamespaceDeclaration && a.Name != SchemaLocation && a.Name != NoNamespaceSchemaLocation)
            .OrderBy(a => a.Name.NamespaceName)
            .ThenBy(a => a.Name.LocalName);
    }
}