import { StrictMode } from "react";
import { createRoot } from "react-dom/client";
import { App } from "./App";
import { initializeTheme, ThemeProvider } from "./theme/theme";

initializeTheme();

createRoot(document.getElementById("root")!).render(
    <StrictMode>
        <ThemeProvider>
            <App />
        </ThemeProvider>
    </StrictMode>,
);
