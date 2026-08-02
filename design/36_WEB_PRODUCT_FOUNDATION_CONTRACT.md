# P1-013 Web製品基盤契約

## 1. 固定ツールと依存

| 項目 | 固定値 |
|---|---|
| Node.js | 22.18.0（`.node-version`） |
| React / React DOM | 19.2.8 |
| TypeScript | 6.0.3 |
| Vite | 8.2.0 |
| `@vitejs/plugin-react` | 6.0.5 |
| ESLint / typescript-eslint | 10.8.0 / 8.65.0 |
| Prettier | 3.9.6 |

`src/Adm.Web/package-lock.json`を依存解決の正本とし、`latest`、浮動版、無制限メジャー追従を使用しない。

## 2. 境界

製品Webは`src/Adm.Web`に配置し、P0 PoCを参照しない。`src/api/client.ts`のAPI clientは公開API境界の差込点だけを提供し、P1-013では業務API、認証、画面状態、Server配信を実装しない。

Viteの`VITE_`環境値は公開bundleへ埋め込まれるため、APIの公開URLなど秘密でない値だけを許可する。秘密、トークン、Cookie、接続文字列、個人情報は環境値・ソース・bundleへ含めない。

## 3. 品質ゲート

```powershell
npm ci
npm run typecheck
npm run lint
npm run format:check
npm run build
npm run verify:bundle
```

`verify:bundle`はproduction bundleから秘密らしい代入、Bearer値、秘密環境キー、Windowsローカル絶対パスを検査する。P1-014でVitestとTesting LibraryによるDOM単体テスト基盤を追加し、`npm run test`でcoverage付き回帰検査を実行する。P1-015では`npm run messages:check`で辞書キー、未使用キー、JSX直書き文言を検査する。
