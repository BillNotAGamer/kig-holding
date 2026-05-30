using KIGHolding.Models.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KIGHolding.Data;

public class DbInitializer
{
    private static readonly DateTimeOffset SeedTime = new(2026, 4, 29, 0, 0, 0, TimeSpan.Zero);

    private static readonly Guid SiteSettingId = Guid.Parse("4a1afd6d-89e8-45b9-ad38-ae5353851d78");

    private static readonly Guid BranchDistrict1Id = Guid.Parse("965000c5-709a-4bea-a592-7a6910ce0ede");
    private static readonly Guid BranchGoVapId = Guid.Parse("16ffa04e-a45f-4c4d-bd85-f7ffd40afa47");
    private static readonly Guid BranchCauGiayId = Guid.Parse("d329b911-5bf2-4fb1-96f6-ffb09b3c7c33");

    private static readonly Guid ChampongMenuGroupId = Guid.Parse("a0d8dd4e-7d90-4282-bb9e-a3ca66a78c56");
    private static readonly Guid GogimaruMenuGroupId = Guid.Parse("2a8ec6cf-7707-4d3c-bf74-0c4a20cd5f70");
    private static readonly Guid KbbCookMenuGroupId = Guid.Parse("569b5831-f436-4438-882c-5123c65f9957");

    private readonly AppDbContext _dbContext;

