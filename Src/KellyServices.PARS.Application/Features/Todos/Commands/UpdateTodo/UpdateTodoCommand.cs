using KellyServices.PARS.Application.Features.Todos.Queries.GetAllTodos;
using MediatR;

namespace KellyServices.PARS.Application.Features.Todos.Commands.UpdateTodo
{
    public class UpdateTodoCommand : IRequest<TodoDto>
    {
        public int Id { get; set; } 
        public string Name { set; get; }
    }
}
