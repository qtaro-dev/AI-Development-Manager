import { useState } from "react";
import { message } from "../../messages/catalog";
import {
    EmptyState,
    ErrorState,
    FeedbackBanner,
    FeedbackDialog,
    ProgressFeedback,
    StatusIndicator,
    Toast,
} from "./Feedback";

export function FeedbackCatalog() {
    const [dialogOpen, setDialogOpen] = useState(false);
    return (
        <section
            className="feedback-catalog"
            aria-labelledby="feedback-catalog-title"
        >
            <div className="feedback-catalog-heading">
                <div>
                    <h2 id="feedback-catalog-title">
                        {message("feedback.catalogTitle")}
                    </h2>
                    <p>{message("feedback.catalogDescription")}</p>
                </div>
                <button type="button" onClick={() => setDialogOpen(true)}>
                    {message("feedback.openDialog")}
                </button>
            </div>
            <div className="feedback-status-grid">
                <StatusIndicator kind="saved" />
                <StatusIndicator kind="unsaved" />
                <StatusIndicator kind="conflict" />
                <StatusIndicator kind="connected" />
                <StatusIndicator kind="warning" />
                <StatusIndicator kind="processing" />
            </div>
            <div className="feedback-catalog-grid">
                <FeedbackBanner
                    kind="danger"
                    title={message("feedback.conflictTitle")}
                    description={message("feedback.conflictDescription")}
                    actionLabel={message("feedback.showDiff")}
                    onAction={() => undefined}
                />
                <FeedbackBanner
                    kind="warning"
                    title={message("feedback.capacityTitle")}
                    description={message("feedback.capacityDescription")}
                    actionLabel={message("feedback.latestVersion")}
                    onAction={() => undefined}
                />
                <ProgressFeedback
                    kind="upload"
                    value={64}
                    current="12 KB"
                    total="18 KB"
                    onCancel={() => undefined}
                />
                <ProgressFeedback
                    kind="restore"
                    value={40}
                    current="世代20"
                    total="manifest"
                    onCancel={() => undefined}
                />
                <Toast onClose={() => undefined}>
                    {message("feedback.savedTitle")}
                </Toast>
                <EmptyState />
                <ErrorState onRetry={() => undefined} />
            </div>
            {dialogOpen && (
                <FeedbackDialog onClose={() => setDialogOpen(false)}>
                    <p>{message("feedback.dialogBody")}</p>
                    <button type="button" data-dialog-autofocus>
                        {message("feedback.latestVersion")}
                    </button>
                </FeedbackDialog>
            )}
        </section>
    );
}
