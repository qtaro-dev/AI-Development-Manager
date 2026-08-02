import { useState } from "react";

export interface InteractiveStatusFixtureProps {
    readonly load?: () => Promise<string>;
}

export function InteractiveStatusFixture({
    load = defaultLoad,
}: InteractiveStatusFixtureProps) {
    const [status, setStatus] = useState<
        "idle" | "loading" | "ready" | "error"
    >("idle");
    const [dialogOpen, setDialogOpen] = useState(false);

    async function handleLoad() {
        setStatus("loading");
        try {
            await load();
            setStatus("ready");
        } catch {
            setStatus("error");
        }
    }

    return (
        <section aria-labelledby="fixture-title">
            <h2 id="fixture-title">接続状態</h2>
            <button
                type="button"
                onClick={handleLoad}
                disabled={status === "loading"}
            >
                接続確認
            </button>
            <button type="button" onClick={() => setDialogOpen(true)}>
                詳細を表示
            </button>
            <p role="status" aria-live="polite">
                {status === "idle" && "未確認"}
                {status === "loading" && "確認中"}
                {status === "ready" && "接続済み"}
            </p>
            {status === "error" && <p role="alert">接続できませんでした。</p>}
            {dialogOpen && (
                <div
                    role="dialog"
                    aria-modal="true"
                    aria-labelledby="fixture-dialog-title"
                >
                    <h3 id="fixture-dialog-title">接続の詳細</h3>
                    <button type="button" onClick={() => setDialogOpen(false)}>
                        閉じる
                    </button>
                </div>
            )}
        </section>
    );
}

async function defaultLoad() {
    return "ready";
}
