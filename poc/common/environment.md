# PoC共通評価環境

版: 1.0
対象: P0-003
取得日: 2026-08-02
状態: 基準環境確定

## 1. 目的

後続PoCの性能、UI、互換性、安全性の結果を同じ条件で比較するための基準環境を定義する。ここに記録する値は測定条件であり、製品の動作保証範囲を自動的に拡張しない。

## 2. 実測した基準PC

| 項目 | 基準値 | 取得方法 |
|---|---|---|
| OS | Windows 11 Pro 64-bit / version 10.0.26200 / build 26200 | `Get-CimInstance Win32_OperatingSystem` |
| CPU | Intel Core i7-8700 @ 3.20 GHz / 6 cores / 12 logical processors | `Get-CimInstance Win32_Processor` |
| RAM | 68,658,524,160 bytes（約64 GiB） | `Get-CimInstance Win32_ComputerSystem` |
| 評価ディスク | D: / NTFS / 1,000,081,453,056 bytes / 空き259,738,820,608 bytes（約242 GiB） | `Get-CimInstance Win32_LogicalDisk` |
| GPU・画面 | NVIDIA GeForce RTX 4060 Ti / 1920 x 1080 | `Get-CimInstance Win32_VideoController` |
| システムDPI | 96 DPI（100%） | `user32!GetDpiForSystem()` |
| .NET SDK | 9.0.316 | `dotnet --version` |
| Edge | 150.0.4078.105 | `msedge.exe` FileVersion |
| Chrome | 150.0.7871.187 | `chrome.exe` FileVersion |
| WebView2 Runtime | 150.0.4078.105 | `Microsoft\EdgeWebView\Application` |

### 再取得コマンド

実行時の日時、ユーザー、秘密情報はログへ含めない。結果はこの表の形式へ転記する。

```powershell
Get-CimInstance Win32_OperatingSystem | Select Caption,Version,BuildNumber,OSArchitecture
Get-CimInstance Win32_Processor | Select -First 1 Name,NumberOfCores,NumberOfLogicalProcessors,MaxClockSpeed
Get-CimInstance Win32_ComputerSystem | Select TotalPhysicalMemory
Get-CimInstance Win32_LogicalDisk -Filter "DeviceID='D:'" | Select DeviceID,FileSystem,Size,FreeSpace
dotnet --version
```

## 3. クライアント評価条件

| 条件 | 必須組合せ | 備考 |
|---|---|---|
| ブラウザ | Edge、Chrome | 同一ビルド番号を結果へ記録する |
| WPF埋込 | WebView2 Evergreen Runtime | Runtime版を結果へ記録する |
| DPI | 100%、125%、150%、200% | UI PoCでは各倍率で主要操作を確認する |
| 解像度 | 1920 x 1080を基準、狭幅ケースも別途確認 | 解像度とウィンドウサイズを分けて記録する |
| 表示モード | ライト、ダーク | 色だけでなく文字・アイコンも確認する |
| 言語・入力 | 日本語Windows、日本語IME | Tab、Enter、Esc、Ctrl+Sを確認する |

100%以外のDPIは、Windows表示設定を変更後にサインイン状態、表示倍率、ブラウザ／WebView2を記録してから測定する。DPIの違う結果を同一集計へ混ぜない。

## 4. ツールチェーン差異

設計上の製品基準は.NET 10 LTSだが、この基準取得時点の実機には.NET SDK 9.0.316しかインストールされていない。P0-002のPoCは`net9.0`でビルドしたため、.NET 10固有の性能・API互換性をこの資料から推定しない。.NET 10を導入した時点で、同じ取得コマンドを再実行し、環境版を更新する。

## 5. ログと証拠の保存場所

- 生ログ、一時計測ファイル、画面録画: `%TEMP%\AI-Development-Manager\poc\<ticket>\<run-id>\`
- 結果の要約: 対象チケットの「実施結果」節またはレビュー用の小容量Markdown
- 再生成可能な`bin/`、`obj/`、CSV中間ファイル: コミットしない
- 証明書、トークン、実データ、バックアップ、キャッシュ: 保存もコミットもしない

`run-id`は`yyyyMMdd-HHmmss-<short-commit>`形式とする。ログにはコマンド、開始・終了時刻、環境識別子、終了コード、エラー件数を含め、認証情報と実データ本文は含めない。
