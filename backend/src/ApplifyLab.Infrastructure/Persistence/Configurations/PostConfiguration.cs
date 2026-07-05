using ApplifyLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApplifyLab.Infrastructure.Persistence.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("posts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SequenceNumber).UseIdentityAlwaysColumn();
        builder.Property(x => x.Content).HasMaxLength(5000).IsRequired();
        builder.Property(x => x.Visibility).HasConversion<string>().HasMaxLength(20);

        builder.HasOne(x => x.Author)
            .WithMany(u => u.Posts)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(x => x.AuthorId);
        // Primary feed access pattern: filter by visibility, sort by recency (cursor pagination).
        builder.HasIndex(x => new { x.Visibility, x.CreatedAt, x.SequenceNumber });
    }
}
