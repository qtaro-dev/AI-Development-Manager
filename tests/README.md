# Product tests

製品自動テストは責務ごとに分離します。参照方向・Windows依存境界の検査は`Adm.Architecture.Tests`（P1-003）に、xUnit・TestServerはP1-004のテストプロジェクト群に配置します。

P1-002ではテストコードやPoCテストを製品側へコピーしません。
