using KellyServices.PARS.Application.Features.Todos.Queries.GetAllTodos;
using MediatR;

namespace KellyServices.PARS.Application.Features.Todos.Queries.GetTodo
{
    public class GetTodoQuery : IRequest<TodoDto>
    {
        public int Id { set; get; }
    }
}
