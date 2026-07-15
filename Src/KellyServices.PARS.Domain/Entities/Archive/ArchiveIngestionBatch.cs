using KellyServices.PARS.Domain.Common;
using KellyServices.PARS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace KellyServices.PARS.Domain.Entities.Archive
{
    public class ArchiveIngestionBatch : BaseEntity
    {
        public string MetadataFilePath { get; set; }
        public string MetadataChecksum { get; set; }
        public IngestionBatchStatus Status { get; set; } = IngestionBatchStatus.Running;
        public DateTimeOffset StartedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public int RecordsDiscovered { get; set; }
        public int RecordsTransferred { get; set; }
        public int RecordsSkipped { get; set; }
        public int RecordsFailed { get; set; }
        public string LastError { get; set; }
        public ICollection<ArchiveDocument> Documents { get; set; } = new List<ArchiveDocument>();
    }
}
