using KellyServices.PARS.Domain.Entities.Archive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KellyServices.PARS.Persistence.Configurations.Archive
{
    public class ArchiveAuditEventConfiguration : IEntityTypeConfiguration<ArchiveAuditEvent>
    {
        public void Configure(EntityTypeBuilder<ArchiveAuditEvent> builder)
        {
            builder.ToTable("ArchiveAuditEvents");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.ActorId).HasMaxLength(200).IsRequired();
            builder.Property(item => item.ActorDisplayName).HasMaxLength(200).IsRequired();
            builder.Property(item => item.Action).HasConversion<string>().HasMaxLength(32);
            builder.Property(item => item.Outcome).HasMaxLength(64).IsRequired();
            builder.Property(item => item.Details).HasMaxLength(2000);
            builder.Property(item => item.CorrelationId).HasMaxLength(100);
            builder.HasIndex(item => item.OccurredAt);
            builder.HasIndex(item => new { item.ActorId, item.OccurredAt });
            builder.HasIndex(item => new { item.EmployeeArchiveId, item.OccurredAt });
            builder.HasOne(item => item.EmployeeArchive).WithMany().HasForeignKey(item => item.EmployeeArchiveId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(item => item.ArchiveDocument).WithMany().HasForeignKey(item => item.ArchiveDocumentId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
