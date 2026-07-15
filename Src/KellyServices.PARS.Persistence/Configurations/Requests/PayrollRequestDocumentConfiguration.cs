using KellyServices.PARS.Domain.Entities.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace KellyServices.PARS.Persistence.Configurations.Requests
{
    public class PayrollRequestDocumentConfiguration : IEntityTypeConfiguration<PayrollRequestDocument>
    {
        public void Configure(EntityTypeBuilder<PayrollRequestDocument> builder)
        {
            builder.ToTable("PayrollRequestDocuments"); builder.HasKey(item => item.Id); builder.Property(item => item.ReviewedBy).HasMaxLength(200);
            builder.HasIndex(item => new { item.PayrollDataRequestId, item.ArchiveDocumentId }).IsUnique();
            builder.HasOne(item => item.PayrollDataRequest).WithMany(item => item.Documents).HasForeignKey(item => item.PayrollDataRequestId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(item => item.ArchiveDocument).WithMany().HasForeignKey(item => item.ArchiveDocumentId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
