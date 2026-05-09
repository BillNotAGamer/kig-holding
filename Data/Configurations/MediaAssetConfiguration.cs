using KIGHolding.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIGHolding.Data.Configurations;

public class MediaAssetConfiguration : IEntityTypeConfiguration<MediaAsset>
{
    public void Configure(EntityTypeBuilder<MediaAsset> builder)
    {
        builder.ToTable("MediaAssets", table =>
        {
            table.HasCheckConstraint("CK_MediaAssets_SizeInBytes", "\"SizeInBytes\" >= 0");
        });

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => x.Url).IsUnique();
        builder.HasIndex(x => x.Folder);

        builder.Property(x => x.FileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.OriginalFileName).HasMaxLength(260).IsRequired();
        builder.Property(x => x.Url).HasMaxLength(500).IsRequired();
        builder.Property(x => x.MimeType).HasMaxLength(120).IsRequired();
        builder.Property(x => x.AltText).HasMaxLength(180);
        builder.Property(x => x.Folder).HasMaxLength(180);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
    }
}
