using ApplifyLab.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApplifyLab.Infrastructure.Persistence.Configurations;

public class CommentConfiguration : IEntityTypeConfiguration<Comment>
{
    public void Configure(EntityTypeBuilder<Comment> builder)
    {
        builder.ToTable("comments");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.SequenceNumber).UseIdentityAlwaysColumn();
        builder.Property(x => x.Content).HasMaxLength(2000).IsRequired();

        builder.HasOne(x => x.Post)
            .WithMany(p => p.Comments)
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Author)
            .WithMany(u => u.Comments)
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ParentComment)
            .WithMany(x => x.Replies)
            .HasForeignKey(x => x.ParentCommentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.PostId, x.CreatedAt, x.SequenceNumber });
        builder.HasIndex(x => x.ParentCommentId);
    }
}
