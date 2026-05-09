using KIGHolding.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIGHolding.Data.Configurations;

public class SiteSettingConfiguration : IEntityTypeConfiguration<SiteSetting>
{
    public void Configure(EntityTypeBuilder<SiteSetting> builder)
    {
        builder.ToTable("SiteSettings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.SiteName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.BrandName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Slogan).HasMaxLength(300).IsRequired();
        builder.Property(x => x.Hotline).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(160).IsRequired();
        builder.Property(x => x.FacebookUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.ZaloUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.TiktokUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Address).HasMaxLength(300).IsRequired();
        builder.Property(x => x.GoogleMapUrl).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.LogoUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.FaviconUrl).HasMaxLength(500).IsRequired();
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");
    }
}
