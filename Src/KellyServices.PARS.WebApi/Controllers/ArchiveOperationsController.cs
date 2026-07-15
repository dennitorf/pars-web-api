using KellyServices.PARS.Domain.Enums;
using KellyServices.PARS.Persistence.Contexts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace KellyServices.PARS.WebApi.Controllers
{
    [ApiController]
    [Route("api/archive-operations")]
    [Produces("application/json")]
    public class ArchiveOperationsController : ControllerBase
    {
        private readonly AppDbContext dbContext;
        public ArchiveOperationsController(AppDbContext dbContext) => this.dbContext = dbContext;

        [HttpGet("employees")]
        public async Task<ActionResult<EmployeeArchiveSearchResponse>> GetEmployees([FromQuery] string query = null, [FromQuery] string status = null,
            [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);
            var employees = dbContext.EmployeeArchives.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(query)) employees = employees.Where(item => item.KellyId.Contains(query) || item.EmployeeName.Contains(query));
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<ArchiveStorageStatus>(status, true, out var parsedStatus))
                employees = employees.Where(item => item.StorageStatus == parsedStatus);

            var total = await employees.CountAsync(cancellationToken);
            var items = await employees.OrderBy(item => item.EmployeeName).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(item => new EmployeeArchiveSummary(item.Id, item.KellyId, item.EmployeeName,
                    item.Documents.Count(document => document.Status == ArchiveDocumentStatus.Available),
                    item.Documents.Count(document => document.Status == ArchiveDocumentStatus.Available && document.DocumentType == "W-2"),
                    item.Documents.Count(document => document.Status == ArchiveDocumentStatus.Available && document.DocumentType == "Paystub"),
                    item.Documents.Where(document => document.Status == ArchiveDocumentStatus.Available).Min(document => (int?)document.DocumentYear),
                    item.Documents.Where(document => document.Status == ArchiveDocumentStatus.Available).Max(document => (int?)document.DocumentYear),
                    item.StorageStatus.ToString(), item.ModifiedDate == default ? item.CreatedDate : item.ModifiedDate))
                .ToListAsync(cancellationToken);
            return Ok(new EmployeeArchiveSearchResponse(total, page, pageSize, items));
        }

        [HttpGet("audit-events")]
        public async Task<ActionResult<ArchiveAuditSearchResponse>> GetAuditEvents([FromQuery] string query = null, [FromQuery] string action = null,
            [FromQuery] DateTimeOffset? from = null, [FromQuery] DateTimeOffset? to = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 100,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 500);
            var events = dbContext.ArchiveAuditEvents.AsNoTracking().Include(item => item.EmployeeArchive).Include(item => item.ArchiveDocument).AsQueryable();
            if (!string.IsNullOrWhiteSpace(query))
                events = events.Where(item => item.ActorId.Contains(query) || item.ActorDisplayName.Contains(query) || item.CorrelationId.Contains(query)
                    || (item.EmployeeArchive != null && (item.EmployeeArchive.KellyId.Contains(query) || item.EmployeeArchive.EmployeeName.Contains(query)))
                    || (item.ArchiveDocument != null && item.ArchiveDocument.OriginalFileName.Contains(query)));
            if (!string.IsNullOrWhiteSpace(action) && Enum.TryParse<ArchiveAuditAction>(action, true, out var parsedAction)) events = events.Where(item => item.Action == parsedAction);
            if (from.HasValue) events = events.Where(item => item.OccurredAt >= from.Value);
            if (to.HasValue) events = events.Where(item => item.OccurredAt <= to.Value);

            var total = await events.CountAsync(cancellationToken);
            var items = await events.OrderByDescending(item => item.OccurredAt).Skip((page - 1) * pageSize).Take(pageSize)
                .Select(item => new ArchiveAuditEventResponse(item.Id, item.OccurredAt, item.ActorId, item.ActorDisplayName, item.Action.ToString(),
                    item.EmployeeArchive != null ? item.EmployeeArchive.KellyId : null, item.EmployeeArchive != null ? item.EmployeeArchive.EmployeeName : null,
                    item.ArchiveDocument != null ? item.ArchiveDocument.OriginalFileName : null, item.Outcome, item.CorrelationId))
                .ToListAsync(cancellationToken);
            return Ok(new ArchiveAuditSearchResponse(total, page, pageSize, items));
        }
    }

    public record EmployeeArchiveSummary(Guid Id, string EmployeeId, string EmployeeName, int DocumentCount, int W2Count, int PaystubCount,
        int? CoverageFromYear, int? CoverageToYear, string StorageStatus, DateTime LastIndexedAt);
    public record EmployeeArchiveSearchResponse(int Total, int Page, int PageSize, IReadOnlyList<EmployeeArchiveSummary> Items);
    public record ArchiveAuditEventResponse(Guid Id, DateTimeOffset Timestamp, string ActorId, string ActorDisplayName, string Action,
        string EmployeeId, string EmployeeName, string Document, string Outcome, string CorrelationId);
    public record ArchiveAuditSearchResponse(int Total, int Page, int PageSize, IReadOnlyList<ArchiveAuditEventResponse> Items);
}
