import { useEffect, useState } from "react";
import { message, type MessageKey } from "../messages/catalog";
import type { ExecutionProfile, ExecutionProfileMode } from "../data-access";
import { FeedbackBanner } from "../components/feedback/Feedback";
import type { StartupStatus } from "./startupState";

export type StartupView = "startup" | "settings" | "connection-failed";

type StartupExperienceProps = {
    readonly view: StartupView;
    readonly profile: ExecutionProfile;
    readonly startupStatus?: StartupStatus;
    readonly onContinueLocal: () => Promise<boolean>;
    readonly onSave: (
        mode: ExecutionProfileMode,
        serverUri: string | null,
    ) => Promise<boolean>;
    readonly onRetry: () => void;
    readonly onExit: () => void;
    readonly onCancel: () => void;
};

export function StartupExperience({
    view,
    profile,
    startupStatus = "ready",
    onContinueLocal,
    onSave,
    onRetry,
    onExit,
    onCancel,
}: StartupExperienceProps) {
    const [mode, setMode] = useState<ExecutionProfileMode>(profile.mode);
    const [serverUri, setServerUri] = useState(profile.serverUri ?? "");
    const [saving, setSaving] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [localSaveError, setLocalSaveError] = useState(false);

    useEffect(() => {
        setMode(profile.mode);
        setServerUri(profile.serverUri ?? "");
    }, [profile]);

    async function saveProfile() {
        setError(null);
        if (mode === "server" && !isHttpsUrl(serverUri)) {
            setError(message("startup.invalidServerUrl"));
            return;
        }

        setSaving(true);
        const saved = await onSave(
            mode,
            mode === "server" ? serverUri.trim() : null,
        );
        setSaving(false);
        if (!saved) setError(message("startup.profileSaveFailed"));
    }

    async function continueLocal() {
        setLocalSaveError(false);
        const saved = await onContinueLocal();
        if (!saved) setLocalSaveError(true);
    }

    if (view === "settings") {
        return (
            <>
                <StartupStatusFeedback status={startupStatus} />
                <SettingsPanel
                    mode={mode}
                    serverUri={serverUri}
                    saving={saving}
                    error={error}
                    onModeChange={setMode}
                    onServerUriChange={setServerUri}
                    onSave={() => void saveProfile()}
                    onCancel={onCancel}
                />
            </>
        );
    }

    const failed = view === "connection-failed";
    return (
        <main className="startup-screen" aria-labelledby="startup-title">
            <section className="startup-panel">
                <StartupStatusFeedback status={startupStatus} />
                <p className="eyebrow">{message("startup.eyebrow")}</p>
                <div
                    className={`startup-status-icon${failed ? " is-failed" : ""}`}
                    aria-hidden="true"
                >
                    {failed ? "!" : "✓"}
                </div>
                <h1 id="startup-title">
                    {failed
                        ? message("startup.connectionFailedTitle")
                        : message("startup.title")}
                </h1>
                <p className="startup-description">
                    {failed
                        ? message("startup.connectionFailedDescription")
                        : message("startup.description")}
                </p>
                {failed && (
                    <div className="startup-notice" role="status">
                        {message("startup.connectionFailedDescription")}
                    </div>
                )}
                {!failed && (
                    <div className="startup-mode-card">
                        <strong>{message("startup.localTitle")}</strong>
                        <span>{message("startup.localDescription")}</span>
                    </div>
                )}
                <div
                    className="startup-actions"
                    aria-label={message("shell.primaryActions")}
                >
                    <button
                        className="startup-primary-action"
                        type="button"
                        onClick={() => void continueLocal()}
                        autoFocus
                    >
                        {message("startup.continueLocal")}
                    </button>
                    <button
                        className="startup-secondary-action"
                        type="button"
                        onClick={onCancel}
                    >
                        {message("startup.openSettings")}
                    </button>
                    {failed && (
                        <button
                            className="startup-secondary-action"
                            type="button"
                            onClick={onRetry}
                            disabled={startupStatus === "retrying"}
                        >
                            {message("startup.retry")}
                        </button>
                    )}
                    <button
                        className="startup-secondary-action"
                        type="button"
                        onClick={onExit}
                    >
                        {message("startup.exit")}
                    </button>
                </div>
                {localSaveError && (
                    <p className="startup-error" role="alert">
                        {message("startup.profileSaveFailed")}
                    </p>
                )}
                <p className="startup-footnote">
                    {message("startup.localReady")}
                </p>
            </section>
        </main>
    );
}

