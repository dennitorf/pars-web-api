using KellyServices.PARS.Application.Features.ArchiveIngestion;
using KellyServices.PARS.Application.Features.ArchiveIngestion.Commands.RunArchiveIngestion;
using KellyServices.PARS.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;

namespace KellyServices.PARS.Application.UnitTests;

[TestFixture]
[NonParallelizable]
public class ArchiveIngestionCommandTests
{
    [Test]
    public async Task Handle_MissingManifest_ReturnsNoManifestWithoutDatabaseChanges()
    {
        await using var db = TestDb.Create();
        var result = await Handler(db, new TestSftpArchiveSource(), new TestArchiveFileStore()).Handle(new("OnDemand"), default);
        Assert.Multiple(() => { Assert.That(result.Status, Is.EqualTo("NoManifest")); Assert.That(db.ArchiveIngestionBatches, Is.Empty); });
    }

    [Test]
    public async Task Handle_ValidManifest_TransfersFileAndCompletesBatch()
    {
        await using var db = TestDb.Create();
        var sftp = new TestSftpArchiveSource();
        var store = new TestArchiveFileStore();
        var file = Encoding.UTF8.GetBytes("payroll-document");
        var sha = Convert.ToHexString(SHA256.HashData(file)).ToLowerInvariant();
        sftp.Files["/files/w2.pdf"] = file;
        sftp.Files["/manifest.csv"] = Manifest("/files/w2.pdf", file.Length, sha);

        var result = await Handler(db, sftp, store).Handle(new("Scheduled"), default);

        var document = await db.ArchiveDocuments.Include(x => x.EmployeeArchive).SingleAsync();
        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(IngestionBatchStatus.Completed.ToString()));
            Assert.That(result.Transferred, Is.EqualTo(1));
            Assert.That(document.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(document.Status, Is.EqualTo(ArchiveDocumentStatus.Available));
            Assert.That(document.EmployeeArchive.StorageStatus, Is.EqualTo(ArchiveStorageStatus.Complete));
            Assert.That(store.Uploads, Has.Count.EqualTo(1));
            Assert.That(sftp.Moved, Is.EquivalentTo(new[] { "/files/w2.pdf", "/manifest.csv" }));
            Assert.That(db.ArchiveAuditEvents.Single().Action, Is.EqualTo(ArchiveAuditAction.Ingested));
        });
    }

    [Test]
    public async Task Handle_ChecksumMismatch_RecordsFailureAndDoesNotMoveManifest()
    {
        await using var db = TestDb.Create();
        var sftp = new TestSftpArchiveSource();
        sftp.Files["/files/w2.pdf"] = Encoding.UTF8.GetBytes("wrong-content");
        sftp.Files["/manifest.csv"] = Manifest("/files/w2.pdf", 0, new string('a', 64));

        var result = await Handler(db, sftp, new TestArchiveFileStore()).Handle(new("Scheduled"), default);

        Assert.Multiple(() =>
        {
            Assert.That(result.Status, Is.EqualTo(IngestionBatchStatus.CompletedWithErrors.ToString()));
            Assert.That(result.Failed, Is.EqualTo(1));
            Assert.That(db.ArchiveDocuments.Single().Status, Is.EqualTo(ArchiveDocumentStatus.Failed));
            Assert.That(sftp.Moved, Does.Not.Contain("/manifest.csv"));
        });
    }

    private static RunArchiveIngestionCommandHandler Handler(KellyServices.PARS.Persistence.Contexts.AppDbContext db, TestSftpArchiveSource sftp, TestArchiveFileStore store) =>
        new(db, sftp, store, new ArchiveMetadataCsvParser(), Options.Create(new ArchiveIngestionOptions { MetadataRemotePath = "/manifest.csv", ProcessedDirectory = "/processed", BlobContainer = "archive", MaxRecordsPerRun = 10 }), NullLogger<RunArchiveIngestionCommandHandler>.Instance);

    private static byte[] Manifest(string path, long length, string sha) => Encoding.UTF8.GetBytes(
        "KellyId,EmployeeName,MaskedTaxId,DocumentType,DocumentYear,DocumentPeriod,RemoteFilePath,FileSizeBytes,Sha256\n" +
        $"K100,Kelly Worker,***-**-1234,W-2,2024,Annual,{path},{length},{sha}\n");
}
