using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace KellyServices.PARS.WebApi.Controllers
{
    [ApiController]
    [Route("api/archive-operations")]
    [Produces("application/json")]
    public class ArchiveOperationsController : ControllerBase
    {
        private static readonly IReadOnlyList<EmployeeArchiveSummary> Employees = new[]
        {
            new EmployeeArchiveSummary("K1048291", "Jordan Ellis", 184, 7, 177, 2019, 2025, "Complete", DateTimeOffset.Parse("2026-07-15T10:42:00-04:00")),
            new EmployeeArchiveSummary("K2075520", "Morgan Chen", 156, 7, 149, 2019, 2025, "Complete", DateTimeOffset.Parse("2026-07-15T10:39:00-04:00")),
            new EmployeeArchiveSummary("K3301844", "Taylor Brooks", 121, 6, 115, 2020, 2025, "Review", DateTimeOffset.Parse("2026-07-15T09:58:00-04:00")),
            new EmployeeArchiveSummary("K4481920", "Cameron Diaz", 87, 4, 83, 2022, 2025, "Processing", DateTimeOffset.Parse("2026-07-15T09:41:00-04:00"))
        };

        private static readonly IReadOnlyList<ArchiveAuditEvent> Events = new[]
        {
            new ArchiveAuditEvent(Guid.Parse("a2b18fd7-6d5c-4c55-8f5c-51ef9d1129ce"), DateTimeOffset.Parse("2026-07-15T11:14:08-04:00"), "Priya Shah", "Viewed", "K1048291", "Jordan Ellis", "W-2 · 2024", "Success"),
            new ArchiveAuditEvent(Guid.Parse("7c01e6a3-e970-49cc-bec9-a44b09dc5435"), DateTimeOffset.Parse("2026-07-15T11:11:32-04:00"), "Priya Shah", "Searched", "K1048291", "Jordan Ellis", "All documents", "Success"),
            new ArchiveAuditEvent(Guid.Parse("b9064d20-46d3-472c-8578-4f72cf1982d3"), DateTimeOffset.Parse("2026-07-15T10:53:19-04:00"), "Marcus Reed", "Downloaded", "K2075520", "Morgan Chen", "W-2 · 2022", "Success"),
            new ArchiveAuditEvent(Guid.Parse("6f7a019c-43b5-4b29-bae9-d5a5d6b319f4"), DateTimeOffset.Parse("2026-07-15T10:48:44-04:00"), "Elena Ruiz", "Emailed", "K3301844", "Taylor Brooks", "Paystub · Dec 2024", "PendingReview")
        };

        /// <summary>Queries per-employee archive counts and storage completeness.</summary>
        [HttpGet("employees")]
        [ProducesResponseType(typeof(EmployeeArchiveSearchResponse), 200)]
        public ActionResult<EmployeeArchiveSearchResponse> GetEmployees([FromQuery] string query = null, [FromQuery] string status = null)
        {
            var results = Employees.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                results = results.Where(employee => employee.EmployeeId.Contains(query, StringComparison.OrdinalIgnoreCase) || employee.EmployeeName.Contains(query, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(status))
            {
                results = results.Where(employee => employee.StorageStatus.Equals(status, StringComparison.OrdinalIgnoreCase));
            }

            var items = results.ToList();
            return Ok(new EmployeeArchiveSearchResponse(items.Count, items));
        }

        /// <summary>Queries immutable archive audit activity by user, employee, document, event ID, or action.</summary>
        [HttpGet("audit-events")]
        [ProducesResponseType(typeof(ArchiveAuditSearchResponse), 200)]
        public ActionResult<ArchiveAuditSearchResponse> GetAuditEvents([FromQuery] string query = null, [FromQuery] string action = null, [FromQuery] DateTimeOffset? from = null, [FromQuery] DateTimeOffset? to = null)
        {
            var results = Events.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(query))
            {
                results = results.Where(item => $"{item.Id} {item.Actor} {item.EmployeeId} {item.EmployeeName} {item.Document}".Contains(query, StringComparison.OrdinalIgnoreCase));
            }
            if (!string.IsNullOrWhiteSpace(action)) results = results.Where(item => item.Action.Equals(action, StringComparison.OrdinalIgnoreCase));
            if (from.HasValue) results = results.Where(item => item.Timestamp >= from.Value);
            if (to.HasValue) results = results.Where(item => item.Timestamp <= to.Value);

            var items = results.OrderByDescending(item => item.Timestamp).ToList();
            return Ok(new ArchiveAuditSearchResponse(items.Count, items));
        }
    }

    public record EmployeeArchiveSummary(string EmployeeId, string EmployeeName, int DocumentCount, int W2Count, int PaystubCount, int CoverageFromYear, int CoverageToYear, string StorageStatus, DateTimeOffset LastIndexedAt);
    public record EmployeeArchiveSearchResponse(int Total, IReadOnlyList<EmployeeArchiveSummary> Items);
    public record ArchiveAuditEvent(Guid Id, DateTimeOffset Timestamp, string Actor, string Action, string EmployeeId, string EmployeeName, string Document, string Outcome);
    public record ArchiveAuditSearchResponse(int Total, IReadOnlyList<ArchiveAuditEvent> Items);
}
