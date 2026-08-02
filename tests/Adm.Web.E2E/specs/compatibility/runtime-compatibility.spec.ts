import { expect, test } from "@playwright/test";

test.describe("P1-023 browser runtime compatibility", () => {
    test.beforeEach(async ({ page }) => {
        const consoleErrors: string[] = [];
        const failedResponses: string[] = [];
        page.on("console", (message) => {
            if (message.type() === "error") consoleErrors.push(message.text());
        });
        page.on("pageerror", (error) => consoleErrors.push(error.message));
        page.on("response", (response) => {
            if (response.status() >= 400) failedResponses.push(`${response.status()} ${response.url()}`);
        });

        await page.goto("/");
        await expect(page.getByRole("heading", { name: "AI Development Manager" })).toBeVisible();
        await expect(page.getByText("基盤準備完了", { exact: true })).toBeVisible();
        await expect(page.locator(".app-shell")).toBeVisible();
        expect(consoleErrors, consoleErrors.join("\n")).toEqual([]);
        expect(failedResponses, failedResponses.join("\n")).toEqual([]);
    });

    test("keeps the shell and primary navigation available", async ({ page }) => {
        await expect(page.getByRole("main")).toBeVisible();
        await expect(page.getByRole("link", { name: "チケット" })).toBeVisible();
        await expect(page.getByText("WPF Bridge許可操作", { exact: true })).toBeVisible();
    });

    test("supports theme, dialog keyboard operation, and focus return", async ({ page }) => {
        await page.evaluate(() => localStorage.setItem("adm.theme", "dark"));
        await page.reload();
        await expect(page.locator("html[data-theme='dark']")).toHaveCount(1);
        const trigger = page.getByRole("button", { name: "確認ダイアログを表示" });
        await expect(trigger).toBeVisible();
        await trigger.focus();
        await trigger.press("Enter");
        const dialog = page.getByRole("dialog");
        await expect(dialog).toBeVisible();
        const close = dialog.getByRole("button", { name: "閉じる" });
        await expect(close).toBeFocused();
        await page.keyboard.press("Escape");
        await expect(dialog).toBeHidden();
        await expect(trigger).toBeFocused();
    });

    test("preserves the SPA deep link contract", async ({ page }) => {
        await page.goto("/tickets/p1-023-runtime-compatibility");
        await expect(page.getByRole("heading", { name: "AI Development Manager" })).toBeVisible();
        await page.reload();
        await expect(page.getByText("/api/v1", { exact: true })).toBeVisible();
    });
});
