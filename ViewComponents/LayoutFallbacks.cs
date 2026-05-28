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
            CreateNavItem("Thực đơn", "/thuc-don", path),
            CreateNavItem("Đặt bàn", "/dat-ban", path),
            CreateNavItem("Chi nhánh", "/chi-nhanh", path),
            CreateNavItem("Tin tức", "/tin-tuc", path),
            CreateNavItem(
                "KIG Holding",
                string.Empty,
                path,
                children:
                [
                    CreateNavItem("Về KIG Holding", "/gioi-thieu", path, exact: true),
                    // Future route: create page/controller later if needed.
                    CreateNavItem("Thành viên", "/thanh-vien", path, exact: true),
                    CreateNavItem("Liên hệ", "/lien-he", path, exact: true)
                ]),
            // Future route: create page/controller later if needed.
            CreateNavItem("Liên hệ nhượng quyền", "/lien-he-nhuong-quyen", path, exact: true)
        ];
    }

    public static IReadOnlyList<NavItemViewModel> CreateFooterNavItems()
    {
        return
        [
            new NavItemViewModel
            {
                Label = "Trang chủ",
                Url = "/",
                IsActive = false
            },
            new NavItemViewModel
            {
                Label = "Thực đơn",
                Url = "/thuc-don",
                IsActive = false
            },
            new NavItemViewModel
            {
                Label = "Chi nhánh",
                Url = "/chi-nhanh",
                IsActive = false
            },
            // Future route placeholder: keep the footer link visible even though the page is not implemented yet.
            new NavItemViewModel
            {
                Label = "Khuyến mãi",
                Url = "/khuyen-mai",
                IsActive = false
            },
            // Future route placeholder: keep the footer link visible even though the page is not implemented yet.
            new NavItemViewModel
            {
                Label = "Tuyển dụng",
                Url = "/tuyen-dung",
                IsActive = false
            },
            new NavItemViewModel
            {
                Label = "Về thương hiệu",
                Url = "/gioi-thieu",
                IsActive = false
            },
            // Future route placeholder: keep the footer link visible even though the page is not implemented yet.
            new NavItemViewModel
            {
                Label = "Thành viên",
                Url = "/thanh-vien",
                IsActive = false
            },
            new NavItemViewModel
            {
                Label = "Liên hệ",
                Url = "/lien-he",
                IsActive = false
            }
        ];
    }

    public static IReadOnlyList<FooterBranchViewModel> CreateFallbackBranches()
    {
        return
        [
            new FooterBranchViewModel
            {
                Name = "Champong Quáº­n 1",
                Address = "12 Nguyá»…n Thiá»‡p, Quáº­n 1",
                Url = "/chi-nhanh#champong-quan-1"
            },
            new FooterBranchViewModel
            {
                Name = "Champong GÃ² Váº¥p",
                Address = "88 Phan VÄƒn Trá»‹, GÃ² Váº¥p",
                Url = "/chi-nhanh#champong-go-vap"
            },
            new FooterBranchViewModel
            {
                Name = "Champong Cáº§u Giáº¥y",
                Address = "25 Tráº§n ThÃ¡i TÃ´ng, Cáº§u Giáº¥y",
                Url = "/chi-nhanh#champong-cau-giay"
            }
        ];
    }

    public static IReadOnlyList<BookingBranchOptionViewModel> CreateFallbackBranchOptions()
    {
        return
        [
            new BookingBranchOptionViewModel { Id = Guid.Empty, Name = "Champong Quáº­n 1" },
            new BookingBranchOptionViewModel { Id = Guid.Empty, Name = "Champong GÃ² Váº¥p" },
            new BookingBranchOptionViewModel { Id = Guid.Empty, Name = "Champong Cáº§u Giáº¥y" }
        ];
    }

    public static HeaderViewModel CreateHeader(SiteSetting? setting, string? requestPath)
    {
        return new HeaderViewModel
        {
            BrandName = string.IsNullOrWhiteSpace(setting?.BrandName) ? "Truyền Thuyết Champong" : setting!.BrandName,
            LogoUrl = string.IsNullOrWhiteSpace(setting?.LogoUrl) ? "/images/general/kig-no-bg-logo.png" : setting!.LogoUrl,
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
            FooterLogoUrl = "/images/general/kig-no-bg-logo.png",
            Address = string.IsNullOrWhiteSpace(setting?.Address) ? "Địa chỉ đang cập nhật" : setting!.Address,
            GoogleMapUrl = string.IsNullOrWhiteSpace(setting?.GoogleMapUrl) ? string.Empty : setting!.GoogleMapUrl,
            Hotline = string.IsNullOrWhiteSpace(setting?.Hotline) ? "0909 888 777" : setting!.Hotline,
            Email = string.IsNullOrWhiteSpace(setting?.Email) ? "truyenthuyetchamponghcm@gmail.com" : setting!.Email,
            FacebookUrl = string.IsNullOrWhiteSpace(setting?.FacebookUrl) ? "#" : setting!.FacebookUrl,
            ZaloUrl = string.IsNullOrWhiteSpace(setting?.ZaloUrl) ? "#" : setting!.ZaloUrl,
            TiktokUrl = string.IsNullOrWhiteSpace(setting?.TiktokUrl) ? "#" : setting!.TiktokUrl,
            CompanyLegalName = "CÔNG TY TNHH KIG HOLDING VIỆT NAM",
            BusinessRegistrationNumber = "Mã số DN: 0123456789",
            QrPlaceholderText = "QR",
            AppStorePlaceholderText = "App Store",
            GooglePlayPlaceholderText = "Google Play",
            OpeningHours = BuildOpeningHours(branches),
            QuickLinks = CreateFooterNavItems(),
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

    private static NavItemViewModel CreateNavItem(string label, string url, string currentPath, bool exact = false, IReadOnlyList<NavItemViewModel>? children = null)
    {
        var hasChildren = children is { Count: > 0 };
        var ownMatch = !string.IsNullOrWhiteSpace(url) && (exact
            ? string.Equals(currentPath, url, StringComparison.OrdinalIgnoreCase)
            : currentPath.StartsWith(url, StringComparison.OrdinalIgnoreCase));
        var isActive = ownMatch || (hasChildren && children!.Any(child => child.IsActive));

        return new NavItemViewModel
        {
            Label = label,
            Url = url,
            IsActive = isActive,
            Children = children ?? []
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

