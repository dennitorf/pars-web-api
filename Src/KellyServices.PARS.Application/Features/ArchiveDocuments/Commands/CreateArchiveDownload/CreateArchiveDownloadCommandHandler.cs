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
namespace KellyServices.PARS.Application.Features.ArchiveDocuments.Commands.CreateArchiveDownload
{
    public class CreateArchiveDownloadCommandHandler : IRequestHandler<CreateArchiveDownloadCommand, DocumentDownloadResponse>
    {
        private readonly AppDbContext db; private readonly ICurrentUserService user;
        public CreateArchiveDownloadCommandHandler(AppDbContext db, ICurrentUserService user) { this.db = db; this.user = user; }
        public async Task<DocumentDownloadResponse> Handle(CreateArchiveDownloadCommand request, CancellationToken cancellationToken)
        {
            var document = await db.ArchiveDocuments.SingleOrDefaultAsync(item => item.Id == request.DocumentId && item.Status == ArchiveDocumentStatus.Available, cancellationToken)
                ?? throw new NotFoundException(nameof(ArchiveDocument), request.DocumentId);
            var audit = new ArchiveAuditEvent
            {
                Id = Guid.NewGuid(),
                OccurredAt = DateTimeOffset.UtcNow,
                ActorId = user.UserId,
                ActorDisplayName = user.DisplayName,
                Action = ArchiveAuditAction.Downloaded,
                Outcome = "Success",
                EmployeeArchiveId = document.EmployeeArchiveId,
                ArchiveDocumentId = document.Id,
                Details = "Secure download created.",
                CorrelationId = user.CorrelationId,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = user.UserId,
                IsActive = true
            };
            db.ArchiveAuditEvents.Add(audit); await db.SaveChangesAsync(cancellationToken);
            return new DocumentDownloadResponse(audit.Id, document.Id, "Ready", $"/api/archive-documents/{document.Id}/content?disposition=attachment");
        }
    }
}
