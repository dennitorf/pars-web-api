using KellyServices.PARS.Application.Features.ArchiveOperations.Models;
using KellyServices.PARS.Domain.Enums;
using KellyServices.PARS.Persistence.Contexts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace KellyServices.PARS.Application.Features.ArchiveOperations.Queries.GetArchiveAuditEvents
{
    public class GetArchiveAuditEventsQueryHandler : IRequestHandler<GetArchiveAuditEventsQuery, ArchiveAuditSearchResponse>
    {
        private readonly AppDbContext db;
        public GetArchiveAuditEventsQueryHandler(AppDbContext db) => this.db = db;
        public async Task<ArchiveAuditSearchResponse> Handle(GetArchiveAuditEventsQuery request, CancellationToken cancellationToken)
        {
            var page = Math.Max(1, request.Page); var pageSize = Math.Clamp(request.PageSize, 1, 500);
            var query = db.ArchiveAuditEvents.AsNoTracking().Include(item => item.EmployeeArchive).Include(item => item.ArchiveDocument).AsQueryable();
            if (!string.IsNullOrWhiteSpace(request.Query)) query = query.Where(item => item.ActorId.Contains(request.Query) || item.ActorDisplayName.Contains(request.Query) || item.CorrelationId.Contains(request.Query)
                || (item.EmployeeArchive != null && (item.EmployeeArchive.KellyId.Contains(request.Query) || item.EmployeeArchive.EmployeeName.Contains(request.Query))) || (item.ArchiveDocument != null && item.ArchiveDocument.OriginalFileName.Contains(request.Query)));
            if (!string.IsNullOrWhiteSpace(request.Action) && Enum.TryParse<ArchiveAuditAction>(request.Action, true, out var action)) query = query.Where(item => item.Action == action);
            if (request.From.HasValue) query = query.Where(item => item.OccurredAt >= request.From.Value); if (request.To.HasValue) query = query.Where(item => item.OccurredAt <= request.To.Value);
            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(item => item.OccurredAt).Skip((page - 1) * pageSize).Take(pageSize).Select(item => new ArchiveAuditEventResponse(item.Id, item.OccurredAt, item.ActorId, item.ActorDisplayName,
                item.Action.ToString(), item.EmployeeArchive != null ? item.EmployeeArchive.KellyId : null, item.EmployeeArchive != null ? item.EmployeeArchive.EmployeeName : null,
                item.ArchiveDocument != null ? item.ArchiveDocument.OriginalFileName : null, item.Outcome, item.CorrelationId)).ToListAsync(cancellationToken);
            return new ArchiveAuditSearchResponse(total, page, pageSize, items);
        }
    }
}
