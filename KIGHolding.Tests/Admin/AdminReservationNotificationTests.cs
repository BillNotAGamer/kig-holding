using System.Reflection;
using KIGHolding.Data;
using KIGHolding.Hubs;
using KIGHolding.Models.Enums;
using KIGHolding.Services;
using KIGHolding.Services.Notifications;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;

namespace KIGHolding.Tests.Admin;

public sealed class AdminReservationNotificationTests
{
    [Fact]
    public void Hub_RequiresExistingAdminCookieAuthentication()
    {
        var authorizeAttribute = Assert.Single(
            typeof(AdminReservationNotificationHub).GetCustomAttributes<AuthorizeAttribute>());

        Assert.Equal(CookieAuthenticationDefaults.AuthenticationScheme, authorizeAttribute.AuthenticationSchemes);
    }

    [Fact]
    public async Task Notifier_SendsExpectedEventPayloadToAllClients()
    {
        var clients = new CapturingHubClients();
        var notifier = new SignalRAdminReservationNotifier(new CapturingHubContext(clients));
        using var cts = new CancellationTokenSource();
        var notification = new AdminReservationCreatedNotification(
            Guid.NewGuid(),
            "Nguyen Van A",
            "Truyen Thuyet Champong",
            new DateOnly(2026, 8, 4),
            new TimeOnly(18, 30),
            4,
            DateTimeOffset.Parse("2026-08-03T12:00:00+00:00"),
            ReservationSource.Website.ToString());

        await notifier.NotifyReservationCreatedAsync(notification, cts.Token);

        Assert.Equal(AdminReservationNotificationEvents.ReservationCreated, clients.Proxy.Method);
        var argument = Assert.Single(clients.Proxy.Arguments);
        Assert.Same(notification, argument);
        Assert.Equal(cts.Token, clients.Proxy.CancellationToken);
        Assert.Equal(1, clients.AllCallCount);
    }

