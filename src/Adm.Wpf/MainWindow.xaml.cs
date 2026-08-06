using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Web.WebView2.Core;
using Adm.Wpf.Bridge;
using Adm.Application.ExecutionProfiles;
using Adm.Wpf.Composition;
using Adm.Wpf.Configuration;
using Adm.Wpf.LocalChannel;
using Adm.Wpf.Shell;

namespace Adm.Wpf;

public partial class MainWindow : Window, IDisposable
{
    private static readonly TimeSpan ReadinessTimeout = TimeSpan.FromSeconds(8);
    private static readonly TimeSpan WebViewInitializationTimeout = TimeSpan.FromSeconds(10);
    private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(1) };
    private static readonly JsonSerializerOptions ProfileJsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };
    private ServerConnectionOptions connectionOptions;
    private readonly LocalCompositionRoot localCompositionRoot;
    private readonly ProjectFolderPickerBridge projectFolderPickerBridge;
    private readonly ExecutionProfileService executionProfiles;
    private readonly string[] commandLineArgs;
    private readonly WindowLifecycleCoordinator lifecycle = new();
    private bool openLocalSettings;
    private bool isInitialized;
    private bool webViewEventsRegistered;
    private bool isDisposed;

    public MainWindow(
        ExecutionProfileService executionProfiles,
        LocalCompositionRoot localCompositionRoot,
        ProjectFolderPickerBridge projectFolderPickerBridge)
    {
        InitializeComponent();
        commandLineArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
        this.executionProfiles = executionProfiles;
        this.localCompositionRoot = localCompositionRoot;
        this.projectFolderPickerBridge = projectFolderPickerBridge;
        connectionOptions = ServerConnectionOptions.FromArguments(commandLineArgs);
        UpdateServerUrlText();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await RunEventHandlerAsync(async () =>
        {
            FitToWorkArea();

            if (!ServerConnectionOptions.HasServerUrlArgument(commandLineArgs))
            {
                var saved = await executionProfiles.GetAsync(lifecycle.LifetimeToken);
                if (isDisposed)
                {
                    return;
                }

                connectionOptions = ServerConnectionOptions.FromProfile(saved.Profile);
                UpdateServerUrlText();
            }

            await ConnectAsync();
        });
    }

    private void FitToWorkArea()
    {
        var workArea = SystemParameters.WorkArea;
        Width = Math.Min(Width, workArea.Width);
        Height = Math.Min(Height, workArea.Height);
        Left = workArea.Left + Math.Max(0, (workArea.Width - Width) / 2);
        Top = workArea.Top + Math.Max(0, (workArea.Height - Height) / 2);
    }

    private async void RetryButton_Click(object sender, RoutedEventArgs e) => await RunEventHandlerAsync(ConnectAsync);

    private async Task ConnectAsync()
    {
        if (isDisposed)
        {
            return;
        }

        using var attempt = lifecycle.BeginAttempt();
        RetryButton.IsEnabled = false;
        FallbackActions.Visibility = Visibility.Collapsed;
        MessagePanel.Visibility = Visibility.Visible;
        WebView.Visibility = Visibility.Collapsed;

        if (connectionOptions.IsLocal)
        {
            SetMessage("ローカルUIを準備しています。", "サーバーに接続せず、このアプリに組み込まれた画面を表示しています。");
        }

        if (!connectionOptions.IsLocal && !await WaitForServerAsync(attempt.Token))
        {
            if (!lifecycle.IsCurrent(attempt))
            {
                return;
            }

            SetMessage("サーバーに接続できません。", "このPCだけで続けるか、接続先を確認してから再試行できます。");
            ShowFallbackActions(true);
            RetryButton.IsEnabled = true;
            return;
        }

        try
        {
            await InitializeWebViewAsync(attempt.Token);
            if (!lifecycle.IsCurrent(attempt))
            {
                return;
            }

            WebView.Source = connectionOptions.IsLocal
                ? GetLocalStartUri()
                : connectionOptions.ServerUri;
            WebView.Visibility = Visibility.Visible;
            MessagePanel.Visibility = Visibility.Collapsed;
            StatusText.Text = connectionOptions.IsLocal
                ? "ローカルUIを表示しています。"
                : "共通Web UIを表示しています。";
        }
        catch (OperationCanceledException) when (attempt.Token.IsCancellationRequested || isDisposed)
        {
        }
        catch (WebAssetUnavailableException)
        {
            if (!lifecycle.IsCurrent(attempt)) return;
            SetMessage("Web UIの配布物がありません。", "アプリを修復または再インストールしてから、再試行してください。終了する場合は、この画面を閉じてください。");
            ShowFallbackActions(false);
            RetryButton.IsEnabled = true;
        }
        catch (Exception exception) when (IsRuntimeMissing(exception))
        {
            if (!lifecycle.IsCurrent(attempt)) return;
            SetMessage("WebView2 Runtimeが必要です。", "Microsoft Edge WebView2 Runtimeをインストールしてから、再試行してください。");
            ShowFallbackActions(false);
            RetryButton.IsEnabled = true;
        }
        catch (Exception)
        {
            if (!lifecycle.IsCurrent(attempt)) return;
            SetMessage("Web UIを表示できません。", "WebView2の初期化に失敗しました。再試行してください。");
            ShowFallbackActions(false);
            RetryButton.IsEnabled = true;
        }
    }

    private void UpdateServerUrlText() => ServerUrlText.Text = connectionOptions.IsLocal
        ? "Local mode"
        : $"Server: {connectionOptions.ServerUri!.AbsoluteUri}";

    private Uri GetLocalStartUri() => openLocalSettings
        ? new Uri(LocalWebViewPolicy.StartUri + "?settings=1")
        : LocalWebViewPolicy.StartUri;

    private void ShowFallbackActions(bool showLocalOptions)
    {
        FallbackActions.Visibility = Visibility.Visible;
        ContinueLocalButton.Visibility = showLocalOptions ? Visibility.Visible : Visibility.Collapsed;
        OpenSettingsButton.Visibility = showLocalOptions ? Visibility.Visible : Visibility.Collapsed;
    }

    private async void ContinueLocalButton_Click(object sender, RoutedEventArgs e)
    {
        await RunEventHandlerAsync(async () =>
        {
            openLocalSettings = false;
            connectionOptions = new ServerConnectionOptions(WpfExecutionMode.Local, null);
            UpdateServerUrlText();
            await ConnectAsync();
        });
    }

    private async void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        await RunEventHandlerAsync(async () =>
        {
            openLocalSettings = true;
            connectionOptions = new ServerConnectionOptions(WpfExecutionMode.Local, null);
            UpdateServerUrlText();
            await ConnectAsync();
        });
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e) => Close();

    private async Task InitializeWebViewAsync(CancellationToken cancellationToken)
    {
        if (isInitialized)
        {
            return;
        }

        WebAssetResolution? assets = null;
        if (connectionOptions.IsLocal && !WebAssetResolver.TryResolve(AppContext.BaseDirectory, out assets))
        {
            throw new WebAssetUnavailableException();
        }

        var userDataFolder = UserDataFolderResolver.GetLocalModeFolder();
        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder)
            .WaitAsync(WebViewInitializationTimeout, cancellationToken);
        await WebView.EnsureCoreWebView2Async(environment)
            .WaitAsync(WebViewInitializationTimeout, cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        RegisterWebViewEvents();
        WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;

        if (connectionOptions.IsLocal)
        {
            WebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                LocalWebViewPolicy.VirtualHostName,
                assets!.RootDirectory,
                CoreWebView2HostResourceAccessKind.DenyCors);
        }

        isInitialized = true;
    }

    private async Task<bool> WaitForServerAsync(CancellationToken cancellationToken)
    {
        var deadline = DateTimeOffset.UtcNow + ReadinessTimeout;
        var readinessUri = new Uri(connectionOptions.ServerUri!, "health/ready");

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var response = await httpClient.GetAsync(readinessUri, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch (HttpRequestException)
            {
            }
            catch (TaskCanceledException)
            {
            }
            catch (OperationCanceledException)
            {
                return false;
            }

            await Task.Delay(250, cancellationToken);
        }

        return false;
    }

    private void CoreWebView2_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (isDisposed || lifecycle.LifetimeToken.IsCancellationRequested)
        {
            e.Cancel = true;
            return;
        }

        var allowed = Uri.TryCreate(e.Uri, UriKind.Absolute, out var candidate) &&
            (connectionOptions.IsLocal
                ? LocalWebViewPolicy.IsAllowedNavigation(candidate)
                : ShellNavigationPolicy.IsAllowed(connectionOptions.ServerUri!, candidate));

        if (!allowed)
        {
            e.Cancel = true;
            StatusText.Text = "安全のため、許可されていないページを開きませんでした。";
        }
    }

    private void CoreWebView2_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (isDisposed)
        {
            return;
        }

        if (!e.IsSuccess)
        {
            MessagePanel.Visibility = Visibility.Visible;
            WebView.Visibility = Visibility.Collapsed;
            SetMessage("画面を読み込めません。", "アプリの画面を準備できませんでした。もう一度読み込めます。");
            ShowFallbackActions(false);
            RetryButton.IsEnabled = true;
        }
    }

    private static void CoreWebView2_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e) => e.Handled = true;

    private void CoreWebView2_WebResourceRequested(object? sender, CoreWebView2WebResourceRequestedEventArgs e)
    {
        if (isDisposed)
        {
            return;
        }

        if (!connectionOptions.IsLocal || !Uri.TryCreate(e.Request.Uri, UriKind.Absolute, out var candidate) || LocalWebViewPolicy.IsAllowedResource(candidate))
        {
            return;
        }

        e.Response = WebView.CoreWebView2.Environment.CreateWebResourceResponse(
            new MemoryStream(System.Text.Encoding.UTF8.GetBytes("blocked")),
            403,
            "Forbidden",
            "Content-Type: text/plain; charset=utf-8");
    }

    private async void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        await RunEventHandlerAsync(() => HandleWebMessageReceivedAsync(e));
    }

    private async Task HandleWebMessageReceivedAsync(CoreWebView2WebMessageReceivedEventArgs e)
    {
        if (isDisposed || lifecycle.LifetimeToken.IsCancellationRequested)
        {
            return;
        }

        if (TryGetProjectFolderRequest(e.WebMessageAsJson, e.Source, out var projectFolderRequest))
        {
            await HandleProjectFolderRequestAsync(projectFolderRequest);
            return;
        }

        if (connectionOptions.IsLocal)
        {
            string localMessage;
            try
            {
                localMessage = e.TryGetWebMessageAsString();
            }
            catch (InvalidOperationException)
            {
                localMessage = e.WebMessageAsJson;
            }

            if (string.Equals(localMessage, "exit", StringComparison.Ordinal) &&
                LocalChannelProtocol.IsAllowedTopLevelSource(e.Source))
            {
                await Dispatcher.InvokeAsync(Close);
                return;
            }

            LocalChannelRequest? request = null;
            try
            {
                request = LocalChannelProtocol.ParseRequest(localMessage, e.Source);
            }
            catch (LocalChannelProtocolException)
            {
            }
            var localResponse = await localCompositionRoot.DispatchAsync(localMessage, e.Source);
            if (isDisposed || lifecycle.LifetimeToken.IsCancellationRequested)
            {
                return;
            }

            WebView.CoreWebView2.PostWebMessageAsString(localResponse);
            if (request?.Operation == "executionProfile.update")
            {
                ApplyExecutionProfileUpdate(localResponse);
            }
            return;
        }

        string response;
        try
        {
            var allowedOrigin = connectionOptions.IsLocal
                ? LocalWebViewPolicy.Origin
                : connectionOptions.ServerUri!;
            var request = BridgeProtocol.ParseRequest(e.WebMessageAsJson, e.Source, allowedOrigin);
            response = request.MessageType == "cancel"
                ? BridgeProtocol.Cancelled(request.RequestId)
                : BridgeProtocol.Success(request);
        }
        catch (BridgeProtocolException exception)
        {
            response = BridgeProtocol.Error(exception.Code, exception.Message, exception.RequestId);
        }
        catch (JsonException)
        {
            response = BridgeProtocol.Error("invalid_json", "Bridgeメッセージの形式が正しくありません。", null);
        }
        catch (Exception)
        {
            response = BridgeProtocol.Error("bridge_error", "Bridgeメッセージを処理できませんでした。", null);
        }
        if (!isDisposed && !lifecycle.LifetimeToken.IsCancellationRequested)
        {
            WebView.CoreWebView2.PostWebMessageAsJson(response);
        }
    }

    private bool TryGetProjectFolderRequest(string json, string source, out BridgeRequest request)
    {
        request = null!;
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object ||
                document.RootElement.GetProperty("operation").GetString() != BridgeProtocol.SelectProjectFolder)
            {
                return false;
            }

            var allowedOrigin = connectionOptions.IsLocal
                ? LocalWebViewPolicy.Origin
                : connectionOptions.ServerUri!;
            request = BridgeProtocol.ParseRequest(json, source, allowedOrigin);
            return true;
        }
        catch (Exception) when (json.Length <= BridgeProtocol.MaxMessageBytes)
        {
            return false;
        }
    }

    private async Task HandleProjectFolderRequestAsync(BridgeRequest request)
    {
        string response;
        try
        {
            response = await projectFolderPickerBridge.DispatchAsync(request, this, lifecycle.LifetimeToken);
        }
        catch (BridgeProtocolException exception)
        {
            response = BridgeProtocol.Error(exception.Code, exception.Message, exception.RequestId, BridgeProtocol.SelectProjectFolder);
        }
        catch (Exception)
        {
            response = BridgeProtocol.Error("bridge_error", "フォルダー選択を処理できませんでした。", request.RequestId, BridgeProtocol.SelectProjectFolder);
        }

        if (!isDisposed && !lifecycle.LifetimeToken.IsCancellationRequested)
        {
            WebView.CoreWebView2.PostWebMessageAsJson(response);
        }
    }

    private async void ApplyExecutionProfileUpdate(string responseJson)
    {
        await RunEventHandlerAsync(() => ApplyExecutionProfileUpdateAsync(responseJson));
    }

    private async Task ApplyExecutionProfileUpdateAsync(string responseJson)
    {
        if (isDisposed || lifecycle.LifetimeToken.IsCancellationRequested)
        {
            return;
        }

        try
        {
            if (LocalChannelProtocol.ParseMessage(responseJson) is not LocalChannelResponse response || response.Result is not JsonElement result)
            {
                return;
            }

            var profile = JsonSerializer.Deserialize<ExecutionProfile>(result.GetRawText(), ProfileJsonOptions);
            if (profile is null)
            {
                return;
            }

            connectionOptions = ServerConnectionOptions.FromProfile(profile);
            openLocalSettings = false;
            UpdateServerUrlText();
            await ConnectAsync();
        }
        catch (JsonException)
        {
        }
        catch (LocalChannelProtocolException)
        {
        }
    }

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        Dispose();
    }

    public void Dispose()
    {
        if (isDisposed)
        {
            return;
        }

        isDisposed = true;
        lifecycle.Dispose();
        UnregisterWebViewEvents();
        localCompositionRoot.Dispose();
        WebView.Dispose();
        GC.SuppressFinalize(this);
    }

    private void RegisterWebViewEvents()
    {
        if (webViewEventsRegistered || WebView.CoreWebView2 is null)
        {
            return;
        }

        WebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
        WebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
        WebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
        WebView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        WebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
        WebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        webViewEventsRegistered = true;
    }

    private void UnregisterWebViewEvents()
    {
        if (!webViewEventsRegistered || WebView.CoreWebView2 is null)
        {
            return;
        }

        WebView.CoreWebView2.NavigationStarting -= CoreWebView2_NavigationStarting;
        WebView.CoreWebView2.NavigationCompleted -= CoreWebView2_NavigationCompleted;
        WebView.CoreWebView2.NewWindowRequested -= CoreWebView2_NewWindowRequested;
        WebView.CoreWebView2.WebResourceRequested -= CoreWebView2_WebResourceRequested;
        WebView.CoreWebView2.WebMessageReceived -= CoreWebView2_WebMessageReceived;
        webViewEventsRegistered = false;
    }

    private async Task RunEventHandlerAsync(Func<Task> handler)
    {
        try
        {
            await handler();
        }
        catch (OperationCanceledException) when (isDisposed || lifecycle.LifetimeToken.IsCancellationRequested)
        {
        }
        catch (Exception) when (!isDisposed)
        {
            SetMessage("Web UIを表示できません。", "WebView2の初期化に失敗しました。再試行してください。");
            ShowFallbackActions(false);
            RetryButton.IsEnabled = true;
        }
    }

    private void SetMessage(string title, string description)
    {
        MessageTitle.Text = title;
        MessageDescription.Text = description;
        StatusText.Text = title;
    }

    private static bool IsRuntimeMissing(Exception exception) =>
        exception.GetType().Name.Contains("RuntimeNotFound", StringComparison.OrdinalIgnoreCase) ||
        exception.Message.Contains("WebView2", StringComparison.OrdinalIgnoreCase) && exception.Message.Contains("runtime", StringComparison.OrdinalIgnoreCase);
}

public sealed class WebAssetUnavailableException : Exception
{
}
