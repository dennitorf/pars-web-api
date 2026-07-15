using KellyServices.PARS.Domain.Entities.Archive;
using KellyServices.PARS.Domain.Entities.Requests;
using KellyServices.PARS.Domain.Enums;

namespace KellyServices.PARS.Domain.UnitTests;

[TestFixture]
public class EntityDefaultsTests
{
    [Test]
    public void BaseEntities_UseNonEmptyGuidPrimaryKeys()
    {
        Assert.Multiple(() =>
        {
            Assert.That(new EmployeeArchive().Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(new ArchiveDocument().Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(new PayrollDataRequest().Id, Is.Not.EqualTo(Guid.Empty));
        });
    }

    [Test]
    public void ArchiveEntities_HaveSafeWorkflowDefaults()
    {
        var employee = new EmployeeArchive();
        var document = new ArchiveDocument();
        var batch = new ArchiveIngestionBatch();
        var fulfillment = new ArchiveFulfillment();

        Assert.Multiple(() =>
        {
            Assert.That(employee.StorageStatus, Is.EqualTo(ArchiveStorageStatus.Processing));
            Assert.That(employee.Documents, Is.Empty);
            Assert.That(document.Status, Is.EqualTo(ArchiveDocumentStatus.Pending));
            Assert.That(document.ContentType, Is.EqualTo("application/pdf"));
            Assert.That(batch.Status, Is.EqualTo(IngestionBatchStatus.Running));
            Assert.That(batch.Documents, Is.Empty);
            Assert.That(fulfillment.Status, Is.EqualTo(FulfillmentStatus.PendingReview));
        });
    }

    [Test]
    public void PayrollRequestEntities_HaveSafeWorkflowDefaults()
    {
        var request = new PayrollDataRequest();
        var candidate = new PayrollRequestCandidate();

        Assert.Multiple(() =>
        {
            Assert.That(request.Status, Is.EqualTo(PayrollRequestStatus.Submitted));
            Assert.That(request.Candidates, Is.Empty);
            Assert.That(request.Documents, Is.Empty);
            Assert.That(candidate.Status, Is.EqualTo(PayrollRequestCandidateStatus.Suggested));
        });
    }
}
