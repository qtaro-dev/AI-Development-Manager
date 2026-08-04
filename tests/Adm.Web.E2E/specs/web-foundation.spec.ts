import { expect, test } from "@playwright/test";

const viewports = [
    { name: "desktop", width: 1440, height: 900 },
    { name: "minimum-supported", width: 1280, height: 720 },
    { name: "tablet", width: 820, height: 900 },
    { name: "mobile", width: 320, height: 760 },
] as const;

test.describe("production Web foundation", () => {
    test.beforeEach(async ({ page }) => {
        const consoleErrors: string[] = [];
        const failedResponses: string[] = [];
        page.on("console", (message) => {
            if (message.type() === "error") consoleErrors.push(message.text());
        });
        page.on("response", (response) => {
            if (response.status() >= 400) failedResponses.push(`${response.status()} ${response.url()}`);
        });
        await page.goto("/");
        await expect(page.getByRole("heading", { name: "AI Development Manager" })).toBeVisible();
        await expect(page.getByText("基盤準備完了")).toBeVisible();
        await expect(page.locator(".app-shell")).toBeVisible();
        await expect.poll(() => consoleErrors).toEqual([]);
        await expect.poll(() => failedResponses).toEqual([]);
    });

    for (const viewport of viewports) {
        test(`keeps the shell and primary content visible at ${viewport.name}`, async ({ page }, testInfo) => {
            await page.setViewportSize({ width: viewport.width, height: viewport.height });
            await expect(page.getByRole("main")).toBeVisible();
            await expect(page.getByRole("heading", { name: "チケット" })).toBeVisible();
            await expect(page.getByRole("heading", { name: "WPF Bridge許可操作" })).toBeVisible();
            await page.screenshot({ path: testInfo.outputPath(`viewport-${viewport.name}.png`), fullPage: true });
        });
    }

    test("keeps the minimum supported viewport within the horizontal viewport", async ({ page }) => {
        await page.setViewportSize({ width: 1280, height: 720 });
        await expect(page.getByRole("heading", { name: "チケット" })).toBeVisible();

        const metrics = await page.evaluate(() => ({
            viewportWidth: document.documentElement.clientWidth,
            documentWidth: document.documentElement.scrollWidth,
            bodyWidth: document.body.scrollWidth,
            appWidth: document.querySelector<HTMLElement>(".app-shell")?.scrollWidth ?? 0,
        }));

        expect(metrics.documentWidth).toBeLessThanOrEqual(metrics.viewportWidth);
        expect(metrics.bodyWidth).toBeLessThanOrEqual(metrics.viewportWidth);
        expect(metrics.appWidth).toBeLessThanOrEqual(metrics.viewportWidth);
    });

    test("serves a deep link through SPA fallback and reloads it", async ({ page }) => {
        await page.goto("/tickets/demo");
        await expect(page.getByRole("heading", { name: "チケット" })).toBeVisible();
        await page.reload();
        await expect(page.getByText("/api/v1")).toBeVisible();
    });

    test("supports keyboard dialog flow and restores focus after Escape", async ({ page }) => {
        const openButton = page.getByRole("button", { name: "確認ダイアログを表示" });
        await openButton.focus();
        await page.keyboard.press("Enter");
        const dialog = page.getByRole("dialog");
        await expect(dialog).toBeVisible();
        await expect(dialog.getByRole("button", { name: "閉じる" })).toBeFocused();
        await page.keyboard.press("Escape");
        await expect(dialog).toBeHidden();
        await expect(openButton).toBeFocused();
    });

    test("renders the selected dark theme without JavaScript or asset errors", async ({ browser }) => {
        const context = await browser.newContext({
            viewport: { width: 1440, height: 900 },
        });
        await context.addInitScript(() => localStorage.setItem("adm.theme", "dark"));
        const page = await context.newPage();
        await page.goto("/");
        await expect(page.locator("html")).toHaveAttribute("data-theme", "dark");
        await expect(page.getByRole("heading", { name: "AI Development Manager" })).toBeVisible();
        await page.screenshot({ path: "../../artifacts/ci-evidence/playwright/screenshots/dark-theme.png", fullPage: true });
        await context.close();
    });
});
