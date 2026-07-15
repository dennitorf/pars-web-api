using KellyServices.PARS.Application.Features.ArchiveOperations.Models;
using MediatR;
using System;
namespace KellyServices.PARS.Application.Features.ArchiveOperations.Queries.GetArchiveAuditEvents
{
    public class GetArchiveAuditEventsQuery : IRequest<ArchiveAuditSearchResponse>
    {
        public string Query { get; set; }
        public string Action { get; set; }
        public DateTimeOffset? From { get; set; }
        public DateTimeOffset? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 100;
    }
}
