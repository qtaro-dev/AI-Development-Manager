using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace Adm.Poc.P1029;

public partial class MainWindow : Window
{
    private const string VirtualHost = "p1-029.local";
    private static readonly Uri StartUri = new($"https://{VirtualHost}/index.html");
    private readonly PocOptions options;
    private readonly PocTelemetry telemetry;
    private readonly Stopwatch startupClock = Stopwatch.StartNew();
    private bool navigationCompleted;
    private bool screenshotCaptured;
    private bool probesStarted;
    private bool telemetrySaved;
    private DateTimeOffset? uiReadyAt;
    private string initializationStage = "not_started";
    private string? userDataFolder;
    private string? webView2RuntimeVersion;
    private CoreWebView2DevToolsProtocolEventReceiver? consoleReceiver;

    public MainWindow(PocOptions options)
    {
        InitializeComponent();
        this.options = options;
        telemetry = new PocTelemetry(options.EvidencePath);
        telemetry.Record("process", new { process_id = Environment.ProcessId, assets = options.AssetsPath });
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            if (!Directory.Exists(options.AssetsPath))
            {
                throw new DirectoryNotFoundException($"Assets directory was not found: {options.AssetsPath}");
            }

            userDataFolder = Path.GetFullPath(Path.Combine(options.EvidencePath, "webview2-user-data"));
            telemetry.Record("webview2_initialization", new { stage = "environment_create_started", user_data_folder = userDataFolder, shared_user_data_folder = false });
            initializationStage = "environment_create";
            var environment = await AwaitWithTimeout(
                CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder),
                TimeSpan.FromSeconds(20),
                "environment_create");
            webView2RuntimeVersion = environment.BrowserVersionString;
            telemetry.Record("webview2_initialization", new { stage = "environment_created", user_data_folder = userDataFolder, browser_version = webView2RuntimeVersion, shared_user_data_folder = false });
            telemetry.Record("webview2_initialization", new { stage = "ensure_core_started" });
            initializationStage = "ensure_core";
            await AwaitWithTimeout(
                WebView.EnsureCoreWebView2Async(environment),
                TimeSpan.FromSeconds(30),
                "ensure_core");
            telemetry.Record("webview2_initialization", new { stage = "ensure_core_completed", browser_version = webView2RuntimeVersion });
            initializationStage = "configured";
            var core = WebView.CoreWebView2;
            core.SetVirtualHostNameToFolderMapping(VirtualHost, options.AssetsPath, CoreWebView2HostResourceAccessKind.DenyCors);
            core.Settings.AreDevToolsEnabled = false;
            core.NavigationStarting += Core_NavigationStarting;
            core.NavigationCompleted += Core_NavigationCompleted;
            core.NewWindowRequested += Core_NewWindowRequested;
            core.WebResourceRequested += Core_WebResourceRequested;
            core.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
            consoleReceiver = core.GetDevToolsProtocolEventReceiver("Runtime.consoleAPICalled");
            consoleReceiver.DevToolsProtocolEventReceived += Core_DevToolsProtocolEventReceived;
            await AwaitWithTimeout(core.CallDevToolsProtocolMethodAsync("Runtime.enable", "{}"), TimeSpan.FromSeconds(10), "devtools_console");
            telemetry.Record("origin", new { origin = StartUri.GetLeftPart(UriPartial.Authority), mapped_folder = options.AssetsPath, access = "DenyCors" });
            StatusText.Text = $"仮想HTTPS originを表示しています: {StartUri.GetLeftPart(UriPartial.Authority)}";
            core.Navigate(StartUri.ToString());
        }
        catch (Exception exception)
        {
            telemetry.Record("startup_error", new { stage = initializationStage, type = exception.GetType().FullName, message = exception.Message });
            StatusText.Text = "PoC起動に失敗しました。";
            SaveTelemetry(exitCode: 1);
            Close();
        }
    }

    private static async Task<T> AwaitWithTimeout<T>(Task<T> operation, TimeSpan timeout, string stage)
    {
        var completed = await Task.WhenAny(operation, Task.Delay(timeout));
        if (completed != operation)
        {
            throw new TimeoutException($"WebView2 {stage} timed out after {timeout.TotalSeconds:0} seconds.");
        }

        return await operation;
    }

    private static async Task AwaitWithTimeout(Task operation, TimeSpan timeout, string stage)
    {
        var completed = await Task.WhenAny(operation, Task.Delay(timeout));
        if (completed != operation)
        {
            throw new TimeoutException($"WebView2 {stage} timed out after {timeout.TotalSeconds:0} seconds.");
        }

        await operation;
    }

    private void Core_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        var allowed = Uri.TryCreate(e.Uri, UriKind.Absolute, out var uri) &&
                      uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) &&
                      uri.Host.Equals(VirtualHost, StringComparison.OrdinalIgnoreCase);
        telemetry.Record("navigation", new { uri = e.Uri, allowed, is_user_initiated = e.IsUserInitiated });
        if (!allowed)
        {
            e.Cancel = true;
            StatusText.Text = "外部Navigationを拒否しました。";
        }
    }

    private void Core_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        navigationCompleted = true;
        telemetry.Record("navigation_completed", new { uri = WebView.Source?.ToString(), success = e.IsSuccess, status = e.HttpStatusCode, error = e.WebErrorStatus.ToString() });
        if (!e.IsSuccess)
        {
            return;
        }

        uiReadyAt ??= DateTimeOffset.UtcNow;
        startupClock.Stop();
        StatusText.Text = $"ローカル資産の表示に成功しました（{startupClock.ElapsedMilliseconds} ms）。";
        _ = CaptureScreenshotAndRunProbesAsync();
    }

    private void Core_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        telemetry.Record("new_window", new { uri = e.Uri, action = "blocked" });
    }

    private void Core_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        var isAllowed = Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out var uri) &&
                        uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase) &&
                        uri.Host.Equals(VirtualHost, StringComparison.OrdinalIgnoreCase);
        telemetry.Record("resource", new { uri = e.Request.Uri, method = e.Request.Method, allowed = isAllowed });
        if (!isAllowed)
        {
            e.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(
                new MemoryStream(Encoding.UTF8.GetBytes("blocked by P1-029")),
                403,
                "Blocked",
                "Content-Type: text/plain; charset=utf-8");
        }
    }

    private void Core_DevToolsProtocolEventReceived(object? sender, CoreWebView2DevToolsProtocolEventReceivedEventArgs e)
    {
        telemetry.Record("console", new { protocol_event = "Runtime.consoleAPICalled", payload = e.ParameterObjectAsJson });
    }

    private async Task CaptureScreenshotAndRunProbesAsync()
    {
        try
        {
            if (screenshotCaptured)
            {
                return;
            }

            screenshotCaptured = true;
            Directory.CreateDirectory(options.EvidencePath);
            await using (var screenshot = File.Create(Path.Combine(options.EvidencePath, "screenshot-initial.png")))
            {
                await WebView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, screenshot);
            }
            telemetry.Record("screenshot", new { path = "screenshot-initial.png", captured = true });

            if (!probesStarted)
            {
                probesStarted = true;
                await WebView.CoreWebView2.ExecuteScriptAsync("console.info('p1-029-console-probe'); window.open('https://example.com/p1-029-new-window', '_blank');");
                WebView.CoreWebView2.Navigate("https://example.com/p1-029-navigation");
                await Task.Delay(250);
                WebView.CoreWebView2.Navigate(StartUri.ToString());
            }

            if (options.AutoExitMilliseconds > 0)
            {
                await Task.Delay(options.AutoExitMilliseconds);
                Dispatcher.Invoke(Close);
            }
        }
        catch (Exception exception)
        {
            telemetry.Record("probe_error", new { type = exception.GetType().FullName, message = exception.Message });
            SaveTelemetry(exitCode: 1);
            Dispatcher.Invoke(Close);
        }
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        SaveTelemetry(exitCode: navigationCompleted ? 0 : 1);
        WebView.Dispose();
    }

    private void SaveTelemetry(int exitCode)
    {
        if (telemetrySaved)
        {
            return;
        }

        telemetrySaved = true;
        telemetry.Save(options.MeasurementPath, new
        {
            exit_code = exitCode,
            navigation_completed = navigationCompleted,
            ui_ready_at_utc = uiReadyAt,
            startup_elapsed_ms = uiReadyAt is null ? (long?)null : startupClock.ElapsedMilliseconds,
            origin = StartUri.GetLeftPart(UriPartial.Authority),
            initialization_stage = initializationStage,
            user_data_folder = userDataFolder,
            shared_user_data_folder = false,
            webview2_runtime_version = webView2RuntimeVersion,
            server_used = false,
            localhost_port_used = false
        });
    }
}
