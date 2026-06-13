using KIGHolding.Data;
using KIGHolding.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Services;

public sealed class AdminLegacyCredentialGuard : IAdminLegacyCredentialGuard
{
    private const string LegacySeedDetectedMessage = "Phát hiện tài khoản quản trị khởi tạo lịch sử chưa được bảo mật. Hãy cấu hình AdminBootstrap và bật RemediateLegacySeed trước khi khởi động hệ thống.";
    private const string MissingBootstrapConfigurationMessage = "RemediateLegacySeed đã được bật nhưng AdminBootstrap__Username và AdminBootstrap__Password hợp lệ vẫn là bắt buộc để bảo mật tài khoản quản trị khởi tạo lịch sử.";

    private readonly AppDbContext _dbContext;
    private readonly AdminBootstrapConfigurationResolver _bootstrapConfigurationResolver;
    private readonly IPasswordHasher<AdminUser> _passwordHasher;
    private readonly ILogger<AdminLegacyCredentialGuard> _logger;

    public AdminLegacyCredentialGuard(
        AppDbContext dbContext,
        AdminBootstrapConfigurationResolver bootstrapConfigurationResolver,
        IPasswordHasher<AdminUser> passwordHasher,
        ILogger<AdminLegacyCredentialGuard> logger)
    {
        _dbContext = dbContext;
        _bootstrapConfigurationResolver = bootstrapConfigurationResolver;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task EnsureSecureAsync(CancellationToken cancellationToken = default)
    {
        AdminUser? legacyAdmin;

        try
        {
            legacyAdmin = await _dbContext.AdminUsers
                .FirstOrDefaultAsync(user => user.Id == LegacyAdminSeedFingerprint.AdminId, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unable to verify the admin legacy credential guard during startup.");
            throw new InvalidOperationException(
                "Không thể kiểm tra trạng thái bảo mật tài khoản quản trị. Hãy áp dụng đầy đủ Phase 1 schema và cấu hình kết nối trước khi khởi động hệ thống.",
                exception);
        }

        if (legacyAdmin is null || !LegacyAdminSeedFingerprint.IsExactUnremediatedHistoricalSeed(legacyAdmin))
        {
            return;
        }

        if (!_bootstrapConfigurationResolver.IsLegacyRemediationEnabled)
        {
            throw new InvalidOperationException(LegacySeedDetectedMessage);
        }

        var bootstrapConfiguration = _bootstrapConfigurationResolver.GetRequiredConfiguration(MissingBootstrapConfigurationMessage);

        try
        {
            var usernameInUse = await _dbContext.AdminUsers
                .AsNoTracking()
                .AnyAsync(
                    user => user.Id != legacyAdmin.Id &&
                        user.Username == bootstrapConfiguration.Username,
                    cancellationToken);

            if (usernameInUse)
            {
                throw new InvalidOperationException(
                    "Cấu hình AdminBootstrap không hợp lệ vì tên đăng nhập đã thuộc về một tài khoản quản trị khác.");
            }

            if (!string.IsNullOrWhiteSpace(bootstrapConfiguration.NormalizedEmail))
            {
                var emailInUse = await _dbContext.AdminUsers
                    .AsNoTracking()
                    .AnyAsync(
                        user => user.Id != legacyAdmin.Id &&
                            user.NormalizedEmail == bootstrapConfiguration.NormalizedEmail,
                        cancellationToken);

                if (emailInUse)
                {
                    throw new InvalidOperationException(
                        "Cấu hình AdminBootstrap không hợp lệ vì email đã thuộc về một tài khoản quản trị khác.");
                }
            }

            legacyAdmin.Username = bootstrapConfiguration.Username;
            legacyAdmin.PasswordHash = _passwordHasher.HashPassword(legacyAdmin, bootstrapConfiguration.Password);
            legacyAdmin.Email = bootstrapConfiguration.Email;
            legacyAdmin.NormalizedEmail = bootstrapConfiguration.NormalizedEmail;
            legacyAdmin.EmailConfirmed = false;
            legacyAdmin.SecurityStamp = AdminSecurityStampGenerator.Create();
            legacyAdmin.Role = LegacyAdminSeedFingerprint.Role;
            legacyAdmin.IsActive = true;
            legacyAdmin.UpdatedAt = DateTimeOffset.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (InvalidOperationException)
        {
            throw;
        }
        catch (DbUpdateException exception)
        {
            _logger.LogWarning(exception, "Legacy admin remediation failed due to a database update conflict.");
            throw new InvalidOperationException(
                "Không thể bảo mật tài khoản quản trị khởi tạo lịch sử bằng cấu hình hiện tại. Hãy kiểm tra tên đăng nhập hoặc email quản trị rồi khởi động lại hệ thống.",
                exception);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Legacy admin remediation failed during startup.");
            throw new InvalidOperationException(
                "Không thể bảo mật tài khoản quản trị khởi tạo lịch sử. Hãy kiểm tra cấu hình quản trị và trạng thái cơ sở dữ liệu trước khi khởi động lại hệ thống.",
                exception);
        }

        _logger.LogInformation("Legacy admin remediation completed during startup.");
    }
}
