using MediatR;

namespace KellyServices.PARS.Application.Features.TodoItems.Commands.DeleteTodoItem
{
    public class DeleteTodoItemCommand : IRequest
    {
        public int Id { set; get; }
    }
}
