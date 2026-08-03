import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { StartupExperience } from "./StartupExperience";
import type { ExecutionProfile } from "../data-access";
import { renderWithProviders } from "../test/test-utils";

const localProfile: ExecutionProfile = {
    schemaVersion: 1,
    mode: "local",
    serverUri: null,
};

describe("StartupExperience", () => {
    it("shows the first-run Local route and all recovery actions", async () => {
        const user = userEvent.setup();
        const handlers = createHandlers();
        renderWithProviders(
            <StartupExperience
                {...handlers}
                view="startup"
                profile={localProfile}
            />,
        );

        expect(screen.getByRole("heading", { name: /初回設定/ })).toBeVisible();
        await user.click(
            screen.getByRole("button", { name: "このPCで続ける" }),
        );
        await user.click(screen.getByRole("button", { name: "接続先を設定" }));
        await user.click(screen.getByRole("button", { name: "終了" }));
        expect(handlers.onContinueLocal).toHaveBeenCalledOnce();
        expect(handlers.onCancel).toHaveBeenCalledOnce();
        expect(handlers.onExit).toHaveBeenCalledOnce();
    });

    it("keeps Server URL disabled in Local mode and enables it for Server", async () => {
        const user = userEvent.setup();
        const handlers = createHandlers();
        renderWithProviders(
            <StartupExperience
                {...handlers}
                view="settings"
                profile={localProfile}
            />,
        );

        const url = screen.getByLabelText("Server URL");
        expect(url).toBeDisabled();
        await user.click(
            screen.getByRole("radio", { name: /LAN Serverへ接続/ }),
        );
        expect(url).toBeEnabled();
        await user.type(url, "https://server.example/");
        await user.click(screen.getByRole("button", { name: "保存" }));
        expect(handlers.onSave).toHaveBeenCalledWith(
            "server",
            "https://server.example/",
        );
    });

    it("rejects a non-HTTPS Server URL before saving", async () => {
        const user = userEvent.setup();
        const handlers = createHandlers();
        renderWithProviders(
            <StartupExperience
                {...handlers}
                view="settings"
                profile={localProfile}
            />,
        );

        await user.click(
            screen.getByRole("radio", { name: /LAN Serverへ接続/ }),
        );
        await user.type(screen.getByLabelText("Server URL"), "http://server/");
        await user.click(screen.getByRole("button", { name: "保存" }));
        expect(screen.getByRole("alert")).toHaveTextContent("HTTPS");
        expect(handlers.onSave).not.toHaveBeenCalled();
    });

    it("shows retry and Local recovery in the connection failure state", async () => {
        const user = userEvent.setup();
        const handlers = createHandlers();
        renderWithProviders(
            <StartupExperience
                {...handlers}
                view="connection-failed"
                profile={localProfile}
            />,
        );

        await user.click(screen.getByRole("button", { name: "もう一度試す" }));
        await user.click(
            screen.getByRole("button", { name: "このPCで続ける" }),
        );
        expect(handlers.onRetry).toHaveBeenCalledOnce();
        expect(handlers.onContinueLocal).toHaveBeenCalledOnce();
    });

    it("keeps the setup screen open when Local profile saving fails", async () => {
        const user = userEvent.setup();
        const handlers = createHandlers(false);
        renderWithProviders(
            <StartupExperience
                {...handlers}
                view="startup"
                profile={localProfile}
            />,
        );

        await user.click(
            screen.getByRole("button", { name: "このPCで続ける" }),
        );
        expect(screen.getByRole("alert")).toHaveTextContent(
            "設定を保存できませんでした。",
        );
        expect(screen.getByRole("heading", { name: /初回設定/ })).toBeVisible();
    });
});

function createHandlers(continueLocalResult = true) {
    return {
        onContinueLocal: vi.fn(async () => continueLocalResult),
        onSave: vi.fn(async () => true),
        onRetry: vi.fn(),
        onExit: vi.fn(),
        onCancel: vi.fn(),
    };
}
