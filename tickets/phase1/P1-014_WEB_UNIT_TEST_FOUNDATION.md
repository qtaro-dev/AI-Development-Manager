# P1-014 Web単体テスト基盤

## 目的

Reactコンポーネントの表示、操作、状態、アクセシビリティをVitestとTesting Libraryで検証する標準基盤を作る。

## 背景

P0-021のガードレールを後続UIで維持するには、目視確認だけでなく小さな部品単位の自動回帰が必要である。

## 前提・依存関係

- P1-013完了
- `design/ui/ui-regression-checklist.md`

## 実装結果

- Vitest 4.1.10、jsdom 29.1.1、Testing Library、user-event、`@vitest/coverage-v8`を固定依存として追加した。
- `vitest.config.ts`でjsdom、共通setup、coverage閾値、テスト対象を標準化した。
- `src/test/test-utils.tsx`に共通render入口を追加し、後続のRouter／Theme／辞書Providerを差し込めるwrapper境界を用意した。
- App表示、role／label、Tab／Enter／Escape、loading／error、fake timer、fetch mock境界、ARIAをサンプルテストで検証した。
- Playwright、WebView2、業務画面、P1-015以降のProvider実装は対象外とした。

## 再現コマンド

```powershell
cd D:\Dev\AI Development Manager\src\Adm.Web
npm ci
npm run test
```

`coverage/`は生成物として`.gitignore`へ追加し、Gitへ含めない。

## 対象範囲

- Vitest、Testing Library、DOM環境
- user-event、fake timer、fetch mock境界
- アクセシビリティ基礎検査
- テスト証拠とcoverage基準

## 対象外

- Playwright E2E
- WebView2実機確認
- 業務画面テスト

## 対象ファイルまたは対象モジュール

- `src/Adm.Web`のテスト設定
- `src/Adm.Web/src/test`

## 具体的な実装内容

1. VitestとTesting Libraryを固定依存で追加する。
2. 共通render、Router、Theme、辞書Provider用fixtureを作る。
3. DOM cleanupとテスト分離を標準化する。
4. キーボード、aria、非同期状態のサンプルテストを作る。
5. coverageを記録し、数値だけを目的にしない規則を作る。

## テスト内容

- テキスト・role・labelによる要素取得
- Tab/Enter/Escape操作
- 非同期loading/error状態
- aria-label欠落の検出例
- 並列・繰返し実行

## 完了条件

- clean install後にWeb単体テストを再現実行できる。
- 実装詳細でなく利用者から見えるrole/label/状態を検証する。
- キーボードとアクセシビリティの退行を検出できる。
- 後続コンポーネントが共通fixtureを再実装しない。

## ユーザーが目視確認する内容

- テスト対象と検査できる操作一覧
- 意図的なARIA欠落が失敗する例

## 想定されるリスク

- Snapshotだけに依存する
- ブラウザ実挙動をDOM模擬だけで合格扱いにする
- 共通fixtureが製品挙動を隠す

## 完了後に更新すべき設計資料

- Web UIテスト方針
- `design/ui/ui-regression-checklist.md`
- `tickets/phase1/00_PHASE_1_TICKET_INDEX.md`
