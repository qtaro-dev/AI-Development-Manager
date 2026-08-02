# P0-025 FTS索引・検索最適化契約

版: 1.0-p0-025
状態: 測定完了、現方式の正式採用は保留
基準日: 2026-08-02

## 測定環境

- .NET SDK: 10.0.302（`global.json`固定値と`dotnet --version`実測値が一致）
- Runtime: 10.0.10 / Windows x64
- 固定seed: 15015、10,000件、合計999,740,243 bytesの匿名合成Markdown
- 測定対象: unicode61外部コンテンツ、パス・見出し限定trigram、全検索列trigram比較対照
- 証跡: `%TEMP%\AI-Development-Manager\poc\P0-025\<run-id>\result.json`

## 結果

| 構成 | 構築ms | DB bytes | 標準日本語p95 | integrity | 採否 |
|---|---:|---:|---:|---|---|
| unicode61 external-content | 32,969.8 | 1,120,768,000 | 1,063.640 | 合格 | 保留 |
| scoped trigram（パス・ファイル名・見出し） | 31,412.2 | 1,121,992,704 | 1,247.367 | 合格 | 保留 |
| full trigram（比較対照） | 63,966.7 | 1,557,057,536 | 2,827.656 | 合格 | 不採用候補 |

全構成で更新、改名、削除、再構築、`PRAGMA integrity_check`は合格した。scoped trigramはパス部分一致を検出し、full trigramは本文の任意位置部分一致を検出した。一方、unicode61の日本語中ヒット語は正解集合を満たさず、標準検索p95 500ms、広域検索p95 1,000msも未達だった。

## 判定

P0-025は測定完了とするが、現測定方式の正式採用は保留する。unicode61は容量と保守性の基礎候補、scoped trigramはパス・識別子の限定フォールバック候補、full trigramは本文部分一致の品質比較用だが、容量・検索時間が大きい。日本語検索品質、実データ検索語、依存パッケージ安全性はP0-023およびP0-026で最終判断する。
