using System;
using System.Collections.Generic;
using System.IO;
namespace KellyServices.PARS.Application.Features.ArchiveDocuments.Models
{
    public record ArchiveDocumentSummary(Guid Id, string EmployeeId, string EmployeeName, string MaskedTaxId, string DocumentType, int Year, string Period, long SizeBytes, string Status);
    public record SearchArchiveResponse(int Total, int Page, int PageSize, IReadOnlyList<ArchiveDocumentSummary> Items);
    public record DocumentPreviewResponse(ArchiveDocumentSummary Document, string ContentUrl, string AuditNotice);
    public record DocumentDownloadResponse(Guid RequestId, Guid DocumentId, string Status, string ContentUrl);
    public record EmailFulfillmentResponse(Guid FulfillmentId, Guid DocumentId, string EmployeeEmail, string Status, DateTimeOffset QueuedAt);
    public record ArchiveDocumentContent(Stream Stream, string ContentType, string FileName);
}
