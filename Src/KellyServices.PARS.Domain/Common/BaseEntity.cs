using System;

namespace KellyServices.PARS.Domain.Common
{
    public class BaseEntity
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public DateTime CreatedDate { get; set; }

        public string CreatedBy { get; set; }

        public DateTime ModifiedDate { get; set; }

        public string LastModifiedBy { get; set; }
        public bool IsActive { set; get; }
    }
}
