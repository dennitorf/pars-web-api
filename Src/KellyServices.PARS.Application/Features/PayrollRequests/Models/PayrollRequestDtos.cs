using System;
using System.Collections.Generic;
namespace KellyServices.PARS.Application.Features.PayrollRequests.Models
{
    public record PayrollRequestSummary(Guid Id, string RequestNumber, string EmployeeName, string EmployeeEmail, DateTime FromDate, DateTime ToDate, string Status, string AssignedTo, DateTimeOffset SubmittedAt, Guid? ConfirmedEmployeeArchiveId);
    public record RequestCandidateResponse(Guid Id, Guid EmployeeArchiveId, string KellyId, string EmployeeName, string MaskedTaxId, decimal ConfidenceScore, string MatchedAttributes, string Status);
    public record RequestDocumentResponse(Guid DocumentId, string DocumentType, int Year, string Period, long SizeBytes, bool IsSelected, string PreviewUrl);
    public record PayrollRequestDetail(Guid Id, string RequestNumber, string EmployeeName, string EmployeeEmail, string KellyId, DateTime FromDate, DateTime ToDate, string DocumentTypes, string Status,
        string AssignedTo, DateTimeOffset SubmittedAt, string SpecialistNotes, IReadOnlyList<RequestCandidateResponse> Candidates, IReadOnlyList<RequestDocumentResponse> Documents);
}
