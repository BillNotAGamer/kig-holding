using KIGHolding.Models.Entities;

namespace KIGHolding.Services;

public static class LegacyAdminSeedFingerprint
{
    public static readonly Guid AdminId = Guid.Parse("2d7d0dd3-7d9d-4d9b-93d8-8e2d2b8ed9b1");
    public static readonly DateTimeOffset SeedTimestamp = new(2026, 5, 8, 0, 0, 0, TimeSpan.Zero);

    public const string Username = "admin";
    public const string Role = "SuperAdmin";

    public static bool IsExactUnremediatedHistoricalSeed(AdminUser user)
    {
        // This is a narrow structural marker derived from immutable seed fields in the
        // historical migration. It intentionally does not rely on the historical password
        // or a deterministic password hash, because the original migration generated a
        // salted hash per database at execution time.
        return user.Id == AdminId &&
            string.Equals(user.Username, Username, StringComparison.Ordinal) &&
            string.Equals(user.Role, Role, StringComparison.Ordinal) &&
            user.IsActive &&
            user.CreatedAt == SeedTimestamp &&
            user.UpdatedAt == SeedTimestamp &&
            string.IsNullOrWhiteSpace(user.Email) &&
            string.IsNullOrWhiteSpace(user.NormalizedEmail) &&
            !user.EmailConfirmed;
    }
}
