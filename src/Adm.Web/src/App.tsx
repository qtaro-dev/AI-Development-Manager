import { RouteOutlet } from "./routes/RouteOutlet";
import { FeedbackCatalog } from "./components/feedback/FeedbackCatalog";
import { BridgeCatalog } from "./platform-bridge/BridgeCatalog";
import { message } from "./messages/catalog";
import type { DataAccessPort } from "./data-access";
import "./styles.css";

export function App({
    dataAccess,
    apiBoundary,
}: {
    dataAccess: DataAccessPort;
    apiBoundary: string;
}) {
    // The Port is injected at the composition boundary; business operations are
    // intentionally added by later tickets without changing this shell.
    void dataAccess;

    return (
        <RouteOutlet pageTitle={message("shell.navTickets")}>
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