function StartupStatusFeedback({ status }: { readonly status: StartupStatus }) {
    if (status === "ready") return null;
    type StatusFeedback = readonly [
        kind: "warning" | "danger" | "info",
        title: MessageKey,
        description: MessageKey,
    ];
    const content: Record<Exclude<StartupStatus, "ready">, StatusFeedback> = {
        loading: ["info", "startup.loadingTitle", "startup.loading"],
        degraded: ["warning", "startup.degradedTitle", "startup.degraded"],
        recovered: ["info", "startup.recoveredTitle", "startup.recovered"],
        error: ["danger", "startup.errorTitle", "startup.error"],
        retrying: ["info", "startup.retryingTitle", "startup.retrying"],
    };
    const [kind, title, description] = content[status];
    return (
        <FeedbackBanner
            kind={kind}
            title={message(title)}
            description={message(description)}
        />
    );
}

function SettingsPanel({
    mode,
    serverUri,
    saving,
    error,
    onModeChange,
    onServerUriChange,
    onSave,
    onCancel,
}: {
    readonly mode: ExecutionProfileMode;
    readonly serverUri: string;
    readonly saving: boolean;
    readonly error: string | null;
    readonly onModeChange: (mode: ExecutionProfileMode) => void;
    readonly onServerUriChange: (value: string) => void;
    readonly onSave: () => void;
    readonly onCancel: () => void;
}) {
    return (
        <main className="startup-screen" aria-labelledby="profile-title">
            <section className="profile-panel">
                <p className="eyebrow">{message("startup.eyebrow")}</p>
                <h1 id="profile-title">{message("startup.profileTitle")}</h1>
                <p className="startup-description">
                    {message("startup.profileDescription")}
                </p>
                <div
                    className="profile-options"
                    role="radiogroup"
                    aria-label={message("startup.profileTitle")}
                >
                    <ProfileOption
                        checked={mode === "local"}
                        title={message("startup.profileLocal")}
                        description={message("startup.profileLocalDescription")}
                        onSelect={() => onModeChange("local")}
                    />
                    <ProfileOption
                        checked={mode === "server"}
                        title={message("startup.profileServer")}
                        description={message(
                            "startup.profileServerDescription",
                        )}
                        onSelect={() => onModeChange("server")}
                    />
                </div>
                <label className="profile-url-field" htmlFor="server-url">
                    <span>{message("startup.serverUrl")}</span>
                    <input
                        id="server-url"
                        aria-label={message("startup.serverUrl")}
                        type="url"
                        value={mode === "server" ? serverUri : ""}
                        disabled={mode !== "server" || saving}
                        placeholder={
                            mode === "server"
                                ? message("startup.serverUrlPlaceholder")
                                : message("startup.serverUrlDisabled")
                        }
                        onChange={(event) =>
                            onServerUriChange(event.target.value)
                        }
                    />
                    <small>{message("startup.httpsOnly")}</small>
                </label>
                {error && (
                    <p className="startup-error" role="alert">
                        {error}
                    </p>
                )}
                <div className="startup-actions profile-actions">
                    <button
                        className="startup-secondary-action"
                        type="button"
                        onClick={onCancel}
                        disabled={saving}
                    >
                        {message("startup.cancel")}
                    </button>
                    <button
                        className="startup-primary-action"
                        type="button"
                        onClick={onSave}
                        disabled={saving}
                    >
                        {saving
                            ? message("startup.saving")
                            : message("startup.save")}
                    </button>
                </div>
            </section>
        </main>
    );
}

function ProfileOption({
    checked,
    title,
    description,
    onSelect,
}: {
    readonly checked: boolean;
    readonly title: string;
    readonly description: string;
    readonly onSelect: () => void;
}) {
    return (
        <button
            className={`profile-option${checked ? " is-selected" : ""}`}
            type="button"
            role="radio"
            aria-checked={checked}
            onClick={onSelect}
        >
            <span className="profile-radio" aria-hidden="true" />
            <span>
                <strong>{title}</strong>
                <small>{description}</small>
            </span>
        </button>
    );
}

function isHttpsUrl(value: string): boolean {
    try {
        return new URL(value.trim()).protocol === "https:";
    } catch {
        return false;
    }
}
