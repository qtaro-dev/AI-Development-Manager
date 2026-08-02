import { readRuntimeConfig } from "./env";
import { RouteOutlet } from "./routes/RouteOutlet";
import { FeedbackCatalog } from "./components/feedback/FeedbackCatalog";
import { message } from "./messages/catalog";
import "./styles.css";

const runtimeConfig = readRuntimeConfig();

export function App() {
    return (
        <RouteOutlet pageTitle={message("shell.navTickets")}>
            <section className="foundation-card" aria-labelledby="app-title">
                <p className="eyebrow">{message("app.eyebrow")}</p>
                <h1 id="app-title">{message("app.title")}</h1>
                <p className="description">{message("app.description")}</p>
                <dl className="runtime-details">
                    <div>
                        <dt>{message("app.apiBoundary")}</dt>
                        <dd>{runtimeConfig.apiBaseUrl}</dd>
                    </div>
                    <div>
                        <dt>{message("app.status")}</dt>
                        <dd>{message("app.foundationReady")}</dd>
                    </div>
                </dl>
            </section>
            <FeedbackCatalog />
        </RouteOutlet>
    );
}
