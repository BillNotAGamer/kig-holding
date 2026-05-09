using KIGHolding.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIGHolding.Data.Configurations;

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ToTable("MenuItems", table =>
        {
            table.HasCheckConstraint("CK_MenuItems_Price", "\"Price\" >= 0");
            table.HasCheckConstraint("CK_MenuItems_OriginalPrice", "\"OriginalPrice\" IS NULL OR \"OriginalPrice\" >= 0");
            table.HasCheckConstraint("CK_MenuItems_SpicyLevel", "\"SpicyLevel\" >= 0 AND \"SpicyLevel\" <= 5");
            table.HasCheckConstraint("CK_MenuItems_Calories", "\"Calories\" IS NULL OR \"Calories\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.CategoryId, x.IsAvailable, x.DisplayOrder });
        builder.HasIndex(x => x.IsSignature);
        builder.HasIndex(x => x.IsBestSeller);

        builder.Property(x => x.Name).HasMaxLength(180).IsRequired();
        builder.Property(x => x.KoreanName).HasMaxLength(180);
        builder.Property(x => x.Slug).HasMaxLength(180).IsRequired();
        builder.Property(x => x.ShortDescription).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.Price).HasPrecision(12, 2);
        builder.Property(x => x.OriginalPrice).HasPrecision(12, 2);
        builder.Property(x => x.ThumbnailUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.SpicyLevel).HasDefaultValue(0);
        builder.Property(x => x.ServingSize).HasMaxLength(120);
        builder.Property(x => x.IsAvailable).HasDefaultValue(true);
        builder.Property(x => x.DisplayOrder).HasDefaultValue(0);
        builder.Property(x => x.SeoTitle).HasMaxLength(180);
        builder.Property(x => x.SeoDescription).HasMaxLength(320);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");

        builder
            .HasOne(x => x.Category)
            .WithMany(x => x.MenuItems)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
