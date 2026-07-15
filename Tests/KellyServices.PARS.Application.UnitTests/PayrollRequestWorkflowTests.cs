using KellyServices.PARS.Application.Common.Exceptions;
using KellyServices.PARS.Application.Features.PayrollRequests;
using KellyServices.PARS.Domain.Entities.Archive;
using KellyServices.PARS.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace KellyServices.PARS.Application.UnitTests;

[TestFixture]
public class PayrollRequestWorkflowTests
{
    [Test]
    public async Task Submit_ExactIdentifiers_CreatesRankedCandidateAndAudit()
    {
        await using var db = TestDb.Create();
        var employee = Employee("K100", "Kelly Worker", "***-**-1234");
        db.EmployeeArchives.Add(employee);
        await db.SaveChangesAsync();

        var result = await Service(db).SubmitAsync(" Kelly ", " Worker ", "worker@example.com", "K100", "1234", new(2021, 1, 1), new(2024, 12, 31), "W-2", default);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(PayrollRequestStatus.CandidateReview.ToString()));
            Assert.That(result.Candidates, Has.Count.EqualTo(1));
            Assert.That(result.Candidates[0].ConfidenceScore, Is.EqualTo(100));
            Assert.That(db.ArchiveAuditEvents.Count(), Is.EqualTo(1));
            Assert.That(result.RequestNumber, Does.StartWith("PARS-"));
        });
    }

    [Test]
    public async Task Submit_NoMatch_RequiresDatabaseSearch()
    {
        await using var db = TestDb.Create();
        var result = await Service(db).SubmitAsync("Missing", "Worker", "missing@example.com", "K999", null!, new(2022, 1, 1), new(2023, 1, 1), null!, default);
        Assert.That(result.Status, Is.EqualTo(PayrollRequestStatus.DatabaseSearchRequired.ToString()));
    }

    [Test]
    public async Task DeepSearch_PartialLastName_FindsCandidateAndAssignsSpecialist()
    {
        await using var db = TestDb.Create();
        db.EmployeeArchives.Add(Employee("K200", "Alex McWorker", "***-**-9999"));
        await db.SaveChangesAsync();
        var submitted = await Service(db).SubmitAsync("Alex", "Worker", "alex@example.com", "K404", null!, new(2020, 1, 1), new(2024, 1, 1), "W-2", default);

        var result = await Service(db).SearchAsync(submitted.Id, default);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(PayrollRequestStatus.CandidateReview.ToString()));
            Assert.That(result.Candidates.Single().ConfidenceScore, Is.EqualTo(10));
            Assert.That(result.AssignedTo, Is.EqualTo("payroll.specialist@kelly.test"));
        });
    }

    [Test]
    public async Task ConfirmCandidate_LoadsOnlyAvailableRequestedDocumentsInDateRange()
    {
        await using var db = TestDb.Create();
        var employee = Employee("K100", "Kelly Worker", "***-**-1234");
        var batch = Batch();
        db.AddRange(employee, batch,
            Document(employee, batch, "W-2", 2023, ArchiveDocumentStatus.Available),
            Document(employee, batch, "Paystub", 2023, ArchiveDocumentStatus.Available),
            Document(employee, batch, "W-2", 2018, ArchiveDocumentStatus.Available),
            Document(employee, batch, "W-2", 2024, ArchiveDocumentStatus.Failed));
        await db.SaveChangesAsync();
        var service = Service(db);
        var submitted = await service.SubmitAsync("Kelly", "Worker", "worker@example.com", "K100", "1234", new(2022, 1, 1), new(2024, 1, 1), "W-2", default);

        var result = await service.ConfirmAsync(submitted.Id, submitted.Candidates.Single().Id, "Identity verified", default);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(PayrollRequestStatus.DocumentReview.ToString()));
            Assert.That(result.Documents, Has.Count.EqualTo(1));
            Assert.That(result.Documents[0].DocumentType, Is.EqualTo("W-2"));
            Assert.That(result.SpecialistNotes, Is.EqualTo("Identity verified"));
        });
    }

    [Test]
    public async Task SelectAndFulfill_QueuesOnlySelectedDocuments()
    {
        await using var db = TestDb.Create();
        var employee = Employee("K100", "Kelly Worker", "***-**-1234");
        var batch = Batch();
        var document = Document(employee, batch, "W-2", 2023, ArchiveDocumentStatus.Available);
        db.AddRange(employee, batch, document);
        await db.SaveChangesAsync();
        var service = Service(db);
        var submitted = await service.SubmitAsync("Kelly", "Worker", "worker@example.com", "K100", "1234", new(2022, 1, 1), new(2024, 1, 1), "W-2", default);
        await service.ConfirmAsync(submitted.Id, submitted.Candidates.Single().Id, "Verified", default);
        await service.SelectDocumentAsync(submitted.Id, document.Id, true, default);

        var result = await service.FulfillAsync(submitted.Id, "delivery@example.com", "Approved", default);

        var fulfillment = await db.ArchiveFulfillments.SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(PayrollRequestStatus.FulfillmentQueued.ToString()));
            Assert.That(fulfillment.ArchiveDocumentId, Is.EqualTo(document.Id));
            Assert.That(fulfillment.EmployeeEmail, Is.EqualTo("delivery@example.com"));
            Assert.That(fulfillment.Status, Is.EqualTo(FulfillmentStatus.PendingReview));
        });
    }

    [Test]
    public async Task Fulfill_WithoutConfirmedCandidate_ThrowsValidationError()
    {
        await using var db = TestDb.Create();
        var submitted = await Service(db).SubmitAsync("Missing", "Worker", "missing@example.com", "K999", null!, new(2022, 1, 1), new(2023, 1, 1), "W-2", default);
        Assert.That(async () => await Service(db).FulfillAsync(submitted.Id, null!, "Approved", default), Throws.TypeOf<ValidationException>());
    }

    [Test]
    public async Task Get_MissingRequest_ThrowsNotFound()
    {
        await using var db = TestDb.Create();
        Assert.That(async () => await Service(db).GetAsync(Guid.NewGuid(), default), Throws.TypeOf<NotFoundException>());
    }

    private static PayrollRequestWorkflowService Service(KellyServices.PARS.Persistence.Contexts.AppDbContext db) => new(db, new TestCurrentUser());
    private static EmployeeArchive Employee(string id, string name, string tax) => new() { KellyId = id, EmployeeName = name, MaskedTaxId = tax, IsActive = true };
    private static ArchiveIngestionBatch Batch() => new() { MetadataFilePath = "/manifest.csv", MetadataChecksum = Guid.NewGuid().ToString("N"), StartedAt = DateTimeOffset.UtcNow, IsActive = true };
    private static ArchiveDocument Document(EmployeeArchive employee, ArchiveIngestionBatch batch, string type, int year, ArchiveDocumentStatus status) => new()
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
