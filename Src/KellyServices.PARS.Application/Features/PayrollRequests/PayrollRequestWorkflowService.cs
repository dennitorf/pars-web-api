using FluentValidation.Results;
using KellyServices.PARS.Application.Common.Exceptions;
using KellyServices.PARS.Application.Common.Interfaces.Identity;
using KellyServices.PARS.Application.Features.PayrollRequests.Models;
using KellyServices.PARS.Domain.Entities.Archive;
using KellyServices.PARS.Domain.Entities.Requests;
using KellyServices.PARS.Domain.Enums;
using KellyServices.PARS.Persistence.Contexts;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace KellyServices.PARS.Application.Features.PayrollRequests
{
    public class PayrollRequestWorkflowService
    {
        private readonly AppDbContext db; private readonly ICurrentUserService user;
        public PayrollRequestWorkflowService(AppDbContext db, ICurrentUserService user) { this.db = db; this.user = user; }

        public async Task<PayrollRequestDetail> SubmitAsync(string firstName, string lastName, string email, string kellyId, string lastFour, DateTime from, DateTime to, string documentTypes, CancellationToken cancellationToken)
        {
            var request = new PayrollDataRequest
            {
                Id = Guid.NewGuid(),
                RequestNumber = $"PARS-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
                EmployeeFirstName = firstName.Trim(),
                EmployeeLastName = lastName.Trim(),
                EmployeeEmail = email.Trim(),
                KellyId = kellyId?.Trim(),
                TaxIdLastFour = lastFour?.Trim(),
                FromDate = from.Date,
                ToDate = to.Date,
                RequestedDocumentTypes = string.IsNullOrWhiteSpace(documentTypes) ? "W-2,Paystub" : documentTypes,
                SubmittedAt = DateTimeOffset.UtcNow,
                Status = PayrollRequestStatus.Submitted,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "employee-request",
                IsActive = true
            };
            db.PayrollDataRequests.Add(request); await FindCandidatesAsync(request, false, cancellationToken);
            db.ArchiveAuditEvents.Add(Audit(ArchiveAuditAction.RequestSubmitted, "Success", null, null, request.Id, $"Request {request.RequestNumber} submitted; status={request.Status}."));
            await db.SaveChangesAsync(cancellationToken); return await GetAsync(request.Id, cancellationToken);
        }
        public async Task<IReadOnlyList<PayrollRequestSummary>> ListAsync(string status, string search, CancellationToken cancellationToken)
        {
            var query = db.PayrollDataRequests.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PayrollRequestStatus>(status, true, out var parsed)) query = query.Where(item => item.Status == parsed);
            if (!string.IsNullOrWhiteSpace(search)) query = query.Where(item => item.RequestNumber.Contains(search) || item.EmployeeEmail.Contains(search) || item.EmployeeFirstName.Contains(search) || item.EmployeeLastName.Contains(search) || item.KellyId.Contains(search));
            return await query.OrderByDescending(item => item.SubmittedAt).Take(200).Select(item => new PayrollRequestSummary(item.Id, item.RequestNumber, item.EmployeeFirstName + " " + item.EmployeeLastName,
                item.EmployeeEmail, item.FromDate, item.ToDate, item.Status.ToString(), item.AssignedTo, item.SubmittedAt, item.ConfirmedEmployeeArchiveId)).ToListAsync(cancellationToken);
        }
        public async Task<PayrollRequestDetail> GetAsync(Guid id, CancellationToken cancellationToken)
        {
            var request = await db.PayrollDataRequests.AsNoTracking().Include(item => item.Candidates).ThenInclude(item => item.EmployeeArchive).Include(item => item.Documents).ThenInclude(item => item.ArchiveDocument)
                .SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new NotFoundException(nameof(PayrollDataRequest), id);
            return new PayrollRequestDetail(request.Id, request.RequestNumber, request.EmployeeFirstName + " " + request.EmployeeLastName, request.EmployeeEmail, request.KellyId, request.FromDate, request.ToDate,
                request.RequestedDocumentTypes, request.Status.ToString(), request.AssignedTo, request.SubmittedAt, request.SpecialistNotes,
                request.Candidates.OrderByDescending(item => item.ConfidenceScore).Select(item => new RequestCandidateResponse(item.Id, item.EmployeeArchiveId, item.EmployeeArchive.KellyId, item.EmployeeArchive.EmployeeName, item.EmployeeArchive.MaskedTaxId, item.ConfidenceScore, item.MatchedAttributes, item.Status.ToString())).ToList(),
                request.Documents.OrderByDescending(item => item.ArchiveDocument.DocumentYear).Select(item => new RequestDocumentResponse(item.ArchiveDocumentId, item.ArchiveDocument.DocumentType, item.ArchiveDocument.DocumentYear,
                    item.ArchiveDocument.DocumentPeriod, item.ArchiveDocument.FileSizeBytes, item.IsSelected, $"/api/archive-documents/{item.ArchiveDocumentId}/preview")).ToList());
        }
        public async Task<PayrollRequestDetail> SearchAsync(Guid id, CancellationToken cancellationToken)
        {
            var request = await db.PayrollDataRequests.Include(item => item.Candidates).SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new NotFoundException(nameof(PayrollDataRequest), id);
            request.Status = PayrollRequestStatus.DatabaseSearchInProgress; request.SearchInitiatedAt = DateTimeOffset.UtcNow; request.AssignedTo ??= user.UserId;
            await FindCandidatesAsync(request, true, cancellationToken); db.ArchiveAuditEvents.Add(Audit(ArchiveAuditAction.RequestSearched, "Success", null, null, request.Id, $"Deep database search completed; candidates={request.Candidates.Count}."));
            await db.SaveChangesAsync(cancellationToken); return await GetAsync(id, cancellationToken);
        }
        public async Task<PayrollRequestDetail> ConfirmAsync(Guid id, Guid candidateId, string notes, CancellationToken cancellationToken)
        {
            var request = await db.PayrollDataRequests.Include(item => item.Candidates).Include(item => item.Documents).SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new NotFoundException(nameof(PayrollDataRequest), id);
            var selected = request.Candidates.SingleOrDefault(item => item.Id == candidateId) ?? throw new NotFoundException(nameof(PayrollRequestCandidate), candidateId);
            foreach (var candidate in request.Candidates) { candidate.Status = candidate.Id == candidateId ? PayrollRequestCandidateStatus.Confirmed : PayrollRequestCandidateStatus.Rejected; candidate.ReviewedAt = DateTimeOffset.UtcNow; candidate.ReviewedBy = user.UserId; }
            request.ConfirmedEmployeeArchiveId = selected.EmployeeArchiveId; request.Status = PayrollRequestStatus.DocumentReview; request.AssignedTo = user.UserId; request.ReviewedAt = DateTimeOffset.UtcNow; request.SpecialistNotes = notes;
            await LoadDocumentsAsync(request, cancellationToken); db.ArchiveAuditEvents.Add(Audit(ArchiveAuditAction.CandidateReviewed, "Confirmed", selected.EmployeeArchiveId, null, request.Id, $"Candidate {candidateId} confirmed."));
            await db.SaveChangesAsync(cancellationToken); return await GetAsync(id, cancellationToken);
        }
        public async Task<PayrollRequestDetail> SelectDocumentAsync(Guid id, Guid documentId, bool selected, CancellationToken cancellationToken)
        {
            var item = await db.PayrollRequestDocuments.SingleOrDefaultAsync(value => value.PayrollDataRequestId == id && value.ArchiveDocumentId == documentId, cancellationToken) ?? throw new NotFoundException(nameof(PayrollRequestDocument), documentId);
            item.IsSelected = selected; item.ReviewedAt = DateTimeOffset.UtcNow; item.ReviewedBy = user.UserId; item.ModifiedDate = DateTime.UtcNow; item.LastModifiedBy = user.UserId;
            await db.SaveChangesAsync(cancellationToken); return await GetAsync(id, cancellationToken);
        }
        public async Task<PayrollRequestDetail> FulfillAsync(Guid id, string email, string notes, CancellationToken cancellationToken)
        {
            var request = await db.PayrollDataRequests.Include(item => item.Documents).SingleOrDefaultAsync(item => item.Id == id, cancellationToken) ?? throw new NotFoundException(nameof(PayrollDataRequest), id);
            if (!request.ConfirmedEmployeeArchiveId.HasValue) Invalid("Candidate", "A payroll specialist must confirm the employee record first.");
            var documents = request.Documents.Where(item => item.IsSelected).ToList(); if (documents.Count == 0) Invalid("Documents", "At least one reviewed document must be selected.");
            var recipient = string.IsNullOrWhiteSpace(email) ? request.EmployeeEmail : email;
            foreach (var item in documents) db.ArchiveFulfillments.Add(new ArchiveFulfillment
            {
                Id = Guid.NewGuid(),
                ArchiveDocumentId = item.ArchiveDocumentId,
                PayrollDataRequestId = request.Id,
                EmployeeEmail = recipient,
                RequestedBy = user.UserId,
                BusinessReason = $"Payroll request {request.RequestNumber}: {notes}",
                Status = FulfillmentStatus.PendingReview,
                RequestedAt = DateTimeOffset.UtcNow,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = user.UserId,
                IsActive = true
            });
            request.Status = PayrollRequestStatus.FulfillmentQueued; request.SpecialistNotes = notes; request.ModifiedDate = DateTime.UtcNow; request.LastModifiedBy = user.UserId;
            db.ArchiveAuditEvents.Add(Audit(ArchiveAuditAction.RequestFulfilled, "PendingReview", request.ConfirmedEmployeeArchiveId, null, request.Id, $"{documents.Count} documents queued for {recipient}."));
            await db.SaveChangesAsync(cancellationToken); return await GetAsync(id, cancellationToken);
        }
        private async Task FindCandidatesAsync(PayrollDataRequest request, bool deep, CancellationToken cancellationToken)
        {
            var first = request.EmployeeFirstName.Trim(); var last = request.EmployeeLastName.Trim(); var full = $"{first} {last}"; var query = db.EmployeeArchives.AsNoTracking().AsQueryable();
            query = deep ? query.Where(item => (!string.IsNullOrEmpty(request.KellyId) && item.KellyId.Contains(request.KellyId)) || item.EmployeeName.Contains(first) || item.EmployeeName.Contains(last) || (!string.IsNullOrEmpty(request.TaxIdLastFour) && item.MaskedTaxId.EndsWith(request.TaxIdLastFour)))
                : query.Where(item => (!string.IsNullOrEmpty(request.KellyId) && item.KellyId == request.KellyId) || item.EmployeeName == full || (!string.IsNullOrEmpty(request.TaxIdLastFour) && item.MaskedTaxId.EndsWith(request.TaxIdLastFour)));
            foreach (var employee in await query.Take(50).ToListAsync(cancellationToken))
            {
                var score = 0m; var matched = new List<string>(); if (!string.IsNullOrEmpty(request.KellyId) && employee.KellyId.Equals(request.KellyId, StringComparison.OrdinalIgnoreCase)) { score += 60; matched.Add("Kelly ID"); }
                if (employee.EmployeeName.Equals(full, StringComparison.OrdinalIgnoreCase)) { score += 25; matched.Add("employee name"); } else if (deep && employee.EmployeeName.Contains(last, StringComparison.OrdinalIgnoreCase)) { score += 10; matched.Add("last name"); }
                if (!string.IsNullOrEmpty(request.TaxIdLastFour) && employee.MaskedTaxId.EndsWith(request.TaxIdLastFour)) { score += 15; matched.Add("tax ID last four"); }
                if (score == 0 || request.Candidates.Any(item => item.EmployeeArchiveId == employee.Id)) continue;
                var candidate = new PayrollRequestCandidate
                {
                    Id = Guid.NewGuid(),
                    PayrollDataRequestId = request.Id,
                    EmployeeArchiveId = employee.Id,
                    ConfidenceScore = score,
                    MatchedAttributes = string.Join(", ", matched),
                    Status = PayrollRequestCandidateStatus.Suggested,
                    CreatedDate = DateTime.UtcNow,
                    CreatedBy = user.UserId,
                    IsActive = true
                };
                db.PayrollRequestCandidates.Add(candidate);
            }
            request.Status = request.Candidates.Count > 0 ? PayrollRequestStatus.CandidateReview : (deep ? PayrollRequestStatus.UnableToFulfill : PayrollRequestStatus.DatabaseSearchRequired);
        }
        private async Task LoadDocumentsAsync(PayrollDataRequest request, CancellationToken cancellationToken)
        {
            var types = request.RequestedDocumentTypes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var documents = await db.ArchiveDocuments.AsNoTracking().Where(item => item.EmployeeArchiveId == request.ConfirmedEmployeeArchiveId && item.Status == ArchiveDocumentStatus.Available && item.DocumentYear >= request.FromDate.Year && item.DocumentYear <= request.ToDate.Year && types.Contains(item.DocumentType)).ToListAsync(cancellationToken);
            foreach (var document in documents)
            {
                if (request.Documents.Any(item => item.ArchiveDocumentId == document.Id)) continue;
                var requestDocument = new PayrollRequestDocument { Id = Guid.NewGuid(), PayrollDataRequestId = request.Id, ArchiveDocumentId = document.Id, CreatedDate = DateTime.UtcNow, CreatedBy = user.UserId, IsActive = true };
                db.PayrollRequestDocuments.Add(requestDocument);
            }
        }
        private ArchiveAuditEvent Audit(ArchiveAuditAction action, string outcome, Guid? employeeId, Guid? documentId, Guid requestId, string details) => new()
        {
            Id = Guid.NewGuid(),
            OccurredAt = DateTimeOffset.UtcNow,
            ActorId = user.UserId,
            ActorDisplayName = user.DisplayName,
            Action = action,
            Outcome = outcome,
            EmployeeArchiveId = employeeId,
            ArchiveDocumentId = documentId,
            Details = details,
            CorrelationId = requestId.ToString(),
            CreatedDate = DateTime.UtcNow,
            CreatedBy = user.UserId,
            IsActive = true
        };
        private static void Invalid(string field, string message) => throw new ValidationException(new List<ValidationFailure> { new(field, message) });
    }
}
