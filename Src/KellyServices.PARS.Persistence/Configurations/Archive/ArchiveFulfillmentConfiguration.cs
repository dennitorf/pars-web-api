using KellyServices.PARS.Domain.Entities.Archive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KellyServices.PARS.Persistence.Configurations.Archive
{
    public class ArchiveFulfillmentConfiguration : IEntityTypeConfiguration<ArchiveFulfillment>
    {
        public void Configure(EntityTypeBuilder<ArchiveFulfillment> builder)
        {
            builder.ToTable("ArchiveFulfillments");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.EmployeeEmail).HasMaxLength(320).IsRequired();
            builder.Property(item => item.RequestedBy).HasMaxLength(200).IsRequired();
            builder.Property(item => item.BusinessReason).HasMaxLength(1000).IsRequired();
            builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(32);
            builder.Property(item => item.FailureReason).HasMaxLength(2000);
            builder.HasIndex(item => new { item.ArchiveDocumentId, item.RequestedAt });
            builder.HasIndex(item => new { item.Status, item.RequestedAt });
            builder.HasOne(item => item.ArchiveDocument).WithMany().HasForeignKey(item => item.ArchiveDocumentId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(item => item.PayrollDataRequest).WithMany().HasForeignKey(item => item.PayrollDataRequestId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
