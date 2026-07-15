using AutoMapper;
using KellyServices.PARS.Application.Common.Exceptions;
using KellyServices.PARS.Domain.Entities.Sample;
using KellyServices.PARS.Persistence.Contexts;
using MediatR;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace KellyServices.PARS.Application.Features.Todos.Commands.DeleteTodo
{
    public class DeleteTodoCommandHandler : IRequestHandler<DeleteTodoCommand>
    {
        private ILogger logger;
        private AppDbContext db;
        private IMapper mapper;

        public DeleteTodoCommandHandler(ILogger<DeleteTodoCommandHandler> logger, AppDbContext db, IMapper mapper)
        {
            this.logger = logger;
            this.db = db;
            this.mapper = mapper;
        }

        public async Task Handle(DeleteTodoCommand request, CancellationToken cancellationToken)
        {
            var ent = await db.Todos.FindAsync(request.Id);

            if (ent == null)
                throw new NotFoundException(nameof(Todo), request.Id);

            db.Todos.Remove(ent);
            await db.SaveChangesAsync(cancellationToken);

            return;
        }
    }
}
