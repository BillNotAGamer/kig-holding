using KIGHolding.Services;

namespace KIGHolding.Tests.Storage;

public sealed class CloudflareR2ObjectKeyBuilderTests
{
    [Theory]
    [InlineData(ImageCategory.Branches, "branches/")]
    [InlineData(ImageCategory.Posts, "news/")]
    [InlineData(ImageCategory.MenuPages, "menu-pages/")]
    [InlineData(ImageCategory.MenuGroupCovers, "brands/")]
    public void BuildObjectKey_UsesConfiguredCategoryPrefix(ImageCategory category, string expectedPrefix)
    {
        var key = CloudflareR2ObjectKeyBuilder.BuildObjectKey(
            StorageTestHelpers.CreateR2Settings(),
            category,
            "My Image.WEBP",
            ".WEBP");

        Assert.StartsWith(expectedPrefix, key);
    }

    [Fact]
    public void BuildObjectKey_SanitizesFilename()
    {
        var key = CloudflareR2ObjectKeyBuilder.BuildObjectKey(
            StorageTestHelpers.CreateR2Settings(),
            ImageCategory.Posts,
            "Mon \u0110ac Biet !!!.jpg",
            ".jpg");

        Assert.StartsWith("news/mon-dac-biet-", key);
    }

    [Fact]
    public void BuildObjectKey_GeneratesDifferentKeysForRepeatedUploads()
    {
        var settings = StorageTestHelpers.CreateR2Settings();

        var first = CloudflareR2ObjectKeyBuilder.BuildObjectKey(settings, ImageCategory.MenuPages, "menu.png", ".png");
        var second = CloudflareR2ObjectKeyBuilder.BuildObjectKey(settings, ImageCategory.MenuPages, "menu.png", ".png");

        Assert.NotEqual(first, second);
    }

    [Theory]
    [InlineData("truyen-thuyet-champong")]
    [InlineData("gogi-maru")]
    public void TryNormalizeStorageScopeSegment_AcceptsValidSlug(string scope)
    {
        Assert.True(ImageStoragePathUtilities.TryNormalizeStorageScopeSegment(scope, out var normalized));
        Assert.Equal(scope, normalized);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("../news")]
    [InlineData("/absolute")]
    [InlineData("menu/group")]
    [InlineData("menu\\group")]
    [InlineData("https://example.test/menu")]
    [InlineData("Truyen Thuyet Champong")]
    [InlineData("Truyền Thuyết Champong")]
    public void TryNormalizeStorageScopeSegment_RejectsUnsafeScope(string scope)
    {
        Assert.False(ImageStoragePathUtilities.TryNormalizeStorageScopeSegment(scope, out _));
    }

    [Fact]
    public void BuildObjectKey_WithMenuPageScope_UsesNestedPrefix()
    {
        var key = CloudflareR2ObjectKeyBuilder.BuildObjectKey(
            StorageTestHelpers.CreateR2Settings(),
            ImageCategory.MenuPages,
            "1.png",
            ".png",
            "truyen-thuyet-champong");

        Assert.StartsWith("menu-pages/truyen-thuyet-champong/1-", key);
        Assert.False(key.StartsWith('/'));
        Assert.DoesNotContain("..", key);
        Assert.True(ImageStoragePathUtilities.IsSafeObjectKey(key));
        Assert.Equal(3, key.Split('/').Length);
    }

    [Fact]
    public void BuildObjectKey_WithMenuPageScope_PreservesFilenameGuidBehavior()
    {
        var settings = StorageTestHelpers.CreateR2Settings();

        var first = CloudflareR2ObjectKeyBuilder.BuildObjectKey(settings, ImageCategory.MenuPages, "Menu Page.PNG", ".PNG", "gogi-maru");
        var second = CloudflareR2ObjectKeyBuilder.BuildObjectKey(settings, ImageCategory.MenuPages, "Menu Page.PNG", ".PNG", "gogi-maru");

        Assert.StartsWith("menu-pages/gogi-maru/menu-page-", first);
        Assert.EndsWith(".png", first);
        Assert.NotEqual(first, second);
    }

    [Fact]
    public void BuildObjectKey_WithDifferentMenuPageScopes_UsesDifferentPrefixes()
    {
        var settings = StorageTestHelpers.CreateR2Settings();

        var first = CloudflareR2ObjectKeyBuilder.BuildObjectKey(settings, ImageCategory.MenuPages, "1.png", ".png", "gogi-maru");
        var second = CloudflareR2ObjectKeyBuilder.BuildObjectKey(settings, ImageCategory.MenuPages, "1.png", ".png", "kbb-cook");

        Assert.StartsWith("menu-pages/gogi-maru/", first);
        Assert.StartsWith("menu-pages/kbb-cook/", second);
    }

    [Theory]
    [InlineData(ImageCategory.Branches, "branches/branch-")]
    [InlineData(ImageCategory.Posts, "news/post-")]
    [InlineData(ImageCategory.MenuGroupCovers, "brands/menu-group-cover-")]
    public void BuildObjectKey_UnscopedNonMenuCategoriesRemainUnchanged(ImageCategory category, string expectedPrefix)
    {
        var key = CloudflareR2ObjectKeyBuilder.BuildObjectKey(
            StorageTestHelpers.CreateR2Settings(),
            category,
            string.Empty,
            ".webp");

        Assert.StartsWith(expectedPrefix, key);
    }

    [Fact]
    public void BuildObjectKey_RejectsScopeForNonMenuPageCategory()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CloudflareR2ObjectKeyBuilder.BuildObjectKey(
                StorageTestHelpers.CreateR2Settings(),
                ImageCategory.Branches,
                "branch.webp",
                ".webp",
                "gogi-maru"));
    }

    [Fact]
    public void BuildObjectKey_HasNoLeadingSlashOrTraversal()
    {
        var key = CloudflareR2ObjectKeyBuilder.BuildObjectKey(
            StorageTestHelpers.CreateR2Settings(),
            ImageCategory.Branches,
            "../branch.png",
            ".png");

        Assert.False(key.StartsWith('/'));
        Assert.DoesNotContain("..", key);
        Assert.True(ImageStoragePathUtilities.IsSafeObjectKey(key));
    }

    [Theory]
    [InlineData("jpg", ".jpg")]
    [InlineData(".JPEG", ".jpeg")]
    [InlineData(" .WEBP ", ".webp")]
    public void NormalizeExtension_NormalizesExtension(string input, string expected)
    {
        Assert.Equal(expected, CloudflareR2ObjectKeyBuilder.NormalizeExtension(input));
    }
}
