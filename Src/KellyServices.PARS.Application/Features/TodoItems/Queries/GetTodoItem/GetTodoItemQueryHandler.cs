using AutoMapper;
using KellyServices.PARS.Application.Common.Exceptions;
using KellyServices.PARS.Application.Features.TodoItems.Queries.GetAllTodoItems;
using KellyServices.PARS.Persistence.Contexts;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace KellyServices.PARS.Application.Features.TodoItems.Queries.GetTodoItem
{
    public class GetTodoItemQueryHandler : IRequestHandler<GetTodoItemQuery, TodoItemDto>
    {
        private ILogger logger;
        private AppDbContext db;
        private IMapper mapper;

        public GetTodoItemQueryHandler(ILogger<GetTodoItemQueryHandler> logger, AppDbContext db, IMapper mapper)
        {
            this.logger = logger;
            this.db = db;
            this.mapper = mapper;
        }

        public async Task<TodoItemDto> Handle(GetTodoItemQuery request, CancellationToken cancellationToken)
        {
            var ent = await db.TodoItems.FindAsync(request.Id);

            if (ent == null)
                throw new NotFoundException(nameof(TodoItemDto), request.Id);

            return mapper.Map<TodoItemDto>(ent);
        }
    }
}
