# Web E2Eテスト方針（P1-022）

## 対象

P1-022では、実ASP.NET Core Serverが配信するproduction Web成果物をChromium系ブラウザで確認する。認証、業務フロー、WebView2自動操作、全面的なpixel一致は対象外とする。

## 実行方法

`tests/Adm.Web.E2E`の固定lockfileから`npm ci`し、`npm run install:browsers`でChromiumを導入する。`npm test`がDebug build済みの`Adm.Server.Host.dll`を一時ポート5199で自動起動し、readiness確認後に終了させる。CIでは`CI=1`、worker 1、失敗時retry 2回で実行し、ローカルでは既存Serverを再利用できる。

## 回帰項目と証拠

- 1440、820、320pxのshell・本文・Bridge許可操作一覧と代表screenshot
- deep linkのSPA fallbackと再読込
- light／darkの表示
- Tab／Enter／Escapeによる確認Dialogのfocus復帰
- JavaScript console error、page error、HTTP 4xx/5xx応答の検出
- 失敗時のscreenshot、trace、video、JUnit結果

証拠は再生成可能な`artifacts/ci-evidence/playwright`へ出力し、CI artifactとして保持する。`artifacts/`、`test-results/`、ブラウザキャッシュはGit管理対象にしない。

## 安定化境界

動的時刻、乱数、実データ、認証状態をテストへ持ち込まない。画面の主要構造と操作可能性をrole、text、状態、viewportで確認し、全面的なpixel比較は行わない。実ブラウザのIME、DPI、WebView2差分はP1-023へ引き継ぐ。
