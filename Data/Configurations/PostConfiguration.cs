using KIGHolding.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIGHolding.Data.Configurations;

public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.ToTable("Posts");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.IsPublished, x.PublishedAt });
        builder.HasIndex(x => x.Category);

        builder.Property(x => x.Title).HasMaxLength(220).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(220).IsRequired();
        builder.Property(x => x.Excerpt).HasMaxLength(600).IsRequired();
        builder.Property(x => x.Content).IsRequired();
        builder.Property(x => x.ThumbnailUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(80).IsRequired();
        builder.Property(x => x.IsPublished).HasDefaultValue(false);
        builder.Property(x => x.SeoTitle).HasMaxLength(180);
        builder.Property(x => x.SeoDescription).HasMaxLength(320);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
    }
}
