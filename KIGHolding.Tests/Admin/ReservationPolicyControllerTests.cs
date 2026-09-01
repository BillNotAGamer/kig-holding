using System.Reflection;
using KIGHolding.Areas.Admin.ViewModels;
using KIGHolding.Data;
using KIGHolding.Models.Entities;
using KIGHolding.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace KIGHolding.Tests.Admin;

using AdminBaseController = KIGHolding.Areas.Admin.Controllers.AdminBaseController;
using AdminReservationController = KIGHolding.Areas.Admin.Controllers.ReservationController;

public sealed class ReservationPolicyControllerTests
{
    private static readonly TimeProvider AuditTimeProvider =
        new FixedTimeProvider(DateTimeOffset.Parse("2026-08-31T17:00:00+00:00"));

    [Fact]
    public void PolicyController_InheritsAdminAuthorization()
    {
        Assert.True(typeof(AdminBaseController).IsAssignableFrom(typeof(AdminReservationController)));
        Assert.NotNull(typeof(AdminBaseController).GetCustomAttribute<AuthorizeAttribute>());
    }

    [Fact]
    public void PolicyPost_UsesAntiForgeryToken()
    {
        var method = typeof(AdminReservationController)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public)
            .Single(method =>
                method.Name == "Policy" &&
                method.GetParameters().FirstOrDefault()?.ParameterType == typeof(ReservationPolicyViewModel));

        Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
    }

    [Fact]
    public async Task PolicyGet_ReturnsCurrentAndFutureBlockedDatesAndCleansPastDates()
    {
        await using var dbContext = CreateDbContext();
        dbContext.BlockedReservationDates.AddRange(
            new BlockedReservationDate { Date = new DateOnly(2026, 8, 31) },
            new BlockedReservationDate { Date = new DateOnly(2026, 9, 1) },
            new BlockedReservationDate { Date = new DateOnly(2026, 9, 2) },
            new BlockedReservationDate { Date = new DateOnly(2027, 1, 1) });
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        var result = await controller.Policy(year: 2027, month: 1);

        var viewResult = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<ReservationPolicyViewModel>(viewResult.Model);
        Assert.Equal(2027, model.CalendarYear);
        Assert.Equal(1, model.CalendarMonth);
        Assert.Equal(
            [
                new DateOnly(2026, 9, 1),
                new DateOnly(2026, 9, 2),
                new DateOnly(2027, 1, 1)
            ],
            model.BlockedDates);
        Assert.False(await dbContext.BlockedReservationDates.AnyAsync(x => x.Date == new DateOnly(2026, 8, 31)));
    }

    [Fact]
    public async Task PolicyPost_AddsRemovesPreservesAndDeduplicatesFutureDates()
    {
        await using var dbContext = CreateDbContext();
        dbContext.BlockedReservationDates.AddRange(
            new BlockedReservationDate { Date = new DateOnly(2026, 8, 31) },
            new BlockedReservationDate { Date = new DateOnly(2026, 9, 2) },
            new BlockedReservationDate { Date = new DateOnly(2027, 1, 1) });
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        var result = await controller.Policy(new ReservationPolicyViewModel
        {
            CalendarYear = 2026,
            CalendarMonth = 9,
            BlockedDatesInput = "2026-08-30,2026-09-02,2026-09-02,2026-09-03",
            PolicyPayloadPresent = true
        }, CancellationToken.None);

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Policy", redirect.ActionName);
        Assert.Equal(2026, redirect.RouteValues?["year"]);
        Assert.Equal(9, redirect.RouteValues?["month"]);
        Assert.True(controller.TempData.ContainsKey("SuccessMessage"));
        Assert.Equal(
            [
                new DateOnly(2026, 9, 2),
                new DateOnly(2026, 9, 3)
            ],
            await dbContext.BlockedReservationDates
                .OrderBy(x => x.Date)
                .Select(x => x.Date)
                .ToListAsync());
    }

    [Fact]
    public async Task PolicyPost_EmptySetIntentionallyUnblocksAllFutureDates()
    {
        await using var dbContext = CreateDbContext();
        dbContext.BlockedReservationDates.AddRange(
            new BlockedReservationDate { Date = new DateOnly(2026, 9, 2) },
            new BlockedReservationDate { Date = new DateOnly(2027, 1, 1) });
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        var result = await controller.Policy(new ReservationPolicyViewModel
        {
            CalendarYear = 2026,
            CalendarMonth = 9,
            BlockedDatesInput = string.Empty,
            PolicyPayloadPresent = true,
            ConfirmClearAllBlockedDates = true
        }, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(await dbContext.BlockedReservationDates.ToListAsync());
    }

    [Fact]
    public async Task PolicyPost_MalformedDateReturnsViewAndDoesNotReplaceDatabaseSet()
    {
        await using var dbContext = CreateDbContext();
        dbContext.BlockedReservationDates.Add(new BlockedReservationDate { Date = new DateOnly(2026, 9, 2) });
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        var result = await controller.Policy(new ReservationPolicyViewModel
        {
            CalendarYear = 2026,
            CalendarMonth = 9,
            BlockedDatesInput = "2026-09-03,not-a-date",
            PolicyPayloadPresent = true
        }, CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ReservationPolicyViewModel>(viewResult.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(ReservationPolicyViewModel.BlockedDatesInput)));
        Assert.Equal(
            [new DateOnly(2026, 9, 2)],
            await dbContext.BlockedReservationDates.Select(x => x.Date).ToListAsync());
    }

    [Fact]
    public async Task PolicyGet_ReturnsCompleteFutureSetAcrossDisplayedMonth()
    {
        await using var dbContext = CreateDbContext();
        dbContext.BlockedReservationDates.AddRange(
            new BlockedReservationDate { Date = new DateOnly(2026, 9, 2) },
            new BlockedReservationDate { Date = new DateOnly(2027, 1, 1) },
            new BlockedReservationDate { Date = new DateOnly(2028, 9, 4) });
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        var result = await controller.Policy(year: 2026, month: 9);

        var model = Assert.IsType<ReservationPolicyViewModel>(Assert.IsType<ViewResult>(result).Model);
        Assert.Equal(
            "2026-09-02,2027-01-01,2028-09-04",
            model.BlockedDatesInput);
        Assert.True(model.PolicyPayloadPresent);
    }

    [Fact]
    public async Task PolicyPost_MissingPayloadSentinelDoesNotModifyDatabase()
    {
        await using var dbContext = CreateDbContext();
        dbContext.BlockedReservationDates.Add(new BlockedReservationDate { Date = new DateOnly(2026, 9, 2) });
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        var result = await controller.Policy(new ReservationPolicyViewModel
        {
            CalendarYear = 2026,
            CalendarMonth = 9,
            BlockedDatesInput = string.Empty
        }, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(
            [new DateOnly(2026, 9, 2)],
            await dbContext.BlockedReservationDates.Select(x => x.Date).ToListAsync());
    }

    [Fact]
    public async Task PolicyPost_EmptySetWithoutConfirmationDoesNotModifyDatabase()
    {
        await using var dbContext = CreateDbContext();
        dbContext.BlockedReservationDates.AddRange(
            new BlockedReservationDate { Date = new DateOnly(2026, 9, 2) },
            new BlockedReservationDate { Date = new DateOnly(2027, 1, 1) });
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        var result = await controller.Policy(new ReservationPolicyViewModel
        {
            CalendarYear = 2026,
            CalendarMonth = 9,
            BlockedDatesInput = string.Empty,
            PolicyPayloadPresent = true
        }, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(ReservationPolicyViewModel.ConfirmClearAllBlockedDates)));
        Assert.Equal(
            [new DateOnly(2026, 9, 2), new DateOnly(2027, 1, 1)],
            await dbContext.BlockedReservationDates.OrderBy(x => x.Date).Select(x => x.Date).ToListAsync());
    }

    [Fact]
    public async Task PolicyPost_EmptySetWhenAlreadyEmptyIsSafeNoOp()
    {
        await using var dbContext = CreateDbContext();
        var controller = CreateController(dbContext);

        var result = await controller.Policy(new ReservationPolicyViewModel
        {
            CalendarYear = 2026,
            CalendarMonth = 9,
            BlockedDatesInput = string.Empty,
            PolicyPayloadPresent = true
        }, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Empty(await dbContext.BlockedReservationDates.ToListAsync());
    }

    [Fact]
    public async Task PolicyPost_MalformedPayloadDoesNotPartiallyReconcileDatabaseSet()
    {
        await using var dbContext = CreateDbContext();
        dbContext.BlockedReservationDates.AddRange(
            new BlockedReservationDate { Date = new DateOnly(2026, 9, 2) },
            new BlockedReservationDate { Date = new DateOnly(2027, 1, 1) });
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        var result = await controller.Policy(new ReservationPolicyViewModel
        {
            CalendarYear = 2026,
            CalendarMonth = 9,
            BlockedDatesInput = "2026-09-02,INVALID,2027-01-01",
            PolicyPayloadPresent = true
        }, CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Equal(
            [new DateOnly(2026, 9, 2), new DateOnly(2027, 1, 1)],
            await dbContext.BlockedReservationDates.OrderBy(x => x.Date).Select(x => x.Date).ToListAsync());
    }

    [Fact]
    public async Task PolicyPost_SeptemberUpdatePreservesOtherMonths()
    {
        await using var dbContext = CreateDbContext();
        dbContext.BlockedReservationDates.AddRange(
            new BlockedReservationDate { Date = new DateOnly(2026, 9, 2) },
            new BlockedReservationDate { Date = new DateOnly(2026, 9, 3) },
            new BlockedReservationDate { Date = new DateOnly(2027, 1, 1) },
            new BlockedReservationDate { Date = new DateOnly(2027, 9, 2) },
            new BlockedReservationDate { Date = new DateOnly(2028, 9, 4) });
        await dbContext.SaveChangesAsync();
        var controller = CreateController(dbContext);

        var result = await controller.Policy(new ReservationPolicyViewModel
        {
            CalendarYear = 2026,
            CalendarMonth = 9,
            BlockedDatesInput = "2026-09-02,2027-01-01,2027-09-02,2028-09-04",
            PolicyPayloadPresent = true
        }, CancellationToken.None);

        Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(
            [
                new DateOnly(2026, 9, 2),
                new DateOnly(2027, 1, 1),
                new DateOnly(2027, 9, 2),
                new DateOnly(2028, 9, 4)
            ],
            await dbContext.BlockedReservationDates.OrderBy(x => x.Date).Select(x => x.Date).ToListAsync());
    }

    private static AdminReservationController CreateController(AppDbContext dbContext)
    {
        var httpContext = new DefaultHttpContext();

        return new AdminReservationController(
            dbContext,
            new ReservationBlockedDateService(dbContext),
            AuditTimeProvider)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            },
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
        public IDictionary<string, object> LoadTempData(HttpContext context)
        {
            return new Dictionary<string, object>();
        }

        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
        }
    }
}
