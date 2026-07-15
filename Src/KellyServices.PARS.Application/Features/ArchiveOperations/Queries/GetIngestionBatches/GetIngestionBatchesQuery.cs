using KellyServices.PARS.Application.Features.ArchiveOperations.Models;
using MediatR;
using System.Collections.Generic;
namespace KellyServices.PARS.Application.Features.ArchiveOperations.Queries.GetIngestionBatches
{
    public class GetIngestionBatchesQuery : IRequest<IReadOnlyList<IngestionBatchSummary>> { public int Limit { get; set; } = 100; }
}
