import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import {
    EmptyState,
    ErrorState,
    FeedbackBanner,
    FeedbackDialog,
    ProgressFeedback,
    StatusIndicator,
    Toast,
} from "./Feedback";
import { message } from "../../messages/catalog";
import { renderWithProviders } from "../../test/test-utils";

describe("feedback components", () => {
    it("gives every status a visible label and semantic role", () => {
        renderWithProviders(
            <div>
                <StatusIndicator kind="saved" />
                <StatusIndicator kind="unsaved" />
                <StatusIndicator kind="conflict" />
                <StatusIndicator kind="connected" />
                <StatusIndicator kind="error" />
                <StatusIndicator kind="warning" />
                <StatusIndicator kind="processing" />
            </div>,
        );

        expect(screen.getAllByRole("status")).toHaveLength(7);
        expect(screen.getByText("保存済み")).toBeVisible();
        expect(screen.getByText("未保存")).toBeVisible();
        expect(screen.getByText("競合を確認")).toBeVisible();
        expect(screen.getByText("Server接続中")).toBeVisible();
        expect(screen.getByText("保存できません")).toBeVisible();
        expect(screen.getByText("容量警告")).toBeVisible();
        expect(screen.getByText("復元を検証中")).toBeVisible();
    });

    it("provides conflict and error next actions without business decisions", async () => {
        const user = userEvent.setup();
        const onAction = vi.fn();
        renderWithProviders(
            <div>
                <FeedbackBanner
                    kind="danger"
                    title={message("feedback.conflictTitle")}
                    description={message("feedback.conflictDescription")}
                    actionLabel={message("feedback.showDiff")}
                    onAction={onAction}
                    requestId="ADM-ERR-024"
                />
                <ErrorState onRetry={onAction} />
            </div>,
        );

        const alerts = screen.getAllByRole("alert");
        expect(alerts).toHaveLength(2);
        expect(alerts[0]).toHaveTextContent("他の変更と競合しました");
        await user.click(screen.getByRole("button", { name: "差分を表示" }));
        await user.click(screen.getByRole("button", { name: "再試行" }));
        expect(onAction).toHaveBeenCalledTimes(2);
    });

    it("supports progress cancellation, retry, toast close, and empty states", async () => {
        const user = userEvent.setup();
        const onCancel = vi.fn();
        const onRetry = vi.fn();
        const onClose = vi.fn();
        renderWithProviders(
            <div>
                <ProgressFeedback
                    kind="upload"
                    value={64}
                    current="12 KB"
                    total="18 KB"
                    onCancel={onCancel}
                />
                <ProgressFeedback
                    kind="restore"
                    value={0}
                    current=""
                    total=""
                    failed
                    onRetry={onRetry}
                />
                <Toast onClose={onClose}>保存を確認しました。</Toast>
                <EmptyState />
            </div>,
        );

        expect(screen.getAllByRole("progressbar")[0]).toHaveAttribute(
            "aria-valuenow",
            "64",
        );
        await user.click(screen.getByRole("button", { name: "取消" }));
        await user.click(screen.getByRole("button", { name: "再試行" }));
        await user.click(screen.getByRole("button", { name: "閉じる" }));
        expect(onCancel).toHaveBeenCalledOnce();
        expect(onRetry).toHaveBeenCalledOnce();
        expect(onClose).toHaveBeenCalledOnce();
        expect(screen.getByText("表示する項目がありません")).toBeVisible();
    });

    it("traps dialog focus and closes on Escape", async () => {
        const user = userEvent.setup();
        const onClose = vi.fn();
        renderWithProviders(
            <FeedbackDialog onClose={onClose}>
                <button type="button">内容を確認</button>
            </FeedbackDialog>,
        );

        expect(screen.getByRole("dialog")).toBeVisible();
        expect(screen.getByRole("button", { name: "閉じる" })).toHaveFocus();
        await user.keyboard("{Escape}");
        expect(onClose).toHaveBeenCalledOnce();
    });

    it("keeps user-facing wording in the message catalog", () => {
        expect(message("feedback.savedTitle")).toBe("変更は保存されています");
        expect(message("feedback.unsavedTitle")).toContain("未保存");
        expect(message("feedback.unsavedDescription")).toContain("保存");
        expect(message("feedback.latestVersion")).toBe("最新版を使う");
        expect(message("feedback.capacityTitle")).toContain("80%");
        expect(message("feedback.capacityDescription")).toContain("整理");
        expect(message("feedback.saveErrorTitle")).toContain("保存先");
        expect(message("feedback.saveErrorDescription")).toContain(
            "アクセス権",
        );
        expect(message("feedback.restoreTitle")).toContain("復元");
        expect(message("feedback.restoreDescription")).toContain("内容");
        expect(message("feedback.genericErrorTitle")).toContain("問題");
        expect(message("feedback.genericErrorDescription")).toContain("再試行");
        expect(
            message("feedback.progressDetails", {
                percent: 64,
                current: "12 KB",
                total: "18 KB",
            }),
        ).toContain("64%");
    });
});
