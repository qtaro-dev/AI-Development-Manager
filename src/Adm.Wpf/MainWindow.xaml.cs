using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Windows;
using System.Windows.Navigation;
using Microsoft.Web.WebView2.Core;
using Adm.Wpf.Bridge;
using Adm.Wpf.Shell;

namespace Adm.Wpf;

public partial class MainWindow : Window
{
    private static readonly TimeSpan ReadinessTimeout = TimeSpan.FromSeconds(8);
    private static readonly HttpClient httpClient = new() { Timeout = TimeSpan.FromSeconds(1) };
    private readonly ServerConnectionOptions connectionOptions;
    private bool isInitialized;

    public MainWindow()
    {
        InitializeComponent();
        connectionOptions = ServerConnectionOptions.FromArguments(Environment.GetCommandLineArgs().Skip(1).ToArray());
        ServerUrlText.Text = $"Server: {connectionOptions.ServerUri.AbsoluteUri}";
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await ConnectAsync();

    private async void RetryButton_Click(object sender, RoutedEventArgs e) => await ConnectAsync();

    private async Task ConnectAsync()
    {
        RetryButton.IsEnabled = false;
        MessagePanel.Visibility = Visibility.Visible;
        WebView.Visibility = Visibility.Collapsed;
        SetMessage("Serverへの接続を確認しています。", "Serverが起動していることを確認しています。");

        if (!await WaitForServerAsync())
        {
            SetMessage("Serverに接続できません。", "Serverを起動してから、再試行してください。必要なServer URLは上部に表示しています。");
            RetryButton.IsEnabled = true;
            return;
        }

        try
        {
            await InitializeWebViewAsync();
            WebView.Source = connectionOptions.ServerUri;
            WebView.Visibility = Visibility.Visible;
            MessagePanel.Visibility = Visibility.Collapsed;
            StatusText.Text = "共通Web UIを表示しています。";
        }
        catch (Exception exception) when (IsRuntimeMissing(exception))
        {
            SetMessage("WebView2 Runtimeが必要です。", "Microsoft Edge WebView2 Runtimeをインストールしてから、再試行してください。");
            RetryButton.IsEnabled = true;
        }
        catch (Exception)
        {
            SetMessage("Web UIを表示できません。", "WebView2の初期化に失敗しました。再試行してください。");
            RetryButton.IsEnabled = true;
        }
    }

    private async Task InitializeWebViewAsync()
    {
        if (isInitialized)
        {
            return;
        }

        var userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AI Development Manager",
            "WebView2");
        var environment = await CoreWebView2Environment.CreateAsync(userDataFolder: userDataFolder);
        await WebView.EnsureCoreWebView2Async(environment);
        WebView.CoreWebView2.NavigationStarting += CoreWebView2_NavigationStarting;
        WebView.CoreWebView2.NavigationCompleted += CoreWebView2_NavigationCompleted;
        WebView.CoreWebView2.NewWindowRequested += CoreWebView2_NewWindowRequested;
        WebView.CoreWebView2.WebMessageReceived += CoreWebView2_WebMessageReceived;
        WebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        isInitialized = true;
    }

    private async Task<bool> WaitForServerAsync()
    {
        var deadline = DateTimeOffset.UtcNow + ReadinessTimeout;
        var readinessUri = new Uri(connectionOptions.ServerUri, "health/ready");

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
        if (!Uri.TryCreate(e.Uri, UriKind.Absolute, out var candidate) || !ShellNavigationPolicy.IsAllowed(connectionOptions.ServerUri, candidate))
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
            SetMessage("Web UIを読み込めません。", "Serverの状態を確認してから、再試行してください。");
            RetryButton.IsEnabled = true;
        }
    }

    private static void CoreWebView2_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e) => e.Handled = true;

    private void CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string response;
        try
        {
            var request = BridgeProtocol.ParseRequest(e.WebMessageAsJson, e.Source, connectionOptions.ServerUri);
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

    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        WebView.Dispose();
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
