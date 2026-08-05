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
    "startup.eyebrow": "LOCAL FIRST",
    "startup.title": "初回設定: このPCで利用を開始します",
    "startup.description":
        "この画面は初回のみ表示します。次回からはLocalホームへ直接進みます。",
    "startup.localTitle": "Localモード",
    "startup.localDescription":
        "サーバーに接続せず、このアプリに組み込まれた画面を開きます。",
    "startup.continueLocal": "このPCで続ける",
    "startup.openSettings": "接続先を設定",
    "startup.exit": "終了",
    "startup.connectionFailedTitle": "サーバーに接続できません",
    "startup.connectionFailedDescription":
        "このPCだけで続けるか、接続先を確認してから再試行できます。",
    "startup.retry": "もう一度試す",
    "startup.profileTitle": "利用方法",
    "startup.profileDescription":
        "LocalまたはLAN Serverを選択します。Server URLはServer選択時だけ入力できます。",
    "startup.profileLocal": "このPCで利用",
    "startup.profileLocalDescription":
        "サーバーなしでローカルのデータを管理します。",
    "startup.profileServer": "LAN Serverへ接続",
    "startup.profileServerDescription":
        "HTTPSの接続先を指定して共有機能を利用します。",
    "startup.serverUrl": "Server URL",
    "startup.serverUrlDisabled":
        "Local選択中は入力できません（Server選択時に表示）",
    "startup.serverUrlPlaceholder": "https://server.example/",
    "startup.httpsOnly": "HTTPSのみ保存できます。",
    "startup.save": "保存",
    "startup.cancel": "取消",
    "startup.saving": "保存しています。",
    "startup.localReady": "ローカルUIを表示しています。",
    "startup.loading": "基盤を準備しています。",
    "startup.loadingTitle": "準備中",
    "startup.ready": "基盤を利用できます。",
    "startup.degraded": "設定を復旧し、Localモードで継続しています。",
    "startup.degradedTitle": "Localモードへ復旧しました",
    "startup.recovered": "再試行後に基盤を復旧しました。",
    "startup.recoveredTitle": "復旧しました",
    "startup.error": "基盤を準備できません。再試行してください。",
    "startup.errorTitle": "基盤を準備できません",
    "startup.retrying": "再試行しています。",
    "startup.retryingTitle": "再試行中",
    "startup.profileSaveFailed": "設定を保存できませんでした。",
    "startup.invalidServerUrl": "HTTPSのServer URLを入力してください。",
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
    "bridge.eyebrow": "HOST BRIDGE",
    "bridge.title": "WPF Bridge許可操作",
    "bridge.description":
        "WebView2から利用できるWindows操作を許可リストで管理します。",
    "bridge.allowedLabel": "許可されたBridge操作",
    "bridge.allowedGetHostInfo": "Host情報の取得（getHostInfo）",
    "bridge.checkHost": "Host情報を確認",
    "bridge.browserUnavailable":
        "通常のブラウザではHost Bridgeを利用できません。",
    "bridge.rejected": "許可されていないBridgeメッセージは拒否されます。",
    "bridge.securityNote":
        "任意コード実行、任意コマンド実行、自由なファイルアクセスは許可されていません。",
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
