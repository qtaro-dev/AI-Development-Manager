import { RouteOutlet } from "./routes/RouteOutlet";
import { message } from "./messages/catalog";
import { useEffect, useReducer, useRef, useState } from "react";
import type { DataAccessPort } from "./data-access";
import type { ExecutionProfile, ExecutionProfileMode } from "./data-access";
import {
    StartupExperience,
    type StartupView,
} from "./startup/StartupExperience";
import {
    initialStartupState,
    startupReducer,
    type StartupStatus,
} from "./startup/startupState";
import { requestHostExit } from "./platform-bridge/hostExit";
import { ProjectPage } from "./projects/ProjectPage";
import "./styles.css";

const STARTUP_ACKNOWLEDGED_KEY = "adm.startup.localAcknowledged";
const defaultProfile: ExecutionProfile = {
    schemaVersion: 1,
    mode: "local",
    serverUri: null,
};

const startupStatusMessages: Record<StartupStatus, Parameters<typeof message>[0]> = {
    loading: "startup.loading",
    ready: "startup.ready",
    degraded: "startup.degraded",
    recovered: "startup.recovered",
    error: "startup.error",
    retrying: "startup.retrying",
};

export function App({
    dataAccess,
    apiBoundary,
}: {
    dataAccess: DataAccessPort;
    apiBoundary: string;
}) {
    const isLocalRuntime = apiBoundary === "local";
    const initialSettings =
        new URLSearchParams(window.location.search).get("settings") === "1";
    const [profile, setProfile] = useState<ExecutionProfile>(defaultProfile);
    const [startupState, dispatchStartup] = useReducer(
        startupReducer,
        initialStartupState,
    );
    const mounted = useRef(true);
    const requestSequence = useRef(0);
    const retryInFlight = useRef(false);
    const [view, setView] = useState<StartupView | "home">(() => {
        if (initialSettings) return "settings";
        if (
            isLocalRuntime &&
            window.localStorage.getItem(STARTUP_ACKNOWLEDGED_KEY) !== "true"
        ) {
            return "startup";
        }
        return "home";
    });

    async function loadStartup(retry: boolean) {
        if (retry && retryInFlight.current) return;
        if (retry) retryInFlight.current = true;
        const sequence = ++requestSequence.current;
        dispatchStartup({ type: "request", retry });

        try {
            const [foundation, executionProfile] = await Promise.all([
                dataAccess.getFoundationStatus(),
                dataAccess.getExecutionProfile(),
            ]);
            if (!mounted.current || sequence !== requestSequence.current) return;
            if (foundation.kind !== "success") {
                dispatchStartup({ type: "failed", failure: foundation.error });
                return;
            }
            if (executionProfile.kind !== "success") {
                dispatchStartup({ type: "failed", failure: executionProfile.error });
                return;
            }

            dispatchStartup({
                type: "resolved",
                foundation: foundation.value,
                profile: executionProfile.value,
            });
            setProfile(executionProfile.value.profile);
            if (!initialSettings && (view === "connection-failed" || executionProfile.value.hasPersistedProfile && !executionProfile.value.usedLocalFallback)) {
                markStartupAcknowledged();
                setView(
                    executionProfile.value.hasPersistedProfile &&
                        !executionProfile.value.usedLocalFallback
                        ? "home"
                        : "startup",
                );
            }
        } catch {
            if (mounted.current && sequence === requestSequence.current) {
                dispatchStartup({
                    type: "failed",
                    failure: {
                        code: "operation_failed",
                        message: message("startup.error"),
                        retryable: true,
                        nextAction: "retry",
                    },
                });
            }
        } finally {
            if (retry) retryInFlight.current = false;
        }
    }

    useEffect(() => {
        mounted.current = true;
        void loadStartup(false);
        return () => {
            mounted.current = false;
            requestSequence.current += 1;
        };
    }, [dataAccess]);

    function retryStartup() {
        if (startupState.status === "retrying") return;
        setView("connection-failed");
        void loadStartup(true);
    }

    async function saveProfile(
        mode: ExecutionProfileMode,
        serverUri: string | null,
    ): Promise<boolean> {
        const result = await dataAccess.updateExecutionProfile({
            mode,
            serverUri,
        });
        if (result.kind !== "success") return false;
        setProfile(result.value);
        markStartupAcknowledged();
        if (mode === "local") {
            setView("home");
        } else {
            setView("home");
        }
        return true;
    }

    async function continueLocal(): Promise<boolean> {
        const result = await dataAccess.updateExecutionProfile({
            mode: "local",
            serverUri: null,
        });
        if (result.kind !== "success") return false;
        markStartupAcknowledged();
        setProfile(result.value);
        setView("home");
        return true;
    }

    function markStartupAcknowledged() {
        try {
            window.localStorage.setItem(STARTUP_ACKNOWLEDGED_KEY, "true");
        } catch {
            // Local use remains available when browser storage is unavailable.
        }
    }

    const fallbackNeedsReview =
        (startupState.status === "degraded" ||
            startupState.status === "recovered" &&
                startupState.profile?.usedLocalFallback === true) &&
        view === "home";
    const effectiveView =
        startupState.status === "error" && view === "home"
            ? "connection-failed"
            : fallbackNeedsReview
              ? "startup"
              : view;

    if (effectiveView !== "home") {
        return (
            <StartupExperience
                view={effectiveView}
                profile={profile}
                startupStatus={startupState.status}
                onContinueLocal={continueLocal}
                onSave={saveProfile}
                onRetry={retryStartup}
                onExit={requestHostExit}
                onCancel={() =>
                    setView(effectiveView === "settings" ? "home" : "settings")
                }
            />
        );
    }

    return (
        <RouteOutlet
            pageTitle={message("shell.navProjects")}
            onSettings={() => setView("settings")}
        >
            <section className="foundation-card" aria-labelledby="app-title">
                <p className="eyebrow">{message("app.eyebrow")}</p>
                <h1 id="app-title">{message("app.title")}</h1>
                <p className="description">{message("app.description")}</p>
                <dl className="runtime-details">
                    <div>
                        <dt>{message("app.apiBoundary")}</dt>
                        <dd>{apiBoundary}</dd>
                    </div>
                    <div>
                        <dt>{message("app.status")}</dt>
                        <dd>{message(startupStatusMessages[startupState.status])}</dd>
                    </div>
                </dl>
            </section>
            <ProjectPage dataAccess={dataAccess} onSettings={() => setView("settings")} />
        </RouteOutlet>
    );
}
