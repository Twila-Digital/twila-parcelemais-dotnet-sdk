namespace ParceleMais.Orders.Models;

public sealed class InvoiceFile
{
    private InvoiceFile(string fileName, string base64Content)
    {
        FileName = fileName;
        Base64Content = base64Content;
    }

    public string FileName { get; }

    internal string Base64Content { get; }

    public static InvoiceFile FromBytes(byte[] content, string fileName) =>
        new(fileName, Convert.ToBase64String(content));

    public static InvoiceFile FromStream(Stream content, string fileName)
    {
        using var buffer = new MemoryStream();
        content.CopyTo(buffer);
        return FromBytes(buffer.ToArray(), fileName);
    }

    public static async Task<InvoiceFile> FromStreamAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        using var buffer = new MemoryStream();

#if NETSTANDARD2_0
        await content.CopyToAsync(buffer).ConfigureAwait(false);
#else
        await content.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
#endif

        return FromBytes(buffer.ToArray(), fileName);
    }

    public static InvoiceFile FromFile(string path) =>
        FromBytes(File.ReadAllBytes(path), Path.GetFileName(path));
}
