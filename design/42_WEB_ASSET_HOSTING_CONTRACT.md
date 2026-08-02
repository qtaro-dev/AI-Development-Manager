# Webアセット配信契約

版: 0.1-p1-019
状態: P1-019実装済み
基準日: 2026-08-03

## 配布物

`Adm.Web`のproduction buildを.NET build時に生成し、同じServer成果物の`wwwroot`へ取り込む。実行時は`index.html`の存在を起動条件とし、欠落時はWeb UIの配布物不足として起動を拒否する。

## 経路境界

| 経路 | 動作 |
|---|---|
| `/` | `index.html` |
| hash付き静的asset | `wwwroot`から配信 |
| その他のGET/HEAD UI route | `index.html`へSPA fallback |
| `/api`、`/health`、`/openapi` | SPA fallback対象外。404または既存契約を返す |

## キャッシュ

- `index.html`: `no-cache, no-store`
- Viteのhash付きasset: `public, max-age=31536000, immutable`
- その他asset: `no-cache`

## 境界

P1-019では認証、HTTPS、LAN配信、業務API、業務画面を実装しない。Web buildの失敗や配布物欠落は、Serverの起動・配布エラーとして明示する。
