# Adm.Web

React 19、TypeScript、Viteによる製品Web UI基盤です。

## Commands

```powershell
npm ci
npm run typecheck
npm run lint
npm run format:check
npm run build
npm run verify:bundle
npm run dev
```

依存は`package.json`と`package-lock.json`へ固定しています。`VITE_`環境変数は公開bundleへ埋め込まれるため、APIの公開URLなど秘密でない値だけを許可します。認証情報、トークン、秘密鍵、業務データは設定やbundleへ含めません。

API clientは`src/api/client.ts`の差込境界から提供し、P1-013では業務APIを呼び出しません。製品Webの業務画面、文言辞書、Theme、Server配信は後続チケットの責務です。
