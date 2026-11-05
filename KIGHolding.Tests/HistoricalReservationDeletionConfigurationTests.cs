using KIGHolding.Data;
using KIGHolding.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace KIGHolding.Tests;

public sealed class HistoricalReservationDeletionConfigurationTests
{
    [Fact]
    public void ReservationModel_DefinesSoftDeleteFilterMetadataIndexesAndRevisionConcurrency()
    {
        using var dbContext = CreateDbContext();
        var entityType = dbContext.GetService<IDesignTimeModel>().Model.FindEntityType(typeof(Reservation));

        Assert.NotNull(entityType);
        Assert.NotEmpty(entityType.GetDeclaredQueryFilters());
        Assert.Equal(typeof(bool), entityType.FindProperty(nameof(Reservation.IsDeleted))?.ClrType);
        Assert.Equal(false, entityType.FindProperty(nameof(Reservation.IsDeleted))?.GetDefaultValue());
        Assert.Equal(typeof(DateTimeOffset?), entityType.FindProperty(nameof(Reservation.DeletedAt))?.ClrType);
        Assert.Equal(typeof(Guid?), entityType.FindProperty(nameof(Reservation.DeletedByAdminUserId))?.ClrType);

        var version = entityType.FindProperty(nameof(Reservation.Version));
        Assert.Equal(typeof(Guid), version?.ClrType);
        Assert.Equal("uuid", version?.GetColumnType());
        Assert.True(version?.IsConcurrencyToken);
        Assert.Null(version?.FindAnnotation(RelationalAnnotationNames.DefaultValue));
        Assert.Equal("gen_random_uuid()", version?.GetDefaultValueSql());

        Assert.Contains(entityType.GetIndexes(), index =>
            index.Properties.Select(property => property.Name).SequenceEqual([
                nameof(Reservation.IsDeleted), nameof(Reservation.ReservationDate), nameof(Reservation.ReservationTime)]));
        var deletedByForeignKey = entityType.GetForeignKeys().Single(foreignKey =>
            foreignKey.Properties.Single().Name == nameof(Reservation.DeletedByAdminUserId));
        Assert.Equal(DeleteBehavior.Restrict, deletedByForeignKey.DeleteBehavior);
        Assert.Equal(typeof(AdminUser), deletedByForeignKey.PrincipalEntityType.ClrType);
    }

    [Fact]
    public void Migration_AddsSoftDeleteMetadataWithPerRowRevisionBackfill()
    {
        using var dbContext = CreateDbContext();
        var migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();
        var migrationType = migrationsAssembly.Migrations["20261105200906_AddHistoricalReservationSoftDelete"];
        var migration = migrationsAssembly.CreateMigration(migrationType, "Npgsql.EntityFrameworkCore.PostgreSQL");

        var addedColumns = migration.UpOperations.OfType<AddColumnOperation>().ToArray();
        Assert.Contains(addedColumns, column => column.Name == "IsDeleted" && column.DefaultValue is false);
        Assert.Contains(addedColumns, column => column.Name == "DeletedAt" && column.IsNullable);
        Assert.Contains(addedColumns, column => column.Name == "DeletedByAdminUserId" && column.IsNullable);
        var version = Assert.Single(addedColumns, column => column.Name == "Version");
        Assert.Equal("uuid", version.ColumnType);
        Assert.True(version.IsNullable);
        Assert.Null(version.DefaultValue);
        Assert.Contains(migration.UpOperations.OfType<SqlOperation>(), operation =>
            operation.Sql.Contains("UPDATE \"Reservations\"", StringComparison.Ordinal) &&
            operation.Sql.Contains("md5(random()", StringComparison.Ordinal));
        Assert.DoesNotContain("00000000-0000-0000-0000-000000000000", string.Join(Environment.NewLine,
            migration.UpOperations.OfType<SqlOperation>().Select(operation => operation.Sql)), StringComparison.Ordinal);
        Assert.Contains(migration.UpOperations.OfType<AlterColumnOperation>(), operation =>
            operation.Name == "Version" &&
            !operation.IsNullable &&
            operation.DefaultValueSql == "gen_random_uuid()");
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=kig_model_test;Username=test;Password=test")
            .Options;
        return new AppDbContext(options);
    }
}
