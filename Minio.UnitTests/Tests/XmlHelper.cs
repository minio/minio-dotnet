using System.Text;
using System.Xml.Linq;
using Xunit;
using Minio.Helpers;

namespace Minio.UnitTests.Tests;

public class XmlHelperTests
{
    [Fact]
    public async Task CheckLoadXDocumentAsync()
    {
        XNamespace ns1 = "https://test1.example.com"; 
        XNamespace ns2 = "https://test2.example.com";
        var xDocOrg = new XDocument(new XElement(ns1 + "root",
            new XElement(ns1 + "child1",
                new XAttribute(ns1 + "attribute1", "value1"),
                new XAttribute(ns2 + "attribute2", "value2"),
                new XAttribute("attribute3", "value3")),
            new XElement(ns2 + "child2",
                new XAttribute(ns1 + "attribute1", "value1"),
                new XAttribute(ns2 + "attribute2", "value2"),
                new XAttribute("attribute3", "value3"))));
        var xmlOrg = xDocOrg.ToString();

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(xmlOrg));
        var xDocParsed = await XmlHelper.LoadXDocumentAsync(ms, CancellationToken.None).ConfigureAwait(true);
        var xmlParsed = xDocParsed.ToString();

        const string expectedXml = "<root>\n  <child1 attribute1=\"value1\" attribute2=\"value2\" attribute3=\"value3\" />\n  <child2 attribute1=\"value1\" attribute2=\"value2\" attribute3=\"value3\" />\n</root>";
        Assert.Equal(expectedXml, xmlParsed);
    }

}