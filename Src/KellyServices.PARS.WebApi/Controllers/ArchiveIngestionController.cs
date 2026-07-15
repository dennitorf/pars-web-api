using KellyServices.PARS.Application.Features.ArchiveIngestion.Commands.RunArchiveIngestion;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Threading;
using System.Threading.Tasks;
namespace KellyServices.PARS.WebApi.Controllers
{
    [Route("api/archive-ingestion")]
    [Produces("application/json")]
    public class ArchiveIngestionController : BaseController
    {
        public ArchiveIngestionController(IMediator mediator) : base(mediator) { }
        [HttpPost("runs")]
        public async Task<IActionResult> Run(CancellationToken cancellationToken) => Accepted(await Mediator.Send(new RunArchiveIngestionCommand("OnDemand"), cancellationToken));
    }
}
