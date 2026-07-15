using KellyServices.PARS.Application.Features.ArchiveDocuments.Models;
using MediatR;
using System;
namespace KellyServices.PARS.Application.Features.ArchiveDocuments.Commands.PreviewArchiveDocument
{ public record PreviewArchiveDocumentCommand(Guid DocumentId) : IRequest<DocumentPreviewResponse>; }
