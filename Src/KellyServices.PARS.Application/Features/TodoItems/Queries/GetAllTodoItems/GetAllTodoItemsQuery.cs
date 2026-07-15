using MediatR;
using System.Collections.Generic;

namespace KellyServices.PARS.Application.Features.TodoItems.Queries.GetAllTodoItems
{
    public class GetAllTodoItemsQuery : IRequest<IEnumerable<TodoItemDto>>
    {
        public int Id { set; get; }
    }
}
