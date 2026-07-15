using KellyServices.PARS.Application.Common.Exceptions;
using KellyServices.PARS.Application.Common.Interfaces.Archive;
using KellyServices.PARS.Application.Features.ArchiveDocuments.Models;
using KellyServices.PARS.Domain.Entities.Archive;
using KellyServices.PARS.Domain.Enums;
using KellyServices.PARS.Persistence.Contexts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;
namespace KellyServices.PARS.Application.Features.ArchiveDocuments.Queries.GetArchiveDocumentContent
{
    public class GetArchiveDocumentContentQueryHandler : IRequestHandler<GetArchiveDocumentContentQuery, ArchiveDocumentContent>
    {
        private readonly AppDbContext db; private readonly IArchiveFileStore files;
        public GetArchiveDocumentContentQueryHandler(AppDbContext db, IArchiveFileStore files) { this.db = db; this.files = files; }
        public async Task<ArchiveDocumentContent> Handle(GetArchiveDocumentContentQuery request, CancellationToken cancellationToken)
        {
            var document = await db.ArchiveDocuments.AsNoTracking().SingleOrDefaultAsync(item => item.Id == request.DocumentId && item.Status == ArchiveDocumentStatus.Available, cancellationToken)
                ?? throw new NotFoundException(nameof(ArchiveDocument), request.DocumentId);
            return new ArchiveDocumentContent(await files.OpenReadAsync(document.BlobContainer, document.BlobName, cancellationToken), document.ContentType, document.OriginalFileName);
        }
    }
}
