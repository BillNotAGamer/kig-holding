using System.Reflection;
using System.Text.RegularExpressions;
using KIGHolding.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace KIGHolding.Tests;

public sealed class MemberPageTests
{
    [Fact]
    public void Index_ReturnsDefaultView_WithApprovedSeoMetadata()
    {
        var controller = new MemberController();

        var result = controller.Index();

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewName);
        Assert.Equal("Chương trình thành viên Truyền Thuyết Champong | Tích điểm & Đặc quyền", controller.ViewData["Title"]);
        Assert.Equal("Khám phá chương trình thành viên Truyền Thuyết Champong với 4 hạng Đồng, Bạc, Vàng, Kim Cương, quyền lợi tích điểm, ưu đãi sinh nhật và quy tắc nâng hạng.", controller.ViewData["MetaDescription"]);
        Assert.Equal("/thanh-vien", controller.ViewData["CanonicalUrl"]);
        Assert.DoesNotContain("Sắp ra mắt", controller.ViewData["Title"]?.ToString(), StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MemberController_RetainsExpectedRoute()
    {
        var routeAttribute = typeof(MemberController).GetCustomAttribute<RouteAttribute>();
        var getAttribute = typeof(MemberController)
            .GetMethod(nameof(MemberController.Index))?
            .GetCustomAttribute<HttpGetAttribute>();

        Assert.NotNull(routeAttribute);
        Assert.Equal("thanh-vien", routeAttribute!.Template);
        Assert.NotNull(getAttribute);
        Assert.Equal(string.Empty, getAttribute!.Template);
    }

    [Fact]
    public void MemberView_HasExactlyOneH1_AndBirthdayTableSemantics()
    {
        var view = ReadRepoFile("Views", "Member", "Index.cshtml");
        var headingMatches = Regex.Matches(view, @"<h1\b", RegexOptions.IgnoreCase).Cast<Match>();

        Assert.Single(headingMatches);
        Assert.Contains("<table", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<caption>", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("<thead>", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scope=\"col\"", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("scope=\"row\"", view, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MemberView_ContainsAllApprovedTiers_Thresholds_AndRates()
    {
        var view = ReadRepoFile("Views", "Member", "Index.cshtml");

        Assert.Contains("Đồng (Bronze)", view, StringComparison.Ordinal);
        Assert.Contains("Bạc (Silver)", view, StringComparison.Ordinal);
        Assert.Contains("Vàng (Gold)", view, StringComparison.Ordinal);
        Assert.Contains("Kim Cương (Diamond)", view, StringComparison.Ordinal);
        Assert.Contains("Thành viên Đồng", view, StringComparison.Ordinal);
        Assert.Contains("Thành viên Bạc", view, StringComparison.Ordinal);
        Assert.Contains("Thành viên Vàng", view, StringComparison.Ordinal);
        Assert.Contains("Thành viên Kim Cương", view, StringComparison.Ordinal);

        Assert.Contains("0 – dưới 5 triệu", view, StringComparison.Ordinal);
        Assert.Contains("Từ 5 triệu", view, StringComparison.Ordinal);
        Assert.Contains("Từ 12 triệu", view, StringComparison.Ordinal);
        Assert.Contains("Từ 45 triệu", view, StringComparison.Ordinal);

        Assert.Contains("2%", view, StringComparison.Ordinal);
        Assert.Contains("4%", view, StringComparison.Ordinal);
        Assert.Contains("3% + 3%", view, StringComparison.Ordinal);
        Assert.Contains("5% + 5%", view, StringComparison.Ordinal);
    }

    [Fact]
    public void MemberView_RendersAccessibleTierTabs_WithBronzeSelectedByDefault()
    {
        var view = ReadRepoFile("Views", "Member", "Index.cshtml");
        var tabIdMatches = Regex.Matches(view, "id=\"member-tier-tab-[^\"]+\"", RegexOptions.IgnoreCase);
        var panelIdMatches = Regex.Matches(view, "id=\"member-tier-panel-[^\"]+\"", RegexOptions.IgnoreCase);
        var allIds = Regex.Matches(view, "id=\"member-tier-[^\"]+\"", RegexOptions.IgnoreCase)
            .Cast<Match>()
            .Select(match => match.Value)
            .ToArray();

        Assert.Equal(4, Regex.Matches(view, "role=\"tab\"", RegexOptions.IgnoreCase).Count);
        Assert.Equal(4, Regex.Matches(view, "role=\"tabpanel\"", RegexOptions.IgnoreCase).Count);
        Assert.Equal(4, tabIdMatches.Count);
        Assert.Equal(4, panelIdMatches.Count);
        Assert.Equal(allIds.Length, allIds.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.Contains("role=\"tablist\"", view, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("id=\"member-tier-tab-bronze\" type=\"button\" role=\"tab\" aria-selected=\"true\"", view, StringComparison.Ordinal);
        Assert.Contains("id=\"member-tier-tab-silver\" type=\"button\" role=\"tab\" aria-selected=\"false\"", view, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"member-tier-panel-diamond\"", view, StringComparison.Ordinal);
    }

    [Fact]
    public void MemberView_MergesDuplicatedTierGrids_AndUsesStaticProgressMessages()
    {
        var view = ReadRepoFile("Views", "Member", "Index.cshtml");

        Assert.DoesNotContain("member-program__rate-card", view, StringComparison.Ordinal);
        Assert.DoesNotContain("member-program__rate-grid", view, StringComparison.Ordinal);
        Assert.DoesNotContain("member-program__tier-card", view, StringComparison.Ordinal);
        Assert.DoesNotContain("member-program__tier-grid", view, StringComparison.Ordinal);
        Assert.Contains("Mốc tiếp theo: Hạng Bạc từ 5 triệu đồng", view, StringComparison.Ordinal);
        Assert.Contains("Mốc tiếp theo: Hạng Vàng từ 12 triệu đồng", view, StringComparison.Ordinal);
        Assert.Contains("Mốc tiếp theo: Hạng Kim Cương từ 45 triệu đồng", view, StringComparison.Ordinal);
        Assert.Contains("Hạng thành viên cao nhất", view, StringComparison.Ordinal);
        Assert.Contains("không phải tiến độ cá nhân của người dùng", view, StringComparison.Ordinal);
        Assert.DoesNotContain("Bạn cần 500 điểm", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bạn cần 1.200 điểm", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Bạn cần 4.500 điểm", view, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void MemberView_ReferencesApprovedAssets_AndPageSpecificScript()
    {
        var view = ReadRepoFile("Views", "Member", "Index.cshtml");

        Assert.Equal(4, Regex.Matches(view, "/images/member/kig-metallic-logo.webp", RegexOptions.IgnoreCase).Count);
        Assert.Contains("/images/member/benefit-gift.svg", view, StringComparison.Ordinal);
        Assert.Contains("/images/member/benefit-points.svg", view, StringComparison.Ordinal);
        Assert.Contains("/images/member/benefit-priority.svg", view, StringComparison.Ordinal);
        Assert.Contains("/images/member/benefit-discount.svg", view, StringComparison.Ordinal);
        Assert.Contains("member-tier-tabs.js", view, StringComparison.Ordinal);
        Assert.Contains("@section Scripts", view, StringComparison.Ordinal);
        Assert.True(Regex.Matches(view, "class=\"member-tier__benefit\"", RegexOptions.IgnoreCase).Count >= 12);
    }

    [Fact]
    public void MemberView_ContainsApprovedInvoiceExamples_AndGoldPointValue()
    {
        var view = ReadRepoFile("Views", "Member", "Index.cshtml");

        Assert.Equal(4, Regex.Matches(view, "Hóa đơn mẫu 1.000.000 VNĐ", RegexOptions.IgnoreCase).Count);
        Assert.Contains("Điểm tiêu dùng: chưa áp dụng", view, StringComparison.Ordinal);
        Assert.Contains("40.000 điểm", view, StringComparison.Ordinal);
        Assert.Contains("29.000 điểm", view, StringComparison.Ordinal);
        Assert.Contains("47.500 điểm", view, StringComparison.Ordinal);
    }

    [Fact]
    public void MemberView_ContainsValidity_TierProgress_Birthday_AndDowngradeRules()
    {
        var view = ReadRepoFile("Views", "Member", "Index.cshtml");

        Assert.Contains("24 tháng", view, StringComparison.Ordinal);
        Assert.Contains("ngày đầu tiên của tháng thứ 25", view, StringComparison.Ordinal);
        Assert.Contains("10.000 VNĐ = 1 điểm thăng hạng", view, StringComparison.Ordinal);
        Assert.Contains("12 tháng gần nhất", view, StringComparison.Ordinal);
        Assert.Contains("ngày 1 hàng tháng", view, StringComparison.Ordinal);
        Assert.Contains("Tuần được tính từ Thứ 2 đến Chủ nhật", view, StringComparison.Ordinal);
        Assert.Contains("4%</td>", view, StringComparison.Ordinal);
        Assert.Contains("8%</td>", view, StringComparison.Ordinal);
        Assert.Contains("6%</td>", view, StringComparison.Ordinal);
        Assert.Contains("10%</td>", view, StringComparison.Ordinal);
        Assert.Contains("tối đa 1 lần hạ hạng trong vòng 6 tháng", view, StringComparison.Ordinal);
    }

    [Fact]
    public void MemberView_RemovesPlaceholderAndCrossBrandMembershipClaims()
    {
        var view = ReadRepoFile("Views", "Member", "Index.cshtml");

        Assert.DoesNotContain("Sắp ra mắt", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Lộ trình ra mắt", view, StringComparison.Ordinal);
        Assert.DoesNotContain("Gogimaru", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("KBB Cook", view, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("kết nối trải nghiệm giữa", view, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Sitemap_StillContains_ThanhVienRoute()
    {
        var seoControllerSource = ReadRepoFile("Controllers", "SeoController.cs");

        Assert.Contains("\"/thanh-vien\"", seoControllerSource, StringComparison.Ordinal);
    }

    [Fact]
    public void ReservationSuccessView_StillUsesSharedEditorialButtons()
    {
        var successView = ReadRepoFile("Views", "Reservation", "Success.cshtml");
        var cssSource = ReadRepoFile("wwwroot", "css", "input.css");

        Assert.Contains("public-editorial-page__button public-editorial-page__button--primary", successView, StringComparison.Ordinal);
        Assert.Contains("public-editorial-page__button public-editorial-page__button--secondary", successView, StringComparison.Ordinal);
        Assert.Contains(".public-editorial-page__button {", cssSource, StringComparison.Ordinal);
        Assert.Contains(".public-editorial-page__button--primary {", cssSource, StringComparison.Ordinal);
        Assert.Contains(".public-editorial-page__button--secondary {", cssSource, StringComparison.Ordinal);
    }

    [Fact]
    public void MemberTierScript_ImplementsKeyboardNavigation_AndIdempotentBinding()
    {
        var script = ReadRepoFile("wwwroot", "js", "member-tier-tabs.js");

        Assert.Contains("data-member-tier-tabs-bound", script, StringComparison.Ordinal);
        Assert.Contains("ArrowRight", script, StringComparison.Ordinal);
        Assert.Contains("ArrowLeft", script, StringComparison.Ordinal);
        Assert.Contains("Home", script, StringComparison.Ordinal);
        Assert.Contains("End", script, StringComparison.Ordinal);
        Assert.Contains("scrollIntoView", script, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion", script, StringComparison.Ordinal);
        Assert.Contains("panel.hidden = !isActive", script, StringComparison.Ordinal);
    }

    [Fact]
    public void MemberView_RendersFooterSourcedAppPromotionAfterFinalCta()
    {
        const string qrPath = "~/images/general/kig-holding-qr-app.webp";
        const string appStoreBadgePath = "~/images/general/icons/app-store-download.svg";
        const string googlePlayBadgePath = "~/images/general/icons/google-play-download.svg";
        const string appStoreUrl = "https://apps.apple.com/vn/app/truy%E1%BB%81n-thuy%E1%BA%BFt-champong-%EC%A0%88%EC%84%A4%EC%9D%98%EC%A7%AC%EB%BD%BB/id6753157313?l=vi";
        const string googlePlayUrl = "https://play.google.com/store/apps/details?id=com.eggstech.champong.app&pcampaignid=web_share";
        var view = ReadRepoFile("Views", "Member", "Index.cshtml");
        var footer = ReadRepoFile("Views", "Shared", "Partials", "_Footer.cshtml");

        Assert.Single(Regex.Matches(view, "class=\"member-app-promo\"", RegexOptions.IgnoreCase).Cast<Match>());
        Assert.Contains(qrPath, footer, StringComparison.Ordinal);
        Assert.Contains(qrPath, view, StringComparison.Ordinal);
        Assert.Contains(appStoreBadgePath, footer, StringComparison.Ordinal);
        Assert.Contains(appStoreBadgePath, view, StringComparison.Ordinal);
        Assert.Contains(googlePlayBadgePath, footer, StringComparison.Ordinal);
        Assert.Contains(googlePlayBadgePath, view, StringComparison.Ordinal);
        Assert.Contains(appStoreUrl, footer, StringComparison.Ordinal);
        Assert.Contains(appStoreUrl, view, StringComparison.Ordinal);
        Assert.Contains(googlePlayUrl, footer, StringComparison.Ordinal);
        Assert.Contains(googlePlayUrl, view, StringComparison.Ordinal);

        var ctaIndex = view.IndexOf("public-editorial-page__section public-editorial-page__section--cta", StringComparison.Ordinal);
        var appPromoIndex = view.IndexOf("class=\"member-app-promo\"", StringComparison.Ordinal);
        var mainCloseIndex = view.IndexOf("</main>", StringComparison.Ordinal);

        Assert.True(ctaIndex >= 0);
        Assert.True(appPromoIndex > ctaIndex);
        Assert.True(mainCloseIndex > appPromoIndex);
        Assert.DoesNotContain("member-app-promo", footer, StringComparison.Ordinal);
    }

    [Fact]
    public void MemberView_AppPromotionHasAccessibleImagesLinksAndPreservesCorePageState()
    {
        var view = ReadRepoFile("Views", "Member", "Index.cshtml");

        Assert.Matches("<section class=\"member-app-promo\"[^>]+aria-labelledby=\"member-app-promo-title\"", view);
        Assert.Matches("<img src=\"~/images/general/kig-holding-qr-app.webp\"[\\s\\S]*?alt=\"[^\"]+\"", view);
        Assert.Contains("aria-label=\"Tải ứng dụng trên App Store\"", view, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Tải ứng dụng trên Google Play\"", view, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(view, "target=\"_blank\"", RegexOptions.IgnoreCase).Count);
        Assert.True(Regex.Matches(view, "rel=\"noopener noreferrer\"", RegexOptions.IgnoreCase).Count >= 2);
        Assert.Single(Regex.Matches(view, @"<h1\b", RegexOptions.IgnoreCase).Cast<Match>());
        Assert.Equal(4, Regex.Matches(view, "role=\"tab\"", RegexOptions.IgnoreCase).Count);
        Assert.Contains("id=\"member-tier-tab-bronze\" type=\"button\" role=\"tab\" aria-selected=\"true\"", view, StringComparison.Ordinal);
        Assert.Contains("member-tier-tabs.js", view, StringComparison.Ordinal);
        Assert.Contains("member-program__table", view, StringComparison.Ordinal);
        Assert.Contains("public-editorial-page__section public-editorial-page__section--cta", view, StringComparison.Ordinal);
        Assert.Contains("29.000", view, StringComparison.Ordinal);
    }

    private static string ReadRepoFile(params string[] relativeSegments)
    {
        var path = Path.Combine(GetRepositoryRoot(), Path.Combine(relativeSegments));
        return File.ReadAllText(path);
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }
}
