using KellyServices.PARS.Domain.Common;
using KellyServices.PARS.Domain.Entities.Archive;
using KellyServices.PARS.Domain.Enums;
using System;
using System.Collections.Generic;

namespace KellyServices.PARS.Domain.Entities.Requests
{
    public class PayrollDataRequest : BaseEntity
    {
        public string RequestNumber { get; set; }
        public string EmployeeFirstName { get; set; }
        public string EmployeeLastName { get; set; }
        public string EmployeeEmail { get; set; }
        public string KellyId { get; set; }
        public string TaxIdLastFour { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public string RequestedDocumentTypes { get; set; }
        public PayrollRequestStatus Status { get; set; } = PayrollRequestStatus.Submitted;
        public string AssignedTo { get; set; }
        public string SpecialistNotes { get; set; }
        public DateTimeOffset SubmittedAt { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
        public DateTimeOffset? SearchInitiatedAt { get; set; }
        public DateTimeOffset? CompletedAt { get; set; }
        public Guid? ConfirmedEmployeeArchiveId { get; set; }
        public EmployeeArchive ConfirmedEmployeeArchive { get; set; }
        public ICollection<PayrollRequestCandidate> Candidates { get; set; } = new List<PayrollRequestCandidate>();
        public ICollection<PayrollRequestDocument> Documents { get; set; } = new List<PayrollRequestDocument>();
    }
}