    public DbInitializer(AppDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _dbContext.Database.MigrateAsync(cancellationToken);
        await SeedAsync(cancellationToken);
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        if (!await _dbContext.SiteSettings.AnyAsync(cancellationToken))
        {
            _dbContext.SiteSettings.Add(CreateSiteSetting());
        }

        if (!await _dbContext.AdminUsers.AnyAsync(cancellationToken))
        {
            _dbContext.AdminUsers.Add(CreateAdminUser());
        }

        await AddMissingBySlugAsync(_dbContext.Branches, CreateBranches(), cancellationToken);
        await AddMissingBySlugAsync(_dbContext.MenuGroups, CreateMenuGroups(), cancellationToken);
        await AddMissingByIdAsync(_dbContext.Reviews, CreateReviews(), cancellationToken);
        await AddMissingBySlugAsync(_dbContext.Posts, CreatePosts(), cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private static SiteSetting CreateSiteSetting()
    {
        return new SiteSetting
        {
            Id = SiteSettingId,
            SiteName = "Truyền Thuyết Champong",
            BrandName = "Truyền Thuyết Champong",
            Slogan = "Mì cay Hàn Quốc, nước dùng đậm vị, không gian tối hiện đại.",
            Hotline = "0909 888 777",
            Email = "truyenthuyetchamponghcm@gmail.com",
            FacebookUrl = "https://facebook.com/truyenthuyetchampong",
            ZaloUrl = "https://zalo.me/0909888777",
            TiktokUrl = "https://tiktok.com/@truyenthuyetchampong",
            Address = "Trung tâm TP. Hồ Chí Minh",
            GoogleMapUrl = "https://maps.google.com/?q=Truyen+Thuyet+Champong",
            LogoUrl = "/images/brand/logo.svg",
            FaviconUrl = "/favicon.ico",
            CreatedAt = SeedTime,
            UpdatedAt = SeedTime
        };
    }

    private static AdminUser CreateAdminUser()
    {
        var user = new AdminUser
        {
            Username = "admin",
            Role = "SuperAdmin",
            IsActive = true,
            CreatedAt = SeedTime,
            UpdatedAt = SeedTime
        };

        var passwordHasher = new PasswordHasher<AdminUser>();
        user.PasswordHash = passwordHasher.HashPassword(user, "Password123!");

        return user;
    }

    private static IReadOnlyList<Branch> CreateBranches()
    {
        return
        [
            new Branch
            {
                Id = BranchDistrict1Id,
                Name = "Truyền Thuyết Champong Quận 1",
                Slug = "champong-quan-1",
                Address = "12 Nguyễn Thiệp, Phường Bến Nghé",
                District = "Quận 1",
                City = "TP. Hồ Chí Minh",
                Hotline = "0909 888 701",
                Email = "quan1@truyenthuyetchampong.vn",
                OpeningTime = new TimeOnly(10, 0),
                ClosingTime = new TimeOnly(22, 30),
                Capacity = 86,
                AreaSquareMeters = 180,
                NumberOfFloors = 2,
                Description = "Chi nhánh trung tâm với không gian tối, bếp mở và các món mì cay đặc trưng.",
                ThumbnailUrl = "/images/branches/champong-quan-1.webp",
                GoogleMapUrl = "https://maps.google.com/?q=Truyen+Thuyet+Champong+Quan+1",
                SeoTitle = "Truyền Thuyết Champong Quận 1",
                SeoDescription = "Chi nhánh Truyền Thuyết Champong Quận 1 phục vụ mì cay, BBQ và món Hàn.",
                IsActive = true,
                DisplayOrder = 1,
                CreatedAt = SeedTime,
                UpdatedAt = SeedTime
            },
            new Branch
            {
                Id = BranchGoVapId,
                Name = "Truyền Thuyết Champong Gò Vấp",
                Slug = "champong-go-vap",
                Address = "88 Phan Văn Trị, Phường 10",
                District = "Gò Vấp",
                City = "TP. Hồ Chí Minh",
                Hotline = "0909 888 702",
                Email = "govap@truyenthuyetchampong.vn",
                OpeningTime = new TimeOnly(10, 0),
                ClosingTime = new TimeOnly(23, 0),
                Capacity = 120,
                AreaSquareMeters = 240,
                NumberOfFloors = 3,
                Description = "Không gian rộng cho nhóm bạn và gia đình, tập trung combo lẩu và BBQ.",
                ThumbnailUrl = "/images/branches/champong-go-vap.webp",
                GoogleMapUrl = "https://maps.google.com/?q=Truyen+Thuyet+Champong+Go+Vap",
                SeoTitle = "Truyền Thuyết Champong Gò Vấp",
                SeoDescription = "Nhà hàng Hàn Quốc cay nóng tại Gò Vấp với mì Champong, BBQ và lẩu.",
                IsActive = true,
                DisplayOrder = 2,
                CreatedAt = SeedTime,
                UpdatedAt = SeedTime
            },
            new Branch
            {
                Id = BranchCauGiayId,
                Name = "Truyền Thuyết Champong Cầu Giấy",
                Slug = "champong-cau-giay",
                Address = "25 Trần Thái Tông, Dịch Vọng Hậu",
                District = "Cầu Giấy",
                City = "Hà Nội",
                Hotline = "0909 888 703",
                Email = "caugiay@truyenthuyetchampong.vn",
                OpeningTime = new TimeOnly(10, 30),
                ClosingTime = new TimeOnly(22, 30),
                Capacity = 96,
                AreaSquareMeters = 210,
                NumberOfFloors = 2,
                Description = "Chi nhánh Hà Nội phục vụ mì cay nóng, cơm Hàn và đồ uống Hàn Quốc.",
                ThumbnailUrl = "/images/branches/champong-cau-giay.webp",
                GoogleMapUrl = "https://maps.google.com/?q=Truyen+Thuyet+Champong+Cau+Giay",
                SeoTitle = "Truyền Thuyết Champong Cầu Giấy",
                SeoDescription = "Truyền Thuyết Champong Cầu Giấy dành cho khách yêu món Hàn cay nóng.",
                IsActive = true,
                DisplayOrder = 3,
                CreatedAt = SeedTime,
                UpdatedAt = SeedTime
            }
        ];
    }

    private static IReadOnlyList<MenuGroup> CreateMenuGroups()
    {
        return
        [
            CreateMenuGroup(
                ChampongMenuGroupId,
                "Truyền Thuyết Champong",
                "truyen-thuyet-champong",
                "Thực đơn món Hàn đậm vị với các món chủ lực gắn liền với thương hiệu Truyền Thuyết Champong.",
                1),
            CreateMenuGroup(
                GogimaruMenuGroupId,
                "Gogimaru",
                "gogimaru",
                "Không gian thịt nướng Hàn Quốc với các lựa chọn nướng, lẩu và món ăn kèm phong phú.",
                2),
            CreateMenuGroup(
                KbbCookMenuGroupId,
                "KBB Cook",
                "kbb-cook",
                "Trải nghiệm BBQ Hàn Quốc hiện đại với nguyên liệu chọn lọc và thực đơn phù hợp cho nhóm.",
                3)
        ];
    }

    private static MenuGroup CreateMenuGroup(Guid id, string name, string slug, string shortDescription, int displayOrder)
    {
        return new MenuGroup
        {
            Id = id,
            Name = name,
            Slug = slug,
            ShortDescription = shortDescription,
            Description = null,
            CoverImageUrl = null,
            DisplayOrder = displayOrder,
            IsPublished = true,
            CreatedAt = SeedTime,
            UpdatedAt = SeedTime
        };
    }

    private static IReadOnlyList<Review> CreateReviews()
    {
        return
        [
            CreateReview("37ba5615-6d81-4489-9d8a-e00f28f5008d", "Minh Anh", 5, "Nước dùng Champong cay sâu, hải sản tươi và không gian rất hợp đi tối.", BranchDistrict1Id, 1),
            CreateReview("bdeb890c-ae01-4ecf-90ec-3b6def28f00e", "Hoàng Nam", 5, "Combo BBQ hợp nhóm, thịt ướp đậm và panchan refill nhanh.", BranchGoVapId, 2),
            CreateReview("aec280fe-9cf3-47e6-8d09-9d314e6249d0", "Thảo Vy", 4, "Mì tương đen ngon, sốt không quá ngọt. Sẽ quay lại thử lẩu.", BranchDistrict1Id, 3),
            CreateReview("f187093d-9ec6-4350-9453-6c461c46a3e5", "Quang Huy", 5, "Chi nhánh Cầu Giấy phục vụ nhanh, món cay đúng gu.", BranchCauGiayId, 4),
            CreateReview("1b9b8bcd-6cbf-4f93-9372-6d19b7844e69", "Lan Chi", 5, "Tokbokki phô mai và trà đào cam sả rất hợp khi đi cùng bạn bè.", null, 5)
        ];
    }

    private static Review CreateReview(string id, string customerName, int rating, string content, Guid? branchId, int order)
    {
        return new Review
        {
            Id = Guid.Parse(id),
            CustomerName = customerName,
            AvatarUrl = null,
            Rating = rating,
            Content = content,
            BranchId = branchId,
            IsVisible = true,
            DisplayOrder = order,
            CreatedAt = SeedTime
        };
    }

    private static IReadOnlyList<Post> CreatePosts()
    {
        return
        [
            CreatePost("aa5d0f52-724d-4fa7-a15e-ab20ce6fcc63", "Khai trương chi nhánh Champong Quận 1", "khai-truong-chi-nhanh-champong-q1", "Không gian tối hiện đại cùng thực đơn mì cay Hàn Quốc tại trung tâm thành phố.", "he-thong-chi-nhanh", 1),
            CreatePost("a8366e02-4227-4c3f-ba03-31e5e0dd1004", "Ưu đãi combo trưa Hàn Quốc", "uu-dai-combo-trua", "Combo trưa gọn nhẹ cho khách văn phòng yêu món Hàn cay nóng.", "khuyen-mai-uu-dai", 2),
            CreatePost("4a647b3d-ffa7-48ce-8c15-5f0df8fe738c", "Bí quyết nước dùng Champong đỏ cay", "bi-quyet-nuoc-dung-champong", "Nước dùng được nấu từ hải sản, rau củ và nền gia vị Hàn Quốc.", "menu-mon-moi", 3),
            CreatePost("885f08bc-fe9e-4eb7-bc52-7032d7463e33", "Món mới: BBQ gà phô mai", "mon-moi-bbq-ga-pho-mai", "Gà sốt cay phủ phô mai kéo sợi dành cho nhóm bạn.", "menu-mon-moi", 4),
            CreatePost("a65e5f3c-ffd1-40a6-9660-246d6c1d8e0c", "Lịch hoạt động dịp lễ", "lich-hoat-dong-le", "Cập nhật giờ mở cửa các chi nhánh trong những ngày cao điểm.", "tin-tuc-thong-bao", 5)
        ];
    }

    private static Post CreatePost(string id, string title, string slug, string excerpt, string category, int order)
    {
        return new Post
        {
            Id = Guid.Parse(id),
            Title = title,
            Slug = slug,
            Excerpt = excerpt,
            Content = $"{excerpt}\n\nBài viết chi tiết sẽ được đội ngũ nội dung cập nhật trong giai đoạn xây dựng trang tin tức.",
            ThumbnailUrl = $"/images/posts/{slug}.webp",
            Category = category,
            IsPublished = true,
            PublishedAt = SeedTime.AddDays(order),
            SeoTitle = $"{title} | Truyền Thuyết Champong",
            SeoDescription = excerpt,
            CreatedAt = SeedTime,
            UpdatedAt = SeedTime
        };
    }

    private async Task AddMissingBySlugAsync<TEntity>(
        DbSet<TEntity> dbSet,
        IReadOnlyList<TEntity> seeds,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var existingSlugs = await dbSet
            .Select(entity => EF.Property<string>(entity, "Slug"))
            .ToListAsync(cancellationToken);

        var slugSet = existingSlugs.ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var seed in seeds)
        {
            var slug = GetRequiredPropertyValue<string>(seed, "Slug");
            if (!slugSet.Contains(slug))
            {
                dbSet.Add(seed);
            }
        }
    }

    private async Task AddMissingByIdAsync<TEntity>(
        DbSet<TEntity> dbSet,
        IReadOnlyList<TEntity> seeds,
        CancellationToken cancellationToken)
        where TEntity : class
    {
        var existingIds = await dbSet
            .Select(entity => EF.Property<Guid>(entity, "Id"))
            .ToListAsync(cancellationToken);

        var idSet = existingIds.ToHashSet();

        foreach (var seed in seeds)
        {
            var id = GetRequiredPropertyValue<Guid>(seed, "Id");
            if (!idSet.Contains(id))
            {
                dbSet.Add(seed);
            }
        }
    }

    private static TValue GetRequiredPropertyValue<TValue>(object entity, string propertyName)
    {
        var property = entity.GetType().GetProperty(propertyName)
            ?? throw new InvalidOperationException($"Property '{propertyName}' was not found on {entity.GetType().Name}.");

        return (TValue)(property.GetValue(entity)
            ?? throw new InvalidOperationException($"Property '{propertyName}' on {entity.GetType().Name} is null."));
    }
}
