using KellyServices.PARS.Application.Common.Interfaces.Archive;
using KellyServices.PARS.Domain.Entities.Archive;
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
    [Route("api/archive-documents")]
    [Produces("application/json")]
    public class ArchiveDocumentsController : ControllerBase
    {
        private readonly AppDbContext dbContext;
        private readonly IArchiveFileStore fileStore;

        public ArchiveDocumentsController(AppDbContext dbContext, IArchiveFileStore fileStore)
        {
            this.dbContext = dbContext;
            this.fileStore = fileStore;
        }

        [HttpGet]
        [ProducesResponseType(typeof(SearchArchiveResponse), 200)]
        public async Task<ActionResult<SearchArchiveResponse>> Search([FromQuery] string employee = null, [FromQuery] string documentType = null,
            [FromQuery] int? fromYear = null, [FromQuery] int? toYear = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
            CancellationToken cancellationToken = default)
        {
            page = Math.Max(1, page);
            pageSize = Math.Clamp(pageSize, 1, 200);
            var query = dbContext.ArchiveDocuments.AsNoTracking().Include(item => item.EmployeeArchive)
                .Where(item => item.Status == ArchiveDocumentStatus.Available);
            if (!string.IsNullOrWhiteSpace(employee))
                query = query.Where(item => item.EmployeeArchive.KellyId.Contains(employee) || item.EmployeeArchive.EmployeeName.Contains(employee));
            if (!string.IsNullOrWhiteSpace(documentType)) query = query.Where(item => item.DocumentType == documentType);
            if (fromYear.HasValue) query = query.Where(item => item.DocumentYear >= fromYear.Value);
            if (toYear.HasValue) query = query.Where(item => item.DocumentYear <= toYear.Value);

            var total = await query.CountAsync(cancellationToken);
            var items = await query.OrderByDescending(item => item.DocumentYear).ThenBy(item => item.DocumentType)
                .Skip((page - 1) * pageSize).Take(pageSize).Select(item => new ArchiveDocumentSummary(item.Id, item.EmployeeArchive.KellyId,
                    item.EmployeeArchive.EmployeeName, item.EmployeeArchive.MaskedTaxId, item.DocumentType, item.DocumentYear, item.DocumentPeriod,
                    item.FileSizeBytes, item.Status.ToString())).ToListAsync(cancellationToken);

            dbContext.ArchiveAuditEvents.Add(NewAudit(ArchiveAuditAction.Searched, "Success", null, null,
                $"employee={employee}; documentType={documentType}; fromYear={fromYear}; toYear={toYear}; total={total}"));
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new SearchArchiveResponse(total, page, pageSize, items));
        }

        [HttpGet("{documentId:guid}/preview")]
        public async Task<ActionResult<DocumentPreviewResponse>> Preview(Guid documentId, CancellationToken cancellationToken)
        {
            var document = await FindAsync(documentId, cancellationToken);
            if (document is null) return NotFound();
            dbContext.ArchiveAuditEvents.Add(NewAudit(ArchiveAuditAction.Viewed, "Success", document.EmployeeArchiveId, document.Id, "Preview opened."));
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(new DocumentPreviewResponse(ToSummary(document), $"/api/archive-documents/{document.Id}/content?disposition=inline", "Preview access is audited."));
        }

        [HttpGet("{documentId:guid}/content")]
        public async Task<IActionResult> Content(Guid documentId, [FromQuery] string disposition = "inline", CancellationToken cancellationToken = default)
        {
            var document = await FindAsync(documentId, cancellationToken);
            if (document is null) return NotFound();
            var stream = await fileStore.OpenReadAsync(document.BlobContainer, document.BlobName, cancellationToken);
            return disposition.Equals("attachment", StringComparison.OrdinalIgnoreCase)
                ? File(stream, document.ContentType, document.OriginalFileName)
                : File(stream, document.ContentType, enableRangeProcessing: true);
        }

        [HttpPost("{documentId:guid}/downloads")]
        public async Task<ActionResult<DocumentDownloadResponse>> CreateDownload(Guid documentId, CancellationToken cancellationToken)
        {
            var document = await FindAsync(documentId, cancellationToken);
            if (document is null) return NotFound();
            var audit = NewAudit(ArchiveAuditAction.Downloaded, "Success", document.EmployeeArchiveId, document.Id, "Secure download created.");
            dbContext.ArchiveAuditEvents.Add(audit);
            await dbContext.SaveChangesAsync(cancellationToken);
            return Created($"/api/archive-documents/{document.Id}/downloads/{audit.Id}", new DocumentDownloadResponse(audit.Id, document.Id, "Ready", $"/api/archive-documents/{document.Id}/content?disposition=attachment"));
        }

        [HttpPost("{documentId:guid}/email-fulfillments")]
        public async Task<ActionResult<EmailFulfillmentResponse>> Email(Guid documentId, [FromBody] EmailFulfillmentRequest request, CancellationToken cancellationToken)
        {
            var document = await FindAsync(documentId, cancellationToken);
            if (document is null) return NotFound();
            if (string.IsNullOrWhiteSpace(request.EmployeeEmail) || string.IsNullOrWhiteSpace(request.BusinessReason)) return BadRequest("EmployeeEmail and BusinessReason are required.");

            var fulfillment = new ArchiveFulfillment
            {
                Id = Guid.NewGuid(), ArchiveDocumentId = document.Id, EmployeeEmail = request.EmployeeEmail,
                RequestedBy = ActorId(), BusinessReason = request.BusinessReason, Status = FulfillmentStatus.PendingReview,
                RequestedAt = DateTimeOffset.UtcNow, CreatedDate = DateTime.UtcNow, CreatedBy = ActorId(), IsActive = true
            };
            dbContext.ArchiveFulfillments.Add(fulfillment);
            dbContext.ArchiveAuditEvents.Add(NewAudit(ArchiveAuditAction.Emailed, "PendingReview", document.EmployeeArchiveId, document.Id, $"Fulfillment {fulfillment.Id} queued."));
            await dbContext.SaveChangesAsync(cancellationToken);
            return Accepted(new EmailFulfillmentResponse(fulfillment.Id, document.Id, fulfillment.EmployeeEmail, fulfillment.Status.ToString(), fulfillment.RequestedAt));
        }

        private Task<ArchiveDocument> FindAsync(Guid documentId, CancellationToken cancellationToken) => dbContext.ArchiveDocuments
            .Include(item => item.EmployeeArchive).SingleOrDefaultAsync(item => item.Id == documentId && item.Status == ArchiveDocumentStatus.Available, cancellationToken);

        private ArchiveAuditEvent NewAudit(ArchiveAuditAction action, string outcome, Guid? employeeId, Guid? documentId, string details) => new()
        {
            Id = Guid.NewGuid(), OccurredAt = DateTimeOffset.UtcNow, ActorId = ActorId(), ActorDisplayName = User.Identity?.Name ?? "PARS user",
            Action = action, Outcome = outcome, EmployeeArchiveId = employeeId, ArchiveDocumentId = documentId, Details = details,
            CorrelationId = HttpContext.TraceIdentifier, CreatedDate = DateTime.UtcNow, CreatedBy = ActorId(), IsActive = true
        };

        private string ActorId() => User.FindFirst("oid")?.Value ?? User.Identity?.Name ?? "anonymous";
        private static ArchiveDocumentSummary ToSummary(ArchiveDocument item) => new(item.Id, item.EmployeeArchive.KellyId, item.EmployeeArchive.EmployeeName,
            item.EmployeeArchive.MaskedTaxId, item.DocumentType, item.DocumentYear, item.DocumentPeriod, item.FileSizeBytes, item.Status.ToString());
    }

    public record ArchiveDocumentSummary(Guid Id, string EmployeeId, string EmployeeName, string MaskedTaxId, string DocumentType, int Year, string Period, long SizeBytes, string Status);
    public record SearchArchiveResponse(int Total, int Page, int PageSize, IReadOnlyList<ArchiveDocumentSummary> Items);
    public record DocumentPreviewResponse(ArchiveDocumentSummary Document, string ContentUrl, string AuditNotice);
    public record DocumentDownloadResponse(Guid RequestId, Guid DocumentId, string Status, string ContentUrl);
    public record EmailFulfillmentRequest(string EmployeeEmail, string BusinessReason);
    public record EmailFulfillmentResponse(Guid FulfillmentId, Guid DocumentId, string EmployeeEmail, string Status, DateTimeOffset QueuedAt);
}
