using KellyServices.PARS.Application.Features.ArchiveDocuments.Commands.CreateArchiveDownload;
using KellyServices.PARS.Application.Features.ArchiveDocuments.Commands.CreateEmailFulfillment;
using KellyServices.PARS.Application.Features.ArchiveDocuments.Commands.PreviewArchiveDocument;
using KellyServices.PARS.Application.Features.ArchiveDocuments.Queries.GetArchiveDocumentContent;
using KellyServices.PARS.Application.Features.ArchiveDocuments.Queries.SearchArchiveDocuments;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace KellyServices.PARS.WebApi.Controllers
{
    [Route("api/archive-documents")]
    [Produces("application/json")]
    public class ArchiveDocumentsController : BaseController
    {
        public ArchiveDocumentsController(IMediator mediator) : base(mediator) { }
        [HttpGet]
        public async Task<IActionResult> Search([FromQuery] SearchArchiveDocumentsQuery query, CancellationToken cancellationToken) => Ok(await Mediator.Send(query, cancellationToken));
        [HttpGet("{documentId:guid}/preview")]
        public async Task<IActionResult> Preview(Guid documentId, CancellationToken cancellationToken) => Ok(await Mediator.Send(new PreviewArchiveDocumentCommand(documentId), cancellationToken));
        [HttpGet("{documentId:guid}/content")]
        public async Task<IActionResult> Content(Guid documentId, [FromQuery] string disposition = "inline", CancellationToken cancellationToken = default)
        {
            var content = await Mediator.Send(new GetArchiveDocumentContentQuery(documentId), cancellationToken);
            return disposition.Equals("attachment", StringComparison.OrdinalIgnoreCase) ? File(content.Stream, content.ContentType, content.FileName) : File(content.Stream, content.ContentType, enableRangeProcessing: true);
        }
        [HttpPost("{documentId:guid}/downloads")]
        public async Task<IActionResult> Download(Guid documentId, CancellationToken cancellationToken)
        {
            var result = await Mediator.Send(new CreateArchiveDownloadCommand(documentId), cancellationToken);
            return Created($"/api/archive-documents/{documentId}/downloads/{result.RequestId}", result);
        }
        [HttpPost("{documentId:guid}/email-fulfillments")]
        public async Task<IActionResult> Email(Guid documentId, [FromBody] CreateEmailFulfillmentCommand command, CancellationToken cancellationToken)
        { command.DocumentId = documentId; return Accepted(await Mediator.Send(command, cancellationToken)); }
    }
}
