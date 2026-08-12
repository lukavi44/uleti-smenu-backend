using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class ModerationReportConfiguration : IEntityTypeConfiguration<ModerationReport>
    {
        public void Configure(EntityTypeBuilder<ModerationReport> builder)
        {
            builder.HasKey(report => report.Id);

            builder.Property(report => report.ReporterUserId).IsRequired();
            builder.Property(report => report.TargetType).IsRequired().HasConversion<string>().HasMaxLength(32);
            builder.Property(report => report.TargetId).IsRequired();
            builder.Property(report => report.Reason).IsRequired().HasMaxLength(80);
            builder.Property(report => report.Details).HasMaxLength(2000);
            builder.Property(report => report.CreatedAtUtc).IsRequired();
            builder.Property(report => report.Status).IsRequired().HasConversion<string>().HasMaxLength(32);
            builder.Property(report => report.AdminNotes).HasMaxLength(2000);

            builder.HasOne<User>()
                .WithMany()
                .HasForeignKey(report => report.ReporterUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(report => report.CreatedAtUtc);
            builder.HasIndex(report => report.Status);
            builder.HasIndex(report => new { report.TargetType, report.TargetId });
            builder.HasIndex(report => new { report.ReporterUserId, report.TargetType, report.TargetId, report.Status });

            builder.ToTable("ModerationReports");
        }
    }
}
