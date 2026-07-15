using MediatR;

namespace KellyServices.PARS.Application.Features.Todos.Commands.DeleteTodo
{
    public class DeleteTodoCommand : IRequest
    {
        public int Id { set; get; }
    }
}
