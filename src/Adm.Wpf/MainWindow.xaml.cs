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
    private readonly ExecutionProfileService executionProfiles;
    private readonly string[] commandLineArgs;
    private bool openLocalSettings;
    private bool isInitialized;
    private bool isDisposed;

    public MainWindow()
    {
        InitializeComponent();
        commandLineArgs = Environment.GetCommandLineArgs().Skip(1).ToArray();
        var allowLoopbackHttp = commandLineArgs.Contains("--allow-loopback-http", StringComparer.OrdinalIgnoreCase);
        executionProfiles = new ExecutionProfileService(new JsonExecutionProfileStore(), allowLoopbackHttp);
        localCompositionRoot = new LocalCompositionRoot(executionProfiles);
        connectionOptions = ServerConnectionOptions.FromArguments(commandLineArgs);
        UpdateServerUrlText();
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        if (!ServerConnectionOptions.HasServerUrlArgument(commandLineArgs))
        {
            var saved = await executionProfiles.GetAsync();
            connectionOptions = ServerConnectionOptions.FromProfile(saved.Profile);
            UpdateServerUrlText();
        }

        await ConnectAsync();
    }

    private async void RetryButton_Click(object sender, RoutedEventArgs e) => await ConnectAsync();

    private async Task ConnectAsync()
    {
        RetryButton.IsEnabled = false;
        FallbackActions.Visibility = Visibility.Collapsed;
        MessagePanel.Visibility = Visibility.Visible;
        WebView.Visibility = Visibility.Collapsed;

        if (connectionOptions.IsLocal)
        {
            SetMessage("ローカルUIを準備しています。", "サーバーに接続せず、このアプリに組み込まれた画面を表示しています。");
        }

        if (!connectionOptions.IsLocal && !await WaitForServerAsync())
        {
            SetMessage("サーバーに接続できません。", "このPCだけで続けるか、接続先を確認してから再試行できます。");
            ShowFallbackActions(true);
            RetryButton.IsEnabled = true;
            return;
        }

        try
        {
            await InitializeWebViewAsync();
            WebView.Source = connectionOptions.IsLocal
                ? GetLocalStartUri()
                : connectionOptions.ServerUri;
            WebView.Visibility = Visibility.Visible;
            MessagePanel.Visibility = Visibility.Collapsed;
            StatusText.Text = connectionOptions.IsLocal
                ? "ローカルUIを表示しています。"
                : "共通Web UIを表示しています。";
        }
        catch (WebAssetUnavailableException)
        {
            SetMessage("Web UIの配布物がありません。", "アプリを修復または再インストールしてから、再試行してください。終了する場合は、この画面を閉じてください。");
            ShowFallbackActions(false);
            RetryButton.IsEnabled = true;
        }
        catch (Exception exception) when (IsRuntimeMissing(exception))
        {
            SetMessage("WebView2 Runtimeが必要です。", "Microsoft Edge WebView2 Runtimeをインストールしてから、再試行してください。");
            ShowFallbackActions(false);
            RetryButton.IsEnabled = true;
        }
        catch (Exception)
        {
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
        openLocalSettings = false;
        connectionOptions = new ServerConnectionOptions(WpfExecutionMode.Local, null);
        UpdateServerUrlText();
        await ConnectAsync();
    }

    private async void OpenSettingsButton_Click(object sender, RoutedEventArgs e)
    {
        openLocalSettings = true;
        connectionOptions = new ServerConnectionOptions(WpfExecutionMode.Local, null);
        UpdateServerUrlText();
        await ConnectAsync();
    }

    private void ExitButton_Click(object sender, RoutedEventArgs e) => Close();

    private async Task InitializeWebViewAsync()
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
            .WaitAsync(WebViewInitializationTimeout);
        await WebView.EnsureCoreWebView2Async(environment)
            .WaitAsync(WebViewInitializationTimeout);
        WebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
        WebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
        WebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
        WebView.CoreWebView2.AddWebResourceRequestedFilter("*", CoreWebView2WebResourceContext.All);
        WebView.CoreWebView2.WebResourceRequested += CoreWebView2_WebResourceRequested;
        WebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
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

    private async Task<bool> WaitForServerAsync()
    {
        var deadline = DateTimeOffset.UtcNow + ReadinessTimeout;
        var readinessUri = new Uri(connectionOptions.ServerUri!, "health/ready");

        while (DateTimeOffset.UtcNow < deadline)
        {
            try
            {
                using var response = await httpClient.GetAsync(readinessUri);
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

            await Task.Delay(250);
        }

        return false;
    }

    private void CoreWebView2_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
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
        if (connectionOptions.IsLocal)
        {
            LocalChannelRequest? request = null;
            try
            {
                request = LocalChannelProtocol.ParseRequest(e.WebMessageAsJson, e.Source);
            }
            catch (LocalChannelProtocolException)
            {
            }
            var localResponse = await localCompositionRoot.DispatchAsync(e.WebMessageAsJson, e.Source);
            WebView.CoreWebView2.PostWebMessageAsJson(localResponse);
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
        WebView.CoreWebView2.PostWebMessageAsJson(response);
    }

    private async void ApplyExecutionProfileUpdate(string responseJson)
    {
        try
        {
            if (LocalChannelProtocol.ParseMessage(responseJson) is not LocalChannelResponse response || response.Result is not JsonElement result)
            {
                return;
            }

            var profile = JsonSerializer.Deserialize<ExecutionProfile>(result.GetRawText(), ProfileJsonOptions);
            if (profile is null || profile.Mode != ExecutionProfileMode.Server)
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
        localCompositionRoot.Dispose();
        WebView.Dispose();
        GC.SuppressFinalize(this);
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
