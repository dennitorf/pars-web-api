using KellyServices.PARS.Domain.Entities.Archive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KellyServices.PARS.Persistence.Configurations.Archive
{
    public class ArchiveIngestionBatchConfiguration : IEntityTypeConfiguration<ArchiveIngestionBatch>
    {
        public void Configure(EntityTypeBuilder<ArchiveIngestionBatch> builder)
        {
            builder.ToTable("ArchiveIngestionBatches");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.MetadataFilePath).HasMaxLength(1024).IsRequired();
            builder.Property(item => item.MetadataChecksum).HasMaxLength(128).IsRequired();
            builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(32);
            builder.Property(item => item.LastError).HasMaxLength(4000);
            builder.HasIndex(item => new { item.MetadataFilePath, item.MetadataChecksum }).IsUnique();
            builder.HasIndex(item => item.StartedAt);
        }
    }
}
