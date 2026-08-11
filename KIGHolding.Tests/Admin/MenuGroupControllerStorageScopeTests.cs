using KIGHolding.Areas.Admin.Controllers;
using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIGHolding.Tests.Admin;

public sealed class MenuGroupControllerStorageScopeTests
{
    [Fact]
    public async Task Upload_UsesPersistedMenuGroupSlugForEveryFile()
    {
        await using var dbContext = CreateDbContext();
        var menuGroupId = Guid.NewGuid();
        dbContext.MenuGroups.Add(CreateMenuGroup(menuGroupId, "gogi-maru"));
        dbContext.MenuPageImages.Add(new MenuPageImage
        {
            Id = Guid.NewGuid(),
            MenuGroupId = menuGroupId,
            ImageUrl = "https://media.example.test/menu-pages/legacy.webp",
            AltText = "Existing",
            DisplayOrder = 3,
            IsPublished = true
        });
        await dbContext.SaveChangesAsync();

        var storage = new FakeImageStorageService();
        var controller = CreateController(dbContext, storage);

        var result = await controller.Upload(menuGroupId, new MenuPageImageUploadViewModel
        {
            ImageFiles =
            [
                CreateImage("client-folder-1.webp"),
                CreateImage("../news.webp")
            ],
            AltText = "Menu page",
            IsPublished = true
        });

        Assert.IsType<RedirectToRouteResult>(result);
        Assert.Equal(2, storage.Uploads.Count);
        Assert.All(storage.Uploads, upload => Assert.Equal("gogi-maru", upload.StorageScope));
        Assert.All(storage.Uploads, upload => Assert.Equal(ImageCategory.MenuPages, upload.Category));

        var savedImages = await dbContext.MenuPageImages
            .Where(x => x.MenuGroupId == menuGroupId)
            .OrderBy(x => x.DisplayOrder)
            .ToListAsync();
        Assert.Equal([3, 4, 5], savedImages.Select(x => x.DisplayOrder).ToArray());
        Assert.Contains(savedImages, x => x.ImageUrl.Contains("/menu-pages/gogi-maru/", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("../news")]
    [InlineData("Truyền Thuyết Champong")]
    public async Task Upload_WithInvalidPersistedSlug_UploadsNoObjectAndSavesNoImage(string persistedSlug)
    {
        await using var dbContext = CreateDbContext();
        var menuGroupId = Guid.NewGuid();
        dbContext.MenuGroups.Add(CreateMenuGroup(menuGroupId, persistedSlug));
        await dbContext.SaveChangesAsync();

        var storage = new FakeImageStorageService();
        var controller = CreateController(dbContext, storage);

        var result = await controller.Upload(menuGroupId, new MenuPageImageUploadViewModel
        {
            ImageFiles = [CreateImage("1.webp")],
            IsPublished = true
        });

        Assert.IsType<RedirectToRouteResult>(result);
        Assert.Empty(storage.Uploads);
        Assert.Empty(storage.Deletes);
        Assert.False(await dbContext.MenuPageImages.AnyAsync());
    }

    [Fact]
    public async Task Upload_PartialFailure_DeletesPreviouslyUploadedNestedObjects()
    {
        await using var dbContext = CreateDbContext();
        var menuGroupId = Guid.NewGuid();
        dbContext.MenuGroups.Add(CreateMenuGroup(menuGroupId, "kbb-cook"));
        await dbContext.SaveChangesAsync();

        var storage = new FakeImageStorageService { ThrowOnUploadCall = 2 };
        var controller = CreateController(dbContext, storage);

        var result = await controller.Upload(menuGroupId, new MenuPageImageUploadViewModel
        {
            ImageFiles = [CreateImage("1.webp"), CreateImage("2.webp")],
            IsPublished = true
        });

        Assert.IsType<RedirectToRouteResult>(result);
        Assert.Equal(2, storage.Uploads.Count);
        var firstUrl = Assert.Single(storage.Deletes);
        Assert.Contains("/menu-pages/kbb-cook/", firstUrl.ImageUrl, StringComparison.Ordinal);
        Assert.False(await dbContext.MenuPageImages.AnyAsync());
    }

    [Fact]
    public async Task Upload_DbSaveFailure_DeletesAllNewlyUploadedNestedObjects()
    {
        await using var dbContext = CreateFailingDbContext();
        var menuGroupId = Guid.NewGuid();
        dbContext.MenuGroups.Add(CreateMenuGroup(menuGroupId, "truyen-thuyet-champong"));
        await dbContext.SaveChangesAsync();
        dbContext.FailSaveChanges = true;

        var storage = new FakeImageStorageService();
        var controller = CreateController(dbContext, storage);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            controller.Upload(menuGroupId, new MenuPageImageUploadViewModel
            {
                ImageFiles = [CreateImage("1.webp"), CreateImage("2.webp")],
                IsPublished = true
            }));

        Assert.Equal(2, storage.Uploads.Count);
        Assert.Equal(2, storage.Deletes.Count);
        Assert.All(storage.Deletes, delete => Assert.Contains("/menu-pages/truyen-thuyet-champong/", delete.ImageUrl, StringComparison.Ordinal));
    }

    private static MenuGroupController CreateController(AppDbContext dbContext, IImageStorageService storage)
    {
        var httpContext = new DefaultHttpContext();
        var controller = new MenuGroupController(
            dbContext,
            storage,
            NullLogger<MenuGroupController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };

        return controller;
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private static FailingSaveDbContext CreateFailingDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new FailingSaveDbContext(options);
    }

    private static MenuGroup CreateMenuGroup(Guid id, string slug)
    {
        return new MenuGroup
        {
            Id = id,
            Name = "Test Menu Group",
            Slug = slug,
            ShortDescription = "Short",
            Description = "Description",
            IsPublished = true,
            DisplayOrder = 1
        };
    }

    private static IFormFile CreateImage(string fileName)
    {
        var bytes = new byte[] { 1, 2, 3, 4 };
        return new FormFile(new MemoryStream(bytes), 0, bytes.Length, "ImageFiles", fileName)
        {
            Headers = new HeaderDictionary(),
            ContentType = "image/webp"
        };
    }

    private sealed class FakeImageStorageService : IImageStorageService
    {
        public int ThrowOnUploadCall { get; init; }
        public List<UploadCall> Uploads { get; } = [];
        public List<DeleteCall> Deletes { get; } = [];

        public Task<string> UploadAsync(IFormFile file, ImageCategory category, CancellationToken cancellationToken = default)
        {
            return UploadAsync(file, category, storageScope: string.Empty, cancellationToken);
        }

        public Task<string> UploadAsync(IFormFile file, ImageCategory category, string storageScope, CancellationToken cancellationToken = default)
        {
            var call = new UploadCall(file.FileName, category, storageScope);
            Uploads.Add(call);

            if (ThrowOnUploadCall == Uploads.Count)
            {
                throw new InvalidOperationException("simulated upload failure");
            }

            return Task.FromResult($"https://media.example.test/menu-pages/{storageScope}/{Uploads.Count}-{Guid.NewGuid():N}.webp");
        }

        public Task DeleteAsync(string? imageUrlOrPath, ImageCategory category, CancellationToken cancellationToken = default)
        {
            Deletes.Add(new DeleteCall(imageUrlOrPath ?? string.Empty, category));
            return Task.CompletedTask;
        }
    }

    private sealed record UploadCall(string FileName, ImageCategory Category, string StorageScope);
    private sealed record DeleteCall(string ImageUrl, ImageCategory Category);

    private sealed class FailingSaveDbContext(DbContextOptions<AppDbContext> options) : AppDbContext(options)
    {
        public bool FailSaveChanges { get; set; }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            return FailSaveChanges
                ? throw new InvalidOperationException("simulated database save failure")
                : base.SaveChangesAsync(cancellationToken);
        }

        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
        {
            return FailSaveChanges
                ? throw new InvalidOperationException("simulated database save failure")
                : base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
