using KellyServices.PARS.Domain.Entities.Requests;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KellyServices.PARS.Persistence.Configurations.Requests
{
    public class PayrollDataRequestConfiguration : IEntityTypeConfiguration<PayrollDataRequest>
    {
        public void Configure(EntityTypeBuilder<PayrollDataRequest> builder)
        {
            builder.ToTable("PayrollDataRequests"); builder.HasKey(item => item.Id);
            builder.Property(item => item.RequestNumber).HasMaxLength(32).IsRequired();
            builder.Property(item => item.EmployeeFirstName).HasMaxLength(100).IsRequired();
            builder.Property(item => item.EmployeeLastName).HasMaxLength(100).IsRequired();
            builder.Property(item => item.EmployeeEmail).HasMaxLength(320).IsRequired();
            builder.Property(item => item.KellyId).HasMaxLength(64); builder.Property(item => item.TaxIdLastFour).HasMaxLength(4);
            builder.Property(item => item.RequestedDocumentTypes).HasMaxLength(200).IsRequired();
            builder.Property(item => item.Status).HasConversion<string>().HasMaxLength(40);
            builder.Property(item => item.AssignedTo).HasMaxLength(200); builder.Property(item => item.SpecialistNotes).HasMaxLength(2000);
            builder.HasIndex(item => item.RequestNumber).IsUnique(); builder.HasIndex(item => new { item.Status, item.SubmittedAt }); builder.HasIndex(item => new { item.EmployeeEmail, item.SubmittedAt });
            builder.HasOne(item => item.ConfirmedEmployeeArchive).WithMany().HasForeignKey(item => item.ConfirmedEmployeeArchiveId).OnDelete(DeleteBehavior.Restrict);
        }
    }
}
