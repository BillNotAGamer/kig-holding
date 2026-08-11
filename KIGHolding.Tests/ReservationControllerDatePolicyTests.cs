using System.Text.Json;
using KIGHolding.Controllers;
using KIGHolding.Models.Entities;
using KIGHolding.Options;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace KIGHolding.Tests;

public sealed class ReservationControllerDatePolicyTests
{
    private static readonly TimeProvider AuditTimeProvider =
        new FixedTimeProvider(DateTimeOffset.Parse("2026-08-05T17:00:00+00:00"));

    [Fact]
    public async Task IndexPost_IncidentSunday_RendersFormWithDateErrorAndNoEmail()
    {
        var reservationService = new CapturingReservationService();
        var emailService = new CapturingEmailService();
        var controller = CreateController(reservationService, emailService);

        var result = await controller.Index(CreateValidModel(new DateOnly(2026, 8, 9)), CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ReservationCreateViewModel>(viewResult.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.True(controller.ModelState.ContainsKey(nameof(ReservationCreateViewModel.ReservationDate)));
        Assert.Equal(0, reservationService.CreateCallCount);
        Assert.Equal(0, emailService.ReservationNotificationCallCount);
    }

    [Fact]
    public async Task IndexPost_ClosedCalendarDate_RendersFormWithDateErrorAndNoEmail()
    {
        var reservationService = new CapturingReservationService();
        var emailService = new CapturingEmailService();
        var controller = CreateController(reservationService, emailService);

        var result = await controller.Index(CreateValidModel(new DateOnly(2029, 1, 1)), CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState[nameof(ReservationCreateViewModel.ReservationDate)]!.Errors, error =>
            error.ErrorMessage == VietnamHolidayEvaluator.GetReservationDatePolicyMessage(ReservationDatePolicyStatus.BookingCalendarClosed));
        Assert.Equal(0, reservationService.CreateCallCount);
        Assert.Equal(0, emailService.ReservationNotificationCallCount);
    }

    [Fact]
    public async Task QuickPost_IncidentSunday_ReturnsBadRequestJsonAndNoEmail()
    {
        var reservationService = new CapturingReservationService();
        var emailService = new CapturingEmailService();
        var controller = CreateController(reservationService, emailService);

        var result = await controller.Quick(CreateValidModel(new DateOnly(2026, 8, 9)), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var json = JsonSerializer.Serialize(badRequest.Value);

        Assert.Contains("\"ok\":false", json, StringComparison.Ordinal);
        Assert.Contains(nameof(ReservationCreateViewModel.ReservationDate), json, StringComparison.Ordinal);
        Assert.DoesNotContain("\"ok\":true", json, StringComparison.Ordinal);
        Assert.Equal(0, reservationService.CreateCallCount);
        Assert.Equal(0, emailService.ReservationNotificationCallCount);
    }

    [Fact]
    public async Task QuickPost_ClosedCalendarDate_ReturnsBadRequestJsonAndNoEmail()
    {
        var reservationService = new CapturingReservationService();
        var emailService = new CapturingEmailService();
        var controller = CreateController(reservationService, emailService);

        var result = await controller.Quick(CreateValidModel(new DateOnly(2029, 1, 1)), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var json = JsonSerializer.Serialize(badRequest.Value);
        using var document = JsonDocument.Parse(json);
        var dateErrors = document
            .RootElement
            .GetProperty("errors")
            .GetProperty(nameof(ReservationCreateViewModel.ReservationDate))
            .EnumerateArray()
            .Select(error => error.GetString())
            .ToArray();

        Assert.Contains("\"ok\":false", json, StringComparison.Ordinal);
        Assert.Contains(VietnamHolidayEvaluator.GetReservationDatePolicyMessage(ReservationDatePolicyStatus.BookingCalendarClosed), dateErrors);
        Assert.Equal(0, reservationService.CreateCallCount);
        Assert.Equal(0, emailService.ReservationNotificationCallCount);
    }

    private static ReservationController CreateController(
        CapturingReservationService reservationService,
        CapturingEmailService emailService)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=kig_test;Username=test;Password=test"
            })
            .Build();

        var controller = new ReservationController(
            reservationService,
            new StubBranchService(),
            new StubSiteSettingService(),
            emailService,
            Microsoft.Extensions.Options.Options.Create(new ResendSettings()),
            configuration,
            NullLogger<ReservationController>.Instance,
            AuditTimeProvider);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        return controller;
    }

    private static ReservationCreateViewModel CreateValidModel(DateOnly reservationDate)
    {
        return new ReservationCreateViewModel
        {
            CustomerName = "Nguyen Van A",
            PhoneNumber = "0900000000",
            BranchId = Guid.NewGuid(),
            GuestCount = 2,
            ReservationDate = reservationDate,
            ReservationTime = new TimeOnly(18, 30)
        };
    }

    private sealed class CapturingReservationService : IReservationService
    {
        public int CreateCallCount { get; private set; }

        public Task<ReservationCreateResult> CreateReservationAsync(
            ReservationCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            CreateCallCount++;
            return Task.FromResult(ReservationCreateResult.Success(Guid.NewGuid()));
        }

        public Task<Reservation?> GetReservationByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Reservation?>(null);
        }
    }

    private sealed class CapturingEmailService : IEmailService
    {
        public int ReservationNotificationCallCount { get; private set; }

        public Task SendEmailAsync(
            string recipientEmail,
            string subject,
            string htmlBody,
            string textBody,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task SendReservationNotificationAsync(
            string recipientEmail,
            string subject,
            string htmlBody,
            string textBody,
            CancellationToken cancellationToken = default)
        {
            ReservationNotificationCallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubBranchService : IBranchService
    {
        public Task<IReadOnlyList<Branch>> GetActiveBranchesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Branch>>([CreateBranch()]);
        }

        public Task<IReadOnlyList<Branch>> GetReservableBranchesAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Branch>>([CreateBranch()]);
        }

        public Task<Branch?> GetBranchBySlugAsync(string slug, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Branch?>(CreateBranch());
        }

        public Task<IReadOnlyList<Review>> GetVisibleReviewsAsync(int take = 6, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<Review>>([]);
        }

        public void InvalidateActiveBranchesCache()
        {
        }

        private static Branch CreateBranch()
        {
            return new Branch
            {
                Id = Guid.NewGuid(),
                Name = "Audit Branch",
                Slug = "audit-branch",
                Address = "Audit",
                District = "Audit",
                City = "Ho Chi Minh City",
                Hotline = "0900000000",
                Email = "audit@example.test",
                OpeningTime = new TimeOnly(10, 0),
                ClosingTime = new TimeOnly(22, 0),
                Capacity = 100,
                IsActive = true,
                AllowsReservations = true
            };
        }
    }

    private sealed class StubSiteSettingService : ISiteSettingService
    {
        public Task<SiteSetting?> GetSettingsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult<SiteSetting?>(new SiteSetting
            {
                Email = "audit@example.test",
                Hotline = "0900000000"
            });
        }

        public void InvalidateCache()
        {
        }
    }
}
