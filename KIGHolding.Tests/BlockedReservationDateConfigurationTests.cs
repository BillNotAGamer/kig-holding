using KIGHolding.Data;
using KIGHolding.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Tests;

public sealed class BlockedReservationDateConfigurationTests
{
    [Fact]
    public void BlockedReservationDateConfiguration_DefinesNaturalDatePrimaryKey()
    {
        using var dbContext = CreateDbContext();
        var entityType = dbContext.Model.FindEntityType(typeof(BlockedReservationDate));

        Assert.NotNull(entityType);
        Assert.Equal(
            [nameof(BlockedReservationDate.Date)],
            entityType.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal("date", entityType.FindProperty(nameof(BlockedReservationDate.Date))?.GetColumnType());
        Assert.Equal("timestamp with time zone", entityType.FindProperty(nameof(BlockedReservationDate.CreatedAt))?.GetColumnType());
        Assert.Equal("now()", entityType.FindProperty(nameof(BlockedReservationDate.CreatedAt))?.GetDefaultValueSql());
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=kig_model_test;Username=test;Password=test")
            .Options;

        return new AppDbContext(options);
    }
}
