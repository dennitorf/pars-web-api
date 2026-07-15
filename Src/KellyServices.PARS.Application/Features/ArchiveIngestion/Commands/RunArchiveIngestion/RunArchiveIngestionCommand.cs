using MediatR;
using System;
namespace KellyServices.PARS.Application.Features.ArchiveIngestion.Commands.RunArchiveIngestion
{
    public record RunArchiveIngestionCommand(string TriggeredBy = "OnDemand") : IRequest<ArchiveIngestionRunResult>;
    public record ArchiveIngestionRunResult(Guid? BatchId, string Status, int Discovered, int Transferred, int Skipped, int Failed, string Message);
}
