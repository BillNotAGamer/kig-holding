using System.Text.RegularExpressions;
using KIGHolding.Controllers;
using KIGHolding.Models;
using KIGHolding.Models.Content;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using KIGHolding.ViewModels.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIGHolding.Tests;

public sealed class BlogHtmlSeoPhase2Tests
{
    [Fact]
    public void NewsDetailView_RendersRawPostContentOnlyInsideHtmlModeBranch()
    {
        var view = ReadRepoFile("Views", "News", "Detail.cshtml");

        var modeBranchIndex = view.IndexOf("post.ContentMode == PostContentMode.Html", StringComparison.Ordinal);
        var rawIndex = view.IndexOf("@Html.Raw(post.Content)", StringComparison.Ordinal);

        Assert.True(modeBranchIndex >= 0, "Expected a ContentMode.Html branch.");
        Assert.True(rawIndex > modeBranchIndex, "Expected raw Post.Content rendering only after the Html mode check.");
        Assert.Contains("<p>@paragraph</p>", view, StringComparison.Ordinal);
        Assert.Contains("IBlogHtmlSanitizer", view, StringComparison.Ordinal);
        Assert.DoesNotContain("@Html.Raw(Model.Post.Content)", view, StringComparison.Ordinal);
    }

    [Fact]
    public void NewsDetailView_EmitsJsonLdThroughHeadSectionOnly()
    {
        var view = ReadRepoFile("Views", "News", "Detail.cshtml");

        Assert.Contains("@section Head", view, StringComparison.Ordinal);
        Assert.Contains("application/ld+json", view, StringComparison.Ordinal);
        Assert.Contains("@Html.Raw(Model.ArticleJsonLd)", view, StringComparison.Ordinal);
        Assert.DoesNotContain("@Html.Raw(Model.Seo", view, StringComparison.Ordinal);
        Assert.DoesNotContain("@Html.Raw(post.Seo", view, StringComparison.Ordinal);
    }

