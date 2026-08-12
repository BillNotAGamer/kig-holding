using KIGHolding.Areas.Admin.Controllers;
using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models;
using KIGHolding.Models.Content;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using KIGHolding.ViewModels.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Tests.Admin;

public sealed class PostControllerHtmlSeoTests
{
    [Fact]
    public async Task Create_VisualPost_StoresPlainTextWithoutHtmlSanitizing()
    {
        using var dbContext = CreateDbContext();
        var sanitizer = new SpyBlogHtmlSanitizer();
        var controller = CreateController(dbContext, sanitizer);
        var model = CreateValidCreateModel();
        model.ContentMode = PostContentMode.Visual;
        model.Content = "  <p>Hello</p><script>alert(1)</script>  ";

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);
        var post = await dbContext.Posts.SingleAsync();
        Assert.Equal(PostContentMode.Visual, post.ContentMode);
        Assert.Equal("<p>Hello</p><script>alert(1)</script>", post.Content);
        Assert.Equal(0, sanitizer.SanitizeCallCount);
    }

    [Fact]
    public async Task Create_HtmlPost_SanitizesBeforePersisting()
    {
        using var dbContext = CreateDbContext();
        var sanitizer = new SpyBlogHtmlSanitizer();
        var controller = CreateController(dbContext, sanitizer);
        var model = CreateValidCreateModel();
        model.ContentMode = PostContentMode.Html;
        model.Content = "<p>Safe</p><img src=\"https://example.com/a.jpg\" onerror=\"alert(1)\"><script>alert(1)</script>";

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);
        var post = await dbContext.Posts.SingleAsync();
        Assert.Equal(PostContentMode.Html, post.ContentMode);
        Assert.Contains("<p>Safe</p>", post.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("src=\"https://example.com/a.jpg\"", post.Content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", post.Content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", post.Content, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(1, sanitizer.SanitizeCallCount);
    }

    [Fact]
    public async Task Create_HtmlPost_RejectsSanitizedEmptyContent()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidCreateModel();
        model.ContentMode = PostContentMode.Html;
        model.Content = "<script>alert(1)</script>";

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        Assert.True(controller.ModelState.ContainsKey(nameof(PostCreateViewModel.Content)));
        Assert.Empty(dbContext.Posts);
    }

    [Fact]
    public async Task Edit_HtmlPost_SanitizesMaliciousEdits()
    {
        using var dbContext = CreateDbContext();
        var post = SeedPost(dbContext, PostContentMode.Html, "<p>Original</p>");
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidEditModel(post);
        model.ContentMode = PostContentMode.Html;
        model.Content = "<h2>Updated</h2><img src=\"https://example.com/a.jpg\" onload=\"alert(1)\"><script>alert(1)</script>";

        var result = await controller.Edit(post.Id, model);

        Assert.IsType<RedirectToActionResult>(result);
        var updatedPost = await dbContext.Posts.SingleAsync();
        Assert.Equal(PostContentMode.Html, updatedPost.ContentMode);
        Assert.Contains("<h2>Updated</h2>", updatedPost.Content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onload", updatedPost.Content, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", updatedPost.Content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Edit_ExistingVisualPost_RemainsPlainTextCompatible()
    {
        using var dbContext = CreateDbContext();
        var post = SeedPost(dbContext, PostContentMode.Visual, "Plain text");
        var sanitizer = new SpyBlogHtmlSanitizer();
        var controller = CreateController(dbContext, sanitizer);
        var model = CreateValidEditModel(post);
        model.ContentMode = PostContentMode.Visual;
        model.Content = "  <h2>Looks like HTML</h2><script>alert(1)</script>  ";

        var result = await controller.Edit(post.Id, model);

        Assert.IsType<RedirectToActionResult>(result);
        var updatedPost = await dbContext.Posts.SingleAsync();
        Assert.Equal(PostContentMode.Visual, updatedPost.ContentMode);
        Assert.Equal("<h2>Looks like HTML</h2><script>alert(1)</script>", updatedPost.Content);
        Assert.Equal(0, sanitizer.SanitizeCallCount);
    }

    [Fact]
    public async Task Create_MapsSeoMetadataFields()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidCreateModel();
        model.FocusKeyword = "  lau ga  ";
        model.CanonicalUrl = "  https://example.com/tin-tuc/demo  ";
        model.OgTitle = "  OG title  ";
        model.OgDescription = "  OG description  ";
        model.OgImageUrl = "  /uploads/posts/og.jpg  ";
        model.RobotsIndex = false;
        model.RobotsFollow = true;

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);
        var post = await dbContext.Posts.SingleAsync();
        Assert.Equal("lau ga", post.FocusKeyword);
        Assert.Equal("https://example.com/tin-tuc/demo", post.CanonicalUrl);
        Assert.Equal("OG title", post.OgTitle);
        Assert.Equal("OG description", post.OgDescription);
        Assert.Equal("/uploads/posts/og.jpg", post.OgImageUrl);
        Assert.False(post.RobotsIndex);
        Assert.True(post.RobotsFollow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Create_BlankSeoTitle_PersistsNull(string? seoTitle)
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidCreateModel();
        model.SeoTitle = seoTitle;

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);
        var post = await dbContext.Posts.SingleAsync();
        Assert.Null(post.SeoTitle);
    }

    [Fact]
    public async Task Create_WhitespaceSeoTitle_PersistsNull()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidCreateModel();
        model.SeoTitle = "   ";

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);
        var post = await dbContext.Posts.SingleAsync();
        Assert.Null(post.SeoTitle);
    }

    [Fact]
    public async Task Create_ExplicitSeoTitle_PersistsTrimmedOverride()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidCreateModel();
        model.SeoTitle = "  Custom SEO title  ";

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);
        var post = await dbContext.Posts.SingleAsync();
        Assert.Equal("Custom SEO title", post.SeoTitle);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Create_BlankSeoDescription_PersistsNull(string? seoDescription)
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidCreateModel();
        model.SeoDescription = seoDescription;

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);
        var post = await dbContext.Posts.SingleAsync();
        Assert.Null(post.SeoDescription);
    }

    [Fact]
    public async Task Create_ExplicitSeoDescription_PersistsTrimmedOverride()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidCreateModel();
        model.SeoDescription = "  Custom SEO description  ";

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);
        var post = await dbContext.Posts.SingleAsync();
        Assert.Equal("Custom SEO description", post.SeoDescription);
    }

    [Fact]
    public async Task Create_BlankSeoOverrideFields_PersistNull()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidCreateModel();
        model.SeoTitle = " ";
        model.SeoDescription = " ";
        model.FocusKeyword = " ";
        model.CanonicalUrl = " ";
        model.OgTitle = " ";
        model.OgDescription = " ";
        model.OgImageUrl = " ";

        var result = await controller.Create(model);

        Assert.IsType<RedirectToActionResult>(result);
        var post = await dbContext.Posts.SingleAsync();
        Assert.Null(post.SeoTitle);
        Assert.Null(post.SeoDescription);
        Assert.Null(post.FocusKeyword);
        Assert.Null(post.CanonicalUrl);
        Assert.Null(post.OgTitle);
        Assert.Null(post.OgDescription);
        Assert.Null(post.OgImageUrl);
    }

    [Fact]
    public async Task EditGet_PopulatesContentModeAndSeoMetadata()
    {
        using var dbContext = CreateDbContext();
        var post = SeedPost(dbContext, PostContentMode.Html, "<p>Safe</p>");
        post.FocusKeyword = "keyword";
        post.CanonicalUrl = "https://example.com/post";
        post.OgTitle = "OG title";
        post.OgDescription = "OG description";
        post.OgImageUrl = "/uploads/posts/og.jpg";
        post.RobotsIndex = false;
        post.RobotsFollow = false;
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());

        var result = await controller.Edit(post.Id);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PostEditViewModel>(viewResult.Model);
        Assert.Equal(PostContentMode.Html, model.ContentMode);
        Assert.Equal("keyword", model.FocusKeyword);
        Assert.Equal("https://example.com/post", model.CanonicalUrl);
        Assert.Equal("OG title", model.OgTitle);
        Assert.Equal("OG description", model.OgDescription);
        Assert.Equal("/uploads/posts/og.jpg", model.OgImageUrl);
        Assert.False(model.RobotsIndex);
        Assert.False(model.RobotsFollow);
    }

    [Fact]
    public async Task EditPost_PersistsSeoMetadataChanges()
    {
        using var dbContext = CreateDbContext();
        var post = SeedPost(dbContext, PostContentMode.Visual, "Plain text");
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidEditModel(post);
        model.FocusKeyword = "keyword";
        model.CanonicalUrl = "/tin-tuc/custom";
        model.OgTitle = "OG title";
        model.OgDescription = "OG description";
        model.OgImageUrl = "https://example.com/og.jpg";
        model.RobotsIndex = false;
        model.RobotsFollow = false;

        var result = await controller.Edit(post.Id, model);

        Assert.IsType<RedirectToActionResult>(result);
        var updatedPost = await dbContext.Posts.SingleAsync();
        Assert.Equal("keyword", updatedPost.FocusKeyword);
        Assert.Equal("/tin-tuc/custom", updatedPost.CanonicalUrl);
        Assert.Equal("OG title", updatedPost.OgTitle);
        Assert.Equal("OG description", updatedPost.OgDescription);
        Assert.Equal("https://example.com/og.jpg", updatedPost.OgImageUrl);
        Assert.False(updatedPost.RobotsIndex);
        Assert.False(updatedPost.RobotsFollow);
    }

    [Fact]
    public async Task Edit_BlankSeoTitle_PreservesDynamicTitleFallback()
    {
        using var dbContext = CreateDbContext();
        var post = SeedPost(dbContext, PostContentMode.Visual, "Plain text");
        post.Title = "Title A";
        post.SeoTitle = null;
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidEditModel(post);
        model.Title = "Title B";
        model.SeoTitle = " ";

        var result = await controller.Edit(post.Id, model);

        Assert.IsType<RedirectToActionResult>(result);
        var updatedPost = await dbContext.Posts.SingleAsync();
        Assert.Equal("Title B", updatedPost.Title);
        Assert.Null(updatedPost.SeoTitle);
        Assert.Equal("Title B", updatedPost.SeoTitle ?? updatedPost.Title);
    }

    [Fact]
    public async Task Edit_ExplicitSeoTitle_RemainsAuthoritative()
    {
        using var dbContext = CreateDbContext();
        var post = SeedPost(dbContext, PostContentMode.Visual, "Plain text");
        post.Title = "Title B";
        post.SeoTitle = null;
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidEditModel(post);
        model.Title = "Title C";
        model.SeoTitle = "  Custom SEO  ";

        var result = await controller.Edit(post.Id, model);

        Assert.IsType<RedirectToActionResult>(result);
        var updatedPost = await dbContext.Posts.SingleAsync();
        Assert.Equal("Title C", updatedPost.Title);
        Assert.Equal("Custom SEO", updatedPost.SeoTitle);
        Assert.Equal("Custom SEO", updatedPost.SeoTitle ?? updatedPost.Title);
    }

    [Fact]
    public async Task Edit_BlankSeoDescription_PreservesDynamicExcerptFallback()
    {
        using var dbContext = CreateDbContext();
        var post = SeedPost(dbContext, PostContentMode.Visual, "Plain text");
        post.Excerpt = "Excerpt A";
        post.SeoDescription = null;
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidEditModel(post);
        model.Excerpt = "Excerpt B";
        model.SeoDescription = "";

        var result = await controller.Edit(post.Id, model);

        Assert.IsType<RedirectToActionResult>(result);
        var updatedPost = await dbContext.Posts.SingleAsync();
        Assert.Equal("Excerpt B", updatedPost.Excerpt);
        Assert.Null(updatedPost.SeoDescription);
        Assert.Equal("Excerpt B", updatedPost.SeoDescription ?? updatedPost.Excerpt);
    }

    [Fact]
    public async Task Edit_ExplicitSeoDescription_RemainsAuthoritative()
    {
        using var dbContext = CreateDbContext();
        var post = SeedPost(dbContext, PostContentMode.Visual, "Plain text");
        post.Excerpt = "Excerpt B";
        post.SeoDescription = null;
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidEditModel(post);
        model.Excerpt = "Excerpt C";
        model.SeoDescription = "  Custom Description  ";

        var result = await controller.Edit(post.Id, model);

        Assert.IsType<RedirectToActionResult>(result);
        var updatedPost = await dbContext.Posts.SingleAsync();
        Assert.Equal("Excerpt C", updatedPost.Excerpt);
        Assert.Equal("Custom Description", updatedPost.SeoDescription);
        Assert.Equal("Custom Description", updatedPost.SeoDescription ?? updatedPost.Excerpt);
    }

    [Fact]
    public async Task Edit_BlankSeoOverrideFields_PersistNull()
    {
        using var dbContext = CreateDbContext();
        var post = SeedPost(dbContext, PostContentMode.Visual, "Plain text");
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidEditModel(post);
        model.SeoTitle = " ";
        model.SeoDescription = " ";
        model.FocusKeyword = " ";
        model.CanonicalUrl = " ";
        model.OgTitle = " ";
        model.OgDescription = " ";
        model.OgImageUrl = " ";

        var result = await controller.Edit(post.Id, model);

        Assert.IsType<RedirectToActionResult>(result);
        var updatedPost = await dbContext.Posts.SingleAsync();
        Assert.Null(updatedPost.SeoTitle);
        Assert.Null(updatedPost.SeoDescription);
        Assert.Null(updatedPost.FocusKeyword);
        Assert.Null(updatedPost.CanonicalUrl);
        Assert.Null(updatedPost.OgTitle);
        Assert.Null(updatedPost.OgDescription);
        Assert.Null(updatedPost.OgImageUrl);
    }

    [Fact]
    public async Task Create_RejectsInvalidCanonicalUrl()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidCreateModel();
        model.CanonicalUrl = "javascript:alert(1)";

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        Assert.True(controller.ModelState.ContainsKey(nameof(PostCreateViewModel.CanonicalUrl)));
        Assert.Empty(dbContext.Posts);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(2)]
    [InlineData(999)]
    public async Task Create_RejectsUndefinedContentModeWithoutPersistence(int contentMode)
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidCreateModel();
        model.ContentMode = (PostContentMode)contentMode;

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        Assert.True(controller.ModelState.ContainsKey(nameof(PostCreateViewModel.ContentMode)));
        Assert.Empty(dbContext.Posts);
    }

    [Theory]
    [InlineData("https://example.com/path", true, "https://example.com/path")]
    [InlineData("http://example.com/path", true, "http://example.com/path")]
    [InlineData("/tin-tuc/test", true, "/tin-tuc/test")]
    [InlineData("javascript:alert(1)", false, null)]
    [InlineData("data:text/html,test", false, null)]
    [InlineData("file:///tmp/test", false, null)]
    [InlineData("//evil.example.com", false, null)]
    [InlineData("   https://example.com", true, "https://example.com")]
    public async Task Create_ValidatesCanonicalUrlMatrix(string canonicalUrl, bool expectedValid, string? expectedStoredValue)
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidCreateModel();
        model.CanonicalUrl = canonicalUrl;

        var result = await controller.Create(model);

        if (expectedValid)
        {
            Assert.IsType<RedirectToActionResult>(result);
            var post = await dbContext.Posts.SingleAsync();
            Assert.Equal(expectedStoredValue, post.CanonicalUrl);
            Assert.False(controller.ModelState.ContainsKey(nameof(PostCreateViewModel.CanonicalUrl)));
        }
        else
        {
            Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey(nameof(PostCreateViewModel.CanonicalUrl)));
            Assert.Empty(dbContext.Posts);
        }
    }

    [Fact]
    public async Task Create_RejectsUnsafeOgImageUrl()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidCreateModel();
        model.OgImageUrl = "data:image/png;base64,AAAA";

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        Assert.True(controller.ModelState.ContainsKey(nameof(PostCreateViewModel.OgImageUrl)));
        Assert.Empty(dbContext.Posts);
    }

    [Theory]
    [InlineData("https://example.com/path", true, "https://example.com/path")]
    [InlineData("http://example.com/path", false, null)]
    [InlineData("/tin-tuc/test", true, "/tin-tuc/test")]
    [InlineData("javascript:alert(1)", false, null)]
    [InlineData("data:text/html,test", false, null)]
    [InlineData("file:///tmp/test", false, null)]
    [InlineData("//evil.example.com", false, null)]
    [InlineData("   https://example.com", true, "https://example.com")]
    public async Task Create_ValidatesOgImageUrlMatrix(string ogImageUrl, bool expectedValid, string? expectedStoredValue)
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidCreateModel();
        model.OgImageUrl = ogImageUrl;

        var result = await controller.Create(model);

        if (expectedValid)
        {
            Assert.IsType<RedirectToActionResult>(result);
            var post = await dbContext.Posts.SingleAsync();
            Assert.Equal(expectedStoredValue, post.OgImageUrl);
            Assert.False(controller.ModelState.ContainsKey(nameof(PostCreateViewModel.OgImageUrl)));
        }
        else
        {
            Assert.IsType<ViewResult>(result);
            Assert.True(controller.ModelState.ContainsKey(nameof(PostCreateViewModel.OgImageUrl)));
            Assert.Empty(dbContext.Posts);
        }
    }

    [Fact]
    public async Task Create_HtmlPost_RejectsSanitizedOutputOverContentLimit()
    {
        using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidCreateModel();
        model.ContentMode = PostContentMode.Html;
        model.Content = string.Concat(Enumerable.Repeat("<a href=/ target=_blank>x</a>", 2_500));

        var result = await controller.Create(model);

        Assert.IsType<ViewResult>(result);
        Assert.True(controller.ModelState.ContainsKey(nameof(PostCreateViewModel.Content)));
        Assert.Empty(dbContext.Posts);
    }

    [Fact]
    public async Task Edit_HtmlPost_RejectsSanitizedOutputOverContentLimitWithoutOverwritingExistingContent()
    {
        using var dbContext = CreateDbContext();
        var post = SeedPost(dbContext, PostContentMode.Html, "<p>Original</p>");
        var controller = CreateController(dbContext, new SpyBlogHtmlSanitizer());
        var model = CreateValidEditModel(post);
        model.ContentMode = PostContentMode.Html;
        model.Content = string.Concat(Enumerable.Repeat("<a href=/ target=_blank>x</a>", 2_500));

        var result = await controller.Edit(post.Id, model);

        Assert.IsType<ViewResult>(result);
        Assert.True(controller.ModelState.ContainsKey(nameof(PostEditViewModel.Content)));
        var unchangedPost = await dbContext.Posts.SingleAsync();
        Assert.Equal("<p>Original</p>", unchangedPost.Content);
    }

    private static PostController CreateController(AppDbContext dbContext, IBlogHtmlSanitizer sanitizer)
    {
        var controller = new PostController(
            dbContext,
            new StubImageStorageService(),
            new StubNewsService(),
            sanitizer);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static PostCreateViewModel CreateValidCreateModel()
    {
        return new PostCreateViewModel
        {
            Title = "Demo post",
            Category = NewsCategories.Default.Slug,
            Excerpt = "Demo excerpt",
            Content = "Demo content",
            ContentMode = PostContentMode.Visual,
            PublishedAt = new DateTime(2026, 8, 11, 10, 0, 0),
            SeoTitle = "Demo SEO title",
            SeoDescription = "Demo SEO description",
            RobotsIndex = true,
            RobotsFollow = true
        };
    }

    private static PostEditViewModel CreateValidEditModel(Post post)
    {
        return new PostEditViewModel
        {
            Id = post.Id,
            Title = post.Title,
            Slug = post.Slug,
            Category = post.Category,
            Excerpt = post.Excerpt,
            Content = post.Content,
            ContentMode = post.ContentMode,
            PublishedAt = post.PublishedAt?.LocalDateTime,
            SeoTitle = post.SeoTitle,
            SeoDescription = post.SeoDescription,
            FocusKeyword = post.FocusKeyword,
            CanonicalUrl = post.CanonicalUrl,
            OgTitle = post.OgTitle,
            OgDescription = post.OgDescription,
            OgImageUrl = post.OgImageUrl,
            RobotsIndex = post.RobotsIndex,
            RobotsFollow = post.RobotsFollow
        };
    }

    private static Post SeedPost(AppDbContext dbContext, PostContentMode contentMode, string content)
    {
        var post = new Post
        {
            Title = "Existing post",
            Slug = $"existing-{Guid.NewGuid():N}",
            Category = NewsCategories.Default.Slug,
            Excerpt = "Existing excerpt",
            Content = content,
            ContentMode = contentMode,
            ThumbnailUrl = string.Empty,
            IsPublished = false,
            SeoTitle = "Existing SEO title",
            SeoDescription = "Existing SEO description",
            RobotsIndex = true,
            RobotsFollow = true
        };

        dbContext.Posts.Add(post);
        dbContext.SaveChanges();
        return post;
    }

    private sealed class SpyBlogHtmlSanitizer : IBlogHtmlSanitizer
    {
        private readonly BlogHtmlSanitizer _inner = new();

        public int SanitizeCallCount { get; private set; }

        public string Sanitize(string html)
        {
            SanitizeCallCount++;
            return _inner.Sanitize(html);
        }

        public bool ContainsMeaningfulContent(string html)
            => _inner.ContainsMeaningfulContent(html);
    }

    private sealed class StubImageStorageService : IImageStorageService
    {
        public Task<string> UploadAsync(IFormFile file, ImageCategory category, CancellationToken cancellationToken = default)
            => Task.FromResult("/uploads/posts/test.jpg");

        public Task<string> UploadAsync(IFormFile file, ImageCategory category, string storageScope, CancellationToken cancellationToken = default)
            => Task.FromResult("/uploads/posts/test.jpg");

        public Task DeleteAsync(string? imageUrlOrPath, ImageCategory category, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
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

        public Task<IReadOnlyList<SuggestedPostCategoryViewModel>> GetSuggestedCategoriesAsync(
            int limit = 4,
            int poolSize = 8,
            bool rotateWithinRecentPool = false,
            CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public void InvalidateNewsCache()
        {
        }
    }
}
