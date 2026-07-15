using KellyServices.PARS.Application.Features.ArchiveIngestion;
using System.Text;

namespace KellyServices.PARS.Application.UnitTests;

[TestFixture]
public class ArchiveMetadataCsvParserTests
{
    private const string Header = "KellyId,EmployeeName,MaskedTaxId,DocumentType,DocumentYear,DocumentPeriod,RemoteFilePath,FileSizeBytes,Sha256\n";
    private static Stream Csv(string body) => new MemoryStream(Encoding.UTF8.GetBytes(Header + body));

    [Test]
    public void Parse_ValidQuotedRow_MapsAllMetadata()
    {
        using var csv = Csv("K100,\"Worker, Kelly\",***-**-1234,W-2,2024,Annual,/files/w2.pdf,42," + new string('a', 64));
        var record = new ArchiveMetadataCsvParser().Parse(csv).Single();
        Assert.Multiple(() => { Assert.That(record.EmployeeName, Is.EqualTo("Worker, Kelly")); Assert.That(record.DocumentYear, Is.EqualTo(2024)); Assert.That(record.FileSizeBytes, Is.EqualTo(42)); });
    }

    [Test]
    public void Parse_MissingRequiredHeader_ThrowsHelpfulError()
    {
        using var csv = new MemoryStream(Encoding.UTF8.GetBytes("KellyId,EmployeeName\nK1,Worker"));
        Assert.That(() => new ArchiveMetadataCsvParser().Parse(csv), Throws.TypeOf<InvalidDataException>().With.Message.Contains("missing required columns"));
    }

    [TestCase("not-a-year", "0", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "DocumentYear")]
    [TestCase("2024", "not-a-size", "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", "FileSizeBytes")]
    [TestCase("2024", "1", "bad", "Sha256")]
    public void Parse_InvalidData_RejectsRow(string year, string size, string sha, string message)
    {
        using var csv = Csv($"K1,Worker,***1234,W-2,{year},Annual,/w2.pdf,{size},{sha}");
        Assert.That(() => new ArchiveMetadataCsvParser().Parse(csv), Throws.TypeOf<InvalidDataException>().With.Message.Contains(message));
    }
}
