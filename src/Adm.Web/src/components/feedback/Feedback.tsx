import { useEffect, useRef, type ReactNode } from "react";
import { message } from "../../messages/catalog";

export type StatusKind =
    | "saved"
    | "unsaved"
    | "conflict"
    | "connected"
    | "error"
    | "warning"
    | "processing";

const statusContent: Record<
    StatusKind,
    { label: string; icon: string; tone: string }
> = {
    saved: {
        label: message("feedback.savedLabel"),
        icon: "●",
        tone: "success",
    },
    unsaved: {
        label: message("feedback.unsavedLabel"),
        icon: "●",
        tone: "warning",
    },
    conflict: {
        label: message("feedback.conflictLabel"),
        icon: "!",
        tone: "danger",
    },
    connected: {
        label: message("shell.connection"),
        icon: "●",
        tone: "success",
    },
    error: {
        label: message("feedback.saveErrorLabel"),
        icon: "!",
        tone: "danger",
    },
    warning: {
        label: message("feedback.capacityLabel"),
        icon: "▲",
        tone: "warning",
    },
    processing: {
        label: message("feedback.restoringLabel"),
        icon: "…",
        tone: "primary",
    },
};

export function StatusIndicator({
    kind,
    detail,
}: {
    kind: StatusKind;
    detail?: string;
}) {
    const content = statusContent[kind];
    return (
        <div className={`feedback-status is-${content.tone}`} role="status">
            <span className="feedback-icon" aria-hidden="true">
                {content.icon}
            </span>
            <span>{content.label}</span>
            {detail && <span className="feedback-detail">{detail}</span>}
        </div>
    );
}

export function FeedbackBanner({
    kind,
    title,
    description,
    actionLabel,
    onAction,
    requestId,
}: {
    kind: "warning" | "danger" | "info";
    title: string;
    description: string;
    actionLabel?: string;
    onAction?: () => void;
    requestId?: string;
}) {
    return (
        <section
            className={`feedback-banner is-${kind}`}
            role={kind === "danger" ? "alert" : "status"}
            aria-live={kind === "danger" ? "assertive" : "polite"}
        >
            <span className="feedback-icon" aria-hidden="true">
                {kind === "danger" ? "!" : kind === "warning" ? "▲" : "i"}
            </span>
            <div className="feedback-content">
                <h2>{title}</h2>
                <p>{description}</p>
                {requestId && <small>追跡ID: {requestId}</small>}
            </div>
            {actionLabel && onAction && (
                <button
                    className="feedback-action"
                    type="button"
                    onClick={onAction}
                >
                    {actionLabel}
                </button>
            )}
        </section>
    );
}

export function Toast({
    children,
    onClose,
    actionLabel,
    onAction,
}: {
    children: ReactNode;
    onClose: () => void;
    actionLabel?: string;
    onAction?: () => void;
}) {
    return (
        <div className="feedback-toast" role="status" aria-live="polite">
            <div className="feedback-content">{children}</div>
            {actionLabel && onAction && (
                <button
                    className="feedback-action"
                    type="button"
                    onClick={onAction}
                >
                    {actionLabel}
                </button>
            )}
            <button
                className="feedback-close"
                type="button"
                aria-label={message("feedback.closeDialog")}
                onClick={onClose}
            >
                ×
            </button>
        </div>
    );
}

