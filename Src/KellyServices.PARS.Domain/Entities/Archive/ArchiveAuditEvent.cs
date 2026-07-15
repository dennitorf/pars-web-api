using KellyServices.PARS.Domain.Common;
using KellyServices.PARS.Domain.Enums;
using System;

namespace KellyServices.PARS.Domain.Entities.Archive
{
    public class ArchiveAuditEvent : BaseEntity
    {
        public DateTimeOffset OccurredAt { get; set; }
        public string ActorId { get; set; }
        public string ActorDisplayName { get; set; }
        public ArchiveAuditAction Action { get; set; }
        public string Outcome { get; set; }
        public Guid? EmployeeArchiveId { get; set; }
        public EmployeeArchive EmployeeArchive { get; set; }
        public Guid? ArchiveDocumentId { get; set; }
        public ArchiveDocument ArchiveDocument { get; set; }
        public string Details { get; set; }
        public string CorrelationId { get; set; }
    }
}
