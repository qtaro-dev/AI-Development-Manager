import { useCallback, useEffect, useRef, useState } from "react";
import type { DataAccessFailure, DataAccessPort, Project, ProjectWarning } from "../data-access";
import { FeedbackBanner, FeedbackDialog } from "../components/feedback/Feedback";
import { message } from "../messages/catalog";
import { isHostBridgeAvailable, selectProjectFolder } from "../platform-bridge/bridge";

type ProjectStatus = "loading" | "ready" | "saving" | "error" | "cancelled";

function failureMessage(error: DataAccessFailure): string {
    switch (error.code) {
        case "timeout": return message("project.errorTimeout");
        case "cancelled": return message("project.errorCancelled");
        case "channel_unavailable": return message("project.errorUnavailable");
        case "adapter_unavailable": return message("project.errorUnavailable");
        case "invalid_result": return message("project.errorInvalidResult");
        default: return message("project.errorGeneric");
    }
}

function warningMessage(warning: ProjectWarning): string {
    return warning.code === "root_unavailable"
        ? message("project.rootUnavailable")
        : message("project.rootWarning");
}

export function ProjectPage({
    dataAccess,
    onSettings,
}: {
    dataAccess: DataAccessPort;
    onSettings: () => void;
}) {
    const [projects, setProjects] = useState<readonly Project[]>([]);
    const [warnings, setWarnings] = useState<readonly ProjectWarning[]>([]);
    const [status, setStatus] = useState<ProjectStatus>("loading");
    const [failure, setFailure] = useState<DataAccessFailure | null>(null);
    const [confirmProject, setConfirmProject] = useState<Project | null>(null);
    const [cancelled, setCancelled] = useState(false);
    const mounted = useRef(true);
    const sequence = useRef(0);
    const activeRequest = useRef<AbortController | null>(null);
    const actionInFlight = useRef(false);

    const loadProjects = useCallback(async () => {
        activeRequest.current?.abort();
        const controller = new AbortController();
        activeRequest.current = controller;
        const requestSequence = ++sequence.current;
        setStatus("loading");
        setFailure(null);
        const result = await dataAccess.listProjects({ signal: controller.signal });
        if (!mounted.current || requestSequence !== sequence.current) return;
        if (result.kind === "failure") {
            if (result.error.code === "cancelled") return;
            setFailure(result.error);
            setStatus("error");
            return;
        }
        setProjects(result.value.projects);
        setWarnings(result.value.warnings);
        setFailure(null);
        setStatus("ready");
    }, [dataAccess]);

    useEffect(() => {
        mounted.current = true;
        void loadProjects();
        return () => {
            mounted.current = false;
            sequence.current += 1;
            activeRequest.current?.abort();
        };
    }, [loadProjects]);

    async function runAction(action: (options: { signal: AbortSignal }) => Promise<{ kind: "success" } | { kind: "failure"; error: DataAccessFailure }>) {
        if (actionInFlight.current) return;
        actionInFlight.current = true;
        const controller = new AbortController();
        activeRequest.current?.abort();
        activeRequest.current = controller;
        setStatus("saving");
        setFailure(null);
        const result = await action({ signal: controller.signal });
        if (!mounted.current) return;
        actionInFlight.current = false;
        if (result.kind === "failure") {
            if (result.error.code === "cancelled") {
                setStatus("cancelled");
                return;
            }
            setFailure(result.error);
            setStatus("error");
            return;
        }
        await loadProjects();
    }

    async function registerProject() {
        if (actionInFlight.current || !isHostBridgeAvailable()) return;
        actionInFlight.current = true;
        setStatus("saving");
        setFailure(null);
        setCancelled(false);
        try {
            const selection = await selectProjectFolder();
            if (!mounted.current) return;
            if (!selection.selected) {
                setCancelled(true);
                setStatus("cancelled");
                actionInFlight.current = false;
                return;
            }
            const controller = new AbortController();
            activeRequest.current?.abort();
            activeRequest.current = controller;
            const result = await dataAccess.registerProject(
                { root: selection.path, displayName: null },
                { signal: controller.signal },
            );
            if (!mounted.current) return;
            actionInFlight.current = false;
            if (result.kind === "failure") {
                setFailure(result.error);
                setStatus("error");
                return;
            }
            await loadProjects();
        } catch {
            if (!mounted.current) return;
            actionInFlight.current = false;
            setFailure({ code: "operation_failed", message: "", retryable: true, nextAction: "retry" });
            setStatus("error");
        }
    }

    async function selectProject(projectId: string | null) {
        await runAction(({ signal }) => dataAccess.selectProject(projectId, { signal }));
    }

    async function unregisterProject() {
        if (!confirmProject) return;
        const projectId = confirmProject.id;
        setConfirmProject(null);
        await runAction(({ signal }) => dataAccess.unregisterProject(projectId, { signal }));
    }

    const canRegister = isHostBridgeAvailable() && status !== "saving" && status !== "loading";
    const statusLabel = status === "loading"
        ? message("project.loading")
        : status === "saving"
          ? message("project.saving")
          : status === "cancelled"
            ? message("project.cancelled")
            : message("project.ready");

    return (
        <div className="project-page">
            <div className="project-toolbar">
                <div>
                    <p className="eyebrow">{message("project.eyebrow")}</p>
                    <h2>{message("project.title")}</h2>
                    <p className="project-description">{message("project.description")}</p>
                </div>
                <button type="button" className="project-primary-action" disabled={!canRegister} onClick={() => void registerProject()}>
                    {message("project.register")}
                </button>
            </div>

            <div className="project-status" role="status" aria-live="polite">{statusLabel}</div>

            {!isHostBridgeAvailable() && (
                <FeedbackBanner kind="info" title={message("project.browserTitle")} description={message("project.browserDescription")} />
            )}
            {cancelled && <FeedbackBanner kind="info" title={message("project.cancelledTitle")} description={message("project.cancelledDescription")} />}
            {failure && (
                <FeedbackBanner
                    kind="danger"
                    title={message("project.errorTitle")}
                    description={failureMessage(failure)}
                    actionLabel={failure.nextAction === "checkSettings" ? message("project.checkSettings") : message("common.retry")}
                    onAction={failure.nextAction === "checkSettings" ? onSettings : () => void loadProjects()}
                />
            )}
            {warnings.map((warning) => (
                <FeedbackBanner key={`${warning.projectId}-${warning.code}`} kind="warning" title={message("project.rootWarningTitle")} description={warningMessage(warning)} />
            ))}

            {status === "loading" && <div className="project-empty" role="status">{message("project.loading")}</div>}
            {status !== "loading" && projects.length === 0 && <div className="project-empty" role="status"><h3>{message("project.emptyTitle")}</h3><p>{message("project.emptyDescription")}</p></div>}
            {projects.length > 0 && (
                <ul className="project-list" aria-label={message("project.listLabel")}>
                    {projects.map((project) => (
                        <li className={`project-card${project.isSelected ? " is-selected" : ""}`} key={project.id}>
                            <div className="project-card-content">
                                <h3>{project.displayName}</h3>
                                <p className="project-root">{project.root}</p>
                                {project.isSelected && <span className="project-selected">{message("project.selected")}</span>}
                            </div>
                            <div className="project-card-actions">
                                <button type="button" disabled={status === "saving"} onClick={() => void selectProject(project.isSelected ? null : project.id)}>
                                    {project.isSelected ? message("project.clearSelection") : message("project.select")}
                                </button>
                                <button type="button" disabled={status === "saving"} onClick={() => setConfirmProject(project)}>{message("project.unregister")}</button>
                            </div>
                        </li>
                    ))}
                </ul>
            )}

            {confirmProject && (
                <FeedbackDialog onClose={() => setConfirmProject(null)}>
                    <p>{message("project.unregisterConfirm", { name: confirmProject.displayName })}</p>
                    <p className="project-retention-note">{message("project.filesRemain")}</p>
                    <div className="project-dialog-actions">
                        <button type="button" onClick={() => setConfirmProject(null)}>{message("common.cancel")}</button>
                        <button type="button" onClick={() => void unregisterProject()}>{message("project.unregister")}</button>
                    </div>
                </FeedbackDialog>
            )}
        </div>
    );
}
