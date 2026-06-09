using Core.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Database.Configurations
{
    internal class MatchReviewConfiguration : IEntityTypeConfiguration<MatchReview>
    {
        public void Configure(EntityTypeBuilder<MatchReview> builder)
        {
            builder.HasKey(review => review.Id);

            builder.Property(review => review.ApplicationId).IsRequired();
            builder.Property(review => review.ReviewerId).IsRequired();
            builder.Property(review => review.RevieweeId).IsRequired();
            builder.Property(review => review.Rating).IsRequired();
            builder.Property(review => review.Comment).HasMaxLength(MatchReview.MaxCommentLength);
            builder.Property(review => review.CreatedAtUtc).IsRequired();

            builder.HasIndex(review => new { review.ApplicationId, review.ReviewerId }).IsUnique();
            builder.HasIndex(review => review.RevieweeId);

            builder.ToTable("MatchReviews");
        }
    }
}
