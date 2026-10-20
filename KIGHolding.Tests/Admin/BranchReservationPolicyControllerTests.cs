using System.Reflection;
using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using KIGHolding.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIGHolding.Tests.Admin;

using AdminBaseController = KIGHolding.Areas.Admin.Controllers.AdminBaseController;
using AdminReservationController = KIGHolding.Areas.Admin.Controllers.ReservationController;

public sealed class BranchReservationPolicyControllerTests
{
    private static readonly TimeProvider TestTimeProvider =
        new FixedTimeProvider(DateTimeOffset.Parse("2026-08-31T17:00:00+00:00"));
    private static readonly DateOnly FutureDate = new(2026, 9, 20);

    [Fact]
    public void BranchPolicyActions_UseExistingAdminAuthorizationAndPostAntiForgery()
    {
        Assert.True(typeof(AdminBaseController).IsAssignableFrom(typeof(AdminReservationController)));
        Assert.NotNull(typeof(AdminBaseController).GetCustomAttribute<AuthorizeAttribute>());

        var post = typeof(AdminReservationController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method => method.Name == "BranchPolicy" &&
                method.GetParameters().FirstOrDefault()?.ParameterType == typeof(BranchReservationPolicyEditViewModel));

        Assert.NotNull(post.GetCustomAttribute<HttpPostAttribute>());
        Assert.NotNull(post.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public async Task BranchPolicyGet_ReturnsEligibleBranchesGlobalOverlayAndUnderlyingRuleWithoutMutation()
    {
        await using var dbContext = CreateDbContext();
        var activeBranch = CreateBranch("Trung tam");
        var inactiveBranch = CreateBranch("Tam dung", isActive: false);
        var rule = CreatePolicy(activeBranch.Id, FutureDate, BranchReservationRuleType.MinimumGuests, 6);
        dbContext.AddRange(activeBranch, inactiveBranch, rule, new BlockedReservationDate { Date = FutureDate });
        await dbContext.SaveChangesAsync();
        var originalVersion = rule.Version;

        var result = await CreateController(dbContext).BranchPolicy(
            activeBranch.Id, FutureDate.Year, FutureDate.Month, FutureDate, CancellationToken.None);

        var model = Assert.IsType<BranchReservationPolicyViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(activeBranch.Id, model.SelectedBranchId);
        Assert.Single(model.BranchOptions);
        Assert.Contains(FutureDate, model.GlobalBlockedDates);
        var returnedRule = Assert.Single(model.Policies);
        Assert.Equal(6, returnedRule.MinGuests);
        Assert.Equal(originalVersion, returnedRule.Version);
        Assert.Equal(originalVersion, (await dbContext.BranchReservationDatePolicies.SingleAsync()).Version);
    }

    [Theory]
    [InlineData(BranchPolicyAdminRuleType.Closed, 6)]
    [InlineData(BranchPolicyAdminRuleType.MinimumGuests, null)]
    [InlineData(BranchPolicyAdminRuleType.MinimumGuests, 0)]
    [InlineData(BranchPolicyAdminRuleType.MinimumGuests, 101)]
    [InlineData((BranchPolicyAdminRuleType)99, null)]
    public async Task BranchPolicyPost_RejectsInvalidRuleCombinations(
        BranchPolicyAdminRuleType ruleType,
        int? minGuests)
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        var result = await controller.BranchPolicy(CreateEditor(branch.Id, ruleType, minGuests), CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(await dbContext.BranchReservationDatePolicies.ToListAsync());
    }

    [Fact]
    public async Task BranchPolicyPost_RejectsInvalidBranchAndPastDate()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);
        var invalidBranch = CreateEditor(Guid.NewGuid(), BranchPolicyAdminRuleType.Closed);

        Assert.IsType<ViewResult>(await controller.BranchPolicy(invalidBranch, CancellationToken.None));
        Assert.False(controller.ModelState.IsValid);
        controller.ModelState.Clear();

        var branch = CreateBranch();
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();
        var invalidDate = CreateEditor(branch.Id, BranchPolicyAdminRuleType.Closed);
        invalidDate.Date = default;

        Assert.IsType<ViewResult>(await controller.BranchPolicy(invalidDate, CancellationToken.None));
        Assert.False(controller.ModelState.IsValid);
        controller.ModelState.Clear();

        var past = CreateEditor(branch.Id, BranchPolicyAdminRuleType.Closed);
        past.Date = new DateOnly(2026, 8, 31);

        Assert.IsType<ViewResult>(await controller.BranchPolicy(past, CancellationToken.None));
        Assert.False(controller.ModelState.IsValid);
        Assert.Empty(await dbContext.BranchReservationDatePolicies.ToListAsync());
    }

