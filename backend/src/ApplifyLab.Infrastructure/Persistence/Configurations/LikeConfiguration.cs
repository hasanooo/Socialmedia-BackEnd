using ApplifyLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApplifyLab.Infrastructure.Persistence.Configurations;

public class LikeConfiguration : IEntityTypeConfiguration<Like>
{
    public void Configure(EntityTypeBuilder<Like> builder)
    {
        builder.ToTable("likes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SequenceNumber).UseIdentityAlwaysColumn();
        builder.Property(x => x.LikeableType).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(x => x.User)
            .WithMany(u => u.Likes)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Prevents double-likes and makes unlike/toggle an O(1) lookup.
        builder.HasIndex(x => new { x.UserId, x.LikeableType, x.LikeableId }).IsUnique();
        // Supports "liked by" listing and count reconciliation.
        builder.HasIndex(x => new { x.LikeableType, x.LikeableId });
    }
}
