using KellyServices.PARS.Application.Features.TodoItems.Queries.GetAllTodoItems;
using MediatR;

namespace KellyServices.PARS.Application.Features.TodoItems.Commands.CreateTodoItem
{
    public class CreateTodoItemCommand : IRequest<TodoItemDto>
    {
        public string Name { set; get; }
        public int TodoId { set; get; }
    }
}
