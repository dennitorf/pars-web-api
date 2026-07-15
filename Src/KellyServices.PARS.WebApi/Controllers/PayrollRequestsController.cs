using KellyServices.PARS.Domain.Entities.Archive;
using KellyServices.PARS.Domain.Entities.Requests;
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
    [Route("api/payroll-requests")]
    [Produces("application/json")]
    public class PayrollRequestsController : ControllerBase
    {
        private readonly AppDbContext dbContext;
        public PayrollRequestsController(AppDbContext dbContext) => this.dbContext = dbContext;

        [HttpPost]
        public async Task<ActionResult<PayrollRequestDetail>> Submit([FromBody] SubmitPayrollRequest command, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(command.FirstName) || string.IsNullOrWhiteSpace(command.LastName) || string.IsNullOrWhiteSpace(command.Email))
                return BadRequest("FirstName, LastName, and Email are required.");
            if (command.FromDate.Date > command.ToDate.Date) return BadRequest("FromDate must be on or before ToDate.");
            if (string.IsNullOrWhiteSpace(command.KellyId) && string.IsNullOrWhiteSpace(command.TaxIdLastFour))
                return BadRequest("KellyId or TaxIdLastFour is required to validate the employee identity.");
            if (!string.IsNullOrWhiteSpace(command.TaxIdLastFour) && (command.TaxIdLastFour.Length != 4 || !command.TaxIdLastFour.All(char.IsDigit)))
                return BadRequest("TaxIdLastFour must contain four digits.");

            var request = new PayrollDataRequest
            {
                Id = Guid.NewGuid(), RequestNumber = $"PARS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
                EmployeeFirstName = command.FirstName.Trim(), EmployeeLastName = command.LastName.Trim(), EmployeeEmail = command.Email.Trim(),
                KellyId = command.KellyId?.Trim(), TaxIdLastFour = command.TaxIdLastFour?.Trim(), FromDate = command.FromDate.Date, ToDate = command.ToDate.Date,
                RequestedDocumentTypes = string.IsNullOrWhiteSpace(command.DocumentTypes) ? "W-2,Paystub" : command.DocumentTypes,
                SubmittedAt = DateTimeOffset.UtcNow, AssignedTo = null, Status = PayrollRequestStatus.Submitted,
                CreatedDate = DateTime.UtcNow, CreatedBy = "employee-request", IsActive = true
            };
            dbContext.PayrollDataRequests.Add(request);
            await FindCandidatesAsync(request, false, cancellationToken);
            dbContext.ArchiveAuditEvents.Add(NewAudit(ArchiveAuditAction.RequestSubmitted, "Success", null, null, request.Id, $"Request {request.RequestNumber} submitted; status={request.Status}."));
            await dbContext.SaveChangesAsync(cancellationToken);
            return CreatedAtAction(nameof(Get), new { requestId = request.Id }, await BuildDetailAsync(request.Id, cancellationToken));
        }

        [HttpGet]
        public async Task<ActionResult<IReadOnlyList<PayrollRequestSummary>>> List([FromQuery] string status = null, [FromQuery] string search = null, CancellationToken cancellationToken = default)
        {
            var query = dbContext.PayrollDataRequests.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PayrollRequestStatus>(status, true, out var parsed)) query = query.Where(item => item.Status == parsed);
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(item => item.RequestNumber.Contains(search) || item.EmployeeEmail.Contains(search) || item.EmployeeFirstName.Contains(search) || item.EmployeeLastName.Contains(search) || item.KellyId.Contains(search));
            var items = await query.OrderByDescending(item => item.SubmittedAt).Take(200).Select(item => new PayrollRequestSummary(item.Id, item.RequestNumber,
                item.EmployeeFirstName + " " + item.EmployeeLastName, item.EmployeeEmail, item.FromDate, item.ToDate, item.Status.ToString(), item.AssignedTo,
                item.SubmittedAt, item.ConfirmedEmployeeArchiveId)).ToListAsync(cancellationToken);
            return Ok(items);
        }

        [HttpGet("{requestId:guid}")]
        public async Task<ActionResult<PayrollRequestDetail>> Get(Guid requestId, CancellationToken cancellationToken)
        {
            var detail = await BuildDetailAsync(requestId, cancellationToken);
            return detail is null ? NotFound() : Ok(detail);
        }

        [HttpPost("{requestId:guid}/database-search")]
        public async Task<ActionResult<PayrollRequestDetail>> SearchDatabase(Guid requestId, CancellationToken cancellationToken)
        {
            var request = await dbContext.PayrollDataRequests.Include(item => item.Candidates).Include(item => item.Documents).SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);
            if (request is null) return NotFound();
            request.Status = PayrollRequestStatus.DatabaseSearchInProgress; request.SearchInitiatedAt = DateTimeOffset.UtcNow; request.AssignedTo ??= ActorId();
            await FindCandidatesAsync(request, true, cancellationToken);
            dbContext.ArchiveAuditEvents.Add(NewAudit(ArchiveAuditAction.RequestSearched, "Success", null, null, request.Id, $"Deep database search completed; candidates={request.Candidates.Count}."));
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(await BuildDetailAsync(requestId, cancellationToken));
        }

        [HttpPost("{requestId:guid}/candidates/{candidateId:guid}/confirm")]
        public async Task<ActionResult<PayrollRequestDetail>> ConfirmCandidate(Guid requestId, Guid candidateId, [FromBody] ReviewCandidateCommand command, CancellationToken cancellationToken)
        {
            var request = await dbContext.PayrollDataRequests.Include(item => item.Candidates).Include(item => item.Documents).SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);
            if (request is null) return NotFound();
            var selected = request.Candidates.SingleOrDefault(item => item.Id == candidateId);
            if (selected is null) return NotFound("Candidate does not belong to this request.");
            foreach (var candidate in request.Candidates) { candidate.Status = candidate.Id == candidateId ? PayrollRequestCandidateStatus.Confirmed : PayrollRequestCandidateStatus.Rejected; candidate.ReviewedAt = DateTimeOffset.UtcNow; candidate.ReviewedBy = ActorId(); }
            request.ConfirmedEmployeeArchiveId = selected.EmployeeArchiveId; request.Status = PayrollRequestStatus.DocumentReview; request.AssignedTo = ActorId(); request.ReviewedAt = DateTimeOffset.UtcNow; request.SpecialistNotes = command?.Notes;
            await LoadRequestDocumentsAsync(request, cancellationToken);
            dbContext.ArchiveAuditEvents.Add(NewAudit(ArchiveAuditAction.CandidateReviewed, "Confirmed", selected.EmployeeArchiveId, null, request.Id, $"Candidate {candidateId} confirmed by {ActorId()}."));
            await dbContext.SaveChangesAsync(cancellationToken);
            return Ok(await BuildDetailAsync(requestId, cancellationToken));
        }

        [HttpPost("{requestId:guid}/documents/{documentId:guid}/selection")]
        public async Task<ActionResult<PayrollRequestDetail>> SelectDocument(Guid requestId, Guid documentId, [FromBody] SelectRequestDocumentCommand command, CancellationToken cancellationToken)
        {
            var item = await dbContext.PayrollRequestDocuments.SingleOrDefaultAsync(value => value.PayrollDataRequestId == requestId && value.ArchiveDocumentId == documentId, cancellationToken);
            if (item is null) return NotFound();
            item.IsSelected = command.IsSelected; item.ReviewedAt = DateTimeOffset.UtcNow; item.ReviewedBy = ActorId(); item.ModifiedDate = DateTime.UtcNow; item.LastModifiedBy = ActorId();
            await dbContext.SaveChangesAsync(cancellationToken); return Ok(await BuildDetailAsync(requestId, cancellationToken));
        }

        [HttpPost("{requestId:guid}/fulfill")]
        public async Task<ActionResult<PayrollRequestDetail>> Fulfill(Guid requestId, [FromBody] FulfillPayrollRequestCommand command, CancellationToken cancellationToken)
        {
            var request = await dbContext.PayrollDataRequests.Include(item => item.Documents).ThenInclude(item => item.ArchiveDocument).SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);
            if (request is null) return NotFound();
            if (!request.ConfirmedEmployeeArchiveId.HasValue) return Conflict("A payroll specialist must confirm the employee record first.");
            var documents = request.Documents.Where(item => item.IsSelected).ToList();
            if (documents.Count == 0) return Conflict("At least one reviewed document must be selected.");
            var recipient = string.IsNullOrWhiteSpace(command?.EmployeeEmail) ? request.EmployeeEmail : command.EmployeeEmail;
            foreach (var item in documents) dbContext.ArchiveFulfillments.Add(new ArchiveFulfillment { Id = Guid.NewGuid(), ArchiveDocumentId = item.ArchiveDocumentId, PayrollDataRequestId = request.Id,
                EmployeeEmail = recipient, RequestedBy = ActorId(), BusinessReason = $"Payroll request {request.RequestNumber}: {command?.SpecialistNotes}",
                Status = FulfillmentStatus.PendingReview, RequestedAt = DateTimeOffset.UtcNow, CreatedDate = DateTime.UtcNow, CreatedBy = ActorId(), IsActive = true });
            request.Status = PayrollRequestStatus.FulfillmentQueued; request.SpecialistNotes = command?.SpecialistNotes; request.ModifiedDate = DateTime.UtcNow; request.LastModifiedBy = ActorId();
            dbContext.ArchiveAuditEvents.Add(NewAudit(ArchiveAuditAction.RequestFulfilled, "PendingReview", request.ConfirmedEmployeeArchiveId, null, request.Id, $"{documents.Count} documents queued for {recipient}."));
            await dbContext.SaveChangesAsync(cancellationToken); return Accepted(await BuildDetailAsync(requestId, cancellationToken));
        }

        private async Task FindCandidatesAsync(PayrollDataRequest request, bool deepSearch, CancellationToken cancellationToken)
        {
            var first = request.EmployeeFirstName.Trim(); var last = request.EmployeeLastName.Trim(); var fullName = $"{first} {last}";
            var query = dbContext.EmployeeArchives.AsNoTracking().AsQueryable();
            if (deepSearch) query = query.Where(item => (!string.IsNullOrEmpty(request.KellyId) && item.KellyId.Contains(request.KellyId)) || item.EmployeeName.Contains(first) || item.EmployeeName.Contains(last) || (!string.IsNullOrEmpty(request.TaxIdLastFour) && item.MaskedTaxId.EndsWith(request.TaxIdLastFour)));
            else query = query.Where(item => (!string.IsNullOrEmpty(request.KellyId) && item.KellyId == request.KellyId) || item.EmployeeName == fullName || (!string.IsNullOrEmpty(request.TaxIdLastFour) && item.MaskedTaxId.EndsWith(request.TaxIdLastFour)));
            var employees = await query.Take(50).ToListAsync(cancellationToken);
            foreach (var employee in employees)
            {
                var score = 0m; var matched = new List<string>();
                if (!string.IsNullOrEmpty(request.KellyId) && employee.KellyId.Equals(request.KellyId, StringComparison.OrdinalIgnoreCase)) { score += 60; matched.Add("Kelly ID"); }
                if (employee.EmployeeName.Equals(fullName, StringComparison.OrdinalIgnoreCase)) { score += 25; matched.Add("employee name"); }
                else if (deepSearch && employee.EmployeeName.Contains(last, StringComparison.OrdinalIgnoreCase)) { score += 10; matched.Add("last name"); }
                if (!string.IsNullOrEmpty(request.TaxIdLastFour) && employee.MaskedTaxId.EndsWith(request.TaxIdLastFour)) { score += 15; matched.Add("tax ID last four"); }
                if (score == 0 || request.Candidates.Any(item => item.EmployeeArchiveId == employee.Id)) continue;
                request.Candidates.Add(new PayrollRequestCandidate { Id = Guid.NewGuid(), EmployeeArchiveId = employee.Id, ConfidenceScore = score,
                    MatchedAttributes = string.Join(", ", matched), Status = PayrollRequestCandidateStatus.Suggested, CreatedDate = DateTime.UtcNow, CreatedBy = ActorId(), IsActive = true });
            }
            request.Status = request.Candidates.Count > 0 ? PayrollRequestStatus.CandidateReview : (deepSearch ? PayrollRequestStatus.UnableToFulfill : PayrollRequestStatus.DatabaseSearchRequired);
        }

        private async Task LoadRequestDocumentsAsync(PayrollDataRequest request, CancellationToken cancellationToken)
        {
            var types = request.RequestedDocumentTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var documents = await dbContext.ArchiveDocuments.AsNoTracking().Where(item => item.EmployeeArchiveId == request.ConfirmedEmployeeArchiveId && item.Status == ArchiveDocumentStatus.Available && item.DocumentYear >= request.FromDate.Year && item.DocumentYear <= request.ToDate.Year && types.Contains(item.DocumentType)).ToListAsync(cancellationToken);
            foreach (var document in documents) if (!request.Documents.Any(item => item.ArchiveDocumentId == document.Id)) request.Documents.Add(new PayrollRequestDocument { Id = Guid.NewGuid(), ArchiveDocumentId = document.Id, IsSelected = false, CreatedDate = DateTime.UtcNow, CreatedBy = ActorId(), IsActive = true });
        }

        private async Task<PayrollRequestDetail> BuildDetailAsync(Guid requestId, CancellationToken cancellationToken)
        {
            var request = await dbContext.PayrollDataRequests.AsNoTracking().Include(item => item.Candidates).ThenInclude(item => item.EmployeeArchive).Include(item => item.Documents).ThenInclude(item => item.ArchiveDocument).SingleOrDefaultAsync(item => item.Id == requestId, cancellationToken);
            if (request is null) return null;
            return new PayrollRequestDetail(request.Id, request.RequestNumber, request.EmployeeFirstName + " " + request.EmployeeLastName, request.EmployeeEmail, request.KellyId,
                request.FromDate, request.ToDate, request.RequestedDocumentTypes, request.Status.ToString(), request.AssignedTo, request.SubmittedAt, request.SpecialistNotes,
                request.Candidates.OrderByDescending(item => item.ConfidenceScore).Select(item => new RequestCandidateResponse(item.Id, item.EmployeeArchiveId, item.EmployeeArchive.KellyId, item.EmployeeArchive.EmployeeName, item.EmployeeArchive.MaskedTaxId, item.ConfidenceScore, item.MatchedAttributes, item.Status.ToString())).ToList(),
                request.Documents.OrderByDescending(item => item.ArchiveDocument.DocumentYear).Select(item => new RequestDocumentResponse(item.ArchiveDocumentId, item.ArchiveDocument.DocumentType, item.ArchiveDocument.DocumentYear, item.ArchiveDocument.DocumentPeriod, item.ArchiveDocument.FileSizeBytes, item.IsSelected, $"/api/archive-documents/{item.ArchiveDocumentId}/preview")).ToList());
        }

        private ArchiveAuditEvent NewAudit(ArchiveAuditAction action, string outcome, Guid? employeeId, Guid? documentId, Guid requestId, string details) => new() { Id = Guid.NewGuid(), OccurredAt = DateTimeOffset.UtcNow, ActorId = ActorId(), ActorDisplayName = User.Identity?.Name ?? "PARS user", Action = action, Outcome = outcome, EmployeeArchiveId = employeeId, ArchiveDocumentId = documentId, Details = details, CorrelationId = requestId.ToString(), CreatedDate = DateTime.UtcNow, CreatedBy = ActorId(), IsActive = true };
        private string ActorId() => User.FindFirst("oid")?.Value ?? User.Identity?.Name ?? "anonymous";
    }

    public record SubmitPayrollRequest(string FirstName, string LastName, string Email, string KellyId, string TaxIdLastFour, DateTime FromDate, DateTime ToDate, string DocumentTypes);
    public record ReviewCandidateCommand(string Notes);
    public record SelectRequestDocumentCommand(bool IsSelected);
    public record FulfillPayrollRequestCommand(string EmployeeEmail, string SpecialistNotes);
    public record PayrollRequestSummary(Guid Id, string RequestNumber, string EmployeeName, string EmployeeEmail, DateTime FromDate, DateTime ToDate, string Status, string AssignedTo, DateTimeOffset SubmittedAt, Guid? ConfirmedEmployeeArchiveId);
    public record RequestCandidateResponse(Guid Id, Guid EmployeeArchiveId, string KellyId, string EmployeeName, string MaskedTaxId, decimal ConfidenceScore, string MatchedAttributes, string Status);
    public record RequestDocumentResponse(Guid DocumentId, string DocumentType, int Year, string Period, long SizeBytes, bool IsSelected, string PreviewUrl);
    public record PayrollRequestDetail(Guid Id, string RequestNumber, string EmployeeName, string EmployeeEmail, string KellyId, DateTime FromDate, DateTime ToDate, string DocumentTypes, string Status, string AssignedTo, DateTimeOffset SubmittedAt, string SpecialistNotes, IReadOnlyList<RequestCandidateResponse> Candidates, IReadOnlyList<RequestDocumentResponse> Documents);
}