    [Fact]
    public void NotificationPayload_DoesNotExposeUnnecessaryPersonalData()
    {
        var propertyNames = typeof(AdminReservationCreatedNotification)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Select(x => x.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        Assert.Contains(nameof(AdminReservationCreatedNotification.ReservationId), propertyNames);
        Assert.Contains(nameof(AdminReservationCreatedNotification.CustomerName), propertyNames);
        Assert.Contains(nameof(AdminReservationCreatedNotification.BranchName), propertyNames);
        Assert.Contains(nameof(AdminReservationCreatedNotification.ReservationDate), propertyNames);
        Assert.Contains(nameof(AdminReservationCreatedNotification.ReservationTime), propertyNames);
        Assert.Contains(nameof(AdminReservationCreatedNotification.GuestCount), propertyNames);
        Assert.Contains(nameof(AdminReservationCreatedNotification.CreatedAt), propertyNames);
        Assert.Contains(nameof(AdminReservationCreatedNotification.Source), propertyNames);
        Assert.DoesNotContain("PhoneNumber", propertyNames);
        Assert.DoesNotContain("Phone", propertyNames);
        Assert.DoesNotContain("Email", propertyNames);
        Assert.DoesNotContain("Note", propertyNames);
        Assert.DoesNotContain("Notes", propertyNames);
        Assert.DoesNotContain("InternalNote", propertyNames);
        Assert.DoesNotContain("DiningOccasionOtherNote", propertyNames);
    }

    [Fact]
    public async Task ReservationService_FailedValidation_DoesNotAttemptNotification()
    {
        await using var dbContext = CreateDbContext();
        var notifier = new CapturingReservationNotifier();
        using var cache = new MemoryCache(new MemoryCacheOptions { SizeLimit = 50_000 });
        var service = new ReservationService(
            dbContext,
            cache,
            notifier,
            new AllowAllReservationBlockedDateService(),
            NullLogger<ReservationService>.Instance,
            TimeProvider.System);

        var result = await service.CreateReservationAsync(new ReservationCreateRequest
        {
            CustomerName = "Nguyen Van A",
            PhoneNumber = "0900000000",
            BranchId = Guid.NewGuid(),
            GuestCount = 0,
            ReservationDate = VietnamClock.GetVietnamToday().AddDays(-1),
            ReservationTime = new TimeOnly(18, 30)
        });

        Assert.False(result.Succeeded);
        Assert.Equal(0, notifier.CallCount);
    }

    [Fact]
    public void AdminLayout_PreservesTempDataToastsAndLoadsNotificationScripts()
    {
        var layout = ReadRepoFile("Areas", "Admin", "Views", "Shared", "_AdminLayout.cshtml");

        Assert.Contains("TempData[\"SuccessMessage\"]", layout, StringComparison.Ordinal);
        Assert.Contains("TempData[\"ErrorMessage\"]", layout, StringComparison.Ordinal);
        Assert.Contains("data-toast", layout, StringComparison.Ordinal);
        Assert.Contains("}, 4000);", layout, StringComparison.Ordinal);
        Assert.Contains("data-admin-reservation-toast-host", layout, StringComparison.Ordinal);
        Assert.Contains("data-admin-reservation-sound-toggle", layout, StringComparison.Ordinal);
        Assert.Contains("data-admin-reservation-sound-activation", layout, StringComparison.Ordinal);
        Assert.Contains("Nhấn để kích hoạt âm thanh", layout, StringComparison.Ordinal);
        Assert.Contains("aria-pressed=\"false\"", layout, StringComparison.Ordinal);
        Assert.Contains("data-admin-reservation-hub-url", layout, StringComparison.Ordinal);
        Assert.Contains("data-admin-reservation-audio-url", layout, StringComparison.Ordinal);

        var signalRIndex = layout.IndexOf("~/lib/microsoft-signalr/signalr.min.js", StringComparison.Ordinal);
        var featureScriptIndex = layout.IndexOf("~/js/admin-reservation-notifications.js", StringComparison.Ordinal);
        Assert.True(signalRIndex >= 0);
        Assert.True(featureScriptIndex > signalRIndex);
    }

    [Fact]
    public void NotificationScript_UsesVisibleTabWebLocksAudioPolicyWithoutBroadcastChannelElection()
    {
        var script = ReadNotificationScript();

        Assert.DoesNotContain("BroadcastChannel", script, StringComparison.Ordinal);
        Assert.DoesNotContain("candidate", script, StringComparison.Ordinal);
        Assert.DoesNotContain("claimed", script, StringComparison.Ordinal);
        Assert.DoesNotContain("elections", script, StringComparison.Ordinal);
        Assert.Contains("navigator.locks.request", script, StringComparison.Ordinal);
        Assert.Contains("\"kig-admin-reservation-audio:\" + reservationId", script, StringComparison.Ordinal);
        Assert.Contains("{ ifAvailable: true, mode: \"exclusive\" }", script, StringComparison.Ordinal);
        Assert.Contains("document.visibilityState !== \"visible\"", script, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationScript_HasGuardedTerminalCloseRecovery()
    {
        var script = ReadNotificationScript();

        Assert.Contains("var startPromise = null;", script, StringComparison.Ordinal);
        Assert.Contains("var retryTimerId = null;", script, StringComparison.Ordinal);
        Assert.Contains("var retryAttempt = 0;", script, StringComparison.Ordinal);
        Assert.Contains("function startDisconnectedConnection(connection)", script, StringComparison.Ordinal);
        Assert.Contains("if (!isConnectionDisconnected(connection))", script, StringComparison.Ordinal);
        Assert.Contains("if (startPromise)", script, StringComparison.Ordinal);
        Assert.Contains("connection.start()", script, StringComparison.Ordinal);
        Assert.Contains(".finally(function ()", script, StringComparison.Ordinal);
        Assert.Contains("connection.onreconnected(function ()", script, StringComparison.Ordinal);
        Assert.Contains("connection.onclose(function (error)", script, StringComparison.Ordinal);
        Assert.Contains("scheduleConnectionRetry(connection);", script, StringComparison.Ordinal);
        Assert.Contains("clearRetryTimer();", script, StringComparison.Ordinal);

        var handlerIndex = script.IndexOf("connection.on(eventName, handleReservationCreated);", StringComparison.Ordinal);
        var startFunctionIndex = script.IndexOf("function startDisconnectedConnection(connection)", StringComparison.Ordinal);
        Assert.True(handlerIndex >= 0);
        Assert.Equal(handlerIndex, script.LastIndexOf("connection.on(eventName, handleReservationCreated);", StringComparison.Ordinal));
        Assert.True(handlerIndex > startFunctionIndex);
    }

    [Fact]
    public void NotificationScript_ValidatesPayloadContractBeforeDedupeAndToast()
    {
        var script = ReadNotificationScript();

        Assert.Contains("function validateReservationDate(dateValue)", script, StringComparison.Ordinal);
        Assert.Contains("function isLeapYear(year)", script, StringComparison.Ordinal);
        Assert.Contains("day > daysInMonth(year, month)", script, StringComparison.Ordinal);
        Assert.Contains("function validateReservationTime(timeValue)", script, StringComparison.Ordinal);
        Assert.Contains("reservationId is missing", script, StringComparison.Ordinal);
        Assert.Contains("customerName is missing", script, StringComparison.Ordinal);
        Assert.Contains("branchName is missing", script, StringComparison.Ordinal);
        Assert.Contains("reservationDate is invalid", script, StringComparison.Ordinal);
        Assert.Contains("reservationTime is invalid", script, StringComparison.Ordinal);
        Assert.Contains("guestCount is invalid", script, StringComparison.Ordinal);
        Assert.Contains("Number.isInteger(normalizedGuestCount)", script, StringComparison.Ordinal);
        Assert.Contains("return match[3] + \"/\" + match[2] + \"/\" + match[1];", script, StringComparison.Ordinal);
        Assert.Contains("return timeValue.slice(0, 5);", script, StringComparison.Ordinal);

        var normalizeIndex = script.IndexOf("var notification = normalizeNotification(payload);", StringComparison.Ordinal);
        var dedupeIndex = script.IndexOf("hasRecentlyHandled(notification.reservationId)", StringComparison.Ordinal);
        var toastIndex = script.IndexOf("showReservationToast(notification);", StringComparison.Ordinal);
        var audioIndex = script.IndexOf("attemptReservationAudio(notification.reservationId);", StringComparison.Ordinal);

        Assert.True(normalizeIndex >= 0);
        Assert.True(dedupeIndex > normalizeIndex);
        Assert.True(toastIndex > dedupeIndex);
        Assert.True(audioIndex > toastIndex);
    }

    [Fact]
    public void NotificationScript_SeparatesPersistentSoundPreferenceFromPageUnlock()
    {
        var script = ReadNotificationScript();

        Assert.Contains("var soundStorageKey = \"kig.admin.reservationNotifications.soundEnabled\";", script, StringComparison.Ordinal);
        Assert.Contains("var soundPreferenceEnabled = false;", script, StringComparison.Ordinal);
        Assert.Contains("var soundUnlockedForPage = false;", script, StringComparison.Ordinal);
        Assert.Contains("var soundPlaybackBlockedByBrowser = false;", script, StringComparison.Ordinal);
        Assert.Contains("var unlockAttemptInFlight = null;", script, StringComparison.Ordinal);
        Assert.Contains("soundToggle.setAttribute(\"aria-pressed\", soundPreferenceEnabled ? \"true\" : \"false\");", script, StringComparison.Ordinal);
        Assert.Contains("soundLabel.textContent = soundPreferenceEnabled", script, StringComparison.Ordinal);
        Assert.DoesNotContain("soundReady", script, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationScript_DefaultsMissingSoundPreferenceToEnabledWithoutOverridingExplicitOff()
    {
        var script = ReadNotificationScript();
        var readStorageFunction = ExtractFunction(script, "function readStorage()");

        Assert.Contains("var storedValue = window.localStorage.getItem(soundStorageKey);", readStorageFunction, StringComparison.Ordinal);
        Assert.Contains("if (storedValue === \"false\")", readStorageFunction, StringComparison.Ordinal);
        Assert.Contains("return false;", readStorageFunction, StringComparison.Ordinal);
        Assert.Contains("if (storedValue === \"true\")", readStorageFunction, StringComparison.Ordinal);
        Assert.Contains("writeStorage(true);", readStorageFunction, StringComparison.Ordinal);
        Assert.Contains("} catch {\n            return true;", readStorageFunction, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationScript_PreparesStoredSoundPreferenceWithoutAudiblePageLoadPlayback()
    {
        var script = ReadNotificationScript();
        var initFunction = ExtractFunction(script, "function initSoundControls()");
        var readinessFunction = ExtractFunction(script, "function attemptPageLoadAudioReadiness()");

        Assert.Contains("soundPreferenceEnabled = readStorage();", initFunction, StringComparison.Ordinal);
        Assert.Contains("soundUnlockedForPage = false;", initFunction, StringComparison.Ordinal);
        Assert.Contains("getAudio();", initFunction, StringComparison.Ordinal);
        Assert.Contains("registerAutomaticUnlockListeners();", initFunction, StringComparison.Ordinal);
        Assert.Contains("attemptPageLoadAudioReadiness();", initFunction, StringComparison.Ordinal);
        Assert.Contains("ensureNotificationAudioData()", readinessFunction, StringComparison.Ordinal);
        Assert.DoesNotContain("getAudioContext()", readinessFunction, StringComparison.Ordinal);
        Assert.DoesNotContain("ensureNotificationAudioBuffer()", readinessFunction, StringComparison.Ordinal);
        Assert.DoesNotContain(".play()", initFunction, StringComparison.Ordinal);
        Assert.DoesNotContain("playNotificationSound", initFunction, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationScript_UsesFirstInteractionUnlockAndRemovesListenersAfterSuccess()
    {
        var script = ReadNotificationScript();
        var gestureFunction = ExtractFunction(script, "async function prepareWebAudioFromGesture()");
        var handleFirstInteractionFunction = ExtractFunction(script, "function handleFirstInteraction(event)");
        var markUnlockedFunction = ExtractFunction(script, "function markSoundUnlockedForPage()");

        Assert.Contains("var unlockInteractionEvents = [\"pointerdown\", \"touchstart\", \"keydown\", \"click\"];", script, StringComparison.Ordinal);
        Assert.Contains("document.addEventListener(eventName, handleFirstInteraction, true);", script, StringComparison.Ordinal);
        Assert.Contains("document.removeEventListener(eventName, handleFirstInteraction, true);", script, StringComparison.Ordinal);
        Assert.Contains("if (!isTrustedUserEvent(event))", handleFirstInteractionFunction, StringComparison.Ordinal);
        Assert.Contains("context.resume()", gestureFunction, StringComparison.Ordinal);
        Assert.Contains("primeWebAudioSilently(context);", gestureFunction, StringComparison.Ordinal);
        Assert.Contains("await ensureNotificationAudioBuffer();", gestureFunction, StringComparison.Ordinal);
        Assert.Contains("updateWebAudioReadyState();", gestureFunction, StringComparison.Ordinal);
        Assert.Contains("removeAutomaticUnlockListeners();", markUnlockedFunction, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationScript_DoesNotTreatMutedHtmlAudioPrimingAsAudibleReadiness()
    {
        var script = ReadNotificationScript();

        Assert.DoesNotContain("instance.muted = true;", script, StringComparison.Ordinal);
        Assert.DoesNotContain("instance.volume = 0;", script, StringComparison.Ordinal);
        Assert.DoesNotContain("primeNotificationAudioSilently", script, StringComparison.Ordinal);
        Assert.Contains("audioContextReady = !!audioContext && audioContext.state === \"running\" && !!notificationAudioBuffer && !webAudioFailed;", script, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationScript_LoadsAndCachesNotificationAudioBuffer()
    {
        var script = ReadNotificationScript();
        var dataFunction = ExtractFunction(script, "function ensureNotificationAudioData()");
        var bufferFunction = ExtractFunction(script, "function ensureNotificationAudioBuffer()");

        Assert.Contains("if (notificationAudioData)", dataFunction, StringComparison.Ordinal);
        Assert.Contains("if (notificationAudioDataPromise)", dataFunction, StringComparison.Ordinal);
        Assert.Contains("window.fetch(audioUrl, { cache: \"force-cache\" })", dataFunction, StringComparison.Ordinal);
        Assert.Contains("notificationAudioData = audioData;", dataFunction, StringComparison.Ordinal);
        Assert.Contains("if (notificationAudioBuffer)", bufferFunction, StringComparison.Ordinal);
        Assert.Contains("if (notificationBufferPromise)", bufferFunction, StringComparison.Ordinal);
        Assert.Contains("ensureNotificationAudioData()", bufferFunction, StringComparison.Ordinal);
        Assert.Contains("decodeAudioData(context, audioData.slice(0))", bufferFunction, StringComparison.Ordinal);
        Assert.Contains("notificationAudioBuffer = decodedBuffer;", bufferFunction, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationScript_PlaysOneWebAudioSourcePerNotificationWithHtmlFallback()
    {
        var script = ReadNotificationScript();
        var playFunction = ExtractFunction(script, "async function playNotificationSound()");
        var webAudioPlayFunction = ExtractFunction(script, "function playNotificationSoundWithWebAudio()");
        var htmlFallbackFunction = ExtractFunction(script, "async function playNotificationSoundWithHtmlAudioFallback()");

        Assert.Contains("playNotificationSoundWithWebAudio()", playFunction, StringComparison.Ordinal);
        Assert.Contains("if (playNotificationSoundWithWebAudio())", playFunction, StringComparison.Ordinal);
        Assert.Contains("return true;", playFunction, StringComparison.Ordinal);
        Assert.Contains("return playNotificationSoundWithHtmlAudioFallback();", playFunction, StringComparison.Ordinal);
        Assert.Contains("stopActiveNotificationSource();", webAudioPlayFunction, StringComparison.Ordinal);
        Assert.Contains("var source = audioContext.createBufferSource();", webAudioPlayFunction, StringComparison.Ordinal);
        Assert.Contains("source.buffer = notificationAudioBuffer;", webAudioPlayFunction, StringComparison.Ordinal);
        Assert.Contains("source.start(0);", webAudioPlayFunction, StringComparison.Ordinal);
        Assert.Contains("await instance.play();", htmlFallbackFunction, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationScript_IgnoresSyntheticUnlockEventsAndUsesToggleClickGesture()
    {
        var script = ReadNotificationScript();
        var trustFunction = ExtractFunction(script, "function isTrustedUserEvent(event)");
        var enableFunction = ExtractFunction(script, "function enableSoundPreferenceFromGesture(event)");
        var disableFunction = ExtractFunction(script, "function disableSoundPreference()");

        Assert.Contains("return !!event", trustFunction, StringComparison.Ordinal);
        Assert.Contains("event.isTrusted === true", trustFunction, StringComparison.Ordinal);
        Assert.Contains("writeStorage(true);", enableFunction, StringComparison.Ordinal);
        Assert.Contains("if (isTrustedUserEvent(event))", enableFunction, StringComparison.Ordinal);
        Assert.Contains("unlockNotificationAudioFromGesture();", enableFunction, StringComparison.Ordinal);
        Assert.Contains("writeStorage(false);", disableFunction, StringComparison.Ordinal);
        Assert.Contains("removeAutomaticUnlockListeners();", disableFunction, StringComparison.Ordinal);
        Assert.Contains("soundPlaybackBlockedByBrowser = false;", disableFunction, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationScript_PlaybackRejectionKeepsPreferenceEnabledAndShowsActivationControl()
    {
        var script = ReadNotificationScript();
        var playFunction = ExtractFunction(script, "async function playNotificationSound()");
        var htmlFallbackFunction = ExtractFunction(script, "async function playNotificationSoundWithHtmlAudioFallback()");
        var unlockFunction = ExtractFunction(script, "async function unlockNotificationAudioFromGesture()");
        var disableFunction = ExtractFunction(script, "function disableSoundPreference()");

        Assert.DoesNotContain("writeStorage(false)", playFunction, StringComparison.Ordinal);
        Assert.DoesNotContain("writeStorage(false)", htmlFallbackFunction, StringComparison.Ordinal);
        Assert.DoesNotContain("writeStorage(false)", unlockFunction, StringComparison.Ordinal);
        Assert.Contains("writeStorage(false);", disableFunction, StringComparison.Ordinal);
        Assert.Contains("handleAudioFailure(error, {", htmlFallbackFunction, StringComparison.Ordinal);
        Assert.Contains("showActivation: true", htmlFallbackFunction, StringComparison.Ordinal);
        Assert.Contains("logBlocked: true", htmlFallbackFunction, StringComparison.Ordinal);
        Assert.Contains("isAutoplayBlocked(error)", script, StringComparison.Ordinal);
        Assert.Contains("soundActivation.hidden = !showActivation;", script, StringComparison.Ordinal);
        Assert.Contains("soundActivation.classList.toggle(\"hidden\", !showActivation);", script, StringComparison.Ordinal);
    }

    [Fact]
    public void NotificationScript_DistinguishesMediaErrorsAndRestoresSilentUnlockState()
    {
        var script = ReadNotificationScript();
        var autoplayBlockedFunction = ExtractFunction(script, "function isAutoplayBlocked(error)");
        var failureFunction = ExtractFunction(script, "function handleAudioFailure(error, options)");
        var unlockFunction = ExtractFunction(script, "async function unlockNotificationAudioFromGesture()");
        var playFunction = ExtractFunction(script, "async function playNotificationSound()");

        Assert.Contains("error.name === \"NotAllowedError\"", autoplayBlockedFunction, StringComparison.Ordinal);
        Assert.Contains("error.name === \"SecurityError\"", autoplayBlockedFunction, StringComparison.Ordinal);
        Assert.Contains("console.error(\"Admin reservation notification sound media error.\", error);", failureFunction, StringComparison.Ordinal);
        Assert.Contains("audioContextReady = false;", failureFunction, StringComparison.Ordinal);
        Assert.Contains("unlockAttemptInFlight = null;", unlockFunction, StringComparison.Ordinal);
        Assert.Contains("if (unlockAttemptInFlight)", playFunction, StringComparison.Ordinal);
        Assert.Contains("await unlockAttemptInFlight;", playFunction, StringComparison.Ordinal);
    }

    private static AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
            .Options;

        return new AppDbContext(options);
    }

    private static string ReadRepoFile(params string[] relativeSegments)
    {
        var path = Path.Combine(GetRepositoryRoot(), Path.Combine(relativeSegments));
        return File.ReadAllText(path);
    }

    private static string ReadNotificationScript()
    {
        return ReadRepoFile("wwwroot", "js", "admin-reservation-notifications.js");
    }

    private static string ExtractFunction(string source, string signature)
    {
        var signatureIndex = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(signatureIndex >= 0, $"Missing function signature: {signature}");

        var openBraceIndex = source.IndexOf('{', signatureIndex);
        Assert.True(openBraceIndex >= 0, $"Missing opening brace for: {signature}");

        var depth = 0;
        for (var index = openBraceIndex; index < source.Length; index++)
        {
            if (source[index] == '{')
            {
                depth++;
            }
            else if (source[index] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source[signatureIndex..(index + 1)];
                }
            }
        }

        throw new InvalidOperationException($"Could not extract function: {signature}");
    }

    private static string GetRepositoryRoot()
    {
        return Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));
    }

    private sealed class CapturingReservationNotifier : IAdminReservationNotifier
    {
        public int CallCount { get; private set; }

        public Task NotifyReservationCreatedAsync(
            AdminReservationCreatedNotification notification,
            CancellationToken cancellationToken = default)
        {
            CallCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class AllowAllReservationBlockedDateService : IReservationBlockedDateService
    {
        public Task<ReservationDatePolicyResult> EvaluateReservationDateAsync(
            DateOnly reservationDate,
            DateOnly vietnamToday,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new ReservationDatePolicyResult(ReservationDatePolicyStatus.Allowed));
        }

        public Task<bool> IsBlockedAsync(DateOnly date, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task<IReadOnlyList<DateOnly>> GetActiveBlockedDatesAsync(
            DateOnly vietnamToday,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<DateOnly>>([]);
        }

        public Task ReplaceActiveBlockedDatesAsync(
            IReadOnlyCollection<DateOnly> dates,
            DateOnly vietnamToday,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<int> CleanupPastDatesAsync(DateOnly vietnamToday, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(0);
        }
    }

    private sealed class CapturingHubContext : IHubContext<AdminReservationNotificationHub>
    {
        public CapturingHubContext(CapturingHubClients clients)
        {
            Clients = clients;
        }

        public IHubClients Clients { get; }
        public IGroupManager Groups { get; } = new UnsupportedGroupManager();
    }

    private sealed class CapturingHubClients : IHubClients
    {
        public CapturingClientProxy Proxy { get; } = new();
        public int AllCallCount { get; private set; }

        public IClientProxy All
        {
            get
            {
                AllCallCount++;
                return Proxy;
            }
        }

        public IClientProxy AllExcept(IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IClientProxy Client(string connectionId) => throw new NotSupportedException();
        public IClientProxy Clients(IReadOnlyList<string> connectionIds) => throw new NotSupportedException();
        public IClientProxy Group(string groupName) => throw new NotSupportedException();
        public IClientProxy GroupExcept(string groupName, IReadOnlyList<string> excludedConnectionIds) => throw new NotSupportedException();
        public IClientProxy Groups(IReadOnlyList<string> groupNames) => throw new NotSupportedException();
        public IClientProxy User(string userId) => throw new NotSupportedException();
        public IClientProxy Users(IReadOnlyList<string> userIds) => throw new NotSupportedException();
    }

    private sealed class CapturingClientProxy : IClientProxy
    {
        public string? Method { get; private set; }
        public object?[] Arguments { get; private set; } = [];
        public CancellationToken CancellationToken { get; private set; }

        public Task SendCoreAsync(string method, object?[] args, CancellationToken cancellationToken = default)
        {
            Method = method;
            Arguments = args;
            CancellationToken = cancellationToken;
            return Task.CompletedTask;
        }
    }

    private sealed class UnsupportedGroupManager : IGroupManager
    {
        public Task AddToGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task RemoveFromGroupAsync(string connectionId, string groupName, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
