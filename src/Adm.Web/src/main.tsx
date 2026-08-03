import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./App";
import { composeDataAccess } from "./data-access";
import {
    createLocalDataAccess,
    createWebView2LocalTransport,
} from "./data-access/local";
import { readRuntimeConfig } from "./env";
import { initializeTheme, ThemeProvider } from "./theme/theme";

initializeTheme();
const runtimeConfig = readRuntimeConfig();
const isEmbeddedLocalMode =
    window.location.origin === "https://app.ai-development-manager.local";
const dataAccess = isEmbeddedLocalMode
    ? composeDataAccess({
          mode: "local",
          adapter: createLocalDataAccess(createWebView2LocalTransport()),
      })
    : composeDataAccess({
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
