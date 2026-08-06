import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import { ProjectPage } from "./ProjectPage";
import type { DataAccessPort, Project } from "../data-access";
import { renderWithProviders } from "../test/test-utils";

const selectProjectFolder = vi.fn();
vi.mock("../platform-bridge/bridge", () => ({
    isHostBridgeAvailable: () => true,
    selectProjectFolder: (...args: unknown[]) => selectProjectFolder(...args),
}));

const project: Project = {
    id: "project-1",
    displayName: "Demo Project",
    root: "C:\\Projects\\Demo",
    registeredAtUtc: "2026-08-06T00:00:00Z",
    isSelected: true,
};

function createDataAccess(projects: readonly Project[] = []): DataAccessPort {
    return {
        getFoundationStatus: vi.fn(),
        getExecutionProfile: vi.fn(),
        updateExecutionProfile: vi.fn(),
        listProjects: vi.fn(async () => ({
            kind: "success" as const,
            value: { projects, selectedProjectId: projects[0]?.id ?? null, warnings: [] },
        })),
        registerProject: vi.fn(async () => ({
            kind: "success" as const,
            value: { project },
        })),
        unregisterProject: vi.fn(async () => ({
            kind: "success" as const,
            value: { projectId: project.id },
        })),
        selectProject: vi.fn(async () => ({
            kind: "success" as const,
            value: { selectedProjectId: project.id },
        })),
    };
}

describe("ProjectPage", () => {
    it("shows the empty state and registers the selected folder", async () => {
        const user = userEvent.setup();
        const dataAccess = createDataAccess();
        selectProjectFolder.mockResolvedValueOnce({ selected: true, path: "C:\\Projects\\New" });
        renderWithProviders(<ProjectPage dataAccess={dataAccess} onSettings={vi.fn()} />);

        await waitFor(() => expect(screen.getByText("登録Projectがありません")).toBeVisible());
        await user.click(screen.getByRole("button", { name: "フォルダーを選択して登録" }));
        await waitFor(() => expect(dataAccess.registerProject).toHaveBeenCalledWith(
            { root: "C:\\Projects\\New", displayName: null },
            expect.objectContaining({ signal: expect.any(AbortSignal) }),
        ));
    });

    it("keeps cancellation visible and prevents duplicate registration", async () => {
        const user = userEvent.setup();
        const dataAccess = createDataAccess();
        selectProjectFolder.mockResolvedValue({ selected: false });
        renderWithProviders(<ProjectPage dataAccess={dataAccess} onSettings={vi.fn()} />);

        const button = await screen.findByRole("button", { name: "フォルダーを選択して登録" });
        await user.click(button);
        await user.click(button);
        await waitFor(() => expect(screen.getByText("登録を取り消しました")).toBeVisible());
        expect(dataAccess.registerProject).not.toHaveBeenCalled();
    });

    it("selects and unregisters a project without deleting its files", async () => {
        const user = userEvent.setup();
        const dataAccess = createDataAccess([project]);
        renderWithProviders(<ProjectPage dataAccess={dataAccess} onSettings={vi.fn()} />);

        await waitFor(() => expect(screen.getByText("Demo Project")).toBeVisible());
        await user.click(screen.getByRole("button", { name: "選択解除" }));
        expect(dataAccess.selectProject).toHaveBeenCalledWith(null, expect.anything());
        await user.click(screen.getAllByRole("button", { name: "登録解除" })[0]);
        expect(screen.getByText(/Projectフォルダー内のファイル/)).toBeVisible();
        await user.click(screen.getAllByRole("button", { name: "登録解除" })[1]);
        await waitFor(() => expect(dataAccess.unregisterProject).toHaveBeenCalledWith(project.id, expect.anything()));
    });

    it("maps a retryable data access failure to a retry action", async () => {
        const user = userEvent.setup();
        const dataAccess = createDataAccess();
        vi.mocked(dataAccess.listProjects)
            .mockResolvedValueOnce({
                kind: "failure",
                error: { code: "timeout", message: "internal", retryable: true, nextAction: "retry" },
            })
            .mockResolvedValueOnce({ kind: "success", value: { projects: [], selectedProjectId: null, warnings: [] } });
        renderWithProviders(<ProjectPage dataAccess={dataAccess} onSettings={vi.fn()} />);

        await waitFor(() => expect(screen.getByText("処理がタイムアウトしました。再試行してください。")).toBeVisible());
        await user.click(screen.getByRole("button", { name: "再試行" }));
        await waitFor(() => expect(screen.getByText("登録Projectがありません")).toBeVisible());
        expect(dataAccess.listProjects).toHaveBeenCalledTimes(2);
    });
});
