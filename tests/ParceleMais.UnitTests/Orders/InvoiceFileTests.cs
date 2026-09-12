using System.Text;
using ParceleMais.Orders.Models;

namespace ParceleMais.UnitTests.Orders;

public class InvoiceFileTests
{
    private static readonly byte[] SampleBytes = Encoding.UTF8.GetBytes("conteúdo da nota fiscal");

    [Fact]
    public void FromBytes_CodificaOConteudoEmBase64()
    {
        var file = InvoiceFile.FromBytes(SampleBytes, "nota.pdf");

        Assert.Equal(Convert.ToBase64String(SampleBytes), file.Base64Content);
        Assert.Equal("nota.pdf", file.FileName);
    }

    [Fact]
    public void FromStream_ProduzOMesmoBase64QueFromBytes()
    {
        using var stream = new MemoryStream(SampleBytes);

        var file = InvoiceFile.FromStream(stream, "nota.pdf");

        Assert.Equal(Convert.ToBase64String(SampleBytes), file.Base64Content);
    }

    [Fact]
    public async Task FromStreamAsync_ProduzOMesmoBase64QueFromBytes()
    {
        using var stream = new MemoryStream(SampleBytes);

        var file = await InvoiceFile.FromStreamAsync(stream, "nota.pdf");

        Assert.Equal(Convert.ToBase64String(SampleBytes), file.Base64Content);
    }

    [Fact]
    public async Task FromFile_ProduzOMesmoBase64QueFromBytes()
    {
        var path = Path.GetTempFileName();
        try
        {
            await File.WriteAllBytesAsync(path, SampleBytes);

            var file = InvoiceFile.FromFile(path);

            Assert.Equal(Convert.ToBase64String(SampleBytes), file.Base64Content);
            Assert.Equal(Path.GetFileName(path), file.FileName);
        }
        finally
        {
            File.Delete(path);
        }
    }
}
