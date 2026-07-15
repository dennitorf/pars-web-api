using KellyServices.PARS.Application.Features.TodoItems.Queries.GetAllTodoItems;
using MediatR;

namespace KellyServices.PARS.Application.Features.TodoItems.Commands.UpdateTodoItem
{
    public class UpdateTodoItemCommand : IRequest<TodoItemDto>
    {
        public int Id { get; set; }
        public string Name { set; get; }
    }
}
