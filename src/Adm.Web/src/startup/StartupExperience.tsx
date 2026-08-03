import { useEffect, useState } from "react";
import { message } from "../messages/catalog";
import type { ExecutionProfile, ExecutionProfileMode } from "../data-access";

export type StartupView = "startup" | "settings" | "connection-failed";

type StartupExperienceProps = {
    readonly view: StartupView;
    readonly profile: ExecutionProfile;
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
        );
    }

    const failed = view === "connection-failed";
    return (
        <main className="startup-screen" aria-labelledby="startup-title">
            <section className="startup-panel">
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
