# P1-029 WebView2 Offline UI PoC

独立したWPF／WebView2 PoC。製品ソリューション、`src/`、`tests/`、`installer/`へ参照・登録しない。

## 実行

リポジトリ直下で固定SDKを確認し、次を実行する。

```powershell
pwsh -NoProfile -File .\poc\p1-029-webview2-offline-ui\Run-Poc.ps1 -Configuration Release
```

既存の`src/Adm.Web/dist`を再Buildし、PoC専用の`artifacts/p1-029-webview2-offline-ui/assets`へコピーする。PoCは`https://p1-029.local/`へ仮想ホスト名を割り当て、WebView2のローカルフォルダーMappingだけで表示する。Server、Kestrel、HTTP API、localhost待受は起動しない。

## 記録

- `artifacts/p1-029-webview2-offline-ui/measurements/`: 5回の起動測定
- `artifacts/p1-029-webview2-offline-ui/run-*/telemetry.json`: Navigation、Resource、Console、起動状態
- `artifacts/p1-029-webview2-offline-ui/run-1/screenshot-initial.png`: 代表スクリーンショット

生成物、WebView2 UserData、ログ、スクリーンショットは`artifacts/`配下であり、Gitへ追加しない。
