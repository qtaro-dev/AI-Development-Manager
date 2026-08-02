import { describe, expect, it } from "vitest";
import { message, messageKeys } from "./catalog";

describe("Japanese message catalog", () => {
    it("exposes every registered key through the typed message API", () => {
        expect(messageKeys).toContain("feedback.savedLabel");
        expect(message("app.title")).toBe("AI Development Manager");
        expect(message("common.retryCount", { count: 2 })).toBe(
            "再試行（2回目）",
        );
    });

    it("keeps reserved common-state wording in Japanese", () => {
        expect(message("common.save")).toBe("保存");
        expect(message("common.cancel")).toBe("取消");
        expect(message("common.retry")).toBe("再試行");
        expect(message("common.conflict")).toContain("競合");
        expect(message("common.blocked")).toBe("ほかの問題で試せません。");
        expect(message("common.checkConnection")).toBe("接続確認");
        expect(message("common.connectionChecking")).toBe("確認中");
        expect(message("common.connectionReady")).toBe("接続済み");
        expect(message("common.connectionFailed")).toBe(
            "接続できませんでした。",
        );
        expect(message("common.connectionDetails")).toBe("接続の詳細");
        expect(message("common.close")).toBe("閉じる");
        expect(message("shell.workspace")).toBe("ワークスペース");
        expect(message("shell.navTickets")).toBe("チケット");
        expect(message("shell.navTestCases")).toBe("テストケース");
        expect(message("shell.navSearch")).toBe("検索");
        expect(message("shell.navKnowledge")).toBe("ナレッジ");
        expect(message("shell.connection")).toBe("Server接続中");
        expect(message("shell.https")).toBe("HTTPS");
        expect(message("shell.settings")).toBe("設定");
        expect(message("shell.roleAdministrator")).toBe("管理者");
        expect(message("shell.skipToContent")).toBe("本文へ移動");
        expect(message("shell.breadcrumbWorkspace")).toBe("Product Core");
        expect(message("shell.breadcrumbSeparator")).toBe("/");
        expect(message("shell.pageDescription")).toContain("共通Web UI");
        expect(message("shell.primaryActions")).toBe("主操作");
        expect(message("shell.navigation")).toBe("メインナビゲーション");
    });
});
