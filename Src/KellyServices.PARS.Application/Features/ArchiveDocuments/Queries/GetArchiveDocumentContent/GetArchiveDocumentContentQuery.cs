using KellyServices.PARS.Application.Features.ArchiveDocuments.Models;
using MediatR;
using System;
namespace KellyServices.PARS.Application.Features.ArchiveDocuments.Queries.GetArchiveDocumentContent
{ public record GetArchiveDocumentContentQuery(Guid DocumentId) : IRequest<ArchiveDocumentContent>; }
