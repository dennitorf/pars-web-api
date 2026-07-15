using KellyServices.PARS.Domain.Common;
using KellyServices.PARS.Domain.Enums;
using System;

namespace KellyServices.PARS.Domain.Entities.Archive
{
    public class ArchiveDocument : BaseEntity
    {
        public Guid EmployeeArchiveId { get; set; }
        public EmployeeArchive EmployeeArchive { get; set; }
        public Guid IngestionBatchId { get; set; }
        public ArchiveIngestionBatch IngestionBatch { get; set; }
        public string DocumentType { get; set; }
        public int DocumentYear { get; set; }
        public string DocumentPeriod { get; set; }
        public string OriginalFileName { get; set; }
        public long FileSizeBytes { get; set; }
        public string ContentType { get; set; } = "application/pdf";
        public string BlobContainer { get; set; }
        public string BlobName { get; set; }
        public string SourcePath { get; set; }
        public string SourceChecksum { get; set; }
        public ArchiveDocumentStatus Status { get; set; } = ArchiveDocumentStatus.Pending;
        public string FailureReason { get; set; }
        public DateTimeOffset? StoredAt { get; set; }
    }
}
