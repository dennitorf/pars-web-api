using KellyServices.PARS.Application.Features.ArchiveDocuments.Models;
using MediatR;
using System;
namespace KellyServices.PARS.Application.Features.ArchiveDocuments.Commands.CreateArchiveDownload
{ public record CreateArchiveDownloadCommand(Guid DocumentId) : IRequest<DocumentDownloadResponse>; }
