using KIGHolding.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIGHolding.Data.Configurations;

public class MenuItemImageConfiguration : IEntityTypeConfiguration<MenuItemImage>
{
    public void Configure(EntityTypeBuilder<MenuItemImage> builder)
    {
        builder.ToTable("MenuItemImages");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.MenuItemId, x.DisplayOrder });

        builder.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.AltText).HasMaxLength(180).IsRequired();
        builder.Property(x => x.DisplayOrder).HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        builder
            .HasOne(x => x.MenuItem)
            .WithMany(x => x.Images)
            .HasForeignKey(x => x.MenuItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
