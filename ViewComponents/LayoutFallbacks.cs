using KIGHolding.Models.Entities;
using KIGHolding.ViewModels.Shared;
using Microsoft.Extensions.Configuration;

namespace KIGHolding.ViewComponents;

internal static class LayoutFallbacks
{
    public static bool HasConfiguredDatabase(IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        return !string.IsNullOrWhiteSpace(connectionString)
            && !connectionString.Contains("your-neon-host", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("your_username", StringComparison.OrdinalIgnoreCase)
            && !connectionString.Contains("your_password", StringComparison.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<NavItemViewModel> CreateNavItems(string? requestPath)
    {
        var path = string.IsNullOrWhiteSpace(requestPath) ? "/" : requestPath;

        return
        [
            CreateNavItem("Trang chủ", "/", path, exact: true),
            CreateNavItem("Giới thiệu", "/gioi-thieu", path),
            CreateNavItem("Thực đơn", "/thuc-don", path),
            CreateNavItem("Đặt bàn", "/dat-ban", path),
            CreateNavItem("Chi nhánh", "/chi-nhanh", path),
            CreateNavItem("Tin tức", "/tin-tuc", path),
            CreateNavItem("Liên hệ", "/lien-he", path)
        ];
    }

    public static IReadOnlyList<FooterBranchViewModel> CreateFallbackBranches()
    {
        return
        [
            new FooterBranchViewModel
            {
                Name = "Champong Quận 1",
                Address = "12 Nguyễn Thiệp, Quận 1",
                Url = "/chi-nhanh#champong-quan-1"
            },
            new FooterBranchViewModel
            {
                Name = "Champong Gò Vấp",
                Address = "88 Phan Văn Trị, Gò Vấp",
                Url = "/chi-nhanh#champong-go-vap"
            },
            new FooterBranchViewModel
            {
                Name = "Champong Cầu Giấy",
                Address = "25 Trần Thái Tông, Cầu Giấy",
                Url = "/chi-nhanh#champong-cau-giay"
            }
        ];
    }

    public static IReadOnlyList<BookingBranchOptionViewModel> CreateFallbackBranchOptions()
    {
        return
        [
            new BookingBranchOptionViewModel { Id = Guid.Empty, Name = "Champong Quận 1" },
            new BookingBranchOptionViewModel { Id = Guid.Empty, Name = "Champong Gò Vấp" },
            new BookingBranchOptionViewModel { Id = Guid.Empty, Name = "Champong Cầu Giấy" }
        ];
    }

    public static HeaderViewModel CreateHeader(SiteSetting? setting, string? requestPath)
    {
        return new HeaderViewModel
        {
            BrandName = string.IsNullOrWhiteSpace(setting?.BrandName) ? "Truyền Thuyết Champong" : setting!.BrandName,
            LogoUrl = string.IsNullOrWhiteSpace(setting?.LogoUrl) ? string.Empty : setting!.LogoUrl,
            Hotline = string.IsNullOrWhiteSpace(setting?.Hotline) ? "0909 888 777" : setting!.Hotline,
            ReservationUrl = "/dat-ban",
            NavItems = CreateNavItems(requestPath)
        };
    }

    public static FooterViewModel CreateFooter(SiteSetting? setting, IReadOnlyList<Branch>? branches)
    {
        return new FooterViewModel
        {
            BrandName = string.IsNullOrWhiteSpace(setting?.BrandName) ? "Truyền Thuyết Champong" : setting!.BrandName,
            Description = string.IsNullOrWhiteSpace(setting?.Slogan) ? "Mì cay Hàn Quốc, BBQ nóng lửa và không gian tối hiện đại cho những bữa ăn đậm vị." : setting!.Slogan,
            LogoUrl = string.IsNullOrWhiteSpace(setting?.LogoUrl) ? string.Empty : setting!.LogoUrl,
            Hotline = string.IsNullOrWhiteSpace(setting?.Hotline) ? "0909 888 777" : setting!.Hotline,
            Email = string.IsNullOrWhiteSpace(setting?.Email) ? "hello@truyenthuyetchampong.vn" : setting!.Email,
            FacebookUrl = string.IsNullOrWhiteSpace(setting?.FacebookUrl) ? "#" : setting!.FacebookUrl,
            ZaloUrl = string.IsNullOrWhiteSpace(setting?.ZaloUrl) ? "#" : setting!.ZaloUrl,
            TiktokUrl = string.IsNullOrWhiteSpace(setting?.TiktokUrl) ? "#" : setting!.TiktokUrl,
            OpeningHours = BuildOpeningHours(branches),
            QuickLinks = CreateNavItems("/").Where(x => x.Url != "/").ToList(),
            Branches = branches is { Count: > 0 }
                ? branches.Take(3).Select(branch => new FooterBranchViewModel
                {
                    Name = branch.Name,
                    Address = $"{branch.Address}, {branch.District}",
                    Url = $"/chi-nhanh#{branch.Slug}"
                }).ToList()
                : CreateFallbackBranches()
        };
    }

    public static FloatingContactButtonsViewModel CreateFloatingButtons(SiteSetting? setting)
    {
        return new FloatingContactButtonsViewModel
        {
            Hotline = string.IsNullOrWhiteSpace(setting?.Hotline) ? "0909 888 777" : setting!.Hotline,
            FacebookUrl = string.IsNullOrWhiteSpace(setting?.FacebookUrl) ? "#" : setting!.FacebookUrl,
            ZaloUrl = string.IsNullOrWhiteSpace(setting?.ZaloUrl) ? "#" : setting!.ZaloUrl,
            ReservationUrl = "/dat-ban"
        };
    }

    private static NavItemViewModel CreateNavItem(string label, string url, string currentPath, bool exact = false)
    {
        var isActive = exact
            ? string.Equals(currentPath, url, StringComparison.OrdinalIgnoreCase)
            : currentPath.StartsWith(url, StringComparison.OrdinalIgnoreCase);

        return new NavItemViewModel
        {
            Label = label,
            Url = url,
            IsActive = isActive
        };
    }

    private static string BuildOpeningHours(IReadOnlyList<Branch>? branches)
    {
        var branch = branches?.FirstOrDefault();

        return branch is null
            ? "10:00 - 22:30"
            : $"{branch.OpeningTime:HH\\:mm} - {branch.ClosingTime:HH\\:mm}";
    }
}
