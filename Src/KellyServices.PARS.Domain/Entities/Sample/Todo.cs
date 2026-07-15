using KellyServices.PARS.Domain.Common;
using System.Collections.Generic;

namespace KellyServices.PARS.Domain.Entities.Sample
{
    public class Todo : BaseEntity
    {
        public string Name { set; get; }        
        public virtual ICollection<TodoItem> TodoItems { get; set; }
    }
}
