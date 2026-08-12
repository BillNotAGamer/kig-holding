using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
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
        builder.Property(x => x.ContentMode)
            .HasConversion<int>()
            .HasDefaultValue(PostContentMode.Visual)
            .IsRequired();
        builder.Property(x => x.ThumbnailUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Category).HasMaxLength(80).IsRequired();
        builder.Property(x => x.IsPublished).HasDefaultValue(false);
        builder.Property(x => x.SeoTitle).HasMaxLength(180);
        builder.Property(x => x.SeoDescription).HasMaxLength(320);
        builder.Property(x => x.FocusKeyword).HasMaxLength(120);
        builder.Property(x => x.CanonicalUrl).HasMaxLength(500);
        builder.Property(x => x.OgTitle).HasMaxLength(180);
        builder.Property(x => x.OgDescription).HasMaxLength(320);
        builder.Property(x => x.OgImageUrl).HasMaxLength(500);
        builder.Property(x => x.RobotsIndex).HasDefaultValue(true);
        builder.Property(x => x.RobotsFollow).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
    }
}
