import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./App";
import { composeDataAccess } from "./data-access";
import { readRuntimeConfig } from "./env";
import { initializeTheme, ThemeProvider } from "./theme/theme";

initializeTheme();
const runtimeConfig = readRuntimeConfig();
const dataAccess = composeDataAccess({
    mode: "server",
    baseUrl: runtimeConfig.apiBaseUrl,
});

createRoot(document.getElementById("root")!).render(
    <StrictMode>
        <ThemeProvider>
            <App
                dataAccess={dataAccess}
                apiBoundary={runtimeConfig.apiBaseUrl}
            />
        </ThemeProvider>
    </StrictMode>,
);
