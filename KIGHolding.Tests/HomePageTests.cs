using System.Text.RegularExpressions;
using KIGHolding.Controllers;
using KIGHolding.Models;
using KIGHolding.Models.Entities;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using KIGHolding.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIGHolding.Tests;

public sealed class HomePageTests
{
    private const string QrPath = "~/images/general/kig-holding-qr-app.webp";
    private const string AppStoreBadgePath = "~/images/general/icons/app-store-download.svg";
    private const string GooglePlayBadgePath = "~/images/general/icons/google-play-download.svg";
    private const string AppStoreUrl = "https://apps.apple.com/vn/app/truy%E1%BB%81n-thuy%E1%BA%BFt-champong-%EC%A0%88%EC%84%A4%EC%9D%98%EC%A7%AC%EB%BD%BB/id6753157313?l=vi";
    private const string GooglePlayUrl = "https://play.google.com/store/apps/details?id=com.eggstech.champong.app&pcampaignid=web_share";

    [Fact]
    public async Task Index_ReturnsDefaultViewEquivalentToHttpOk()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = string.Empty
            })
            .Build();
        var controller = new HomeController(
            new StubSiteSettingService(),
            new StubNewsService(),
            configuration,
            NullLogger<HomeController>.Instance);

        var result = await controller.Index(CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.Null(viewResult.ViewName);
    }

    [Fact]
    public void HomeView_TakeHomeCta_IsOnlyAppModalTrigger_AndKeepsDatBanFallback()
    {
        var view = ReadRepoFile("Views", "Home", "Index.cshtml");
        var section = ExtractTakeHomeSection(view);

        Assert.Single(Regex.Matches(view, "data-home-section=\"take-home\"", RegexOptions.IgnoreCase).Cast<Match>());
        Assert.Single(Regex.Matches(section, "data-home-app-modal-trigger", RegexOptions.IgnoreCase).Cast<Match>());
        Assert.Contains("href=\"/dat-ban\"", section, StringComparison.Ordinal);
        Assert.Contains("aria-haspopup=\"dialog\"", section, StringComparison.Ordinal);
        Assert.Contains("aria-controls=\"home-app-modal\"", section, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Mở tùy chọn tải ứng dụng Truyền Thuyết Champong\"", section, StringComparison.Ordinal);
        Assert.Contains("Đặt ngay", section, StringComparison.Ordinal);
    }

    [Fact]
    public void HomeView_RendersSingleStaticAppModal_WithAccessibleDialogSemantics()
    {
        var view = ReadRepoFile("Views", "Home", "Index.cshtml");

        Assert.Single(Regex.Matches(view, "id=\"home-app-modal\"", RegexOptions.IgnoreCase).Cast<Match>());
        Assert.Contains("class=\"home-app-modal\"", view, StringComparison.Ordinal);
        Assert.Contains("data-home-app-modal", view, StringComparison.Ordinal);
        Assert.Contains("aria-hidden=\"true\"", view, StringComparison.Ordinal);
        Assert.Contains("hidden>", view, StringComparison.Ordinal);
        Assert.Contains("data-home-app-modal-backdrop", view, StringComparison.Ordinal);
        Assert.Contains("data-home-app-modal-dialog", view, StringComparison.Ordinal);
        Assert.Contains("role=\"dialog\"", view, StringComparison.Ordinal);
        Assert.Contains("aria-modal=\"true\"", view, StringComparison.Ordinal);
        Assert.Contains("aria-labelledby=\"home-app-modal-title\"", view, StringComparison.Ordinal);
        Assert.Contains("aria-describedby=\"home-app-modal-description\"", view, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Đóng cửa sổ tải ứng dụng\"", view, StringComparison.Ordinal);
        Assert.Contains("Tiếp tục trên website", view, StringComparison.Ordinal);
    }

    [Fact]
    public void HomeView_ReusesFooterAppDownloadValuesExactly()
    {
        var view = ReadRepoFile("Views", "Home", "Index.cshtml");
        var footer = ReadRepoFile("Views", "Shared", "Partials", "_Footer.cshtml");

        Assert.Contains(QrPath, footer, StringComparison.Ordinal);
        Assert.Contains(AppStoreBadgePath, footer, StringComparison.Ordinal);
        Assert.Contains(GooglePlayBadgePath, footer, StringComparison.Ordinal);
        Assert.Contains(AppStoreUrl, footer, StringComparison.Ordinal);
        Assert.Contains(GooglePlayUrl, footer, StringComparison.Ordinal);

        Assert.Contains(QrPath, view, StringComparison.Ordinal);
        Assert.Contains(AppStoreBadgePath, view, StringComparison.Ordinal);
        Assert.Contains(GooglePlayBadgePath, view, StringComparison.Ordinal);
        Assert.Contains(AppStoreUrl, view, StringComparison.Ordinal);
        Assert.Contains(GooglePlayUrl, view, StringComparison.Ordinal);
        Assert.Contains("target=\"_blank\"", view, StringComparison.Ordinal);
        Assert.True(Regex.Matches(view, "rel=\"noopener noreferrer\"", RegexOptions.IgnoreCase).Count >= 2);
        Assert.Contains("aria-label=\"Tải ứng dụng trên App Store\"", view, StringComparison.Ordinal);
        Assert.Contains("aria-label=\"Tải ứng dụng trên Google Play\"", view, StringComparison.Ordinal);
    }

    [Fact]
    public void HomeView_LoadsPageScopedModalScript_WithoutChangingOtherDatBanLinks()
    {
        var view = ReadRepoFile("Views", "Home", "Index.cshtml");
        var bookingSection = ExtractSectionByDataAttribute(view, "booking");

        Assert.Contains("~/js/home-app-modal.js", view, StringComparison.Ordinal);
        Assert.Contains("asp-append-version=\"true\"", view, StringComparison.Ordinal);
        Assert.Contains("href=\"/dat-ban\"", bookingSection, StringComparison.Ordinal);
        Assert.DoesNotContain("data-home-app-modal-trigger", bookingSection, StringComparison.Ordinal);
        Assert.True(Regex.Matches(view, "href=\"/dat-ban\"", RegexOptions.IgnoreCase).Count >= 3);
    }

    [Fact]
    public void HomeAppModalScript_ImplementsCoordinationFocusAndKeyboardBehavior()
    {
        var script = ReadRepoFile("wwwroot", "js", "home-app-modal.js");

        Assert.Contains("data-home-app-modal-bound", script, StringComparison.Ordinal);
        Assert.Contains("[data-home-app-modal-trigger]", script, StringComparison.Ordinal);
        Assert.Contains("[data-booking-modal]", script, StringComparison.Ordinal);
        Assert.Contains("classList.contains('is-open')", script, StringComparison.Ordinal);
        Assert.Contains("kig:booking-modal:suppress-auto-open", script, StringComparison.Ordinal);
        Assert.Contains("new CustomEvent", script, StringComparison.Ordinal);
        Assert.Contains("home-app-modal-open", script, StringComparison.Ordinal);
        Assert.Contains("preventScroll", script, StringComparison.Ordinal);
        Assert.Contains("Escape", script, StringComparison.Ordinal);
        Assert.Contains("event.key !== 'Tab'", script, StringComparison.Ordinal);
        Assert.Contains("activeTrigger", script, StringComparison.Ordinal);
        Assert.Contains("[data-mobile-menu-close]", script, StringComparison.Ordinal);
        Assert.Contains("[data-floating-contact].is-open", script, StringComparison.Ordinal);
        Assert.Contains("prefers-reduced-motion", script, StringComparison.Ordinal);
    }

    [Fact]
    public void BookingModalScript_SuppressesAutoOpenWithoutRemovingManualTriggerFlow()
    {
        var script = ReadRepoFile("wwwroot", "js", "booking-modal.js");

        Assert.Contains("let autoOpenSuppressed = false", script, StringComparison.Ordinal);
        Assert.Contains("const suppressAutoOpen", script, StringComparison.Ordinal);
        Assert.Contains("kig:booking-modal:suppress-auto-open", script, StringComparison.Ordinal);
        Assert.Contains("markAutoShownThisSession();", script, StringComparison.Ordinal);
        Assert.Contains("document.body.classList.contains('home-app-modal-open')", script, StringComparison.Ordinal);
        Assert.Contains("source: 'manual'", script, StringComparison.Ordinal);
        Assert.Contains("data-booking-modal-trigger", script, StringComparison.Ordinal);
        Assert.Contains("form.addEventListener('submit'", script, StringComparison.Ordinal);
    }

    [Fact]
    public void HomeAppModalCss_IsScopedAndDoesNotUseMemberOrBookingSelectors()
    {
        var css = ReadRepoFile("wwwroot", "css", "input.css");

        Assert.Contains(".home-app-modal {", css, StringComparison.Ordinal);
        Assert.Contains(".home-app-modal__backdrop", css, StringComparison.Ordinal);
        Assert.Contains(".home-app-modal__dialog", css, StringComparison.Ordinal);
        Assert.Contains(".home-app-modal__qr", css, StringComparison.Ordinal);
        Assert.Contains(".home-app-modal__stores", css, StringComparison.Ordinal);
        Assert.Contains("body.home-app-modal-open", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".member-tier .home-app-modal", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".booking-modal .home-app-modal", css, StringComparison.Ordinal);
    }

    [Fact]
    public void FooterAndMemberAppPromotion_DoNotContainHomepageModalMarkup()
    {
        var footer = ReadRepoFile("Views", "Shared", "Partials", "_Footer.cshtml");
        var memberView = ReadRepoFile("Views", "Member", "Index.cshtml");

        Assert.DoesNotContain("home-app-modal", footer, StringComparison.Ordinal);
        Assert.DoesNotContain("data-home-app-modal", footer, StringComparison.Ordinal);
        Assert.DoesNotContain("home-app-modal", memberView, StringComparison.Ordinal);
        Assert.Contains("member-app-promo", memberView, StringComparison.Ordinal);
        Assert.Contains("member-tier-tabs.js", memberView, StringComparison.Ordinal);
    }

    private static string ExtractTakeHomeSection(string view)
        => ExtractSectionByDataAttribute(view, "take-home");

    private static string ExtractSectionByDataAttribute(string view, string dataAttributeValue)
    {
        var start = view.IndexOf($"data-home-section=\"{dataAttributeValue}\"", StringComparison.Ordinal);
        Assert.True(start >= 0, $"Expected data-home-section=\"{dataAttributeValue}\".");

        var sectionStart = view.LastIndexOf("<section", start, StringComparison.Ordinal);
        var sectionEnd = view.IndexOf("</section>", start, StringComparison.Ordinal);
        Assert.True(sectionStart >= 0);
        Assert.True(sectionEnd > sectionStart);

        return view[sectionStart..(sectionEnd + "</section>".Length)];
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

    private sealed class StubSiteSettingService : ISiteSettingService
    {
        public Task<SiteSetting?> GetSettingsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<SiteSetting?>(null);

        public void InvalidateCache()
        {
        }
    }

    private sealed class StubNewsService : INewsService
    {
        public Task<IReadOnlyList<PostCardViewModel>> GetPublishedPostCardsAsync(int? take = null, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PostCardViewModel>>([]);

        public Task<PagedResult<PostCardViewModel>> GetPublishedPostCardsPageAsync(string? category, int page, int pageSize, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Post?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<PostCardViewModel>> GetRelatedPostCardsAsync(string category, Guid excludePostId, int take = 3, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<SuggestedPostCategoryViewModel>> GetSuggestedCategoriesAsync(int limit = 4, int poolSize = 8, bool rotateWithinRecentPool = false, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public void InvalidateNewsCache()
        {
        }
    }
}
