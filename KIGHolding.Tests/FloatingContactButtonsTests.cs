using KIGHolding.Models.Entities;
using KIGHolding.Services;
using KIGHolding.ViewComponents;
using KIGHolding.ViewModels.Shared;
using Microsoft.AspNetCore.Mvc.ViewComponents;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIGHolding.Tests;

public sealed class FloatingContactButtonsTests
{
    [Fact]
    public async Task InvokeAsync_WithoutConfiguredDatabase_ReturnsExpectedBrandFallbacksInOrder()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = string.Empty
            })
            .Build();
        var siteSettingService = new StubSiteSettingService();
        var component = new FloatingContactButtonsViewComponent(
            siteSettingService,
            configuration,
            NullLogger<FloatingContactButtonsViewComponent>.Instance);

        var result = Assert.IsType<ViewViewComponentResult>(await component.InvokeAsync());
        var viewData = result.ViewData ?? throw new InvalidOperationException("Expected floating contact view data.");
        var model = Assert.IsType<FloatingContactButtonsViewModel>(viewData.Model);

        Assert.Equal(0, siteSettingService.CallCount);
        Assert.Equal("/dat-ban", model.ReservationUrl);
        Assert.Collection(
            model.Brands,
            brand => AssertBrand(
                brand,
                "KBB Cook",
                "/images/general/logo-kbbcook.webp",
                "https://www.facebook.com/kbbcook.buffet",
                "https://oa.zalo.me/779613344008241689",
                "floating-contact-fab__logo floating-contact-fab__logo--kbb"),
            brand => AssertBrand(
                brand,
                "Gogi Maru",
                "/images/general/logo-gogi-maru.webp",
                "https://www.facebook.com/gogimaru.bbq",
                "https://oa.zalo.me/1430731076415218116",
                "floating-contact-fab__logo floating-contact-fab__logo--gogi"),
            brand => AssertBrand(
                brand,
                "Seoul Gukbap",
                "/images/general/logo-seoul-gukbap.webp",
                "https://www.facebook.com/seoul.gukbap.sg",
                "https://oa.zalo.me/2150196343037356151",
                "floating-contact-fab__logo floating-contact-fab__logo--seoul"),
            brand => AssertBrand(
                brand,
                "Truyền Thuyết Champong",
                "/images/general/kig-no-bg-logo.png",
                "https://www.facebook.com/champong.official",
                "https://oa.zalo.me/3191309080595223416",
                "floating-contact-fab__logo floating-contact-fab__logo--kig"));

        var seoul = model.Brands[2];
        Assert.Contains("Seoul Gukbap", seoul.ToggleOpenLabel, StringComparison.Ordinal);
        Assert.Contains("Seoul Gukbap", seoul.ToggleCloseLabel, StringComparison.Ordinal);
    }

    private static void AssertBrand(
        FloatingBrandContactLinkViewModel brand,
        string name,
        string logoUrl,
        string facebookUrl,
        string zaloUrl,
        string logoClass)
    {
        Assert.Equal(name, brand.Name);
        Assert.Equal(logoUrl, brand.LogoUrl);
        Assert.Equal(facebookUrl, brand.FacebookUrl);
        Assert.Equal(zaloUrl, brand.ZaloUrl);
        Assert.Equal($"Mở liên hệ nhanh {name}", brand.ToggleOpenLabel);
        Assert.Equal($"Đóng liên hệ nhanh {name}", brand.ToggleCloseLabel);
        Assert.Equal(logoClass, brand.LogoClass);
    }

    private sealed class StubSiteSettingService : ISiteSettingService
    {
        public int CallCount { get; private set; }

        public Task<SiteSetting?> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.FromResult<SiteSetting?>(null);
        }

        public void InvalidateCache()
        {
        }
    }
}