    [Fact]
    public async Task NewsDetail_UsesSeoFallbackHierarchyAndRobotsMetadata()
    {
        var post = CreatePublishedPost();
        post.SeoTitle = "SEO title";
        post.SeoDescription = "SEO description";
        post.OgTitle = null;
        post.OgDescription = "OG description";
        post.OgImageUrl = null;
        post.CanonicalUrl = null;
        post.RobotsIndex = false;
        post.RobotsFollow = true;

        var controller = CreateNewsController(new StubNewsService(post), configuredBaseUrl: "https://kig.example");

        var result = await controller.Detail(post.Slug, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<NewsDetailViewModel>(viewResult.Model);
        Assert.Equal("SEO title", model.SeoTitle);
        Assert.Equal("SEO description", model.SeoDescription);
        Assert.Equal("SEO title", model.OgTitle);
        Assert.Equal("OG description", model.OgDescription);
        Assert.Equal(post.ThumbnailUrl, model.OgImageUrl);
        Assert.Equal($"/tin-tuc/{post.Slug}", model.CanonicalUrl);
        Assert.Equal("noindex,follow", model.RobotsContent);
        Assert.Equal("article", controller.ViewData["OgType"]);
        Assert.Equal("noindex,follow", controller.ViewData["Robots"]);
    }

    [Fact]
    public async Task NewsDetail_UsesExplicitOgAndCanonicalFields()
    {
        var post = CreatePublishedPost();
        post.SeoTitle = null;
        post.SeoDescription = null;
        post.OgTitle = "Custom OG title";
        post.OgDescription = "Custom OG description";
        post.OgImageUrl = "https://cdn.example.com/og.jpg";
        post.CanonicalUrl = "https://example.com/canonical";
        post.RobotsIndex = true;
        post.RobotsFollow = false;

        var controller = CreateNewsController(new StubNewsService(post), configuredBaseUrl: "https://kig.example");

        var result = await controller.Detail(post.Slug, CancellationToken.None);

        var model = Assert.IsType<NewsDetailViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(post.Title, model.SeoTitle);
        Assert.Equal(post.Excerpt, model.SeoDescription);
        Assert.Equal("Custom OG title", model.OgTitle);
        Assert.Equal("Custom OG description", model.OgDescription);
        Assert.Equal("https://cdn.example.com/og.jpg", model.OgImageUrl);
        Assert.Equal("https://example.com/canonical", model.CanonicalUrl);
        Assert.Equal("index,nofollow", model.RobotsContent);
    }

    [Fact]
    public async Task NewsDetail_BlankSeoOverridesTrackCurrentTitleAndExcerptFallbacks()
    {
        var post = CreatePublishedPost();
        post.Title = "Title B";
        post.Excerpt = "Excerpt B";
        post.SeoTitle = null;
        post.SeoDescription = null;
        post.OgTitle = null;
        post.OgDescription = null;

        var controller = CreateNewsController(new StubNewsService(post), configuredBaseUrl: "https://kig.example");

        var result = await controller.Detail(post.Slug, CancellationToken.None);

        var model = Assert.IsType<NewsDetailViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("Title B", model.SeoTitle);
        Assert.Equal("Excerpt B", model.SeoDescription);
        Assert.Equal("Title B", model.OgTitle);
        Assert.Equal("Excerpt B", model.OgDescription);
    }

    [Fact]
    public async Task NewsDetail_ExplicitSeoOverridesRemainAuthoritativeOverTitleAndExcerpt()
    {
        var post = CreatePublishedPost();
        post.Title = "Title C";
        post.Excerpt = "Excerpt C";
        post.SeoTitle = "Custom SEO";
        post.SeoDescription = "Custom Description";
        post.OgTitle = null;
        post.OgDescription = null;

        var controller = CreateNewsController(new StubNewsService(post), configuredBaseUrl: "https://kig.example");

        var result = await controller.Detail(post.Slug, CancellationToken.None);

        var model = Assert.IsType<NewsDetailViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal("Custom SEO", model.SeoTitle);
        Assert.Equal("Custom Description", model.SeoDescription);
        Assert.Equal("Custom SEO", model.OgTitle);
        Assert.Equal("Custom Description", model.OgDescription);
    }

    [Fact]
    public async Task NewsDetail_JsonLdEscapesHostileTextForScriptContext()
    {
        var post = CreatePublishedPost();
        post.SeoTitle = "\"><script>alert(1)</script>";
        post.SeoDescription = "Description </script><script>alert(2)</script>";

        var controller = CreateNewsController(new StubNewsService(post), configuredBaseUrl: "https://kig.example");

        var result = await controller.Detail(post.Slug, CancellationToken.None);

        var model = Assert.IsType<NewsDetailViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.DoesNotContain("</script>", model.ArticleJsonLd, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<script", model.ArticleJsonLd, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("\\u003Cscript", model.ArticleJsonLd, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"author\"", model.ArticleJsonLd, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"publisher\"", model.ArticleJsonLd, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Layout_RendersRobotsTwitterArticleMetadataAndHeadSection()
    {
        var layout = ReadRepoFile("Views", "Shared", "_Layout.cshtml");

        Assert.Contains("ViewData[\"Robots\"]", layout, StringComparison.Ordinal);
        Assert.Contains("meta name=\"robots\"", layout, StringComparison.Ordinal);
        Assert.Contains("meta name=\"twitter:title\"", layout, StringComparison.Ordinal);
        Assert.Contains("meta name=\"twitter:description\"", layout, StringComparison.Ordinal);
        Assert.Contains("meta name=\"twitter:image\"", layout, StringComparison.Ordinal);
        Assert.Contains("article:published_time", layout, StringComparison.Ordinal);
        Assert.Contains("article:modified_time", layout, StringComparison.Ordinal);
        Assert.Contains("RenderSectionAsync(\"Head\"", layout, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Sitemap_ExcludesPublishedNoindexPosts()
    {
        var indexedPost = CreatePostCard("indexed", robotsIndex: true);
        var noindexPost = CreatePostCard("noindex", robotsIndex: false);
        var controller = CreateSeoController([indexedPost, noindexPost], configuredBaseUrl: "https://kig.example");

        var result = await controller.Sitemap(CancellationToken.None);

        var contentResult = Assert.IsType<ContentResult>(result);
        Assert.Contains("https://kig.example/tin-tuc/indexed", contentResult.Content, StringComparison.Ordinal);
        Assert.DoesNotContain("https://kig.example/tin-tuc/noindex", contentResult.Content, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminPostViews_UseSharedContentAndSeoPartials()
    {
        var createView = ReadRepoFile("Areas", "Admin", "Views", "Post", "Create.cshtml");
        var editView = ReadRepoFile("Areas", "Admin", "Views", "Post", "Edit.cshtml");

        Assert.Contains("<partial name=\"_ContentEditor\" model=\"Model\" />", createView, StringComparison.Ordinal);
        Assert.Contains("<partial name=\"_SeoEditor\" model=\"Model\" />", createView, StringComparison.Ordinal);
        Assert.Contains("<partial name=\"_ContentEditor\" model=\"Model\" />", editView, StringComparison.Ordinal);
        Assert.Contains("<partial name=\"_SeoEditor\" model=\"Model\" />", editView, StringComparison.Ordinal);
        Assert.Contains("~/js/admin-post-editor.js", createView, StringComparison.Ordinal);
        Assert.Contains("~/js/admin-post-editor.js", editView, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminContentEditorPartial_HasAccessibleTabsAndSingleCanonicalTextarea()
    {
        var partial = ReadRepoFile("Areas", "Admin", "Views", "Post", "_ContentEditor.cshtml");

        Assert.Contains("role=\"tablist\"", partial, StringComparison.Ordinal);
        Assert.Equal(2, Regex.Matches(partial, "role=\"tab\"").Count);
        Assert.Single(Regex.Matches(partial, "asp-for=\"ContentMode\"").Cast<Match>());
        Assert.Single(Regex.Matches(partial, "<textarea asp-for=\"Content\"").Cast<Match>());
        Assert.Contains("data-post-editor-mode-input", partial, StringComparison.Ordinal);
        Assert.Contains("data-post-editor-content", partial, StringComparison.Ordinal);
        Assert.Contains("Chế độ nâng cao", partial, StringComparison.Ordinal);
    }

    [Fact]
    public void AdminPostEditorScript_DoesNotMutateOrSanitizeContent()
    {
        var script = ReadRepoFile("wwwroot", "js", "admin-post-editor.js");

        Assert.Contains("window.confirm", script, StringComparison.Ordinal);
        Assert.Contains("ArrowRight", script, StringComparison.Ordinal);
        Assert.Contains("ArrowLeft", script, StringComparison.Ordinal);
        Assert.Contains("spellcheck", script, StringComparison.Ordinal);
        Assert.DoesNotContain(".innerHTML", script, StringComparison.Ordinal);
        Assert.DoesNotContain("DOMParser", script, StringComparison.Ordinal);
        Assert.DoesNotContain("sanitize", script, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void SeoMetadata_DoesNotIntroduceMetaKeywords()
    {
        var source = string.Join(
            Environment.NewLine,
            ReadRepoFile("Views", "Shared", "_Layout.cshtml"),
            ReadRepoFile("Views", "News", "Detail.cshtml"),
            ReadRepoFile("Controllers", "NewsController.cs"));

        Assert.DoesNotContain("meta name=\"keywords\"", source, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SeoKeywords", source, StringComparison.Ordinal);
        Assert.DoesNotContain("MetaKeywords", source, StringComparison.Ordinal);
    }

    [Fact]
    public void ArticleContentCss_IsScopedToPublicArticleContainer()
    {
        var css = ReadRepoFile("wwwroot", "css", "input.css");

        Assert.Contains(".article-content {", css, StringComparison.Ordinal);
        Assert.Contains(".article-content h2", css, StringComparison.Ordinal);
        Assert.Contains(".article-content table", css, StringComparison.Ordinal);
        Assert.Contains(".admin-post-editor", css, StringComparison.Ordinal);
        Assert.DoesNotContain("body .article-content", css, StringComparison.Ordinal);
    }

    private static NewsController CreateNewsController(StubNewsService newsService, string configuredBaseUrl)
    {
        var controller = new NewsController(
            newsService,
            BuildConfiguration(configuredBaseUrl),
            NullLogger<NewsController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = CreateHttpContext()
        };

        return controller;
    }

    private static SeoController CreateSeoController(IReadOnlyList<PostCardViewModel> posts, string configuredBaseUrl)
    {
        var controller = new SeoController(
            new StubMenuGroupService(),
            new StubNewsService(posts),
            BuildConfiguration(configuredBaseUrl),
            NullLogger<SeoController>.Instance);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = CreateHttpContext()
        };

        return controller;
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("kig.example");
        return httpContext;
    }

    private static IConfiguration BuildConfiguration(string configuredBaseUrl)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=kig_test",
                ["AppSettings:AppBaseUrl"] = configuredBaseUrl
            })
            .Build();
    }

    private static Post CreatePublishedPost()
    {
        return new Post
        {
            Id = Guid.NewGuid(),
            Title = "Demo title",
            Slug = "demo-title",
            Category = NewsCategories.Default.Slug,
            Excerpt = "Demo excerpt",
            Content = "<p>Demo</p>",
            ContentMode = PostContentMode.Html,
            ThumbnailUrl = "/uploads/posts/demo.jpg",
            IsPublished = true,
            PublishedAt = new DateTimeOffset(2026, 8, 11, 10, 0, 0, TimeSpan.Zero),
            CreatedAt = new DateTimeOffset(2026, 8, 11, 9, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 8, 12, 9, 0, 0, TimeSpan.Zero),
            RobotsIndex = true,
            RobotsFollow = true
        };
    }

    private static PostCardViewModel CreatePostCard(string slug, bool robotsIndex)
    {
        return new PostCardViewModel
        {
            Id = Guid.NewGuid(),
            Title = slug,
            Slug = slug,
            Url = $"/tin-tuc/{slug}",
            ImageUrl = "/uploads/posts/demo.jpg",
            Excerpt = "Excerpt",
            Category = NewsCategories.Default.DisplayName,
            PublishedAt = new DateTimeOffset(2026, 8, 11, 10, 0, 0, TimeSpan.Zero),
            UpdatedAt = new DateTimeOffset(2026, 8, 12, 9, 0, 0, TimeSpan.Zero),
            RobotsIndex = robotsIndex
        };
    }

    private static string ReadRepoFile(params string[] relativeSegments)
    {
        var path = Path.Combine(GetRepositoryRoot(), Path.Combine(relativeSegments));
        return File.ReadAllText(path);
    }

    private static string GetRepositoryRoot()
        => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private sealed class StubNewsService : INewsService
    {
        private readonly Post? _post;
        private readonly IReadOnlyList<PostCardViewModel> _postCards;

        public StubNewsService(Post post)
        {
            _post = post;
            _postCards = [];
        }

        public StubNewsService(IReadOnlyList<PostCardViewModel> postCards)
        {
            _postCards = postCards;
        }

        public Task<IReadOnlyList<PostCardViewModel>> GetPublishedPostCardsAsync(int? take = null, CancellationToken cancellationToken = default)
        {
            IReadOnlyList<PostCardViewModel> posts = take.HasValue && take.Value > 0
                ? _postCards.Take(take.Value).ToList()
                : _postCards;

            return Task.FromResult(posts);
        }

        public Task<PagedResult<PostCardViewModel>> GetPublishedPostCardsPageAsync(string? category, int page, int pageSize, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<Post?> GetPostBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => Task.FromResult(string.Equals(_post?.Slug, slug, StringComparison.OrdinalIgnoreCase) ? _post : null);

        public Task<IReadOnlyList<PostCardViewModel>> GetRelatedPostCardsAsync(string category, Guid excludePostId, int take = 3, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<PostCardViewModel>>([]);

        public Task<IReadOnlyList<SuggestedPostCategoryViewModel>> GetSuggestedCategoriesAsync(
            int limit = 4,
            int poolSize = 8,
            bool rotateWithinRecentPool = false,
            CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<SuggestedPostCategoryViewModel>>([]);

        public void InvalidateNewsCache()
        {
        }
    }

    private sealed class StubMenuGroupService : IMenuGroupService
    {
        public Task<IReadOnlyList<MenuGroup>> GetPublishedGroupsAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<MenuGroup>>([]);

        public Task<MenuGroup?> GetPublishedGroupBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<MenuPageImage>> GetPublishedImagesByGroupSlugAsync(string slug, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<IReadOnlyList<MenuGroup>> GetAllGroupsForAdminAsync(CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<MenuGroup?> GetGroupForAdminAsync(Guid id, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<MenuGroup?> GetGroupForAdminBySlugAsync(string slug, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
