type WebViewHost = {
    postMessage: (message: string) => void;
};

type WebViewWindow = Window & {
    chrome?: {
        webview?: WebViewHost;
    };
};

/** Requests the embedded WPF shell to close the client window. */
export function requestHostExit(): void {
    const webView = (window as WebViewWindow).chrome?.webview;
    if (webView) {
        webView.postMessage("exit");
        return;
    }

    window.close();
}
