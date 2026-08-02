# P1-019 Serverによる共通Web UI配信

## 目的

Reactの同一production build成果物をASP.NET Core Serverから配信し、通常ブラウザとWPFが同じURL・API境界を利用できるようにする。

## 背景

共通Web UIはWPFへ別実装せず、Server配信物をWebView2で表示する方針が確定している。APIとSPA fallbackの競合を避ける必要がある。

## 前提・依存関係

- P1-006完了
- P1-009完了
- P1-013完了
- P1-017完了
- ADR-003、ADR-010

## 対象範囲

- production Web assetsのServer配信
- cache headerとindex更新
- SPA route fallback
- `/api/v1`、health、静的assetsの経路分離
- Build時のWeb成果物取込

## 対象外

- 認証Cookie
- HTTPS・LAN配信
- 業務API・業務画面

## 対象ファイルまたは対象モジュール

- `src/Adm.Server.Host`
- `src/Adm.Web`
- Build統合設定
- Server統合テスト

## 具体的な実装内容

1. Web production buildをServer配布成果物へ再現可能に取り込む。
2. hash付きassetとindex.htmlのcache方針を分ける。
3. SPA routeをindexへfallbackする。
4. API、OpenAPI、healthの404をSPAへ変換しない。
5. bundle欠落時に明示的な起動・配布エラーを出す。

## テスト内容

- `/`とSPA深いRouteの表示
- hash付きasset取得
- `/api/v1`とhealthの非fallback
- Web build欠落時の失敗
- console Hostからのブラウザ表示
- キャッシュ更新後の新index取得

## 完了条件

- 一つのWeb build成果物をServerから表示できる。
- 通常ブラウザと後続WebView2が同じURLを利用できる。
- API/healthとSPA fallbackが競合しない。
- 開発Serverだけに依存せず配布成果物で動作する。

## ユーザーが目視確認する内容

- Server起動後のブラウザ表示
- 直接URL入力と再読込
- Build版情報

## 想定されるリスク

- 古いindexと新しいassetのキャッシュ不整合
- API 404をUIとして200返却する
- Node buildが.NET build順序へ不安定に結合する

## 完了後に更新すべき設計資料

- Web配信・配置設計
- `design/01_INTEGRATED_BASIC_DESIGN.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

- `Adm.Web`のproduction buildを.NET build時に生成し、Server出力先の`wwwroot`へ取り込むMSBuild targetを追加した。
- `index.html`、hash付きasset、その他assetのキャッシュ方針を実装した。
- `/`とSPAの深いrouteを`index.html`へfallbackし、`/api`、`/health`、`/openapi`はfallback対象外とした。
- `index.html`欠落時は、Web UI配布物不足を示す起動エラーにした。
- TestServerを使い、production bundle、SPA route、hash付きasset、予約経路、欠落bundleを検証した。

### 画面確認

```powershell
cd "D:\Dev\AI Development Manager"
dotnet run --project src/Adm.Server.Host -- --Server:Port=5181
```

ブラウザで`http://127.0.0.1:5181/`を開き、AppShell、状態表示カタログ、直接URL入力（例 `/projects/demo`）と再読込を確認する。終了はコンソールで`Ctrl+C`。P1-019では認証・HTTPS・LAN配信は対象外である。
