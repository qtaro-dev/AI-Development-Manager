import { RouteOutlet } from "./routes/RouteOutlet";
import { FeedbackCatalog } from "./components/feedback/FeedbackCatalog";
import { BridgeCatalog } from "./platform-bridge/BridgeCatalog";
import { message } from "./messages/catalog";
import { useEffect } from "react";
import { useState } from "react";
import type { DataAccessPort } from "./data-access";
import type { ExecutionProfile, ExecutionProfileMode } from "./data-access";
import {
    StartupExperience,
    type StartupView,
} from "./startup/StartupExperience";
import "./styles.css";

const STARTUP_ACKNOWLEDGED_KEY = "adm.startup.localAcknowledged";
const defaultProfile: ExecutionProfile = {
    schemaVersion: 1,
    mode: "local",
    serverUri: null,
};

export function App({
    dataAccess,
    apiBoundary,
}: {
    dataAccess: DataAccessPort;
    apiBoundary: string;
}) {
    const isLocalRuntime = apiBoundary === "local";
    const initialSettings = new URLSearchParams(window.location.search).has(
        "settings",
    );
    const [profile, setProfile] = useState<ExecutionProfile>(defaultProfile);
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

    useEffect(() => {
        void dataAccess.getFoundationStatus();
        void Promise.resolve(dataAccess.getExecutionProfile()).then(
            (result) => {
                if (result?.kind === "success") {
                    setProfile(result.value.profile);
                }
            },
        );
    }, [dataAccess]);

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
        if (mode === "local") {
            markStartupAcknowledged();
            setView("home");
        } else {
            setView("home");
        }
        return true;
    }

    async function continueLocal() {
        markStartupAcknowledged();
        setProfile(defaultProfile);
        setView("home");
        await dataAccess.updateExecutionProfile({
            mode: "local",
            serverUri: null,
        });
    }

    function markStartupAcknowledged() {
        try {
            window.localStorage.setItem(STARTUP_ACKNOWLEDGED_KEY, "true");
        } catch {
            // Local use remains available when browser storage is unavailable.
        }
    }

    if (view !== "home") {
        return (
            <StartupExperience
                view={view}
                profile={profile}
                onContinueLocal={continueLocal}
                onSave={saveProfile}
                onRetry={() => setView("home")}
                onExit={() => window.close()}
                onCancel={() =>
                    setView(view === "settings" ? "home" : "settings")
                }
            />
        );
    }

    return (
        <RouteOutlet
            pageTitle={message("shell.navTickets")}
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
                        <dd>{message("app.foundationReady")}</dd>
                    </div>
                </dl>
            </section>
            <FeedbackCatalog />
            <BridgeCatalog />
        </RouteOutlet>
    );
}
