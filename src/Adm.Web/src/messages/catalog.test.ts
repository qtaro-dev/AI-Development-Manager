import { describe, expect, it } from "vitest";
import { message, messageKeys } from "./catalog";

describe("Japanese message catalog", () => {
    it("exposes every registered key through the typed message API", () => {
        expect(messageKeys).toHaveLength(18);
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
    });
});
