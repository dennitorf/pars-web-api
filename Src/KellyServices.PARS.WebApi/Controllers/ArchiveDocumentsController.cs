using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KellyServices.PARS.WebApi.Controllers
{
    [ApiController]
    [Route("api/archive-documents")]
    [Produces("application/json")]
    public class ArchiveDocumentsController : ControllerBase
    {
        private static readonly IReadOnlyList<ArchiveDocument> Documents = new[]
        {
            new ArchiveDocument(Guid.Parse("8f0dfc99-034a-4d84-b1bf-25b77d007622"), "K1048291", "Jordan Ellis", "•••-••-4821", "W-2", 2024, "Tax year 2024", 112_640, "abfss://raw/w2/2024/K1048291/8f0dfc99-034a-4d84-b1bf-25b77d007622.pdf"),
            new ArchiveDocument(Guid.Parse("0f3982e2-80b2-45ed-b29e-0090ef885cbc"), "K1048291", "Jordan Ellis", "•••-••-4821", "Paystub", 2024, "Dec 16–31, 2024", 98_304, "abfss://raw/paystub/2024/K1048291/0f3982e2-80b2-45ed-b29e-0090ef885cbc.pdf"),
            new ArchiveDocument(Guid.Parse("be46b681-b2b7-4a7f-a348-0ea721fca710"), "K1048291", "Jordan Ellis", "•••-••-4821", "Paystub", 2023, "Nov 1–15, 2023", 96_256, "abfss://raw/paystub/2023/K1048291/be46b681-b2b7-4a7f-a348-0ea721fca710.pdf"),
            new ArchiveDocument(Guid.Parse("cf045521-30f0-403a-8e84-d51180d98f86"), "K2075520", "Morgan Chen", "•••-••-1976", "W-2", 2022, "Tax year 2022", 120_832, "abfss://raw/w2/2022/K2075520/cf045521-30f0-403a-8e84-d51180d98f86.pdf")
        };

        /// <summary>Searches the curated archive index without opening document files.</summary>
        [HttpGet]
        [ProducesResponseType(typeof(SearchArchiveResponse), 200)]
        public ActionResult<SearchArchiveResponse> Search(
            [FromQuery] string employee = null,
            [FromQuery] string documentType = null,
            [FromQuery] int? fromYear = null,
            [FromQuery] int? toYear = null)
        {
            var query = Documents.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(employee))
            {
                query = query.Where(document =>
                    document.EmployeeId.Contains(employee, StringComparison.OrdinalIgnoreCase) ||
                    document.EmployeeName.Contains(employee, StringComparison.OrdinalIgnoreCase));
            }

            if (!string.IsNullOrWhiteSpace(documentType))
            {
                query = query.Where(document => document.DocumentType.Equals(documentType, StringComparison.OrdinalIgnoreCase));
            }

            if (fromYear.HasValue) query = query.Where(document => document.Year >= fromYear.Value);
            if (toYear.HasValue) query = query.Where(document => document.Year <= toYear.Value);

            var items = query.Select(ToSummary).ToList();
            return Ok(new SearchArchiveResponse(items.Count, items));
        }

        /// <summary>Returns preview metadata and records a document-view audit event in the production implementation.</summary>
        [HttpGet("{documentId}/preview")]
        [ProducesResponseType(typeof(DocumentPreviewResponse), 200)]
        [ProducesResponseType(404)]
        public ActionResult<DocumentPreviewResponse> Preview(Guid documentId)
        {
            var document = Find(documentId);
            if (document is null) return NotFound();

            return Ok(new DocumentPreviewResponse(
                ToSummary(document),
                $"/api/archive-documents/{document.Id}/content?disposition=inline",
                DateTimeOffset.UtcNow.AddMinutes(5),
                "Preview access is audited and the signed content URL expires after five minutes."));
        }

        /// <summary>Creates a short-lived secure download instruction for an authorized caller.</summary>
        [HttpPost("{documentId}/downloads")]
        [ProducesResponseType(typeof(DocumentDownloadResponse), 201)]
        [ProducesResponseType(404)]
        public ActionResult<DocumentDownloadResponse> CreateDownload(Guid documentId)
        {
            var document = Find(documentId);
            if (document is null) return NotFound();

            var requestId = Guid.NewGuid();
            return Created($"/api/archive-documents/{document.Id}/downloads/{requestId}",
                new DocumentDownloadResponse(requestId, document.Id, "Ready", $"/api/archive-documents/{document.Id}/content?disposition=attachment", DateTimeOffset.UtcNow.AddMinutes(5)));
        }

        /// <summary>Queues an audited employee fulfillment email.</summary>
        [HttpPost("{documentId}/email-fulfillments")]
        [ProducesResponseType(typeof(EmailFulfillmentResponse), 202)]
        [ProducesResponseType(404)]
        public ActionResult<EmailFulfillmentResponse> Email(Guid documentId, [FromBody] EmailFulfillmentRequest request)
        {
            var document = Find(documentId);
            if (document is null) return NotFound();

            var fulfillmentId = Guid.NewGuid();
            return Accepted(new EmailFulfillmentResponse(fulfillmentId, document.Id, request.EmployeeEmail, "PendingReview", DateTimeOffset.UtcNow));
        }

        private static ArchiveDocument Find(Guid documentId) =>
            Documents.FirstOrDefault(document => document.Id == documentId);

        private static ArchiveDocumentSummary ToSummary(ArchiveDocument document) =>
            new ArchiveDocumentSummary(document.Id, document.EmployeeId, document.EmployeeName, document.MaskedTaxId, document.DocumentType, document.Year, document.Period, document.SizeBytes, "Available");
    }

    public record ArchiveDocument(Guid Id, string EmployeeId, string EmployeeName, string MaskedTaxId, string DocumentType, int Year, string Period, long SizeBytes, string StoragePath);
    public record ArchiveDocumentSummary(Guid Id, string EmployeeId, string EmployeeName, string MaskedTaxId, string DocumentType, int Year, string Period, long SizeBytes, string Status);
    public record SearchArchiveResponse(int Total, IReadOnlyList<ArchiveDocumentSummary> Items);
    public record DocumentPreviewResponse(ArchiveDocumentSummary Document, string ContentUrl, DateTimeOffset ExpiresAt, string AuditNotice);
    public record DocumentDownloadResponse(Guid RequestId, Guid DocumentId, string Status, string ContentUrl, DateTimeOffset ExpiresAt);
    public record EmailFulfillmentRequest(string EmployeeEmail, string RequestedBy, string BusinessReason);
    public record EmailFulfillmentResponse(Guid FulfillmentId, Guid DocumentId, string EmployeeEmail, string Status, DateTimeOffset QueuedAt);
}
