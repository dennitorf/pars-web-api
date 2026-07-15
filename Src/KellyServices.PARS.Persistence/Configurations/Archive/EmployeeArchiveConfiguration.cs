using KellyServices.PARS.Domain.Entities.Archive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KellyServices.PARS.Persistence.Configurations.Archive
{
    public class EmployeeArchiveConfiguration : IEntityTypeConfiguration<EmployeeArchive>
    {
        public void Configure(EntityTypeBuilder<EmployeeArchive> builder)
        {
            builder.ToTable("EmployeeArchives");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.KellyId).HasMaxLength(32).IsRequired();
            builder.HasIndex(item => item.KellyId).IsUnique();
            builder.Property(item => item.EmployeeName).HasMaxLength(200).IsRequired();
            builder.Property(item => item.MaskedTaxId).HasMaxLength(32);
            builder.Property(item => item.StorageStatus).HasConversion<string>().HasMaxLength(32);
            builder.Property(item => item.StatusDetail).HasMaxLength(1000);
        }
    }
}
