const catalog = {
    "app.eyebrow": "PRODUCT WEB FOUNDATION",
    "app.title": "AI Development Manager",
    "app.description":
        "React、TypeScript、Viteで構成された製品Web UI基盤です。",
    "app.apiBoundary": "API境界",
    "app.status": "状態",
    "app.foundationReady": "基盤準備完了",
    "common.checkConnection": "接続確認",
    "common.connectionChecking": "確認中",
    "common.connectionReady": "接続済み",
    "common.connectionFailed": "接続できませんでした。",
    "common.connectionDetails": "接続の詳細",
    "common.close": "閉じる",
    "common.save": "保存",
    "common.cancel": "取消",
    "common.retry": "再試行",
    "common.conflict": "変更が競合しています。",
    "common.blocked": "ほかの問題で試せません。",
    "common.retryCount": "再試行（{{count}}回目）",
    "shell.workspace": "ワークスペース",
    "shell.navTickets": "チケット",
    "shell.navTestCases": "テストケース",
    "shell.navSearch": "検索",
    "shell.navKnowledge": "ナレッジ",
    "shell.connection": "Server接続中",
    "shell.https": "HTTPS",
    "shell.settings": "設定",
    "shell.roleAdministrator": "管理者",
    "shell.skipToContent": "本文へ移動",
    "shell.breadcrumbWorkspace": "Product Core",
    "shell.breadcrumbSeparator": "/",
    "shell.pageDescription": "共通Web UIの表示基盤を確認します。",
    "shell.primaryActions": "主操作",
    "shell.navigation": "メインナビゲーション",
    "feedback.savedLabel": "保存済み",
    "feedback.savedTitle": "変更は保存されています",
    "feedback.unsavedLabel": "未保存",
    "feedback.unsavedTitle": "未保存の変更があります",
    "feedback.unsavedDescription": "保存するか、変更を破棄してください。",
    "feedback.conflictLabel": "競合を確認",
    "feedback.conflictTitle": "他の変更と競合しました",
    "feedback.conflictDescription": "最新版・自分の変更・差分を比較できます。",
    "feedback.latestVersion": "最新版を使う",
    "feedback.showDiff": "差分を表示",
    "feedback.uploadingLabel": "アップロード中",
    "feedback.restoringLabel": "復元を検証中",
    "feedback.capacityLabel": "容量警告",
    "feedback.capacityTitle": "保存領域の使用量が80%です",
    "feedback.capacityDescription": "整理候補を確認してください。",
    "feedback.saveErrorLabel": "保存できません",
    "feedback.saveErrorTitle": "保存先を確認してください",
    "feedback.saveErrorDescription":
        "安全な保存先か、アクセス権を確認してください。",
    "feedback.restoreTitle": "復元を検証しています",
    "feedback.restoreDescription": "内容を確認しています。",
    "feedback.emptyTitle": "表示する項目がありません",
    "feedback.emptyDescription": "条件を変えて、もう一度確認してください。",
    "feedback.genericErrorTitle": "問題が起きました",
    "feedback.genericErrorDescription": "時間をおいて再試行してください。",
    "feedback.progressDetails": "{{percent}}%　{{current}} / {{total}}",
    "feedback.confirmationTitle": "確認が必要です",
    "feedback.confirmationDescription":
        "内容を確認してから次へ進んでください。",
    "feedback.closeDialog": "閉じる",
    "feedback.retryAction": "再試行",
    "feedback.cancelAction": "取消",
    "feedback.catalogTitle": "状態表示の見本",
    "feedback.catalogDescription":
        "色・文言・操作を組み合わせた共通状態部品です。",
    "feedback.openDialog": "確認ダイアログを表示",
    "feedback.dialogBody": "内容を確認してから操作を選択できます。",
} as const;

export type MessageKey = keyof typeof catalog;

type MessageArguments = {
    "common.retryCount": { count: number };
    "feedback.progressDetails": {
        percent: number;
        current: string;
        total: string;
    };
};

export function message<K extends MessageKey>(
    key: K,
    ...args: K extends keyof MessageArguments
        ? [values: MessageArguments[K]]
        : []
): string {
    const template = catalog[key];
    const values = args[0] as Record<string, string | number> | undefined;

    return template.replace(/{{(\w+)}}/g, (_, name: string) => {
        const value = values?.[name];
        return value === undefined ? `{{${name}}}` : String(value);
    });
}

export const messageKeys = Object.keys(catalog) as MessageKey[];
