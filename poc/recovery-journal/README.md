# P0-010 保存回復ジャーナルPoC

保存・バックアップ・置換・索引更新の途中停止をジャーナルへ記録し、再起動時に安全な自動回復、ユーザー判断待ち、壊れたジャーナル隔離を分類します。製品回復管理画面ではありません。

## 段階

`Prepared` → `TempFlushed` → `BackupCreated` → `Replaced` → `IndexPending` → `Completed`

再起動時は期待する原本ハッシュ、新内容ハッシュ、一時ファイル、バックアップ、現在原本を照合します。外部変更や証拠不足がある場合は原本を変更せず、`NeedsUserDecision`または`CorruptJournalQuarantined`とします。

## 実行

```powershell
dotnet restore .\poc\recovery-journal\RecoveryJournal.sln
dotnet build .\poc\recovery-journal\RecoveryJournal.sln --configuration Release
dotnet .\poc\recovery-journal\src\RecoveryJournal.Poc\bin\Release\net10.0\RecoveryJournal.Poc.dll
```

SDKはリポジトリ直下の`global.json`で固定します。
