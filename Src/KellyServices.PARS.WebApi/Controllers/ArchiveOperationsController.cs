using KellyServices.PARS.Application.Features.ArchiveOperations.Queries.GetArchiveAuditEvents;
using KellyServices.PARS.Application.Features.ArchiveOperations.Queries.GetEmployeeArchives;
using KellyServices.PARS.Application.Features.ArchiveOperations.Queries.GetIngestionBatches;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
namespace KellyServices.PARS.WebApi.Controllers
{
    [Route("api/archive-operations")]
    [Produces("application/json")]
    public class ArchiveOperationsController : BaseController
    {
        public ArchiveOperationsController(IMediator mediator) : base(mediator) { }
        [HttpGet("employees")]
        public async Task<IActionResult> Employees([FromQuery] GetEmployeeArchivesQuery query, CancellationToken cancellationToken) => Ok(await Mediator.Send(query, cancellationToken));
        [HttpGet("audit-events")]
        public async Task<IActionResult> AuditEvents([FromQuery] GetArchiveAuditEventsQuery query, CancellationToken cancellationToken) => Ok(await Mediator.Send(query, cancellationToken));
        [HttpGet("ingestion-batches")]
        public async Task<IActionResult> IngestionBatches([FromQuery] GetIngestionBatchesQuery query, CancellationToken cancellationToken) => Ok(await Mediator.Send(query, cancellationToken));
    }
}
