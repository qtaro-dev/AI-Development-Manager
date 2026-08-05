import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { App } from "./App";
import type { DataAccessPort } from "./data-access";
import { renderWithProviders } from "./test/test-utils";

describe("App foundation", () => {
    it("exposes the product identity and runtime API boundary", async () => {
        renderWithProviders(
            <App dataAccess={createDataAccess()} apiBoundary="/api/v1" />,
        );

        expect(
            screen.getByRole("heading", { name: "AI Development Manager" }),
        ).toBeVisible();
        expect(screen.getByText("/api/v1")).toBeVisible();
        await waitFor(() =>
            expect(screen.getByText("基盤を利用できます。")).toBeVisible(),
        );
    });

    it("does not show ready when foundation loading fails and retries once", async () => {
        const user = userEvent.setup();
        const dataAccess = createDataAccess();
        vi.mocked(dataAccess.getFoundationStatus)
            .mockResolvedValueOnce({
                kind: "failure",
                error: {
                    code: "operation_failed",
                    message: "failed",
                    retryable: true,
                    nextAction: "retry",
                },
            })
            .mockResolvedValueOnce({
                kind: "success",
                value: {
                    state: "ready",
                    apiVersion: "local",
                    contractVersion: "1.0",
                    serverTimeUtc: "2026-08-04T00:00:00Z",
                },
            });
        renderWithProviders(<App dataAccess={dataAccess} apiBoundary="local" />);

        await waitFor(() => expect(screen.getByRole("alert")).toBeVisible());
        expect(screen.queryByText("基盤を利用できます。")).not.toBeInTheDocument();
        await user.click(screen.getByRole("button", { name: "もう一度試す" }));
        await waitFor(() => expect(screen.getByText("再試行後に基盤を復旧しました。")).toBeVisible());
        expect(dataAccess.getFoundationStatus).toHaveBeenCalledTimes(2);
    });

    it("shows first-run Local setup and then opens the Local home", async () => {
        const user = userEvent.setup();
        window.localStorage.removeItem("adm.startup.localAcknowledged");
        const dataAccess = createDataAccess();
        renderWithProviders(
            <App dataAccess={dataAccess} apiBoundary="local" />,
        );

        expect(screen.getByRole("heading", { name: /初回設定/ })).toBeVisible();
        await user.click(
            screen.getByRole("button", { name: "このPCで続ける" }),
        );
        expect(
            screen.getByRole("heading", { name: "AI Development Manager" }),
        ).toBeVisible();
        expect(dataAccess.updateExecutionProfile).toHaveBeenCalledWith({
            mode: "local",
            serverUri: null,
        });
    });

    it("opens settings from the Local home", async () => {
        const user = userEvent.setup();
        window.localStorage.setItem("adm.startup.localAcknowledged", "true");
        renderWithProviders(
            <App dataAccess={createDataAccess()} apiBoundary="local" />,
        );

        await user.click(screen.getByRole("link", { name: "設定" }));
        expect(screen.getByRole("heading", { name: "利用方法" })).toBeVisible();
        expect(screen.getByLabelText("Server URL")).toBeDisabled();
    });

    it("opens the Local home from a persisted profile without the startup screen", async () => {
        window.localStorage.removeItem("adm.startup.localAcknowledged");
        const dataAccess = createDataAccess(true);
        renderWithProviders(
            <App dataAccess={dataAccess} apiBoundary="local" />,
        );

        await waitFor(() =>
            expect(
                screen.getByRole("heading", {
                    name: "AI Development Manager",
                }),
            ).toBeVisible(),
        );
        expect(
            screen.queryByRole("heading", { name: /初回設定/ }),
        ).not.toBeInTheDocument();
    });
});

function createDataAccess(hasPersistedProfile = false): DataAccessPort {
    return {
        getFoundationStatus: vi.fn(async () => ({
            kind: "success" as const,
            value: {
                state: "ready" as const,
                apiVersion: "local",
                contractVersion: "1.0",
                serverTimeUtc: "2026-08-04T00:00:00Z",
            },
        })),
        getExecutionProfile: vi.fn(async () => ({
            kind: "success" as const,
            value: {
                profile: {
                    schemaVersion: 1 as const,
                    mode: "local" as const,
                    serverUri: null,
                },
                usedLocalFallback: false,
                warningCode: null,
                hasPersistedProfile,
            },
        })),
        updateExecutionProfile: vi.fn(async (update) => ({
            kind: "success" as const,
            value: { schemaVersion: 1 as const, ...update },
        })),
    };
}
