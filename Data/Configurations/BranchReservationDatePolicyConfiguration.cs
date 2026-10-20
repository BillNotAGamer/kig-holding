using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace KIGHolding.Data.Configurations;

public sealed class BranchReservationDatePolicyConfiguration : IEntityTypeConfiguration<BranchReservationDatePolicy>
{
    public void Configure(EntityTypeBuilder<BranchReservationDatePolicy> builder)
    {
        builder.ToTable("BranchReservationDatePolicies", table =>
        {
            table.HasCheckConstraint(
                "CK_BranchReservationDatePolicies_Rule",
                "(\"RuleType\" = 'Closed' AND \"MinGuests\" IS NULL) OR " +
                "(\"RuleType\" = 'MinimumGuests' AND \"MinGuests\" IS NOT NULL AND \"MinGuests\" BETWEEN 1 AND 100)");
        });

        builder.HasKey(x => new { x.BranchId, x.Date });

        builder.HasIndex(x => x.Date)
            .HasDatabaseName("IX_BranchReservationDatePolicies_Date");

        builder.Property(x => x.Date)
            .HasColumnType("date");
        builder.Property(x => x.RuleType)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(x => x.Version)
            .IsRequired()
            .IsConcurrencyToken();
        builder.Property(x => x.CreatedAt)
            .HasDefaultValueSql("now()");
        builder.Property(x => x.UpdatedAt)
            .HasDefaultValueSql("now()");

        builder
            .HasOne(x => x.Branch)
            .WithMany(x => x.ReservationDatePolicies)
            .HasForeignKey(x => x.BranchId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
