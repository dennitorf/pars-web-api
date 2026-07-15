using KellyServices.PARS.Application.Common.Interfaces.Identity;
using KellyServices.PARS.Application.Features.ArchiveDocuments.Models;
using KellyServices.PARS.Domain.Entities.Archive;
using KellyServices.PARS.Domain.Enums;
using KellyServices.PARS.Persistence.Contexts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace KellyServices.PARS.Application.Features.ArchiveDocuments.Queries.SearchArchiveDocuments
{
    public class SearchArchiveDocumentsQueryHandler : IRequestHandler<SearchArchiveDocumentsQuery, SearchArchiveResponse>
    {
        private readonly AppDbContext db; private readonly ICurrentUserService currentUser;
        public SearchArchiveDocumentsQueryHandler(AppDbContext db, ICurrentUserService currentUser) { this.db = db; this.currentUser = currentUser; }
        public async Task<SearchArchiveResponse> Handle(SearchArchiveDocumentsQuery request, CancellationToken cancellationToken)
        {
            var page = Math.Max(1, request.Page); var pageSize = Math.Clamp(request.PageSize, 1, 200);
            var query = db.ArchiveDocuments.AsNoTracking().Include(item => item.EmployeeArchive).Where(item => item.Status == ArchiveDocumentStatus.Available);
            if (!string.IsNullOrWhiteSpace(request.Employee)) query = query.Where(item => item.EmployeeArchive.KellyId.Contains(request.Employee) || item.EmployeeArchive.EmployeeName.Contains(request.Employee));
            if (!string.IsNullOrWhiteSpace(request.DocumentType)) query = query.Where(item => item.DocumentType == request.DocumentType);
            if (request.FromYear.HasValue) query = query.Where(item => item.DocumentYear >= request.FromYear.Value); if (request.ToYear.HasValue) query = query.Where(item => item.DocumentYear <= request.ToYear.Value);
            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(item => item.DocumentYear).ThenBy(item => item.DocumentType).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(item => new ArchiveDocumentSummary(item.Id, item.EmployeeArchive.KellyId, item.EmployeeArchive.EmployeeName, item.EmployeeArchive.MaskedTaxId, item.DocumentType, item.DocumentYear, item.DocumentPeriod, item.FileSizeBytes, item.Status.ToString())).ToListAsync(cancellationToken);
            db.ArchiveAuditEvents.Add(NewAudit(ArchiveAuditAction.Searched, null, null, $"employee={request.Employee}; documentType={request.DocumentType}; fromYear={request.FromYear}; toYear={request.ToYear}; total={total}"));
            await db.SaveChangesAsync(cancellationToken); return new SearchArchiveResponse(total, page, pageSize, items);
        }
        private ArchiveAuditEvent NewAudit(ArchiveAuditAction action, Guid? employeeId, Guid? documentId, string details) => new() { Id = Guid.NewGuid(), OccurredAt = DateTimeOffset.UtcNow, ActorId = currentUser.UserId,
            ActorDisplayName = currentUser.DisplayName, Action = action, Outcome = "Success", EmployeeArchiveId = employeeId, ArchiveDocumentId = documentId, Details = details, CorrelationId = currentUser.CorrelationId,
            CreatedDate = DateTime.UtcNow, CreatedBy = currentUser.UserId, IsActive = true };
    }
}
