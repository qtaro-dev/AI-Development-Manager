import { readRuntimeConfig } from "./env";
import "./styles.css";

const runtimeConfig = readRuntimeConfig();

export function App() {
    return (
        <main className="app-shell">
            <section className="foundation-card" aria-labelledby="app-title">
                <p className="eyebrow">PRODUCT WEB FOUNDATION</p>
                <h1 id="app-title">AI Development Manager</h1>
                <p className="description">
                    React、TypeScript、Viteで構成された製品Web UI基盤です。
                </p>
                <dl className="runtime-details">
                    <div>
                        <dt>API境界</dt>
                        <dd>{runtimeConfig.apiBaseUrl}</dd>
                    </div>
                    <div>
                        <dt>状態</dt>
                        <dd>基盤準備完了</dd>
                    </div>
                </dl>
            </section>
        </main>
    );
}
