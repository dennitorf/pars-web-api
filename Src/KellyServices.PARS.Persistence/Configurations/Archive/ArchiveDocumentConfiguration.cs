using KellyServices.PARS.Domain.Entities.Archive;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KellyServices.PARS.Persistence.Configurations.Archive
{
    public class ArchiveDocumentConfiguration : IEntityTypeConfiguration<ArchiveDocument>
    {
        public void Configure(EntityTypeBuilder<ArchiveDocument> builder)
        {
            builder.ToTable("ArchiveDocuments");
            builder.HasKey(item => item.Id);
            builder.Property(item => item.DocumentType).HasMaxLength(32).IsRequired();
            builder.Property(item => item.DocumentPeriod).HasMaxLength(100);
            builder.Property(item => item.OriginalFileName).HasMaxLength(255).IsRequired();
            builder.Property(item => item.ContentType).HasMaxLength(100).IsRequired();
            builder.Property(item => item.BlobContainer).HasMaxLength(63).IsRequired();
            builder.Property(item => item.BlobName).HasMaxLength(1024).IsRequired();
            builder.Property(item => item.SourcePath).HasMaxLength(1024).IsRequired();
            builder.Property(item => item.SourceChecksum).HasMaxLength(128).IsRequired();
            builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(32);
            builder.Property(item => item.FailureReason).HasMaxLength(2000);
            builder.HasIndex(item => new { item.SourcePath, item.SourceChecksum }).IsUnique();
            builder.HasIndex(item => new { item.BlobContainer, item.BlobName }).IsUnique();
            builder.HasIndex(item => new { item.EmployeeArchiveId, item.DocumentYear, item.DocumentType });
            builder.HasOne(item => item.EmployeeArchive).WithMany(item => item.Documents).HasForeignKey(item => item.EmployeeArchiveId).OnDelete(DeleteBehavior.Restrict);
            builder.HasOne(item => item.IngestionBatch).WithMany(item => item.Documents).HasForeignKey(item => item.IngestionBatchId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
