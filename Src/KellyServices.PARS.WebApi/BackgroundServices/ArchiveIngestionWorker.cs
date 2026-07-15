using KellyServices.PARS.Application.Common.Interfaces.Archive;
using KellyServices.PARS.Application.Features.ArchiveIngestion;
using KellyServices.PARS.Domain.Entities.Archive;
using KellyServices.PARS.Domain.Enums;
using KellyServices.PARS.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KellyServices.PARS.WebApi.BackgroundServices
{
    public class ArchiveIngestionWorker : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ISftpArchiveSource sftpSource;
        private readonly IArchiveFileStore fileStore;
        private readonly ArchiveMetadataCsvParser csvParser;
        private readonly ArchiveIngestionOptions options;
        private readonly ILogger<ArchiveIngestionWorker> logger;

        public ArchiveIngestionWorker(IServiceScopeFactory scopeFactory, ISftpArchiveSource sftpSource, IArchiveFileStore fileStore,
            ArchiveMetadataCsvParser csvParser, IOptions<ArchiveIngestionOptions> options, ILogger<ArchiveIngestionWorker> logger)
        {
            this.scopeFactory = scopeFactory;
            this.sftpSource = sftpSource;
            this.fileStore = fileStore;
            this.csvParser = csvParser;
            this.options = options.Value;
            this.logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!options.Enabled)
            {
                logger.LogInformation("PARS archive ingestion worker is disabled.");
                return;
            }

            if (options.PollIntervalSeconds < 30) throw new InvalidOperationException("ArchiveIngestion PollIntervalSeconds must be at least 30.");
            while (!stoppingToken.IsCancellationRequested)
            {
                try { await RunOnceAsync(stoppingToken); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception exception) { logger.LogError(exception, "PARS archive ingestion cycle failed."); }

                await Task.Delay(TimeSpan.FromSeconds(options.PollIntervalSeconds), stoppingToken);
            }
        }

        internal async Task RunOnceAsync(CancellationToken cancellationToken)
        {
            if (!await sftpSource.ExistsAsync(options.MetadataRemotePath, cancellationToken))
            {
                logger.LogDebug("No PARS metadata file is available at {MetadataPath}.", options.MetadataRemotePath);
                return;
            }

            using var csv = new MemoryStream();
            await sftpSource.DownloadAsync(options.MetadataRemotePath, csv, cancellationToken);
            var metadataChecksum = await ComputeSha256Async(csv, cancellationToken);
            var records = csvParser.Parse(csv);
            if (records.Count > options.MaxRecordsPerRun)
                throw new InvalidDataException($"Metadata CSV contains {records.Count} rows, exceeding the configured limit of {options.MaxRecordsPerRun}. The file was not moved or partially processed.");

            using var scope = scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var batch = await dbContext.ArchiveIngestionBatches.SingleOrDefaultAsync(item => item.MetadataFilePath == options.MetadataRemotePath && item.MetadataChecksum == metadataChecksum, cancellationToken);

            if (batch?.Status == IngestionBatchStatus.Completed)
            {
                await sftpSource.MoveToProcessedAsync(options.MetadataRemotePath, options.ProcessedDirectory, cancellationToken);
                return;
            }

            if (batch is null)
            {
                batch = new ArchiveIngestionBatch
                {
                    Id = Guid.NewGuid(), MetadataFilePath = options.MetadataRemotePath, MetadataChecksum = metadataChecksum,
                    StartedAt = DateTimeOffset.UtcNow, RecordsDiscovered = records.Count, Status = IngestionBatchStatus.Running,
                    CreatedDate = DateTime.UtcNow, CreatedBy = nameof(ArchiveIngestionWorker), IsActive = true
                };
                dbContext.ArchiveIngestionBatches.Add(batch);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            else
            {
                batch.Status = IngestionBatchStatus.Running;
                batch.LastError = null;
                batch.RecordsDiscovered = records.Count;
                batch.RecordsTransferred = 0;
                batch.RecordsSkipped = 0;
                batch.RecordsFailed = 0;
            }

            foreach (var record in records)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ProcessRecordAsync(dbContext, batch, record, cancellationToken);
            }

            batch.CompletedAt = DateTimeOffset.UtcNow;
            batch.Status = batch.RecordsFailed == 0 ? IngestionBatchStatus.Completed : IngestionBatchStatus.CompletedWithErrors;
            batch.ModifiedDate = DateTime.UtcNow;
            batch.LastModifiedBy = nameof(ArchiveIngestionWorker);
            await dbContext.SaveChangesAsync(cancellationToken);

            if (batch.RecordsFailed == 0)
            {
                await sftpSource.MoveToProcessedAsync(options.MetadataRemotePath, options.ProcessedDirectory, cancellationToken);
            }
        }

        private async Task ProcessRecordAsync(AppDbContext dbContext, ArchiveIngestionBatch batch, ArchiveImportRecord record, CancellationToken cancellationToken)
        {
            var document = await dbContext.ArchiveDocuments.Include(item => item.EmployeeArchive)
                .SingleOrDefaultAsync(item => item.SourcePath == record.RemoteFilePath && item.SourceChecksum == record.Sha256, cancellationToken);

            if (document?.Status == ArchiveDocumentStatus.Available)
            {
                batch.RecordsSkipped++;
                if (await sftpSource.ExistsAsync(record.RemoteFilePath, cancellationToken))
                    await sftpSource.MoveToProcessedAsync(record.RemoteFilePath, options.ProcessedDirectory, cancellationToken);
                return;
            }

            var employee = document?.EmployeeArchive ?? await dbContext.EmployeeArchives.SingleOrDefaultAsync(item => item.KellyId == record.KellyId, cancellationToken);
            if (employee is null)
            {
                employee = new EmployeeArchive
                {
                    Id = Guid.NewGuid(), KellyId = record.KellyId, EmployeeName = record.EmployeeName, MaskedTaxId = record.MaskedTaxId,
                    StorageStatus = ArchiveStorageStatus.Processing, CreatedDate = DateTime.UtcNow, CreatedBy = nameof(ArchiveIngestionWorker), IsActive = true
                };
                dbContext.EmployeeArchives.Add(employee);
            }

            var blobName = BuildBlobName(record);
            document ??= new ArchiveDocument
            {
                Id = Guid.NewGuid(), EmployeeArchive = employee, IngestionBatch = batch, DocumentType = record.DocumentType,
                DocumentYear = record.DocumentYear, DocumentPeriod = record.DocumentPeriod, OriginalFileName = Path.GetFileName(record.RemoteFilePath),
                FileSizeBytes = record.FileSizeBytes, ContentType = ContentTypeFor(record.RemoteFilePath), BlobContainer = options.BlobContainer,
                BlobName = blobName, SourcePath = record.RemoteFilePath, SourceChecksum = record.Sha256,
                CreatedDate = DateTime.UtcNow, CreatedBy = nameof(ArchiveIngestionWorker), IsActive = true
            };
            if (dbContext.Entry(document).State == EntityState.Detached) dbContext.ArchiveDocuments.Add(document);

            document.Status = ArchiveDocumentStatus.Transferring;
            employee.StorageStatus = ArchiveStorageStatus.Processing;
            await dbContext.SaveChangesAsync(cancellationToken);

            var temporaryPath = Path.GetTempFileName();
            try
            {
                await using var file = new FileStream(temporaryPath, FileMode.Create, FileAccess.ReadWrite, FileShare.None, 81920,
                    FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);
                await sftpSource.DownloadAsync(record.RemoteFilePath, file, cancellationToken);
                if (record.FileSizeBytes > 0 && file.Length != record.FileSizeBytes)
                    throw new InvalidDataException($"File size mismatch for {record.RemoteFilePath}: expected {record.FileSizeBytes}, received {file.Length}.");

                var checksum = await ComputeSha256Async(file, cancellationToken);
                if (!checksum.Equals(record.Sha256, StringComparison.OrdinalIgnoreCase))
                    throw new InvalidDataException($"SHA-256 mismatch for {record.RemoteFilePath}.");

                await fileStore.UploadAsync(options.BlobContainer, blobName, file, document.ContentType,
                    new Dictionary<string, string> { ["kellyId"] = record.KellyId, ["documentType"] = record.DocumentType, ["documentYear"] = record.DocumentYear.ToString(), ["sha256"] = checksum }, cancellationToken);

                document.Status = ArchiveDocumentStatus.Available;
                document.StoredAt = DateTimeOffset.UtcNow;
                document.FailureReason = null;
                employee.StorageStatus = ArchiveStorageStatus.Complete;
                employee.StatusDetail = null;
                batch.RecordsTransferred++;
                dbContext.ArchiveAuditEvents.Add(new ArchiveAuditEvent
                {
                    Id = Guid.NewGuid(), OccurredAt = DateTimeOffset.UtcNow, ActorId = "system:archive-ingestion", ActorDisplayName = "PARS ingestion worker",
                    Action = ArchiveAuditAction.Ingested, Outcome = "Success", EmployeeArchive = employee, ArchiveDocument = document,
                    Details = $"Transferred {record.RemoteFilePath} to {options.BlobContainer}/{blobName}.", CorrelationId = batch.Id.ToString(),
                    CreatedDate = DateTime.UtcNow, CreatedBy = nameof(ArchiveIngestionWorker), IsActive = true
                });
                await dbContext.SaveChangesAsync(cancellationToken);
                await sftpSource.MoveToProcessedAsync(record.RemoteFilePath, options.ProcessedDirectory, cancellationToken);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                document.Status = ArchiveDocumentStatus.Failed;
                document.FailureReason = exception.Message;
                employee.StorageStatus = ArchiveStorageStatus.Review;
                employee.StatusDetail = $"Transfer failed for {record.RemoteFilePath}.";
                batch.RecordsFailed++;
                batch.LastError = exception.Message;
                logger.LogError(exception, "Failed to ingest {RemotePath} for {KellyId}.", record.RemoteFilePath, record.KellyId);
                await dbContext.SaveChangesAsync(cancellationToken);
            }
        }

        private static async Task<string> ComputeSha256Async(Stream stream, CancellationToken cancellationToken)
        {
            stream.Position = 0;
            var hash = await SHA256.HashDataAsync(stream, cancellationToken);
            stream.Position = 0;
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static string BuildBlobName(ArchiveImportRecord record)
        {
            var extension = Path.GetExtension(record.RemoteFilePath).ToLowerInvariant();
            return $"{record.DocumentYear}/{Safe(record.DocumentType)}/{Safe(record.KellyId)}/{record.Sha256}{extension}";
        }

        private static string Safe(string value)
        {
            var builder = new StringBuilder(value.Length);
            foreach (var character in value.ToLowerInvariant()) builder.Append(char.IsLetterOrDigit(character) || character is '-' or '_' ? character : '-');
            return builder.ToString().Trim('-');
        }

        private static string ContentTypeFor(string path) => Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase) ? "application/pdf" : "application/octet-stream";
    }
}