    [Fact]
    public async Task BranchPolicyPost_UpdatesOnlyRequestedBranchAndDate()
    {
        await using var dbContext = CreateDbContext();
        var branchA = CreateBranch("A");
        var branchB = CreateBranch("B");
        var policyA = CreatePolicy(branchA.Id, FutureDate, BranchReservationRuleType.Closed);
        var otherDate = CreatePolicy(branchA.Id, FutureDate.AddDays(1), BranchReservationRuleType.Closed);
        var policyB = CreatePolicy(branchB.Id, FutureDate, BranchReservationRuleType.Closed);
        dbContext.AddRange(branchA, branchB, policyA, otherDate, policyB);
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        var editor = CreateEditor(branchA.Id, BranchPolicyAdminRuleType.MinimumGuests, 6);
        editor.ExpectedVersion = policyA.Version;
        var result = await controller.BranchPolicy(editor, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(6, (await dbContext.BranchReservationDatePolicies.FindAsync(branchA.Id, FutureDate))!.MinGuests);
        Assert.Equal(BranchReservationRuleType.Closed,
            (await dbContext.BranchReservationDatePolicies.FindAsync(branchA.Id, FutureDate.AddDays(1)))!.RuleType);
        Assert.Equal(policyB.Version,
            (await dbContext.BranchReservationDatePolicies.FindAsync(branchB.Id, FutureDate))!.Version);
    }

    [Fact]
    public async Task BranchPolicyPost_StaleRevisionShowsConflictWithoutOverwrite()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        var policy = CreatePolicy(branch.Id, FutureDate, BranchReservationRuleType.Closed);
        dbContext.AddRange(branch, policy);
        await dbContext.SaveChangesAsync();
        var staleVersion = policy.Version;
        policy.RuleType = BranchReservationRuleType.MinimumGuests;
        policy.MinGuests = 8;
        policy.Version = Guid.NewGuid();
        await dbContext.SaveChangesAsync();

        var editor = CreateEditor(branch.Id, BranchPolicyAdminRuleType.MinimumGuests, 6);
        editor.ExpectedVersion = staleVersion;
        var result = await CreateController(dbContext).BranchPolicy(editor, CancellationToken.None);

        var model = Assert.IsType<BranchReservationPolicyViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.True(model.HasConcurrencyConflict);
        Assert.Equal(8, (await dbContext.BranchReservationDatePolicies.SingleAsync()).MinGuests);
    }

    [Fact]
    public async Task BranchPolicyPost_DeleteRecreateAbaShowsConflict()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        var oldPolicy = CreatePolicy(branch.Id, FutureDate, BranchReservationRuleType.Closed);
        dbContext.AddRange(branch, oldPolicy);
        await dbContext.SaveChangesAsync();
        var staleVersion = oldPolicy.Version;
        dbContext.Remove(oldPolicy);
        await dbContext.SaveChangesAsync();
        var recreated = CreatePolicy(branch.Id, FutureDate, BranchReservationRuleType.MinimumGuests, 7);
        dbContext.Add(recreated);
        await dbContext.SaveChangesAsync();

        var editor = CreateEditor(branch.Id, BranchPolicyAdminRuleType.Allowed);
        editor.ExpectedVersion = staleVersion;
        var result = await CreateController(dbContext).BranchPolicy(editor, CancellationToken.None);