export function ProgressFeedback({
    kind,
    value,
    current,
    total,
    onCancel,
    onRetry,
    failed = false,
}: {
    kind: "upload" | "restore";
    value: number;
    current: string;
    total: string;
    onCancel?: () => void;
    onRetry?: () => void;
    failed?: boolean;
}) {
    const label =
        kind === "upload"
            ? message("feedback.uploadingLabel")
            : message("feedback.restoringLabel");
    return (
        <section className="feedback-progress" aria-live="polite">
            <div className="feedback-progress-heading">
                <StatusIndicator kind={failed ? "error" : "processing"} />
                <strong>{label}</strong>
            </div>
            <div
                className="feedback-progress-bar"
                role="progressbar"
                aria-label={label}
                aria-valuemin={0}
                aria-valuemax={100}
                aria-valuenow={failed ? undefined : value}
            >
                <span
                    style={{ width: `${Math.max(0, Math.min(100, value))}%` }}
                />
            </div>
            <p>
                {failed
                    ? message("feedback.genericErrorDescription")
                    : message("feedback.progressDetails", {
                          percent: value,
                          current,
                          total,
                      })}
            </p>
            <div className="feedback-actions">
                {failed && onRetry && (
                    <button type="button" onClick={onRetry}>
                        {message("feedback.retryAction")}
                    </button>
                )}
                {!failed && onCancel && (
                    <button type="button" onClick={onCancel}>
                        {message("feedback.cancelAction")}
                    </button>
                )}
            </div>
        </section>
    );
}

export function FeedbackDialog({
    children,
    onClose,
}: {
    children: ReactNode;
    onClose: () => void;
}) {
    const dialogRef = useRef<HTMLDivElement>(null);
    const previousFocus = useRef<HTMLElement | null>(null);

    useEffect(() => {
        previousFocus.current = document.activeElement as HTMLElement | null;
        const dialog = dialogRef.current;
        const focusable = () =>
            Array.from(
                dialog?.querySelectorAll<HTMLElement>(
                    'button:not([disabled]), [href], input:not([disabled]), [tabindex]:not([tabindex="-1"])',
                ) ?? [],
            );
        focusable()[0]?.focus();
        const onKeyDown = (event: KeyboardEvent) => {
            if (event.key === "Escape") {
                event.preventDefault();
                onClose();
                return;
            }
            if (event.key !== "Tab") return;
            const elements = focusable();
            if (elements.length === 0) return;
            const first = elements[0];
            const last = elements[elements.length - 1];
            if (event.shiftKey && document.activeElement === first) {
                event.preventDefault();
                last.focus();
            } else if (!event.shiftKey && document.activeElement === last) {
                event.preventDefault();
                first.focus();
            }
        };
        document.addEventListener("keydown", onKeyDown);
        return () => {
            document.removeEventListener("keydown", onKeyDown);
            previousFocus.current?.focus();
        };
    }, [onClose]);

    return (
        <div className="feedback-dialog-backdrop" role="presentation">
            <div
                className="feedback-dialog"
                ref={dialogRef}
                role="dialog"
                aria-modal="true"
                aria-labelledby="feedback-dialog-title"
            >
                <div className="feedback-dialog-heading">
                    <h2 id="feedback-dialog-title">
                        {message("feedback.confirmationTitle")}
                    </h2>
                    <button
                        type="button"
                        className="feedback-close"
                        aria-label={message("feedback.closeDialog")}
                        onClick={onClose}
                    >
                        ×
                    </button>
                </div>
                <p>{message("feedback.confirmationDescription")}</p>
                {children}
            </div>
        </div>
    );
}

export function EmptyState() {
    return (
        <section className="feedback-empty" role="status">
            <span className="feedback-icon" aria-hidden="true">
                ○
            </span>
            <h2>{message("feedback.emptyTitle")}</h2>
            <p>{message("feedback.emptyDescription")}</p>
        </section>
    );
}

export function ErrorState({ onRetry }: { onRetry?: () => void }) {
    return (
        <section
            className="feedback-empty is-error"
            role="alert"
            aria-live="assertive"
        >
            <span className="feedback-icon" aria-hidden="true">
                !
            </span>
            <h2>{message("feedback.genericErrorTitle")}</h2>
            <p>{message("feedback.genericErrorDescription")}</p>
            {onRetry && (
                <button type="button" onClick={onRetry}>
                    {message("feedback.retryAction")}
                </button>
            )}
        </section>
    );
}
