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

    private static readonly Guid ChampongCategoryId = Guid.Parse("0aefe79b-91ed-410a-a9e6-26bf762ffcf8");
    private static readonly Guid JajangCategoryId = Guid.Parse("6541c869-1189-4185-b3ee-b7e84ba65519");
    private static readonly Guid BbqCategoryId = Guid.Parse("dc9b6379-3f0b-4473-bb84-e9be70a9bae9");
    private static readonly Guid HotpotCategoryId = Guid.Parse("57bcdd44-0e17-4c0a-bd58-fd43b829ce73");
    private static readonly Guid KoreanRiceCategoryId = Guid.Parse("66bd46a4-de76-4bb5-8fd5-79ad47e833dd");
    private static readonly Guid ComboCategoryId = Guid.Parse("4e342495-cd19-4bc9-aa54-ebc8e33164c7");
    private static readonly Guid PanchanCategoryId = Guid.Parse("ad2c7f64-b917-46fd-bd2f-7f4422ab5fce");
    private static readonly Guid DrinkCategoryId = Guid.Parse("3906f5fe-dc9c-43bd-a9c7-89cc295b6c0a");

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
        await AddMissingBySlugAsync(_dbContext.MenuCategories, CreateMenuCategories(), cancellationToken);
        await AddMissingBySlugAsync(_dbContext.MenuItems, CreateMenuItems(), cancellationToken);
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
            Email = "hello@truyenthuyetchampong.vn",
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

    private static IReadOnlyList<MenuCategory> CreateMenuCategories()
    {
        return
        [
            CreateCategory(ChampongCategoryId, "Mì Champong", "mi-champong", 1),
            CreateCategory(JajangCategoryId, "Mì tương đen", "mi-tuong-den", 2),
            CreateCategory(BbqCategoryId, "BBQ", "bbq", 3),
            CreateCategory(HotpotCategoryId, "Lẩu", "lau", 4),
            CreateCategory(KoreanRiceCategoryId, "Cơm & món Hàn", "com-mon-han", 5),
            CreateCategory(ComboCategoryId, "Combo", "combo", 6),
            CreateCategory(PanchanCategoryId, "Panchan", "panchan", 7),
            CreateCategory(DrinkCategoryId, "Đồ uống", "do-uong", 8)
        ];
    }

    private static MenuCategory CreateCategory(Guid id, string name, string slug, int order)
    {
        return new MenuCategory
        {
            Id = id,
            Name = name,
            Slug = slug,
            Description = $"Nhóm món {name.ToLowerInvariant()} của Truyền Thuyết Champong.",
            ThumbnailUrl = $"/images/categories/{slug}.webp",
            IconUrl = $"/images/categories/icons/{slug}.svg",
            DisplayOrder = order,
            IsActive = true,
            CreatedAt = SeedTime,
            UpdatedAt = SeedTime
        };
    }

    private static IReadOnlyList<MenuItem> CreateMenuItems()
    {
        return
        [
            CreateMenuItem("7320c429-951d-4483-b6ba-2dd002ec6902", ChampongCategoryId, "Champong hải sản", "해물짬뽕", "champong-hai-san", "Nước dùng đỏ cay với tôm, mực và nghêu.", 149000, 4, 1, true, true),
            CreateMenuItem("264be533-6d56-45b9-a58e-aa0ffda87dfb", ChampongCategoryId, "Champong bò cay", null, "champong-bo-cay", "Mì cay cùng lát bò mềm và rau củ xào lửa lớn.", 159000, 4, 2, true, false),
            CreateMenuItem("dc28e221-4a29-4760-9591-6077eb96f075", ChampongCategoryId, "Champong gà nấm", null, "champong-ga-nam", "Vị cay vừa, nhiều nấm và thịt gà áp chảo.", 139000, 3, 3),
            CreateMenuItem("443dc786-62cd-4f6e-bfd5-01bac0ebdff8", ChampongCategoryId, "Champong đặc biệt", "특짬뽕", "champong-dac-biet", "Topping hải sản, bò, trứng và nước dùng cô đặc.", 189000, 5, 4, true, true),
            CreateMenuItem("ae8a4e37-b8d6-4061-bb4f-973303d87489", JajangCategoryId, "Mì tương đen truyền thống", "짜장면", "mi-tuong-den-truyen-thong", "Sốt tương đen Hàn Quốc nấu cùng thịt và hành tây.", 119000, 0, 1, false, true),
            CreateMenuItem("f3bebf3a-a6bf-4632-a500-865b8189762a", JajangCategoryId, "Mì tương đen hải sản", null, "mi-tuong-den-hai-san", "Sốt tương đen đậm vị với tôm, mực và rau củ.", 145000, 1, 2),
            CreateMenuItem("1de20b52-f331-40d5-90a3-25b312f4a54a", JajangCategoryId, "Jajang bap", null, "jajang-bap", "Cơm trắng nóng phủ sốt tương đen và trứng lòng đào.", 129000, 0, 3),
            CreateMenuItem("598fb186-de86-4e77-bdd4-b804ddf28041", BbqCategoryId, "BBQ ba chỉ heo", null, "bbq-ba-chi-heo", "Ba chỉ heo nướng than ăn kèm rau sống và sốt ssamjang.", 189000, 2, 1, false, true),
            CreateMenuItem("2c3d587e-cf4d-49c5-83a6-3d3240eed95e", BbqCategoryId, "BBQ bò sốt cay", null, "bbq-bo-sot-cay", "Bò ướp sốt cay ngọt, nướng nhanh trên lửa lớn.", 229000, 4, 2, true, true),
            CreateMenuItem("6a81a5ff-db18-4b50-923a-9673196babe3", BbqCategoryId, "BBQ gà phô mai", null, "bbq-ga-pho-mai", "Gà sốt cay phủ phô mai kéo sợi.", 179000, 3, 3, false, false, true),
            CreateMenuItem("0bb7c5ec-910f-4885-b885-f62fbf92230e", HotpotCategoryId, "Lẩu kimchi hải sản", null, "lau-kimchi-hai-san", "Nồi lẩu kimchi cay nồng với hải sản và đậu phụ.", 329000, 4, 1, true, false),
            CreateMenuItem("a6eee88c-71c8-4c98-8cf4-1cfd2d1a1444", HotpotCategoryId, "Lẩu bulgogi", null, "lau-bulgogi", "Nước lẩu ngọt thanh cùng bò bulgogi và nấm.", 349000, 1, 2),
            CreateMenuItem("1a140e16-a4ac-4476-b1d3-ab5978a3fbd9", KoreanRiceCategoryId, "Cơm trộn bibimbap", "비빔밥", "com-tron-bibimbap", "Cơm trộn Hàn Quốc với rau củ, bò và trứng.", 139000, 2, 1, false, true),
            CreateMenuItem("94a5138b-c246-4789-b4e7-75e9c7005a4c", KoreanRiceCategoryId, "Cơm gà sốt cay", null, "com-ga-sot-cay", "Cơm nóng với gà áp chảo và sốt cay Champong.", 129000, 3, 2),
            CreateMenuItem("b8859910-336a-4c3f-908b-1561a4c39921", KoreanRiceCategoryId, "Tokbokki phô mai", null, "tokbokki-pho-mai", "Bánh gạo cay phủ phô mai, dùng nóng tại bàn.", 119000, 3, 3),
            CreateMenuItem("8634b5bb-4989-40c1-9f2c-d3230dc86add", KoreanRiceCategoryId, "Kimbap bò", null, "kimbap-bo", "Kimbap cuộn bò, rau củ và mè rang.", 99000, 0, 4),
            CreateMenuItem("0c34bf38-4003-4e6b-be8c-dd4a4599e4cf", ComboCategoryId, "Combo Champong BBQ", null, "combo-champong-bbq", "Một mì Champong hải sản, BBQ ba chỉ và panchan.", 299000, 4, 1, true, true, false, true, 349000),
            CreateMenuItem("2a443b53-6d91-4275-a922-1400a46eddc1", ComboCategoryId, "Combo gia đình", null, "combo-gia-dinh", "Lẩu kimchi, BBQ bò, kimbap và đồ uống cho 4 người.", 699000, 3, 2, false, true, false, true, 789000),
            CreateMenuItem("05d7defe-428c-4434-b84e-8b104f0c4929", ComboCategoryId, "Combo trưa Hàn Quốc", null, "combo-trua-han-quoc", "Cơm trộn, mì tương đen và nước gạo Hàn Quốc.", 229000, 1, 3, false, false, true, true, 259000),
            CreateMenuItem("6b837eb5-e756-4250-a9e7-093c3fa391ab", PanchanCategoryId, "Kimchi cải thảo", null, "kimchi-cai-thao", "Kimchi cải thảo lên men, vị chua cay cân bằng.", 39000, 2, 1),
            CreateMenuItem("add48e63-7d78-4fb4-9fe6-dae4c25095b8", PanchanCategoryId, "Panchan mix", null, "panchan-mix", "Set món ăn kèm thay đổi theo ngày.", 59000, 1, 2),
            CreateMenuItem("9b1959ac-fe73-49b1-9e92-10aa29e34e64", DrinkCategoryId, "Trà đào cam sả", null, "tra-dao-cam-sa", "Trà trái cây thơm mát cân bằng vị cay.", 49000, 0, 1),
            CreateMenuItem("32c58f56-9e5b-42ad-8dec-9fc72b5e5071", DrinkCategoryId, "Nước gạo Hàn Quốc", null, "nuoc-gao-han-quoc", "Đồ uống gạo rang kiểu Hàn, dịu nhẹ.", 45000, 0, 2),
            CreateMenuItem("c55a1467-abb1-4ac5-b50a-86c8357ec28c", DrinkCategoryId, "Soju không cồn", null, "soju-khong-con", "Mocktail lấy cảm hứng từ soju trái cây.", 69000, 0, 3, false, false, true)
        ];
    }

    private static MenuItem CreateMenuItem(
        string id,
        Guid categoryId,
        string name,
        string? koreanName,
        string slug,
        string shortDescription,
        decimal price,
        int spicyLevel,
        int displayOrder,
        bool isSignature = false,
        bool isBestSeller = false,
        bool isNew = false,
        bool isCombo = false,
        decimal? originalPrice = null)
    {
        return new MenuItem
        {
            Id = Guid.Parse(id),
            CategoryId = categoryId,
            Name = name,
            KoreanName = koreanName,
            Slug = slug,
            ShortDescription = shortDescription,
            Description = $"{shortDescription} Chế biến theo phong cách Hàn Quốc cay nóng, phù hợp dùng tại nhà hàng.",
            Price = price,
            OriginalPrice = originalPrice,
            ThumbnailUrl = $"/images/menu/{slug}.webp",
            SpicyLevel = spicyLevel,
            ServingSize = isCombo ? "2-4 người" : "1 phần",
            Calories = null,
            IsSignature = isSignature,
            IsBestSeller = isBestSeller,
            IsNew = isNew,
            IsCombo = isCombo,
            IsAvailable = true,
            DisplayOrder = displayOrder,
            SeoTitle = $"{name} | Truyền Thuyết Champong",
            SeoDescription = shortDescription,
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
            CreatePost("aa5d0f52-724d-4fa7-a15e-ab20ce6fcc63", "Khai trương chi nhánh Champong Quận 1", "khai-truong-chi-nhanh-champong-q1", "Không gian tối hiện đại cùng thực đơn mì cay Hàn Quốc tại trung tâm thành phố.", "Tin tức", 1),
            CreatePost("a8366e02-4227-4c3f-ba03-31e5e0dd1004", "Ưu đãi combo trưa Hàn Quốc", "uu-dai-combo-trua", "Combo trưa gọn nhẹ cho khách văn phòng yêu món Hàn cay nóng.", "Khuyến mãi", 2),
            CreatePost("4a647b3d-ffa7-48ce-8c15-5f0df8fe738c", "Bí quyết nước dùng Champong đỏ cay", "bi-quyet-nuoc-dung-champong", "Nước dùng được nấu từ hải sản, rau củ và nền gia vị Hàn Quốc.", "Ẩm thực", 3),
            CreatePost("885f08bc-fe9e-4eb7-bc52-7032d7463e33", "Món mới: BBQ gà phô mai", "mon-moi-bbq-ga-pho-mai", "Gà sốt cay phủ phô mai kéo sợi dành cho nhóm bạn.", "Món mới", 4),
            CreatePost("a65e5f3c-ffd1-40a6-9660-246d6c1d8e0c", "Lịch hoạt động dịp lễ", "lich-hoat-dong-le", "Cập nhật giờ mở cửa các chi nhánh trong những ngày cao điểm.", "Tin tức", 5)
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
