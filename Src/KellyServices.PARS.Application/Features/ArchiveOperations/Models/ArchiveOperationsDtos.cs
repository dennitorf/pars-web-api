using System;
using System.Collections.Generic;

namespace KellyServices.PARS.Application.Features.ArchiveOperations.Models
{
    public record EmployeeArchiveSummary(Guid Id, string EmployeeId, string EmployeeName, int DocumentCount, int W2Count, int PaystubCount,
        int? CoverageFromYear, int? CoverageToYear, string StorageStatus, DateTime LastIndexedAt);
    public record EmployeeArchiveSearchResponse(int Total, int Page, int PageSize, IReadOnlyList<EmployeeArchiveSummary> Items);
    public record ArchiveAuditEventResponse(Guid Id, DateTimeOffset Timestamp, string ActorId, string ActorDisplayName, string Action,
        string EmployeeId, string EmployeeName, string Document, string Outcome, string CorrelationId);
    public record ArchiveAuditSearchResponse(int Total, int Page, int PageSize, IReadOnlyList<ArchiveAuditEventResponse> Items);
    public record IngestionBatchSummary(Guid Id, string MetadataFilePath, string Status, DateTimeOffset StartedAt, DateTimeOffset? CompletedAt,
        int Discovered, int Transferred, int Skipped, int Failed, string LastError);
}
