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

    [Theory]
    [InlineData(ReservationPolicyEvaluationStatus.PastDate, "ReservationDate")]
    [InlineData(ReservationPolicyEvaluationStatus.GlobalBlockedDate, "ReservationDate")]
    [InlineData(ReservationPolicyEvaluationStatus.InvalidBranch, "BranchId")]
    [InlineData(ReservationPolicyEvaluationStatus.InvalidGuestCount, "GuestCount")]
    [InlineData(ReservationPolicyEvaluationStatus.BranchClosedDate, "ReservationDate")]
    [InlineData(ReservationPolicyEvaluationStatus.MinimumGuestsNotMet, "GuestCount")]
    public async Task Policy_DeniedResult_ReturnsStableFieldSpecificPayload(
        ReservationPolicyEvaluationStatus status,
        string expectedFieldName)
    {
        var evaluator = new CapturingPolicyEvaluator(
            new ReservationPolicyEvaluationResult(
                status,
                status == ReservationPolicyEvaluationStatus.MinimumGuestsNotMet ? 6 : null));
        var controller = CreateController(new CapturingReservationService(), new CapturingEmailService(), evaluator);

        var result = await controller.Policy(
            Guid.NewGuid(),
            new DateOnly(2026, 8, 9),
            5,
            CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = JsonSerializer.SerializeToElement(okResult.Value);
        Assert.Equal(status.ToString(), payload.GetProperty("status").GetString());
        Assert.Equal(expectedFieldName, payload.GetProperty("fieldName").GetString());
        Assert.False(string.IsNullOrWhiteSpace(payload.GetProperty("message").GetString()));
        Assert.Equal(status == ReservationPolicyEvaluationStatus.MinimumGuestsNotMet ? 6 : null, GetNullableInt(payload, "minGuests"));
    }

    [Fact]
    public async Task Policy_AllowedAtExactThreshold_ReturnsAllowedWithoutCreatingReservationOrSendingEmail()
    {
        var reservationService = new CapturingReservationService();
        var emailService = new CapturingEmailService();
        var evaluator = new CapturingPolicyEvaluator((_, _, guestCount, _, _) =>
            Task.FromResult(new ReservationPolicyEvaluationResult(
                guestCount >= 6
                    ? ReservationPolicyEvaluationStatus.Allowed
                    : ReservationPolicyEvaluationStatus.MinimumGuestsNotMet,
                guestCount >= 6 ? null : 6)));
        var controller = CreateController(reservationService, emailService, evaluator);
        var branchId = Guid.NewGuid();
        var reservationDate = new DateOnly(2026, 8, 9);

        var result = await controller.Policy(branchId, reservationDate, 6, CancellationToken.None);

        var okResult = Assert.IsType<OkObjectResult>(result);
        var payload = JsonSerializer.SerializeToElement(okResult.Value);
        Assert.Equal("Allowed", payload.GetProperty("status").GetString());
        Assert.Equal(JsonValueKind.Null, payload.GetProperty("message").ValueKind);
        Assert.Equal(branchId, evaluator.LastBranchId);
        Assert.Equal(reservationDate, evaluator.LastReservationDate);
        Assert.Equal(6, evaluator.LastGuestCount);
        Assert.Equal(0, reservationService.CreateCallCount);
        Assert.Equal(0, emailService.ReservationNotificationCallCount);
    }

    [Fact]
    public async Task Policy_InvalidGuestCount_ReturnsBadRequestWithoutCallingEvaluator()
    {
        var evaluator = new CapturingPolicyEvaluator(
            new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.Allowed));
        var controller = CreateController(new CapturingReservationService(), new CapturingEmailService(), evaluator);

        var result = await controller.Policy(
            Guid.NewGuid(),
            new DateOnly(2026, 8, 9),
            101,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var payload = JsonSerializer.SerializeToElement(badRequest.Value);
        Assert.Equal("InvalidGuestCount", payload.GetProperty("status").GetString());
        Assert.Equal(0, evaluator.CallCount);
    }

    [Fact]
    public async Task Policy_MalformedBranchBinding_ReturnsSafeFieldErrorWithoutCallingEvaluator()
    {
        var evaluator = new CapturingPolicyEvaluator(
            new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.Allowed));
        var controller = CreateController(new CapturingReservationService(), new CapturingEmailService(), evaluator);
        controller.ModelState.AddModelError("branchId", "raw binder detail");

        var result = await controller.Policy(
            null,
            new DateOnly(2026, 8, 9),
            2,
            CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var payload = JsonSerializer.SerializeToElement(badRequest.Value);
        Assert.Equal("InvalidBranch", payload.GetProperty("status").GetString());
        Assert.Equal("BranchId", payload.GetProperty("fieldName").GetString());
        Assert.DoesNotContain("raw binder detail", JsonSerializer.Serialize(badRequest.Value), StringComparison.Ordinal);
        Assert.Equal(0, evaluator.CallCount);
    }

    [Fact]
    public async Task Policy_EvaluatorFailure_ReturnsFailClosedResponseWithoutInternalDetails()
    {
        var evaluator = new CapturingPolicyEvaluator((_, _, _, _, _) =>
            throw new InvalidOperationException("provider-secret-detail"));
        var controller = CreateController(new CapturingReservationService(), new CapturingEmailService(), evaluator);

        var result = await controller.Policy(
            Guid.NewGuid(),
            new DateOnly(2026, 8, 9),
            2,
            CancellationToken.None);

        var errorResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status500InternalServerError, errorResult.StatusCode);
        var json = JsonSerializer.Serialize(errorResult.Value);
        Assert.Contains("VerificationFailed", json, StringComparison.Ordinal);
        Assert.DoesNotContain("provider-secret-detail", json, StringComparison.Ordinal);
        Assert.DoesNotContain("Allowed", json, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IndexPost_ServiceBlockedDate_RendersFormWithDateErrorAndNoEmail()
    {
        var reservationService = new CapturingReservationService(CreateDatePolicyFailure());
        var emailService = new CapturingEmailService();
        var controller = CreateController(reservationService, emailService);

        var result = await controller.Index(CreateValidModel(new DateOnly(2026, 8, 9)), CancellationToken.None);

        var viewResult = Assert.IsType<ViewResult>(result);
        Assert.IsType<ReservationCreateViewModel>(viewResult.Model);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState[nameof(ReservationCreateViewModel.ReservationDate)]!.Errors, error =>
            error.ErrorMessage == ReservationBlockedDateService.BlockedDateMessage);
        Assert.Equal(1, reservationService.CreateCallCount);
        Assert.Equal(0, emailService.ReservationNotificationCallCount);
    }

    [Fact]
    public async Task IndexPost_SeptemberSecondBlockedByService_RendersFormWithDateErrorAndNoEmail()
    {
        var reservationService = new CapturingReservationService(CreateDatePolicyFailure());
        var emailService = new CapturingEmailService();
        var controller = CreateController(reservationService, emailService);

        var result = await controller.Index(CreateValidModel(new DateOnly(2026, 9, 2)), CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.False(controller.ModelState.IsValid);
        Assert.Contains(controller.ModelState[nameof(ReservationCreateViewModel.ReservationDate)]!.Errors, error =>
            error.ErrorMessage == ReservationBlockedDateService.BlockedDateMessage);
        Assert.Equal(1, reservationService.CreateCallCount);
        Assert.Equal(0, emailService.ReservationNotificationCallCount);
    }

    [Fact]
    public async Task QuickPost_ServiceBlockedDate_ReturnsBadRequestJsonAndNoEmail()
    {
        var reservationService = new CapturingReservationService(CreateDatePolicyFailure());
        var emailService = new CapturingEmailService();
        var controller = CreateController(reservationService, emailService);

        var result = await controller.Quick(CreateValidModel(new DateOnly(2026, 8, 9)), CancellationToken.None);

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
        Assert.Contains(ReservationBlockedDateService.BlockedDateMessage, dateErrors);
        Assert.DoesNotContain("\"ok\":true", json, StringComparison.Ordinal);
        Assert.Equal(1, reservationService.CreateCallCount);
        Assert.Equal(0, emailService.ReservationNotificationCallCount);
    }

    [Fact]
    public async Task QuickPost_SeptemberSecondBlockedByService_ReturnsBadRequestJsonAndNoEmail()
    {
        var reservationService = new CapturingReservationService(CreateDatePolicyFailure());
        var emailService = new CapturingEmailService();
        var controller = CreateController(reservationService, emailService);

        var result = await controller.Quick(CreateValidModel(new DateOnly(2026, 9, 2)), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var json = JsonSerializer.Serialize(badRequest.Value);

        Assert.Contains("\"ok\":false", json, StringComparison.Ordinal);
        Assert.Contains(nameof(ReservationCreateViewModel.ReservationDate), json, StringComparison.Ordinal);
        Assert.Equal(1, reservationService.CreateCallCount);
        Assert.Equal(0, emailService.ReservationNotificationCallCount);
    }

    [Fact]
    public async Task QuickPost_PastDateFailure_ReturnsSafeReservationDateError()
    {
        var reservationService = new CapturingReservationService(ReservationCreateResult.Failed(
        [
            new ReservationServiceError
            {
                FieldName = nameof(ReservationCreateRequest.ReservationDate),
                Message = ReservationBlockedDateService.PastDateMessage
            }
        ]));
        var emailService = new CapturingEmailService();
        var controller = CreateController(reservationService, emailService);

        var result = await controller.Quick(CreateValidModel(new DateOnly(2026, 8, 5)), CancellationToken.None);

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
        Assert.Contains(ReservationBlockedDateService.PastDateMessage, dateErrors);
        Assert.Equal(1, reservationService.CreateCallCount);
        Assert.Equal(0, emailService.ReservationNotificationCallCount);
    }

    [Fact]
    public async Task IndexPost_BranchClosed_RendersDateErrorAndDoesNotSendEmail()
    {
        var reservationService = new CapturingReservationService(ReservationCreateResult.Failed(
        [
            new ReservationServiceError
            {
                FieldName = nameof(ReservationCreateRequest.ReservationDate),
                Message = ReservationPolicyMessages.BranchClosedDate
            }
        ]));
        var emailService = new CapturingEmailService();
        var controller = CreateController(reservationService, emailService);

        var result = await controller.Index(CreateValidModel(new DateOnly(2026, 8, 10)), CancellationToken.None);

        Assert.IsType<ViewResult>(result);
        Assert.Contains(
            controller.ModelState[nameof(ReservationCreateViewModel.ReservationDate)]!.Errors,
            error => error.ErrorMessage == ReservationPolicyMessages.BranchClosedDate);
        Assert.Equal(0, emailService.ReservationNotificationCallCount);
    }

    [Fact]
    public async Task QuickPost_MinimumGuestsFailure_ReturnsGuestCountErrorAndDoesNotSendEmail()
    {
        var minimumGuestsMessage = "Chi nhánh chỉ nhận đặt bàn từ 6 khách trở lên vào ngày này.";
        var reservationService = new CapturingReservationService(ReservationCreateResult.Failed(
        [
            new ReservationServiceError
            {
                FieldName = nameof(ReservationCreateRequest.GuestCount),
                Message = minimumGuestsMessage
            }
        ]));
        var emailService = new CapturingEmailService();
        var controller = CreateController(reservationService, emailService);

        var result = await controller.Quick(CreateValidModel(new DateOnly(2026, 8, 10)), CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        var json = JsonSerializer.Serialize(badRequest.Value);
        using var document = JsonDocument.Parse(json);
        var guestCountErrors = document
            .RootElement
            .GetProperty("errors")
            .GetProperty(nameof(ReservationCreateViewModel.GuestCount))
            .EnumerateArray()
            .Select(error => error.GetString())
            .ToArray();

        Assert.Contains(minimumGuestsMessage, guestCountErrors);
        Assert.Equal(0, emailService.ReservationNotificationCallCount);
    }

    private static ReservationCreateResult CreateDatePolicyFailure()
    {
        return ReservationCreateResult.Failed(
        [
            new ReservationServiceError
            {
                FieldName = nameof(ReservationCreateRequest.ReservationDate),
                Message = ReservationBlockedDateService.BlockedDateMessage
            }
        ]);
    }

    private static ReservationController CreateController(
        CapturingReservationService reservationService,
        CapturingEmailService emailService,
        IReservationPolicyEvaluator? policyEvaluator = null)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=kig_test;Username=test;Password=test"
            })
            .Build();

        var controller = new ReservationController(
            reservationService,
            policyEvaluator ?? new CapturingPolicyEvaluator(
                new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.Allowed)),
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

    private static int? GetNullableInt(JsonElement payload, string propertyName)
    {
        var property = payload.GetProperty(propertyName);
        return property.ValueKind == JsonValueKind.Null ? null : property.GetInt32();
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
        private readonly ReservationCreateResult _result;

        public CapturingReservationService(ReservationCreateResult? result = null)
        {
            _result = result ?? ReservationCreateResult.Success(Guid.NewGuid());
        }

        public int CreateCallCount { get; private set; }

        public Task<ReservationCreateResult> CreateReservationAsync(
            ReservationCreateRequest request,
            CancellationToken cancellationToken = default)
        {
            CreateCallCount++;
            return Task.FromResult(_result);
        }

        public Task<Reservation?> GetReservationByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<Reservation?>(null);
        }
    }

    private sealed class CapturingPolicyEvaluator : IReservationPolicyEvaluator
    {
        private readonly Func<Guid, DateOnly, int, DateOnly, CancellationToken, Task<ReservationPolicyEvaluationResult>> _evaluate;

        public CapturingPolicyEvaluator(ReservationPolicyEvaluationResult result)
            : this((_, _, _, _, _) => Task.FromResult(result))
        {
        }

        public CapturingPolicyEvaluator(
            Func<Guid, DateOnly, int, DateOnly, CancellationToken, Task<ReservationPolicyEvaluationResult>> evaluate)
        {
            _evaluate = evaluate;
        }

        public int CallCount { get; private set; }
        public Guid LastBranchId { get; private set; }
        public DateOnly LastReservationDate { get; private set; }
        public int LastGuestCount { get; private set; }

        public Task<ReservationPolicyEvaluationResult> EvaluateAsync(
            Guid branchId,
            DateOnly reservationDate,
            int guestCount,
            DateOnly vietnamToday,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            LastBranchId = branchId;
            LastReservationDate = reservationDate;
            LastGuestCount = guestCount;
            return _evaluate(branchId, reservationDate, guestCount, vietnamToday, cancellationToken);
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
