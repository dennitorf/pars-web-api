using KellyServices.PARS.Domain.Common;
using KellyServices.PARS.Domain.Entities.Archive;
using System;

namespace KellyServices.PARS.Domain.Entities.Requests
{
    public class PayrollRequestDocument : BaseEntity
    {
        public Guid PayrollDataRequestId { get; set; }
        public PayrollDataRequest PayrollDataRequest { get; set; }
        public Guid ArchiveDocumentId { get; set; }
        public ArchiveDocument ArchiveDocument { get; set; }
        public bool IsSelected { get; set; }
        public string ReviewedBy { get; set; }
        public DateTimeOffset? ReviewedAt { get; set; }
    }
}
