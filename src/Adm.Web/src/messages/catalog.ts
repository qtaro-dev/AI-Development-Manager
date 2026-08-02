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
} as const;

export type MessageKey = keyof typeof catalog;

type MessageArguments = {
    "common.retryCount": { count: number };
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
