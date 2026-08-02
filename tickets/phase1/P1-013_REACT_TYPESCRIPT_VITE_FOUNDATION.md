# P1-013 React・TypeScript・Vite製品基盤

## 目的

React 19、TypeScript、Viteを使用する製品用共通Web UIプロジェクトを作り、依存とビルドを再現可能に固定する。

## 背景

P0-018でReact構成を第一候補として確認し、P0-023で条件付き採用した。PoCの`latest`依存を製品へ流用せず、実機確認可能な製品基盤として再構築する。

## 前提・依存関係

- P1-001完了
- P1-002完了
- `design/21_WEB_UI_CONTRACT.md`
- ADR-003

## 対象範囲

- React 19、TypeScript、Vite
- 製品用package.jsonとlockfile
- Strict TypeScript、lint/format/build
- 開発Serverと本番build
- API client差込境界

## 対象外

- React採否の再比較
- 業務画面、認証、検索
- Serverからの本番配信

## 対象ファイルまたは対象モジュール

- `src/Adm.Web`
- Web共通設定
- ルートNode版基準

## 具体的な実装内容

1. 製品用React/TypeScript/Viteプロジェクトを作る。
2. 実装時点の採用メジャー内安全版を固定し、lockfileを作る。
3. TypeScript strict、import alias、生成物配置を統一する。
4. 環境値を型付き境界から読み、秘密値をbundleへ埋め込まない。
5. API clientをモックと実Serverへ差替えられる境界にする。

## テスト内容

- clean install
- typecheck、lint/format、production build
- lockfile不一致検出
- bundleへの秘密・ローカル絶対パス非混入
- 開発Server表示

## 完了条件

- 固定Node依存から再現buildできる。
- `latest`や無制限メジャー追従が残っていない。
- TypeScript strict違反で品質ゲートが失敗する。
- PoCコード・モック業務画面が製品へ混入していない。
- 後続Theme、辞書、Server配信を追加できる。

## ユーザーが目視確認する内容

- 最小Web画面の表示
- 使用バージョンと依存監査結果
- development/production buildの違い

## 想定されるリスク

- PoC依存をそのままコピーする
- Vite環境変数へ秘密を入れる
- package更新でlockfile再現性を失う

## 完了後に更新すべき設計資料

- ADR-003
- Web UI構成設計
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`

## 実装結果

- `src/Adm.Web`へReact 19、TypeScript、Viteの製品基盤を新規作成した。
- `package.json`と`package-lock.json`で全依存を固定し、Node.js 22.18.0を基準にした。
- TypeScript strict、import alias、ESLint、Prettier、production buildを設定した。
- `VITE_API_BASE_URL`だけを公開設定値として型付きで読み込み、秘密値をbundleへ入れない境界を作った。
- `src/api/client.ts`へ実Server／将来の差替えに対応する最小API client境界を追加した。
- production bundleの秘密・Bearer値・秘密環境キー・Windows絶対パス検査をCIへ追加した。
- PoC UI、モック業務画面、認証、業務API、Server配信、P1-014以降のテスト基盤は実装していない。

## 検証結果

使用Node.js: `22.18.0`、npm: `10.9.3`。

固定依存:

- React / React DOM `19.2.8`
- TypeScript `6.0.3`
- Vite `8.2.0`
- `@vitejs/plugin-react` `6.0.5`

実行したコマンド:

```powershell
npm ci --prefix .\src\Adm.Web
npm run typecheck --prefix .\src\Adm.Web
npm run lint --prefix .\src\Adm.Web
npm run format:check --prefix .\src\Adm.Web
npm run build --prefix .\src\Adm.Web
npm run verify:bundle --prefix .\src\Adm.Web
```

結果:

- npm clean install成功、脆弱性0件
- typecheck、lint、format check成功
- production build成功
- bundle安全検査成功
- 全品質ゲート成功

P1-013は完了状態とし、P1-014以降は対象外とする。
