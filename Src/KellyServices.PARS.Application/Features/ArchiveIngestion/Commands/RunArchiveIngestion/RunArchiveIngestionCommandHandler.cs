using KellyServices.PARS.Application.Common.Interfaces.Archive;
using KellyServices.PARS.Domain.Entities.Archive;
using KellyServices.PARS.Domain.Enums;
using KellyServices.PARS.Persistence.Contexts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KellyServices.PARS.Application.Features.ArchiveIngestion.Commands.RunArchiveIngestion
{
    public class RunArchiveIngestionCommandHandler : IRequestHandler<RunArchiveIngestionCommand, ArchiveIngestionRunResult>
    {
        private static readonly SemaphoreSlim ExecutionLock = new(1, 1);
        private readonly AppDbContext db;
        private readonly ISftpArchiveSource sftp;
        private readonly IArchiveFileStore files;
        private readonly ArchiveMetadataCsvParser parser;
        private readonly ArchiveIngestionOptions options;
        private readonly ILogger<RunArchiveIngestionCommandHandler> logger;
        public RunArchiveIngestionCommandHandler(AppDbContext db, ISftpArchiveSource sftp, IArchiveFileStore files, ArchiveMetadataCsvParser parser,
            IOptions<ArchiveIngestionOptions> options, ILogger<RunArchiveIngestionCommandHandler> logger)
        { this.db = db; this.sftp = sftp; this.files = files; this.parser = parser; this.options = options.Value; this.logger = logger; }

        public async Task<ArchiveIngestionRunResult> Handle(RunArchiveIngestionCommand command, CancellationToken cancellationToken)
        {
            if (!await ExecutionLock.WaitAsync(0, cancellationToken)) return new(null, "AlreadyRunning", 0, 0, 0, 0, "An ingestion command is already running in this API instance.");
            try
            {
                if (!await sftp.ExistsAsync(options.MetadataRemotePath, cancellationToken)) return new(null, "NoManifest", 0, 0, 0, 0, $"No metadata manifest is available at {options.MetadataRemotePath}.");
                using var csv = new MemoryStream(); await sftp.DownloadAsync(options.MetadataRemotePath, csv, cancellationToken);
                var checksum = await ComputeSha256Async(csv, cancellationToken); var records = parser.Parse(csv);
                if (records.Count > options.MaxRecordsPerRun) throw new InvalidDataException($"Metadata CSV contains {records.Count} rows, exceeding the configured limit of {options.MaxRecordsPerRun}.");
                var batch = await db.ArchiveIngestionBatches.SingleOrDefaultAsync(item => item.MetadataFilePath == options.MetadataRemotePath && item.MetadataChecksum == checksum, cancellationToken);
                if (batch?.Status == IngestionBatchStatus.Completed) { await sftp.MoveToProcessedAsync(options.MetadataRemotePath, options.ProcessedDirectory, cancellationToken); return Result(batch, "Manifest was already completed."); }
                if (batch is null) { batch = new ArchiveIngestionBatch { Id = Guid.NewGuid(), MetadataFilePath = options.MetadataRemotePath, MetadataChecksum = checksum, StartedAt = DateTimeOffset.UtcNow,
                    RecordsDiscovered = records.Count, Status = IngestionBatchStatus.Running, CreatedDate = DateTime.UtcNow, CreatedBy = $"ingestion:{command.TriggeredBy}", IsActive = true }; db.ArchiveIngestionBatches.Add(batch); }
                else { batch.Status = IngestionBatchStatus.Running; batch.LastError = null; batch.RecordsDiscovered = records.Count; batch.RecordsTransferred = 0; batch.RecordsSkipped = 0; batch.RecordsFailed = 0; batch.CompletedAt = null; }
                await db.SaveChangesAsync(cancellationToken);
                foreach (var record in records) { cancellationToken.ThrowIfCancellationRequested(); await ProcessRecordAsync(batch, record, cancellationToken); }
                batch.CompletedAt = DateTimeOffset.UtcNow; batch.Status = batch.RecordsFailed == 0 ? IngestionBatchStatus.Completed : IngestionBatchStatus.CompletedWithErrors; batch.ModifiedDate = DateTime.UtcNow; batch.LastModifiedBy = $"ingestion:{command.TriggeredBy}";
                await db.SaveChangesAsync(cancellationToken); if (batch.RecordsFailed == 0) await sftp.MoveToProcessedAsync(options.MetadataRemotePath, options.ProcessedDirectory, cancellationToken);
                return Result(batch, batch.RecordsFailed == 0 ? "Ingestion completed." : "Ingestion completed with record errors.");
            }
            finally { ExecutionLock.Release(); }
        }
        private async Task ProcessRecordAsync(ArchiveIngestionBatch batch, ArchiveImportRecord record, CancellationToken cancellationToken)
        {
            var document = await db.ArchiveDocuments.Include(item => item.EmployeeArchive).SingleOrDefaultAsync(item => item.SourcePath == record.RemoteFilePath && item.SourceChecksum == record.Sha256, cancellationToken);
            if (document?.Status == ArchiveDocumentStatus.Available) { batch.RecordsSkipped++; if (await sftp.ExistsAsync(record.RemoteFilePath, cancellationToken)) await sftp.MoveToProcessedAsync(record.RemoteFilePath, options.ProcessedDirectory, cancellationToken); return; }
            var employee = document?.EmployeeArchive ?? await db.EmployeeArchives.SingleOrDefaultAsync(item => item.KellyId == record.KellyId, cancellationToken);
            if (employee is null) { employee = new EmployeeArchive { Id = Guid.NewGuid(), KellyId = record.KellyId, EmployeeName = record.EmployeeName, MaskedTaxId = record.MaskedTaxId, StorageStatus = ArchiveStorageStatus.Processing,
                CreatedDate = DateTime.UtcNow, CreatedBy = nameof(RunArchiveIngestionCommand), IsActive = true }; db.EmployeeArchives.Add(employee); }
            var blobName = BuildBlobName(record); document ??= new ArchiveDocument { Id = Guid.NewGuid(), EmployeeArchive = employee, IngestionBatch = batch, DocumentType = record.DocumentType, DocumentYear = record.DocumentYear,
                DocumentPeriod = record.DocumentPeriod, OriginalFileName = Path.GetFileName(record.RemoteFilePath), FileSizeBytes = record.FileSizeBytes, ContentType = ContentTypeFor(record.RemoteFilePath), BlobContainer = options.BlobContainer,
                BlobName = blobName, SourcePath = record.RemoteFilePath, SourceChecksum = record.Sha256, CreatedDate = DateTime.UtcNow, CreatedBy = nameof(RunArchiveIngestionCommand), IsActive = true };
            if (db.Entry(document).State == EntityState.Detached) db.ArchiveDocuments.Add(document); document.Status = ArchiveDocumentStatus.Transferring; employee.StorageStatus = ArchiveStorageStatus.Processing; await db.SaveChangesAsync(cancellationToken);
            var temp = Path.GetTempFileName();
            try
            {
                await using var file = new FileStream(temp, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920, FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);
                await sftp.DownloadAsync(record.RemoteFilePath, file, cancellationToken); if (record.FileSizeBytes > 0 && file.Length != record.FileSizeBytes) throw new InvalidDataException($"File size mismatch for {record.RemoteFilePath}.");
                var checksum = await ComputeSha256Async(file, cancellationToken); if (!checksum.Equals(record.Sha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException($"SHA-256 mismatch for {record.RemoteFilePath}.");
                await files.UploadAsync(options.BlobContainer, blobName, file, document.ContentType, new Dictionary<string, string> { ["kellyId"] = record.KellyId, ["documentType"] = record.DocumentType, ["documentYear"] = record.DocumentYear.ToString(), ["sha256"] = checksum }, cancellationToken);
                document.Status = ArchiveDocumentStatus.Available; document.StoredAt = DateTimeOffset.UtcNow; document.FailureReason = null; employee.StorageStatus = ArchiveStorageStatus.Complete; employee.StatusDetail = null; batch.RecordsTransferred++;
                db.ArchiveAuditEvents.Add(new ArchiveAuditEvent { Id = Guid.NewGuid(), OccurredAt = DateTimeOffset.UtcNow, ActorId = "system:archive-ingestion", ActorDisplayName = "PARS ingestion command", Action = ArchiveAuditAction.Ingested,
                    Outcome = "Success", EmployeeArchive = employee, ArchiveDocument = document, Details = $"Transferred {record.RemoteFilePath} to {options.BlobContainer}/{blobName}.", CorrelationId = batch.Id.ToString(), CreatedDate = DateTime.UtcNow, CreatedBy = nameof(RunArchiveIngestionCommand), IsActive = true });
                await db.SaveChangesAsync(cancellationToken); await sftp.MoveToProcessedAsync(record.RemoteFilePath, options.ProcessedDirectory, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException) { document.Status = ArchiveDocumentStatus.Failed; document.FailureReason = exception.Message; employee.StorageStatus = ArchiveStorageStatus.Review; employee.StatusDetail = $"Transfer failed for {record.RemoteFilePath}.";
                batch.RecordsFailed++; batch.LastError = exception.Message; logger.LogError(exception, "Failed to ingest {RemotePath} for {KellyId}.", record.RemoteFilePath, record.KellyId); await db.SaveChangesAsync(cancellationToken); }
        }
        private static ArchiveIngestionRunResult Result(ArchiveIngestionBatch batch, string message) => new(batch.Id, batch.Status.ToString(), batch.RecordsDiscovered, batch.RecordsTransferred, batch.RecordsSkipped, batch.RecordsFailed, message);
        private static async Task<string> ComputeSha256Async(Stream stream, CancellationToken token) { stream.Position = 0; var hash = await SHA256.HashDataAsync(stream, token); stream.Position = 0; return Convert.ToHexString(hash).ToLowerInvariant(); }
        private static string BuildBlobName(ArchiveImportRecord record) => $"{record.DocumentYear}/{Safe(record.DocumentType)}/{Safe(record.KellyId)}/{record.Sha256}{Path.GetExtension(record.RemoteFilePath).ToLowerInvariant()}";
        private static string Safe(string value) { var builder = new StringBuilder(value.Length); foreach (var c in value.ToLowerInvariant()) builder.Append(char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-'); return builder.ToString().Trim('-'); }
        private static string ContentTypeFor(string path) => Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "application/octet-stream";
    }
}
