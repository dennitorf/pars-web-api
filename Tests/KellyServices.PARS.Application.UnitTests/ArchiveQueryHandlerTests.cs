using KellyServices.PARS.Application.Features.ArchiveDocuments.Queries.SearchArchiveDocuments;
using KellyServices.PARS.Application.Features.ArchiveOperations.Queries.GetEmployeeArchives;
using KellyServices.PARS.Domain.Entities.Archive;
using KellyServices.PARS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KellyServices.PARS.Application.UnitTests;

[TestFixture]
public class ArchiveQueryHandlerTests
{
    [Test]
    public async Task EmployeeGrid_CountsOnlyAvailableDocumentsAndFiltersStatus()
    {
        await using var db = TestDb.Create();
        var employee = new EmployeeArchive { KellyId = "K100", EmployeeName = "Kelly Worker", MaskedTaxId = "***1234", StorageStatus = ArchiveStorageStatus.Complete, IsActive = true };
        var batch = new ArchiveIngestionBatch { MetadataFilePath = "/m.csv", MetadataChecksum = "abc", StartedAt = DateTimeOffset.UtcNow, IsActive = true };
        db.AddRange(employee, batch,
            Doc(employee, batch, "W-2", 2021, ArchiveDocumentStatus.Available),
            Doc(employee, batch, "Paystub", 2024, ArchiveDocumentStatus.Available),
            Doc(employee, batch, "W-2", 2025, ArchiveDocumentStatus.Failed));
        await db.SaveChangesAsync();

        var result = await new GetEmployeeArchivesQueryHandler(db).Handle(new GetEmployeeArchivesQuery { Query = "K100", Status = "Complete" }, default);

        var item = result.Items.Single();
        Assert.Multiple(() => { Assert.That(item.DocumentCount, Is.EqualTo(2)); Assert.That(item.W2Count, Is.EqualTo(1)); Assert.That(item.PaystubCount, Is.EqualTo(1)); Assert.That(item.CoverageFromYear, Is.EqualTo(2021)); Assert.That(item.CoverageToYear, Is.EqualTo(2024)); });
    }

    [Test]
    public async Task ArchiveSearch_AppliesFiltersAndWritesAuditEvent()
    {
        await using var db = TestDb.Create();
        var employee = new EmployeeArchive { KellyId = "K100", EmployeeName = "Kelly Worker", MaskedTaxId = "***1234", IsActive = true };
        var batch = new ArchiveIngestionBatch { MetadataFilePath = "/m.csv", MetadataChecksum = "abc", StartedAt = DateTimeOffset.UtcNow, IsActive = true };
        db.AddRange(employee, batch, Doc(employee, batch, "W-2", 2023, ArchiveDocumentStatus.Available), Doc(employee, batch, "Paystub", 2024, ArchiveDocumentStatus.Available));
        await db.SaveChangesAsync();

        var result = await new SearchArchiveDocumentsQueryHandler(db, new TestCurrentUser()).Handle(new SearchArchiveDocumentsQuery { Employee = "Kelly", DocumentType = "W-2", FromYear = 2022, ToYear = 2023 }, default);

        Assert.Multiple(() =>
        {
            Assert.That(result.Total, Is.EqualTo(1));
            Assert.That(result.Items.Single().DocumentType, Is.EqualTo("W-2"));
            Assert.That(db.ArchiveAuditEvents.Single().Action, Is.EqualTo(ArchiveAuditAction.Searched));
            Assert.That(db.ArchiveAuditEvents.Single().CorrelationId, Is.EqualTo("test-correlation"));
        });
    }

    private static ArchiveDocument Doc(EmployeeArchive employee, ArchiveIngestionBatch batch, string type, int year, ArchiveDocumentStatus status) => new()
    {
        EmployeeArchive = employee,
        IngestionBatch = batch,
        DocumentType = type,
        DocumentYear = year,
        DocumentPeriod = "Annual",
        OriginalFileName = "file.pdf",
        FileSizeBytes = 10,
        BlobContainer = "archive",
        BlobName = $"{Guid.NewGuid():N}.pdf",
        SourcePath = $"/{Guid.NewGuid():N}.pdf",
        SourceChecksum = new string('a', 64),
        Status = status,
        IsActive = true
    };
}
