using KellyServices.PARS.Domain.Common;
using KellyServices.PARS.Domain.Entities.Archive;
using KellyServices.PARS.Domain.Enums;
using System;

namespace KellyServices.PARS.Domain.Entities.Requests
{
    public class PayrollRequestCandidate : BaseEntity
    {
        public Guid PayrollDataRequestId { get; set; }
        public PayrollDataRequest PayrollDataRequest { get; set; }
        public Guid EmployeeArchiveId { get; set; }
        public EmployeeArchive EmployeeArchive { get; set; }
        public decimal ConfidenceScore { get; set; }
        public string MatchedAttributes { get; set; }
        public PayrollRequestCandidateStatus Status { get; set; } = PayrollRequestCandidateStatus.Suggested;
        public string ReviewedBy { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
    }
}
