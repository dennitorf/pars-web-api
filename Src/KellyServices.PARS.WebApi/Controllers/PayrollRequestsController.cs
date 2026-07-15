using KellyServices.PARS.Application.Features.PayrollRequests.Commands.ConfirmPayrollRequestCandidate;
using KellyServices.PARS.Application.Features.PayrollRequests.Commands.FulfillPayrollRequest;
using KellyServices.PARS.Application.Features.PayrollRequests.Commands.SearchPayrollRequestDatabase;
using KellyServices.PARS.Application.Features.PayrollRequests.Commands.SelectPayrollRequestDocument;
using KellyServices.PARS.Application.Features.PayrollRequests.Commands.SubmitPayrollRequest;
using KellyServices.PARS.Application.Features.PayrollRequests.Queries.GetPayrollRequest;
using KellyServices.PARS.Application.Features.PayrollRequests.Queries.GetPayrollRequests;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace KellyServices.PARS.WebApi.Controllers
{
    [Route("api/payroll-requests")]
    [Produces("application/json")]
    public class PayrollRequestsController : BaseController
    {
        public PayrollRequestsController(IMediator mediator) : base(mediator) { }
        [HttpPost]
        public async Task<IActionResult> Submit([FromBody] SubmitPayrollRequestCommand command, CancellationToken cancellationToken)
        { var result = await Mediator.Send(command, cancellationToken); return CreatedAtAction(nameof(Get), new { requestId = result.Id }, result); }
        [HttpGet]
        public async Task<IActionResult> List([FromQuery] GetPayrollRequestsQuery query, CancellationToken cancellationToken) => Ok(await Mediator.Send(query, cancellationToken));
        [HttpGet("{requestId:guid}")]
        public async Task<IActionResult> Get(Guid requestId, CancellationToken cancellationToken) => Ok(await Mediator.Send(new GetPayrollRequestQuery(requestId), cancellationToken));
        [HttpPost("{requestId:guid}/database-search")]
        public async Task<IActionResult> SearchDatabase(Guid requestId, CancellationToken cancellationToken) => Ok(await Mediator.Send(new SearchPayrollRequestDatabaseCommand(requestId), cancellationToken));
        [HttpPost("{requestId:guid}/candidates/{candidateId:guid}/confirm")]
        public async Task<IActionResult> Confirm(Guid requestId, Guid candidateId, [FromBody] ConfirmPayrollRequestCandidateCommand command, CancellationToken cancellationToken)
        { command.RequestId = requestId; command.CandidateId = candidateId; return Ok(await Mediator.Send(command, cancellationToken)); }
        [HttpPost("{requestId:guid}/documents/{documentId:guid}/selection")]
        public async Task<IActionResult> Select(Guid requestId, Guid documentId, [FromBody] SelectPayrollRequestDocumentCommand command, CancellationToken cancellationToken)
        { command.RequestId = requestId; command.DocumentId = documentId; return Ok(await Mediator.Send(command, cancellationToken)); }
        [HttpPost("{requestId:guid}/fulfill")]
        public async Task<IActionResult> Fulfill(Guid requestId, [FromBody] FulfillPayrollRequestCommand command, CancellationToken cancellationToken)
        { command.RequestId = requestId; return Accepted(await Mediator.Send(command, cancellationToken)); }
    }
}
