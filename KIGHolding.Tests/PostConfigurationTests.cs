using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Tests;

public sealed class PostConfigurationTests
{
    [Fact]
    public void PostConfiguration_DefinesBlogHtmlAndSeoFoundation()
    {
        using var dbContext = CreateDbContext();
        var entityType = dbContext.Model.FindEntityType(typeof(Post));

        Assert.NotNull(entityType);
        Assert.Equal(PostContentMode.Visual, entityType.FindProperty(nameof(Post.ContentMode))?.GetDefaultValue());
        Assert.Equal(true, entityType.FindProperty(nameof(Post.RobotsIndex))?.GetDefaultValue());
        Assert.Equal(true, entityType.FindProperty(nameof(Post.RobotsFollow))?.GetDefaultValue());
        Assert.Equal(120, entityType.FindProperty(nameof(Post.FocusKeyword))?.GetMaxLength());
        Assert.Equal(500, entityType.FindProperty(nameof(Post.CanonicalUrl))?.GetMaxLength());
        Assert.Equal(180, entityType.FindProperty(nameof(Post.OgTitle))?.GetMaxLength());
        Assert.Equal(320, entityType.FindProperty(nameof(Post.OgDescription))?.GetMaxLength());
        Assert.Equal(500, entityType.FindProperty(nameof(Post.OgImageUrl))?.GetMaxLength());
    }

    [Fact]
    public void NewPost_DefaultsToVisualAndIndexable()
    {
        var post = new Post();

        Assert.Equal(PostContentMode.Visual, post.ContentMode);
        Assert.True(post.RobotsIndex);
        Assert.True(post.RobotsFollow);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }
}
