using KIGHolding.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIGHolding.Data.Configurations;

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews", table =>
        {
            table.HasCheckConstraint("CK_Reviews_Rating", "\"Rating\" >= 1 AND \"Rating\" <= 5");
        });

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.IsVisible, x.DisplayOrder });

        builder.Property(x => x.CustomerName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.AvatarUrl).HasMaxLength(500);
        builder.Property(x => x.Content).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.IsVisible).HasDefaultValue(true);
        builder.Property(x => x.DisplayOrder).HasDefaultValue(0);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");

        builder
            .HasOne(x => x.Branch)
            .WithMany(x => x.Reviews)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
