using KellyServices.PARS.Application.Common.Exceptions;
using KellyServices.PARS.Application.Common.Interfaces.Identity;
using KellyServices.PARS.Application.Features.ArchiveDocuments.Models;
using KellyServices.PARS.Domain.Entities.Archive;
using KellyServices.PARS.Domain.Enums;
using KellyServices.PARS.Persistence.Contexts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace KellyServices.PARS.Application.Features.ArchiveDocuments.Commands.CreateEmailFulfillment
{
    public class CreateEmailFulfillmentCommandHandler : IRequestHandler<CreateEmailFulfillmentCommand, EmailFulfillmentResponse>
    {
        private readonly AppDbContext db; private readonly ICurrentUserService user;
        public CreateEmailFulfillmentCommandHandler(AppDbContext db, ICurrentUserService user) { this.db = db; this.user = user; }
        public async Task<EmailFulfillmentResponse> Handle(CreateEmailFulfillmentCommand request, CancellationToken cancellationToken)
        {
            var document = await db.ArchiveDocuments.SingleOrDefaultAsync(item => item.Id == request.DocumentId && item.Status == ArchiveDocumentStatus.Available, cancellationToken)
                ?? throw new NotFoundException(nameof(ArchiveDocument), request.DocumentId);
            var fulfillment = new ArchiveFulfillment { Id = Guid.NewGuid(), ArchiveDocumentId = document.Id, EmployeeEmail = request.EmployeeEmail, RequestedBy = user.UserId, BusinessReason = request.BusinessReason,
                Status = FulfillmentStatus.PendingReview, RequestedAt = DateTimeOffset.UtcNow, CreatedDate = DateTime.UtcNow, CreatedBy = user.UserId, IsActive = true };
            db.ArchiveFulfillments.Add(fulfillment); db.ArchiveAuditEvents.Add(new ArchiveAuditEvent { Id = Guid.NewGuid(), OccurredAt = DateTimeOffset.UtcNow, ActorId = user.UserId, ActorDisplayName = user.DisplayName,
                Action = ArchiveAuditAction.Emailed, Outcome = "PendingReview", EmployeeArchiveId = document.EmployeeArchiveId, ArchiveDocumentId = document.Id, Details = $"Fulfillment {fulfillment.Id} queued.", CorrelationId = user.CorrelationId,
                CreatedDate = DateTime.UtcNow, CreatedBy = user.UserId, IsActive = true });
            await db.SaveChangesAsync(cancellationToken); return new EmailFulfillmentResponse(fulfillment.Id, document.Id, fulfillment.EmployeeEmail, fulfillment.Status.ToString(), fulfillment.RequestedAt);
        }
    }
}
