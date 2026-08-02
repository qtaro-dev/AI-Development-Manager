# Adm.Wpf

WPF Clientの配置です。P1-020では、ASP.NET Core Serverが配信する共通Web UIをWebView2で表示する最小Shellを提供します。Server URLは`--server-url=http://127.0.0.1:5181/`で指定でき、既定値もlocalhostの同URLです。WPF終了時にServerを停止する処理は持ちません。

参照規則: WPF固有処理をこの境界に閉じ込め、Core/Applicationへの依存は後続チケットで必要最小限に追加します。P1-021までは業務データBridgeを追加しません。`poc/`は参照しません。
