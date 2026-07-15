using KellyServices.PARS.Domain.Entities.Archive;
using KellyServices.PARS.Domain.Entities.Requests;
using Microsoft.EntityFrameworkCore;

namespace KellyServices.PARS.Persistence.Contexts
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
        public DbSet<EmployeeArchive> EmployeeArchives { set; get; }
        public DbSet<ArchiveDocument> ArchiveDocuments { set; get; }
        public DbSet<ArchiveIngestionBatch> ArchiveIngestionBatches { set; get; }
        public DbSet<ArchiveAuditEvent> ArchiveAuditEvents { set; get; }
        public DbSet<ArchiveFulfillment> ArchiveFulfillments { set; get; }
        public DbSet<PayrollDataRequest> PayrollDataRequests { set; get; }
        public DbSet<PayrollRequestCandidate> PayrollRequestCandidates { set; get; }
        public DbSet<PayrollRequestDocument> PayrollRequestDocuments { set; get; }
    }
}
