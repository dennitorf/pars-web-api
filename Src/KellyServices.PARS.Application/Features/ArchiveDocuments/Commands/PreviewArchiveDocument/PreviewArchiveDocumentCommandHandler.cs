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
namespace KellyServices.PARS.Application.Features.ArchiveDocuments.Commands.PreviewArchiveDocument
{
    public class PreviewArchiveDocumentCommandHandler : IRequestHandler<PreviewArchiveDocumentCommand, DocumentPreviewResponse>
    {
        private readonly AppDbContext db; private readonly ICurrentUserService user;
        public PreviewArchiveDocumentCommandHandler(AppDbContext db, ICurrentUserService user) { this.db = db; this.user = user; }
        public async Task<DocumentPreviewResponse> Handle(PreviewArchiveDocumentCommand request, CancellationToken cancellationToken)
        {
            var document = await db.ArchiveDocuments.Include(item => item.EmployeeArchive).SingleOrDefaultAsync(item => item.Id == request.DocumentId && item.Status == ArchiveDocumentStatus.Available, cancellationToken)
                ?? throw new NotFoundException(nameof(ArchiveDocument), request.DocumentId);
            db.ArchiveAuditEvents.Add(Audit(document, ArchiveAuditAction.Viewed, "Preview opened.")); await db.SaveChangesAsync(cancellationToken);
            return new DocumentPreviewResponse(document.ToSummary(), $"/api/archive-documents/{document.Id}/content?disposition=inline", "Preview access is audited.");
        }
        private ArchiveAuditEvent Audit(ArchiveDocument document, ArchiveAuditAction action, string details) => new() { Id = Guid.NewGuid(), OccurredAt = DateTimeOffset.UtcNow, ActorId = user.UserId, ActorDisplayName = user.DisplayName,
            Action = action, Outcome = "Success", EmployeeArchiveId = document.EmployeeArchiveId, ArchiveDocumentId = document.Id, Details = details, CorrelationId = user.CorrelationId, CreatedDate = DateTime.UtcNow, CreatedBy = user.UserId, IsActive = true };
    }
}
