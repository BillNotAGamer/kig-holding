using KIGHolding.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIGHolding.Data.Configurations;

public sealed class BlockedReservationDateConfiguration : IEntityTypeConfiguration<BlockedReservationDate>
{
    public void Configure(EntityTypeBuilder<BlockedReservationDate> builder)
    {
        builder.ToTable("BlockedReservationDates");

        builder.HasKey(x => x.Date);

        builder.Property(x => x.Date)
            .HasColumnType("date");

        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("now()");
    }
}
