using System.Reflection;
using KIGHolding.Controllers;
using KIGHolding.Models.Entities;
using KIGHolding.Models.Enums;
using KIGHolding.Options;
using KIGHolding.Services;
using KIGHolding.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Resend;
using Xunit;
using ExtOptions = Microsoft.Extensions.Options.Options;

namespace KIGHolding.Tests;

public sealed class EmailSecurityConfigurationTests
{
    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "KIGHolding";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }

    private sealed class TestLogger<T> : ILogger<T>
    {
        private readonly List<string> _messages = new();
        public IReadOnlyList<string> Messages => _messages;

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _messages.Add(formatter(state, exception));
        }
    }

    public class FakeResendProxy : DispatchProxy
    {
        public int SendCallCount { get; set; }
        public EmailMessage? LastMessage { get; set; }

        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            if (targetMethod?.Name == nameof(IResend.EmailSendAsync))
            {
                SendCallCount++;
                if (args != null && args.Length > 0 && args[0] is EmailMessage msg)
                {
                    LastMessage = msg;
                }

                var response = new ResendResponse<Guid>(Guid.NewGuid(), default!);
                return Task.FromResult(response);
            }

            return null;
        }
    }

    private static (IResend Proxy, FakeResendProxy Controller) CreateFakeResend()
    {
        var proxy = DispatchProxy.Create<IResend, FakeResendProxy>();
        var controller = (FakeResendProxy)(object)proxy;
        return (proxy, controller);
    }

    private static IServiceProvider BuildServices(string environmentName, Dictionary<string, string?> configValues)
    {
        var services = new ServiceCollection();
        var env = new TestHostEnvironment { EnvironmentName = environmentName };
        var config = new ConfigurationBuilder().AddInMemoryCollection(configValues).Build();

        services.AddSingleton<IHostEnvironment>(env);
        services.AddLogging();
        services.AddConfiguredEmailService(config, env);
        return services.BuildServiceProvider();
    }

    [Fact]
    public void CaseA_Development_WithValidConfiguredApiKey_ResolvesNoOp()
    {
        var sp = BuildServices(Environments.Development, new Dictionary<string, string?>
        {
            ["ResendSettings:ApiKey"] = "re_test_mock_secret_key_12345",
            ["ResendSettings:FromEmail"] = "noreply@example.com",
            ["ResendSettings:EnableOutboundEmail"] = "false"
        });

        var emailService = sp.GetRequiredService<IEmailService>();
        Assert.IsType<NoOpEmailService>(emailService);
        Assert.Null(sp.GetService<IResend>());
    }

    [Fact]
    public void CaseB_Development_WithExplicitEnableOutboundEmailTrue_StillResolvesNoOp()
    {
        var sp = BuildServices(Environments.Development, new Dictionary<string, string?>
        {
            ["ResendSettings:ApiKey"] = "re_test_mock_secret_key_12345",
            ["ResendSettings:FromEmail"] = "noreply@example.com",
            ["ResendSettings:EnableOutboundEmail"] = "true"
        });

        var emailService = sp.GetRequiredService<IEmailService>();
        Assert.IsType<NoOpEmailService>(emailService);
        Assert.Null(sp.GetService<IResend>());
    }

    [Fact]
    public void CaseC_Staging_WithEnableOutboundEmailTrue_AndConfiguredKey_ResolvesNoOp()
    {
        var sp = BuildServices(Environments.Staging, new Dictionary<string, string?>
        {
            ["ResendSettings:ApiKey"] = "re_test_staging_key_12345",
            ["ResendSettings:FromEmail"] = "noreply@example.com",
            ["ResendSettings:EnableOutboundEmail"] = "true"
        });

        var emailService = sp.GetRequiredService<IEmailService>();
        Assert.IsType<NoOpEmailService>(emailService);
        Assert.Null(sp.GetService<IResend>());
    }

    [Theory]
    [InlineData("QA")]
    [InlineData("Testing")]
    [InlineData("Local")]
    public void CaseD_QaOrTestEnvironment_ResolvesNoOp(string envName)
    {
        var sp = BuildServices(envName, new Dictionary<string, string?>
        {
            ["ResendSettings:ApiKey"] = "re_test_qa_key_12345",
            ["ResendSettings:FromEmail"] = "noreply@example.com",
            ["ResendSettings:EnableOutboundEmail"] = "true"
        });

        var emailService = sp.GetRequiredService<IEmailService>();
        Assert.IsType<NoOpEmailService>(emailService);
        Assert.Null(sp.GetService<IResend>());
    }

    [Fact]
    public void CaseE_Production_WithMissingEnableFlag_ResolvesNoOp()
    {
        var sp = BuildServices(Environments.Production, new Dictionary<string, string?>
        {
            ["ResendSettings:ApiKey"] = "re_test_prod_key_12345",
            ["ResendSettings:FromEmail"] = "noreply@example.com"
        });

        var emailService = sp.GetRequiredService<IEmailService>();
        Assert.IsType<NoOpEmailService>(emailService);
        Assert.Null(sp.GetService<IResend>());
    }

    [Fact]
    public void CaseF_Production_WithEnableOutboundEmailFalse_ResolvesNoOp()
    {
        var sp = BuildServices(Environments.Production, new Dictionary<string, string?>
        {
            ["ResendSettings:ApiKey"] = "re_test_prod_key_12345",
            ["ResendSettings:FromEmail"] = "noreply@example.com",
            ["ResendSettings:EnableOutboundEmail"] = "false"
        });

        var emailService = sp.GetRequiredService<IEmailService>();
        Assert.IsType<NoOpEmailService>(emailService);
        Assert.Null(sp.GetService<IResend>());
    }

    [Theory]
    [InlineData("", "noreply@example.com")]
    [InlineData("   ", "noreply@example.com")]
    [InlineData("re_test_prod_key_12345", "")]
    [InlineData("re_test_prod_key_12345", "   ")]
    public void CaseG_Production_WithWhitespaceOrMissingCredentials_ResolvesNoOp(string apiKey, string fromEmail)
    {
        var sp = BuildServices(Environments.Production, new Dictionary<string, string?>
        {
            ["ResendSettings:ApiKey"] = apiKey,
            ["ResendSettings:FromEmail"] = fromEmail,
            ["ResendSettings:EnableOutboundEmail"] = "true"
        });

        var emailService = sp.GetRequiredService<IEmailService>();
        Assert.IsType<NoOpEmailService>(emailService);
        Assert.Null(sp.GetService<IResend>());
    }

    [Fact]
    public void CaseH_Production_WithExplicitEnableOutboundEmailTrue_AndValidConfiguration_SelectsRealEmailService()
    {
        var sp = BuildServices(Environments.Production, new Dictionary<string, string?>
        {
            ["ResendSettings:ApiKey"] = "re_test_prod_valid_token_12345",
            ["ResendSettings:FromEmail"] = "noreply@example.com",
            ["ResendSettings:EnableOutboundEmail"] = "true"
        });

        var emailService = sp.GetRequiredService<IEmailService>();
        Assert.IsType<EmailService>(emailService);
        Assert.NotNull(sp.GetService<IResend>());
    }

    [Fact]
    public async Task CaseI_DirectEmailServiceInvocation_WithDeliveryDisabled_MakesZeroTransportCalls()
    {
        var (fakeResend, controller) = CreateFakeResend();
        var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var settings = ExtOptions.Create(new ResendSettings
        {
            ApiKey = "re_test_key_12345",
            FromEmail = "noreply@example.com",
            EnableOutboundEmail = false
        });

        var emailService = new EmailService(
            fakeResend,
            settings,
            env,
            NullLogger<EmailService>.Instance);

        await emailService.SendReservationNotificationAsync(
            "customer@example.com",
            "Reservation Confirmation",
            "<p>Confirmed</p>",
            "Confirmed");

        await emailService.SendEmailAsync(
            "customer@example.com",
            "Generic Email",
            "<p>Hello</p>",
            "Hello");

        Assert.Equal(0, controller.SendCallCount);
        Assert.Null(controller.LastMessage);
    }

    [Fact]
    public async Task CaseJ_DirectEmailServiceInvocation_OutsideProduction_MakesZeroTransportCalls()
    {
        var (fakeResend, controller) = CreateFakeResend();
        var env = new TestHostEnvironment { EnvironmentName = Environments.Development };
        var settings = ExtOptions.Create(new ResendSettings
        {
            ApiKey = "re_test_key_12345",
            FromEmail = "noreply@example.com",
            EnableOutboundEmail = true
        });

        var emailService = new EmailService(
            fakeResend,
            settings,
            env,
            NullLogger<EmailService>.Instance);

        await emailService.SendReservationNotificationAsync(
            "customer@example.com",
            "Reservation Confirmation",
            "<p>Confirmed</p>",
            "Confirmed");

        await emailService.SendEmailAsync(
            "customer@example.com",
            "Generic Email",
            "<p>Hello</p>",
            "Hello");

        Assert.Equal(0, controller.SendCallCount);
        Assert.Null(controller.LastMessage);
    }

    [Fact]
    public async Task CaseK_SuccessfulReservation_WithNoOpEmailService_PreservesBookingSuccessBehavior()
    {
        var noOpEmail = new NoOpEmailService(NullLogger<NoOpEmailService>.Instance);
        var capturingReservationService = new CapturingReservationService();
        var branchId = Guid.NewGuid();

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();

        var controller = new ReservationController(
            capturingReservationService,
            new CapturingPolicyEvaluator(new ReservationPolicyEvaluationResult(ReservationPolicyEvaluationStatus.Allowed)),
            new StubBranchService(branchId),
            new StubSiteSettingService(),
            noOpEmail,
            ExtOptions.Create(new ResendSettings()),
            config,
            NullLogger<ReservationController>.Instance,
            TimeProvider.System);

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        var model = new ReservationCreateViewModel
        {
            BranchId = branchId,
            CustomerName = "John Doe",
            PhoneNumber = "0901234567",
            GuestCount = 4,
            ReservationDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(2)),
            ReservationTime = new TimeOnly(18, 30)
        };

        var result = await controller.Index(model, CancellationToken.None);
        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal(nameof(ReservationController.Success), redirect.ActionName);
        Assert.Equal(1, capturingReservationService.CreateCallCount);
    }

    [Fact]
    public async Task CaseL_ContactAndFranchiseWorkflows_RemainSuccessful_WhenEmailIsSuppressed()
    {
        var noOpEmail = new NoOpEmailService(NullLogger<NoOpEmailService>.Instance);
        var contactService = new CapturingContactService();
        var siteSettingService = new StubSiteSettingService();
        var branchService = new StubBranchService(Guid.NewGuid());
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Host=localhost;Database=test;Username=test;Password=test"
            })
            .Build();

        // 1. Test ContactController with NoOp
        var contactController = new ContactController(
            contactService,
            siteSettingService,
            branchService,
            noOpEmail,
            ExtOptions.Create(new ResendSettings()),
            config,
            NullLogger<ContactController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), new TestTempDataProvider())
        };

        var contactModel = new ContactCreateViewModel
        {
            FullName = "Jane Doe",
            PhoneNumber = "0912345678",
            Email = "jane@example.com",
            Subject = "General Inquiry",
            Message = "Need information."
        };

        var contactResult = await contactController.Index(contactModel, CancellationToken.None);
        var contactRedirect = Assert.IsType<RedirectToActionResult>(contactResult);
        Assert.Equal(nameof(ContactController.Index), contactRedirect.ActionName);
        Assert.True(contactController.TempData.ContainsKey("ContactSuccess"));
        Assert.Equal(1, contactService.CreateCallCount);

        // 2. Test FranchiseController with NoOp
        var franchiseController = new FranchiseController(
            contactService,
            siteSettingService,
            noOpEmail,
            ExtOptions.Create(new ResendSettings()),
            config,
            NullLogger<FranchiseController>.Instance)
        {
            ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() },
            TempData = new TempDataDictionary(new DefaultHttpContext(), new TestTempDataProvider())
        };

        var franchiseModel = new ContactCreateViewModel
        {
            FullName = "Franchise Partner",
            PhoneNumber = "0987654321",
            Email = "partner@example.com",
            Message = "Franchise cooperation request."
        };

        var franchiseResult = await franchiseController.Index(franchiseModel, CancellationToken.None);
        var franchiseRedirect = Assert.IsType<RedirectToActionResult>(franchiseResult);
        Assert.Equal(nameof(FranchiseController.Index), franchiseRedirect.ActionName);
        Assert.True(franchiseController.TempData.ContainsKey("FranchiseContactSuccess"));
        Assert.Equal(2, contactService.CreateCallCount);
    }

    [Fact]
    public async Task CaseM_NoOpLogs_DoNotContainRecipientSubjectBodyOrSecretInformation()
    {
        var logger = new TestLogger<NoOpEmailService>();
        var noOpEmail = new NoOpEmailService(logger);

        const string sensitiveRecipient = "sensitive-customer-target@kig-holding.test";
        const string sensitiveSubject = "CONFIDENTIAL-SUBJECT-TOKEN-98765";
        const string sensitiveHtml = "<div>TOP_SECRET_RESERVATION_PAYLOAD_HTML</div>";
        const string sensitiveText = "TOP_SECRET_RESERVATION_PAYLOAD_TEXT";

        await noOpEmail.SendEmailAsync(sensitiveRecipient, sensitiveSubject, sensitiveHtml, sensitiveText);
        await noOpEmail.SendReservationNotificationAsync(sensitiveRecipient, sensitiveSubject, sensitiveHtml, sensitiveText);

        Assert.Equal(2, logger.Messages.Count);
        foreach (var message in logger.Messages)
        {
            Assert.Equal("Outbound email suppressed by configured delivery policy.", message);
            Assert.DoesNotContain(sensitiveRecipient, message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(sensitiveSubject, message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(sensitiveHtml, message, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(sensitiveText, message, StringComparison.OrdinalIgnoreCase);
        }
    }

    [Fact]
    public async Task CaseN_FakeResendTransport_VerifiesExplicitlyAuthorizedProductionDeliveryPath_WithoutNetworkCall()
    {
        var (fakeResend, controller) = CreateFakeResend();
        var env = new TestHostEnvironment { EnvironmentName = Environments.Production };
        var settings = ExtOptions.Create(new ResendSettings
        {
            ApiKey = "re_test_authorized_prod_token_12345",
            FromEmail = "bookings@truyenthuyetchampong.vn",
            FromName = "Truyền Thuyết Champong",
            EnableOutboundEmail = true
        });

        var emailService = new EmailService(
            fakeResend,
            settings,
            env,
            NullLogger<EmailService>.Instance);

        await emailService.SendReservationNotificationAsync(
            "recipient@example.com",
            "Reservation Notification",
            "<p>Booking Confirmed</p>",
            "Booking Confirmed");

        Assert.Equal(1, controller.SendCallCount);
        Assert.NotNull(controller.LastMessage);
        Assert.Equal("Reservation Notification", controller.LastMessage.Subject);
        Assert.Equal("<p>Booking Confirmed</p>", controller.LastMessage.HtmlBody);
        Assert.Equal("Booking Confirmed", controller.LastMessage.TextBody);
        Assert.Contains("recipient@example.com", controller.LastMessage.To);
    }

    #region Helper Stubs

    private sealed class CapturingReservationService : IReservationService
    {
        public int CreateCallCount { get; private set; }

        public Task<ReservationCreateResult> CreateReservationAsync(ReservationCreateRequest request, CancellationToken cancellationToken = default)
        {
            CreateCallCount++;
            return Task.FromResult(ReservationCreateResult.Success(Guid.NewGuid()));
        }

        public Task<Reservation?> GetReservationByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Reservation?>(null);
    }

    private sealed class CapturingPolicyEvaluator : IReservationPolicyEvaluator
    {
        private readonly ReservationPolicyEvaluationResult _result;

        public CapturingPolicyEvaluator(ReservationPolicyEvaluationResult result)
        {
            _result = result;
        }

        public Task<ReservationPolicyEvaluationResult> EvaluateAsync(
            Guid branchId,
            DateOnly reservationDate,
            int guestCount,
            DateOnly vietnamToday,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_result);
        }
    }

    private sealed class StubBranchService : IBranchService
    {
        private readonly Guid _branchId;

        public StubBranchService(Guid branchId)
        {
            _branchId = branchId;
        }

        public Task<IReadOnlyList<Branch>> GetActiveBranchesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Branch>>([CreateBranch()]);

        public Task<IReadOnlyList<Branch>> GetReservableBranchesAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Branch>>([CreateBranch()]);

        public Task<Branch?> GetBranchBySlugAsync(string slug, CancellationToken cancellationToken = default) =>
            Task.FromResult<Branch?>(CreateBranch());

        public Task<IReadOnlyList<Review>> GetVisibleReviewsAsync(int take = 6, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Review>>([]);

        public void InvalidateActiveBranchesCache()
        {
        }

        private Branch CreateBranch()
        {
            return new Branch
            {
                Id = _branchId,
                Name = "Audit Branch",
                Slug = "audit-branch",
                Address = "Audit Address",
                District = "District 1",
                City = "Ho Chi Minh City",
                Hotline = "0900000000",
                Email = "branch@example.test",
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
        public Task<SiteSetting?> GetSettingsAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<SiteSetting?>(new SiteSetting { Email = "business@example.com" });

        public Task<SiteSetting> UpdateSettingsAsync(SiteSetting settings, CancellationToken cancellationToken = default) =>
            Task.FromResult(settings);

        public void InvalidateCache()
        {
        }
    }

    private sealed class CapturingContactService : IContactService
    {
        public int CreateCallCount { get; private set; }

        public Task<ContactCreateResult> CreateContactMessageAsync(ContactCreateRequest request, CancellationToken cancellationToken = default)
        {
            CreateCallCount++;
            return Task.FromResult(ContactCreateResult.Success(Guid.NewGuid()));
        }

        public Task<ContactMessage?> GetContactMessageByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<ContactMessage?>(null);
        public Task<List<ContactMessage>> GetAllContactMessagesAsync(CancellationToken cancellationToken = default) => Task.FromResult(new List<ContactMessage>());
        public Task<List<ContactMessage>> GetRecentContactMessagesAsync(int count = 10, CancellationToken cancellationToken = default) => Task.FromResult(new List<ContactMessage>());
        public Task<List<ContactMessage>> GetContactMessagesByStatusAsync(ContactMessageStatus status, CancellationToken cancellationToken = default) => Task.FromResult(new List<ContactMessage>());
        public Task<bool> UpdateStatusAsync(Guid id, ContactMessageStatus status, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<bool> DeleteContactMessageAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<int> GetUnreadCountAsync(CancellationToken cancellationToken = default) => Task.FromResult(0);
    }

    private sealed class TestTempDataProvider : ITempDataProvider
    {
        private readonly Dictionary<string, object> _data = new();
        public IDictionary<string, object> LoadTempData(HttpContext context) => _data;
        public void SaveTempData(HttpContext context, IDictionary<string, object> values)
        {
            _data.Clear();
            foreach (var kvp in values)
            {
                _data[kvp.Key] = kvp.Value;
            }
        }
    }

    #endregion
}
