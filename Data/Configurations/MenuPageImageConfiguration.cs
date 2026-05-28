using KIGHolding.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIGHolding.Data.Configurations;

public class MenuPageImageConfiguration : IEntityTypeConfiguration<MenuPageImage>
{
    public void Configure(EntityTypeBuilder<MenuPageImage> builder)
    {
        builder.ToTable("MenuPageImages");

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.MenuGroupId, x.DisplayOrder });

        builder.Property(x => x.ImageUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.AltText).HasMaxLength(180);
        builder.Property(x => x.DisplayOrder).HasDefaultValue(0);
        builder.Property(x => x.IsPublished).HasDefaultValue(true);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
    }
}
