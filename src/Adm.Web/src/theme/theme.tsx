import {
    createContext,
    useCallback,
    useContext,
    useEffect,
    useMemo,
    useState,
    type ReactNode,
} from "react";

export type ThemeMode = "light" | "dark" | "system";
export type ResolvedTheme = "light" | "dark";

const themeStorageKey = "adm.theme";

type ThemeContextValue = {
    mode: ThemeMode;
    resolvedTheme: ResolvedTheme;
    setMode: (mode: ThemeMode) => void;
};

const ThemeContext = createContext<ThemeContextValue | null>(null);

function systemTheme(): ResolvedTheme {
    return window.matchMedia("(prefers-color-scheme: dark)").matches
        ? "dark"
        : "light";
}

function readStoredMode(): ThemeMode {
    try {
        const stored = window.localStorage.getItem(themeStorageKey);
        return stored === "light" || stored === "dark" || stored === "system"
            ? stored
            : "system";
    } catch {
        return "system";
    }
}

function resolveTheme(mode: ThemeMode): ResolvedTheme {
    return mode === "system" ? systemTheme() : mode;
}

export function initializeTheme(): void {
    if (typeof window === "undefined") return;
    document.documentElement.dataset.theme = resolveTheme(readStoredMode());
}

export function ThemeProvider({
    children,
    initialMode,
}: {
    children: ReactNode;
    initialMode?: ThemeMode;
}) {
    const [mode, setModeState] = useState<ThemeMode>(
        initialMode ?? readStoredMode,
    );
    const [resolvedTheme, setResolvedTheme] = useState<ResolvedTheme>(() =>
        resolveTheme(initialMode ?? "system"),
    );

    const setMode = useCallback((nextMode: ThemeMode) => {
        setModeState(nextMode);
        try {
            window.localStorage.setItem(themeStorageKey, nextMode);
        } catch {
            // Theme preference is optional and must not block the UI.
        }
    }, []);

    useEffect(() => {
        const applyTheme = () => {
            const nextTheme = resolveTheme(mode);
            setResolvedTheme(nextTheme);
            document.documentElement.dataset.theme = nextTheme;
        };

        applyTheme();
        if (mode !== "system") return;

        const media = window.matchMedia("(prefers-color-scheme: dark)");
        media.addEventListener("change", applyTheme);
        return () => media.removeEventListener("change", applyTheme);
    }, [mode]);

    const value = useMemo(
        () => ({ mode, resolvedTheme, setMode }),
        [mode, resolvedTheme, setMode],
    );

    return (
        <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>
    );
}

export function useTheme(): ThemeContextValue {
    const value = useContext(ThemeContext);
    if (!value) throw new Error("useTheme must be used inside ThemeProvider");
    return value;
}
