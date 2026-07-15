using KellyServices.PARS.Application.Features.ArchiveOperations.Models;
using KellyServices.PARS.Persistence.Contexts;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace KellyServices.PARS.Application.Features.ArchiveOperations.Queries.GetIngestionBatches
{
    public class GetIngestionBatchesQueryHandler : IRequestHandler<GetIngestionBatchesQuery, IReadOnlyList<IngestionBatchSummary>>
    {
        private readonly AppDbContext db; public GetIngestionBatchesQueryHandler(AppDbContext db) => this.db = db;
        public async Task<IReadOnlyList<IngestionBatchSummary>> Handle(GetIngestionBatchesQuery request, CancellationToken cancellationToken) => await db.ArchiveIngestionBatches.AsNoTracking().OrderByDescending(item => item.StartedAt)
            .Take(Math.Clamp(request.Limit, 1, 500)).Select(item => new IngestionBatchSummary(item.Id, item.MetadataFilePath, item.Status.ToString(), item.StartedAt, item.CompletedAt,
                item.RecordsDiscovered, item.RecordsTransferred, item.RecordsSkipped, item.RecordsFailed, item.LastError)).ToListAsync(cancellationToken);
    }
}
