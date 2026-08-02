# P0-003 PoC評価環境・測定基準確定

状態: 完了
完了日: 2026-08-02

## 目的

性能、UI、互換性、安全性PoCを同じ条件で比較できる基準を作る。

## 前提・依存関係

- P0-001完了

## 対象範囲

- 基準PC情報
- Windows 11、DPI、ブラウザ、WebView2条件
- 同時利用者5名、AIクライアント2接続の負荷モデル
- 1万文書のデータ量モデル
- 計測方法と結果テンプレート

## 対象外

- 実際の性能改善
- Windows 10、UNC、NASの正式評価
- 本番監視基盤

## 対象ファイルまたは対象モジュール

- `poc/common/environment.md`
- `poc/common/result-template.md`
- `poc/common/workload-profile.md`

## 具体的な実装内容

1. CPU、メモリ、ディスク、Windows版、DPIを記録する。
2. Edge、Chrome、WebView2 Runtimeの評価条件を決める。
3. 1万文書、添付、同時要求の分布を定義する。
4. 時間、メモリ、CPU、ディスク量、エラー率の計測方法を決める。
5. PoC結果記録テンプレートを作る。

## テスト内容

- 同じ短い処理を3回測定し、テンプレートで再現可能に記録できるか確認する。
- 単位、開始条件、ウォームアップ有無が曖昧でないかレビューする。

## 受け入れ条件

- 後続PoCが同じ形式で結果を比較できる。
- 基準値と測定値が区別される。
- 95パーセンタイル等の計算方法が定義される。

## ユーザーが目視確認する内容

- 基準PCと想定利用規模が実運用像に合うこと。
- 結果表が専門知識なしでも比較できること。

## 想定されるリスク

- 基準PCが実運用PCより高性能になる。
- 合成負荷が実際の使い方を反映しない。

## 完了後に更新すべき設計資料

- `design/01_INTEGRATED_BASIC_DESIGN.md` 非機能目標
- `design/03_PHASE_0_POC_PLAN.md`

## 実施結果

### 成果物

- `poc/common/environment.md`: 実測した基準PC、OS、DPI、ブラウザ、WebView2、ツールチェーン、ログ保存場所
- `poc/common/workload-profile.md`: 10,000文書、添付、5人＋AI 2接続、要求分布、シナリオ
- `poc/common/result-template.md`: Run ID、環境、条件、単位、p50/p95/p99、合否、証拠の記録様式

### 確定した基準

- Windows 11 Pro 64-bit build 26200
- Intel Core i7-8700、6コア／12論理プロセッサ、RAM約64 GiB
- D: NTFS、容量約1 TB、取得時空き約242 GiB
- 96 DPI（100%）、UI確認倍率100／125／150／200%
- Edge 150.0.4078.105、Chrome 150.0.7871.187、WebView2 150.0.4078.105
- 1プロジェクト10,000文書、添付付き10%、人5名＋AI 2接続
- ウォームアップを除外し、同条件を最低3回測定。p95は最近順位法で計算
- 生ログは`%TEMP%\AI-Development-Manager\poc\<ticket>\<run-id>\`へ保存し、生成物はコミットしない

### テンプレート再現確認

実行コマンド:

```powershell
$samples = 1..3 | ForEach-Object {
  $sw = [Diagnostics.Stopwatch]::StartNew()
  $sum = 0.0
  1..10000 | ForEach-Object { $sum += [Math]::Sqrt($_) }
  $sw.Stop()
  $sw.Elapsed.TotalMilliseconds
}
```

測定値は`169.04 ms`、`136.47 ms`、`164.58 ms`、エラー0件。3サンプルのp95は最近順位法により最大値の`169.04 ms`となった。単位、測定回数、p95、エラー数、再現コマンドをテンプレートへ記録できることを確認した。

### 未解決事項

基準実機には.NET SDK 9.0.316がインストールされており、設計上の製品基準.NET 10 LTSは未導入である。.NET 10を使用する後続PoCでは環境を再取得し、SDK差異を比較結果へ明記する。
