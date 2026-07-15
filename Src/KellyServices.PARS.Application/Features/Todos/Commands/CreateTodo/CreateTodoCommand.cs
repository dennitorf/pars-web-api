using KellyServices.PARS.Application.Features.Todos.Queries.GetAllTodos;
using MediatR;

namespace KellyServices.PARS.Application.Features.Todos.Commands.CreateTodo
{
    public class CreateTodoCommand : IRequest<TodoDto>
    {
        public string Name { set; get; }
    }
}
