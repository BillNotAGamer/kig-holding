using KIGHolding.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIGHolding.Data.Configurations;

public class BranchConfiguration : IEntityTypeConfiguration<Branch>
{
    public void Configure(EntityTypeBuilder<Branch> builder)
    {
        builder.ToTable("Branches", table =>
        {
            table.HasCheckConstraint("CK_Branches_Capacity", "\"Capacity\" >= 0");
            table.HasCheckConstraint("CK_Branches_AreaSquareMeters", "\"AreaSquareMeters\" IS NULL OR \"AreaSquareMeters\" > 0");
            table.HasCheckConstraint("CK_Branches_NumberOfFloors", "\"NumberOfFloors\" IS NULL OR \"NumberOfFloors\" > 0");
        });

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => new { x.IsActive, x.DisplayOrder });

        builder.Property(x => x.Name).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(180).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(300).IsRequired();
        builder.Property(x => x.District).HasMaxLength(120).IsRequired();
        builder.Property(x => x.City).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Hotline).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(160).IsRequired();
        builder.Property(x => x.AreaSquareMeters).HasPrecision(8, 2);
        builder.Property(x => x.Description).IsRequired();
        builder.Property(x => x.ThumbnailUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.GoogleMapUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.SeoTitle).HasMaxLength(180);
        builder.Property(x => x.SeoDescription).HasMaxLength(320);
        builder.Property(x => x.IsActive).HasDefaultValue(true);
        builder.Property(x => x.DisplayOrder).HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
    }
}
