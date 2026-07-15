using KellyServices.PARS.Domain.Entities.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
namespace KellyServices.PARS.Persistence.Configurations.Requests
{
    public class PayrollRequestCandidateConfiguration : IEntityTypeConfiguration<PayrollRequestCandidate>
    {
        public void Configure(EntityTypeBuilder<PayrollRequestCandidate> builder)
        {
            builder.ToTable("PayrollRequestCandidates"); builder.HasKey(item => item.Id); builder.Property(item => item.ConfidenceScore).HasPrecision(5, 2);
            builder.Property(item => item.MatchedAttributes).HasMaxLength(500).IsRequired(); builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(32); builder.Property(item => item.ReviewedBy).HasMaxLength(200);
            builder.HasIndex(item => new { item.PayrollDataRequestId, item.EmployeeArchiveId }).IsUnique();
            builder.HasOne(item => item.PayrollDataRequest).WithMany(item => item.Candidates).HasForeignKey(item => item.PayrollDataRequestId).OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(item => item.EmployeeArchive).WithMany().HasForeignKey(item => item.EmployeeArchiveId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
