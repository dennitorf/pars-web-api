using KellyServices.PARS.Domain.Common;
using KellyServices.PARS.Domain.Enums;
using System;

namespace KellyServices.PARS.Domain.Entities.Archive
{
    public class ArchiveFulfillment : BaseEntity
    {
        public Guid ArchiveDocumentId { get; set; }
        public ArchiveDocument ArchiveDocument { get; set; }
        public string EmployeeEmail { get; set; }
        public string RequestedBy { get; set; }
        public string BusinessReason { get; set; }
        public FulfillmentStatus Status { get; set; } = FulfillmentStatus.PendingReview;
        public DateTimeOffset RequestedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public string FailureReason { get; set; }
    }
}
