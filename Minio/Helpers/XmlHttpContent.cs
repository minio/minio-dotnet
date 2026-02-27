using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace Minio.Helpers;

internal class XmlHttpContent : HttpContent
{
    private static readonly XmlWriterSettings DefaultXmlWriterSettings = new()
    {
        OmitXmlDeclaration = true,
        Indent = false,
        Async = true
    };
    private readonly XDocument _xDocument;

    public XmlHttpContent(XElement xElement) : this(new XDocument(xElement))
    {
    }
    
    public XmlHttpContent(XDocument xDocument)
    {
        _xDocument = xDocument;
    }
    
    protected override async Task SerializeToStreamAsync(Stream stream, TransportContext? context)
    {
        var xmlWriter = XmlWriter.Create(stream, DefaultXmlWriterSettings);
        await using (xmlWriter.ConfigureAwait(false))
        {
            await _xDocument.WriteToAsync(xmlWriter, default).ConfigureAwait(false);
        }
    }

    protected override bool TryComputeLength(out long length)
    {
        // TODO: We may want to implement a "counting" stream to avoid allocating memory 
        using var ms = new MemoryStream();
        using (var xmlWriter = XmlWriter.Create(ms, DefaultXmlWriterSettings))
        {
            _xDocument.WriteTo(xmlWriter);
        }

        ms.Flush();
        length = ms.Length;
        return true;
    }
}