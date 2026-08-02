# P1-014 Web単体テスト契約

## 目的

Web部品の利用者向け表示、操作、状態、アクセシビリティを、実装詳細に依存しないDOM単体テストで回帰検査する。

## 固定構成

| 項目 | 固定値 |
|---|---|
| Test runner | Vitest 4.1.10 |
| DOM | jsdom 29.1.1 |
| React Testing Library | 16.3.2 |
| DOM Testing Library | 10.4.1 |
| user-event | 14.6.1 |
| jest-dom | 7.0.0 |
| Coverage | `@vitest/coverage-v8` 4.1.10 |

Node.js 22.18.0で動作する版を固定し、依存関係は`package-lock.json`を正本とする。

## 実行契約

```powershell
cd D:\Dev\AI Development Manager\src\Adm.Web
npm ci
npm run test
```

テストは`src/**/*.test.{ts,tsx}`をjsdom環境で実行する。coverageは`coverage/`へ出力し、Git管理対象外とする。基準値はlines／functions／statements 70%、branches 60%とし、数値だけでなく利用者視点の検査内容を必須とする。

## 共通fixture契約

`src/test/test-utils.tsx`の`renderWithProviders`を共通入口とする。Router、Theme、辞書Providerは後続チケットでwrapperへ追加できるが、各テストで個別再実装しない。fixtureは製品挙動を隠す抽象化を避け、必要な利用者向け境界だけを提供する。

## 検査対象

- role／labelによる要素取得
- Tab、Enter、Escapeによる操作
- loading、ready、errorの非同期状態
- fake timerを使用した時間依存処理
- fetch mockをAPI client境界へ注入した通信検査
- `aria-live`、`role=alert`、dialogのARIA属性

## 対象外

Playwright E2E、実ブラウザ差異、WebView2、業務画面、サーバ配信、認証は後続チケットで扱う。DOMテストの成功を実機確認の代替としない。
