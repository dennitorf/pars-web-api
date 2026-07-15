using KellyServices.PARS.Application.Features.ArchiveIngestion;
using KellyServices.PARS.Application.Features.ArchiveIngestion.Commands.RunArchiveIngestion;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Threading;
using System.Threading.Tasks;
namespace KellyServices.PARS.WebApi.BackgroundServices
{
    public class ArchiveIngestionScheduler : BackgroundService
    {
        private readonly IServiceScopeFactory scopeFactory; private readonly ArchiveIngestionOptions options; private readonly ILogger<ArchiveIngestionScheduler> logger;
        public ArchiveIngestionScheduler(IServiceScopeFactory scopeFactory, IOptions<ArchiveIngestionOptions> options, ILogger<ArchiveIngestionScheduler> logger)
        { this.scopeFactory = scopeFactory; this.options = options.Value; this.logger = logger; }
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!options.Enabled) { logger.LogInformation("PARS archive ingestion schedule is disabled; on-demand execution remains available."); return; }
            var schedule = CronSchedule.Parse(options.CronExpression);
            while (!stoppingToken.IsCancellationRequested)
            {
                var next = schedule.GetNextOccurrence(DateTime.UtcNow); var delay = next - DateTime.UtcNow;
                if (delay > TimeSpan.Zero) await Task.Delay(delay, stoppingToken);
                using var scope = scopeFactory.CreateScope(); var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
                try { var result = await mediator.Send(new RunArchiveIngestionCommand("Cron"), stoppingToken); logger.LogInformation("Scheduled ingestion finished with {Status}: {Message}", result.Status, result.Message); }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
                catch (Exception exception) { logger.LogError(exception, "Scheduled PARS ingestion command failed."); }
            }
        }
    }
}
