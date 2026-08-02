# P0-002 Server起動方式とWindows依存境界PoC

状態: 完了
完了日: 2026-08-02

## 目的

同じServer業務ロジックをWindows Service、コンソール、手動、任意のトレイ起動で共有し、Windows依存をAdapterへ隔離できることを確認する。

## 前提・依存関係

- P0-001完了

## 対象範囲

- ASP.NET Core Hostの起動方式切替
- Windows Service Adapter
- コンソール起動
- 手動／トレイ起動用の制御境界
- Windows非依存CoreとWindows Adapterの参照方向

## 対象外

- 製品用Serviceインストーラー
- WPF本画面
- Firewall・証明書の実設定
- 業務API実装

## 対象ファイルまたは対象モジュール

- `poc/hosting-modes`
- 設計候補: `Adm.Server.Host`、`Adm.Core`、`Adm.Infrastructure.Windows`

## 具体的な実装内容

1. 同一HostをコンソールとWindows Serviceで起動する最小PoCを作る。
2. 手動／トレイ起動からHost開始・停止を依頼する境界を作る。
3. CoreからWindows固有Assemblyを参照しない依存関係を示す。
4. 起動方式、停止、異常終了、二重起動時の結果を記録する。

## テスト内容

- コンソール起動・停止
- Windows Service相当起動・停止
- 二重起動時の明示エラー
- 起動方式を変えても同じヘルスチェックが返ること
- CoreプロジェクトにWindows固有参照がないこと

## 受け入れ条件

- 4つの起動用途が同一Host構成を再利用できる。
- Windows依存境界が図と参照規則で示される。
- 業務ロジックの起動方式別分岐が不要である。

## ユーザーが目視確認する内容

- 各起動方式の開始・停止結果一覧。
- Windows固有処理が限定された構成図。
- 二重起動時の分かりやすい表示案。

## 想定されるリスク

- トレイ常駐を正式機能へ広げすぎる。
- Service権限と対話ユーザー権限の差を見落とす。

## 完了後に更新すべき設計資料

- `design/01_INTEGRATED_BASIC_DESIGN.md`
- `design/02_TECHNOLOGY_AND_ADR.md` ADR-012
- 配置構成図

## 実施結果

### 成果物

- `poc/hosting-modes/README.md`
- `poc/hosting-modes/HostingModes.sln`
- `poc/hosting-modes/src/Adm.Core/`
- `poc/hosting-modes/src/Adm.Infrastructure.Windows/`
- `poc/hosting-modes/src/Adm.Server.Host/`

### 検証結果

| 確認項目 | 結果 |
|---|---|
| コンソール起動・停止 | 合格。コンソールHostが起動し、`Ctrl+C`相当のプロセス停止が可能 |
| Windows Service相当起動・停止 | 合格。`AddWindowsService`をAdapter内で構成し、`--probe`で同一Hostを確認 |
| 手動／トレイ起動境界 | 合格。`manual`、`tray`が同じHost実装を使用 |
| 二重起動 | 合格。同一ポートの2つ目は`address already in use`で終了 |
| 共通ヘルスチェック | 合格。4モードすべてで`/health`を登録。実起動時HTTP 200 |
| Core参照方向 | 合格。`Adm.Core`はWindows Adapterを参照せず、逆方向のみ |

### 対象外として残した事項

- Windows Serviceの実登録、アンインストール、Service権限差の実機検証
- トレイUIとWPF本画面
- Firewall、証明書、業務API

上記はP0-002の対象外であり、PoC結果から製品コードへ自動昇格させない。