        var model = Assert.IsType<BranchReservationPolicyViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.True(model.HasConcurrencyConflict);
        Assert.Equal(recreated.Version, (await dbContext.BranchReservationDatePolicies.SingleAsync()).Version);
    }

    [Fact]
    public async Task RestrictivePolicy_WithReservationsRequiresServerConfirmationThenPreservesReservations()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        dbContext.Branches.Add(branch);
        dbContext.Reservations.AddRange(
            CreateReservation(branch.Id, ReservationStatus.Pending),
            CreateReservation(branch.Id, ReservationStatus.Confirmed));
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext, new EphemeralDataProtectionProvider());
        var editor = CreateEditor(branch.Id, BranchPolicyAdminRuleType.Closed);

        var preview = await controller.BranchPolicy(editor, CancellationToken.None);

        var previewModel = Assert.IsType<BranchReservationPolicyViewModel>(Assert.IsType<ViewResult>(preview).Model);
        Assert.True(previewModel.RequiresImpactConfirmation);
        Assert.Equal(2, previewModel.ExistingReservationTotal);
        Assert.False(string.IsNullOrWhiteSpace(previewModel.Editor.ImpactConfirmationToken));
        Assert.Empty(await dbContext.BranchReservationDatePolicies.ToListAsync());

        controller.ModelState.Clear();
        editor.ConfirmExistingReservations = true;
        editor.ImpactConfirmationToken = previewModel.Editor.ImpactConfirmationToken;
        var confirmed = await controller.BranchPolicy(editor, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(confirmed);
        Assert.Single(await dbContext.BranchReservationDatePolicies.ToListAsync());
        Assert.Equal(2, await dbContext.Reservations.CountAsync());
    }

    [Fact]
    public async Task RestrictivePolicy_CraftedOrStaleConfirmationCannotMutatePolicy()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        dbContext.Branches.Add(branch);
        dbContext.Reservations.Add(CreateReservation(branch.Id, ReservationStatus.Pending));
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext, new EphemeralDataProtectionProvider());
        var crafted = CreateEditor(branch.Id, BranchPolicyAdminRuleType.Closed);
        crafted.ConfirmExistingReservations = true;
        crafted.ImpactConfirmationToken = "not-a-valid-protected-token";

        var craftedResult = await controller.BranchPolicy(crafted, CancellationToken.None);
        var refreshed = Assert.IsType<BranchReservationPolicyViewModel>(Assert.IsType<ViewResult>(craftedResult).Model);
        Assert.True(refreshed.RequiresImpactConfirmation);
        Assert.Empty(await dbContext.BranchReservationDatePolicies.ToListAsync());

        controller.ModelState.Clear();
        dbContext.Reservations.Add(CreateReservation(branch.Id, ReservationStatus.Confirmed));
        await dbContext.SaveChangesAsync();
        crafted.ImpactConfirmationToken = refreshed.Editor.ImpactConfirmationToken;
        crafted.ConfirmExistingReservations = true;

        var staleResult = await controller.BranchPolicy(crafted, CancellationToken.None);
        var staleModel = Assert.IsType<BranchReservationPolicyViewModel>(Assert.IsType<ViewResult>(staleResult).Model);
        Assert.True(staleModel.RequiresImpactConfirmation);
        Assert.Equal(2, staleModel.ExistingReservationTotal);
        Assert.Empty(await dbContext.BranchReservationDatePolicies.ToListAsync());
    }

    [Fact]
    public async Task Allowed_RemovesMatchingPolicyWithoutChangingReservations()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        var policy = CreatePolicy(branch.Id, FutureDate, BranchReservationRuleType.Closed);
        dbContext.AddRange(branch, policy, CreateReservation(branch.Id, ReservationStatus.Pending));
        await dbContext.SaveChangesAsync();
        var editor = CreateEditor(branch.Id, BranchPolicyAdminRuleType.Allowed);
        editor.ExpectedVersion = policy.Version;

        var result = await CreateController(dbContext).BranchPolicy(editor, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(await dbContext.BranchReservationDatePolicies.ToListAsync());
        Assert.Single(await dbContext.Reservations.ToListAsync());
    }

    [Fact]
    public async Task UnexpectedPersistenceException_ReturnsSafeMessageWithoutProviderDetails()
    {
        await using var dbContext = CreateDbContext();
        var branch = CreateBranch();
        dbContext.Branches.Add(branch);
        await dbContext.SaveChangesAsync();
        var controller = CreateController(
            dbContext,
            branchPolicyService: new ThrowingMutationPolicyService());

        var result = await controller.BranchPolicy(
            CreateEditor(branch.Id, BranchPolicyAdminRuleType.Closed),
            CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        var errors = controller.ModelState.Values
            .SelectMany(value => value.Errors)
            .Select(error => error.ErrorMessage)
            .ToArray();
        Assert.Contains(errors, error => error.Contains("Không thể cập nhật", StringComparison.Ordinal));
        Assert.DoesNotContain(errors, error => error.Contains("provider-secret", StringComparison.Ordinal));
        Assert.Empty(await dbContext.BranchReservationDatePolicies.ToListAsync());
    }

    private static BranchReservationPolicyEditViewModel CreateEditor(
        Guid branchId,
        BranchPolicyAdminRuleType ruleType,
        int? minGuests = null)
    {
        return new BranchReservationPolicyEditViewModel
        {
            BranchId = branchId,
            Date = FutureDate,
            RuleType = ruleType,
            MinGuests = minGuests,
            CalendarYear = FutureDate.Year,
            CalendarMonth = FutureDate.Month
        };
    }

    private static Branch CreateBranch(string name = "Chi nhanh", bool isActive = true)
    {
        return new Branch
        {
            Name = name,
            Slug = $"branch-{Guid.NewGuid():N}",
            IsActive = isActive,
            AllowsReservations = true,
            OpeningTime = new TimeOnly(9, 0),
            ClosingTime = new TimeOnly(22, 0)
        };
    }

    private static BranchReservationDatePolicy CreatePolicy(
        Guid branchId,
        DateOnly date,
        BranchReservationRuleType ruleType,
        int? minGuests = null)
    {
        return new BranchReservationDatePolicy
        {
            BranchId = branchId,
            Date = date,
            RuleType = ruleType,
            MinGuests = minGuests,
            Version = Guid.NewGuid()
        };
    }

    private static Reservation CreateReservation(Guid branchId, ReservationStatus status)
    {
        return new Reservation
        {
            BranchId = branchId,
            CustomerName = "Khach thu nghiem",
            PhoneNumber = "0900000000",
            GuestCount = 4,
            ReservationDate = FutureDate,
            ReservationTime = new TimeOnly(18, 0),
            Status = status
        };
    }

    private static AdminReservationController CreateController(
        AppDbContext dbContext,
        IDataProtectionProvider? dataProtectionProvider = null,
        IBranchReservationDatePolicyService? branchPolicyService = null)
    {
        var httpContext = new DefaultHttpContext();
        return new AdminReservationController(
            dbContext,
            new ReservationBlockedDateService(dbContext, new TestReservationPolicyLockService()),
            branchPolicyService ?? new BranchReservationDatePolicyService(
                    dbContext,
                    new TestReservationPolicyLockService(),
                    NullLogger<BranchReservationDatePolicyService>.Instance,
                    TestTimeProvider),
            dataProtectionProvider ?? new EphemeralDataProtectionProvider(),
            NullLogger<AdminReservationController>.Instance,
            TestTimeProvider)
        {
            ControllerContext = new ControllerContext { HttpContext = httpContext },
            TempData = new TempDataDictionary(httpContext, new TestTempDataProvider())
        };
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .ConfigureWarnings(warnings => warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new AppDbContext(options);
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        public IDictionary<string, object> LoadTempData(HttpContext context) =>
            new Dictionary<string, object>();

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }

    private sealed class ThrowingMutationPolicyService : IBranchReservationDatePolicyService
    {
        public Task<BranchReservationDatePolicy?> GetPolicyAsync(
            Guid branchId,
            DateOnly date,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<BranchReservationDatePolicy?>(null);

        public Task<IReadOnlyList<BranchReservationDatePolicy>> GetPoliciesAsync(
            Guid branchId,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<BranchReservationDatePolicy>>([]);

        public Task<BranchReservationPolicyEvaluationResult> EvaluateAsync(
            Guid branchId,
            DateOnly date,
            int guestCount,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new BranchReservationPolicyEvaluationResult(
                BranchReservationPolicyEvaluationStatus.Allowed));

        public Task<BranchReservationPolicyMutationResult> SetPolicyAsync(
            BranchReservationPolicyMutation mutation,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("provider-secret: simulated failure");

        public Task<BranchReservationPolicyMutationResult> RemovePolicyAsync(
            Guid branchId,
            DateOnly date,
            Guid expectedVersion,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("provider-secret: simulated failure");

        public Task<IReadOnlyList<ReservationStatusCount>> GetExistingReservationCountsAsync(
            Guid branchId,
            DateOnly date,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<ReservationStatusCount>>([]);

        public Task<int> CleanupPastPoliciesAsync(
            DateOnly vietnamToday,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
    }
}
