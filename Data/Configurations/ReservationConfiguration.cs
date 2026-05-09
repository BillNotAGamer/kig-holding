using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIGHolding.Data.Configurations;

public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
{
    public void Configure(EntityTypeBuilder<Reservation> builder)
    {
        builder.ToTable("Reservations", table =>
        {
            table.HasCheckConstraint("CK_Reservations_GuestCount", "\"GuestCount\" > 0");
        });

        builder.HasKey(x => x.Id);

        builder.HasIndex(x => new { x.BranchId, x.ReservationDate, x.ReservationTime });
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.CreatedAt);

        builder.Property(x => x.CustomerName).HasMaxLength(160).IsRequired();
        builder.Property(x => x.PhoneNumber).HasMaxLength(32).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(160);
        builder.Property(x => x.Note).HasMaxLength(1000);
        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(ReservationStatus.Pending);
        builder.Property(x => x.Source)
            .HasConversion<string>()
            .HasMaxLength(32)
            .HasDefaultValue(ReservationSource.Website);
        builder.Property(x => x.CreatedAt).HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt).HasDefaultValueSql("now()");

        builder
            .HasOne(x => x.Branch)
            .WithMany(x => x.Reservations)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
