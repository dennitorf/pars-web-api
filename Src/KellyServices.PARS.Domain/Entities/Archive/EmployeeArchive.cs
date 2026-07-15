using KellyServices.PARS.Domain.Common;
using KellyServices.PARS.Domain.Enums;
using System.Collections.Generic;

namespace KellyServices.PARS.Domain.Entities.Archive
{
    public class EmployeeArchive : BaseEntity
    {
        public string KellyId { get; set; }
        public string EmployeeName { get; set; }
        public string MaskedTaxId { get; set; }
        public ArchiveStorageStatus StorageStatus { get; set; } = ArchiveStorageStatus.Processing;
        public string StatusDetail { get; set; }
        public ICollection<ArchiveDocument> Documents { get; set; } = new List<ArchiveDocument>();
    }
}
