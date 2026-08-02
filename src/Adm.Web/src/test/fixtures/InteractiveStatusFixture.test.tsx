import { act, fireEvent, screen, waitFor } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import { InteractiveStatusFixture } from "./InteractiveStatusFixture";
import { renderWithProviders, userEvent } from "../test-utils";

describe("InteractiveStatusFixture", () => {
    it("supports accessible keyboard activation and async ready state", async () => {
        const user = userEvent.setup();
        renderWithProviders(
            <InteractiveStatusFixture load={async () => "ok"} />,
        );

        expect(screen.getByRole("heading", { name: "接続状態" })).toBeVisible();
        expect(
            screen.getByRole("button", { name: "接続確認" }),
        ).toHaveAccessibleName("接続確認");

        await user.tab();
        expect(screen.getByRole("button", { name: "接続確認" })).toHaveFocus();
        await user.keyboard("{Enter}");

        await waitFor(() =>
            expect(screen.getByRole("status")).toHaveTextContent("接続済み"),
        );
    });

    it("exposes an error state and closes the dialog", async () => {
        const user = userEvent.setup();
        const load = vi.fn(async () => {
            throw new Error("simulated failure");
        });
        renderWithProviders(<InteractiveStatusFixture load={load} />);

        await user.click(screen.getByRole("button", { name: "接続確認" }));
        expect(await screen.findByRole("alert")).toHaveTextContent(
            "接続できませんでした。",
        );

        await user.click(screen.getByRole("button", { name: "詳細を表示" }));
        expect(
            screen.getByRole("dialog", { name: "接続の詳細" }),
        ).toBeVisible();
        await user.click(screen.getByRole("button", { name: "閉じる" }));
        expect(screen.queryByRole("dialog")).not.toBeInTheDocument();
        expect(load).toHaveBeenCalledOnce();
    });

    it("supports deterministic loading tests with fake timers", async () => {
        vi.useFakeTimers();
        try {
            const load = vi.fn(
                () =>
                    new Promise<string>((resolve) =>
                        setTimeout(() => resolve("ok"), 100),
                    ),
            );
            renderWithProviders(<InteractiveStatusFixture load={load} />);

            fireEvent.click(screen.getByRole("button", { name: "接続確認" }));
            expect(screen.getByRole("status")).toHaveTextContent("確認中");
            await act(async () => {
                await vi.advanceTimersByTimeAsync(100);
            });
            expect(screen.getByRole("status")).toHaveTextContent("接続済み");
        } finally {
            vi.useRealTimers();
        }
    });
});
