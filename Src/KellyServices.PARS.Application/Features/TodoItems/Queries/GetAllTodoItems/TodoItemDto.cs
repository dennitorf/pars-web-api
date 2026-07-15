using KellyServices.PARS.Application.Common.Interfaces.Mappings;
using KellyServices.PARS.Domain.Entities.Sample;
using System;

namespace KellyServices.PARS.Application.Features.TodoItems.Queries.GetAllTodoItems
{
    public class TodoItemDto : IMapFrom<TodoItem>
    {
        public int Id { get; set; }
        public string Name { set; get; }
        public DateTime CreatedDate { get; set; }
        public string CreatedBy { get; set; }
        public DateTime ModifiedDate { get; set; }
        public string LastModifiedBy { get; set; }
        public bool IsActive { set; get; }
        public int TodoId { set; get; }        
    }
}