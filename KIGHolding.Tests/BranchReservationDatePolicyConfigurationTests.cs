using KIGHolding.Data;
using KIGHolding.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace KIGHolding.Tests;

public sealed class BranchReservationDatePolicyConfigurationTests
{
    [Fact]
    public void Configuration_DefinesCompositeKeyConstraintsIndexAndConcurrencyToken()
    {
        using var dbContext = CreateDbContext();
        var entityType = dbContext
            .GetService<IDesignTimeModel>()
            .Model
            .FindEntityType(typeof(BranchReservationDatePolicy));

        Assert.NotNull(entityType);
        Assert.Equal(
            [nameof(BranchReservationDatePolicy.BranchId), nameof(BranchReservationDatePolicy.Date)],
            entityType.FindPrimaryKey()!.Properties.Select(property => property.Name));
        Assert.Equal("date", entityType.FindProperty(nameof(BranchReservationDatePolicy.Date))?.GetColumnType());
        Assert.Equal("character varying(32)", entityType.FindProperty(nameof(BranchReservationDatePolicy.RuleType))?.GetColumnType());
        Assert.Equal(
            typeof(string),
            entityType.FindProperty(nameof(BranchReservationDatePolicy.RuleType))?.GetTypeMapping().Converter?.ProviderClrType);
        var versionProperty = entityType.FindProperty(nameof(BranchReservationDatePolicy.Version));
        Assert.Equal(typeof(Guid), versionProperty?.ClrType);
        Assert.Equal("uuid", versionProperty?.GetColumnType());
        Assert.True(versionProperty?.IsConcurrencyToken);
        Assert.Null(versionProperty?.FindAnnotation(RelationalAnnotationNames.DefaultValue));
        Assert.Null(versionProperty?.GetDefaultValueSql());
        Assert.Equal("now()", entityType.FindProperty(nameof(BranchReservationDatePolicy.CreatedAt))?.GetDefaultValueSql());
        Assert.Equal("now()", entityType.FindProperty(nameof(BranchReservationDatePolicy.UpdatedAt))?.GetDefaultValueSql());

        var dateIndex = entityType.GetIndexes().Single(index =>
            index.Properties.Select(property => property.Name)
                .SequenceEqual([nameof(BranchReservationDatePolicy.Date)]));
        Assert.Equal("IX_BranchReservationDatePolicies_Date", dateIndex.GetDatabaseName());

        var branchForeignKey = entityType.GetForeignKeys().Single();
        Assert.Equal(DeleteBehavior.Restrict, branchForeignKey.DeleteBehavior);
        Assert.Equal(typeof(Branch), branchForeignKey.PrincipalEntityType.ClrType);

        var checkConstraint = entityType.GetCheckConstraints().Single(constraint =>
            constraint.Name == "CK_BranchReservationDatePolicies_Rule");
        Assert.Contains("'Closed'", checkConstraint.Sql, StringComparison.Ordinal);
        Assert.Contains("'MinimumGuests'", checkConstraint.Sql, StringComparison.Ordinal);
        Assert.Contains("\"MinGuests\" IS NOT NULL", checkConstraint.Sql, StringComparison.Ordinal);
        Assert.Contains("BETWEEN 1 AND 100", checkConstraint.Sql, StringComparison.Ordinal);
    }

    [Fact]
    public void Migration_CreatesUuidRevisionWithoutNumericDefault()
    {
        using var dbContext = CreateDbContext();
        var migrationsAssembly = dbContext.GetService<IMigrationsAssembly>();
        var migrationType = migrationsAssembly.Migrations["20261019052426_AddBranchReservationDatePolicies"];
        var migration = migrationsAssembly.CreateMigration(
            migrationType,
            "Npgsql.EntityFrameworkCore.PostgreSQL");
        var createTable = Assert.Single(migration.UpOperations.OfType<CreateTableOperation>());
        var versionColumn = Assert.Single(createTable.Columns, column => column.Name == "Version");
        var ruleConstraint = Assert.Single(
            createTable.CheckConstraints,
            constraint => constraint.Name == "CK_BranchReservationDatePolicies_Rule");

        Assert.Equal("BranchReservationDatePolicies", createTable.Name);
        Assert.Equal(typeof(Guid), versionColumn.ClrType);
        Assert.Equal("uuid", versionColumn.ColumnType);
        Assert.False(versionColumn.IsNullable);
        Assert.Null(versionColumn.DefaultValue);
        Assert.Null(versionColumn.DefaultValueSql);
        Assert.Contains("\"MinGuests\" IS NOT NULL", ruleConstraint.Sql, StringComparison.Ordinal);
        Assert.Contains("BETWEEN 1 AND 100", ruleConstraint.Sql, StringComparison.Ordinal);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=kig_model_test;Username=test;Password=test")
            .Options;

        return new AppDbContext(options);
    }
}
