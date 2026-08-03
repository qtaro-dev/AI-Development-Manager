# Adm.Wpf

WPF Clientの配置です。引数なしではLocal modeとして、WPF出力内の共通React Web UIをWebView2で表示します。Local modeはServer、Kestrel、HTTP API、localhostポートを使用しません。Server modeを明示する場合だけ`--server-url=http://127.0.0.1:5181/`を指定します。WPF終了時にServerを停止する処理は持ちません。

Local modeの仮想originは`https://app.ai-development-manager.local/index.html`、UserDataFolderは`%LOCALAPPDATA%\AI Development Manager\WebView2\Local`です。外部Navigation、外部Resource、新規Windowは拒否します。WebView2 RuntimeまたはWeb資産が不足した場合は日本語案内と再試行を表示します。

参照規則: WPF固有処理をこの境界に閉じ込め、Core/Applicationへの依存は必要最小限とします。Local Application Channel、業務DataAccess、追加Bridge操作は後続チケットで扱い、`poc/`は参照しません。
