import { defineConfig, devices } from "@playwright/test";

const baseURL = "http://127.0.0.1:5199";
const viewport = { width: 1440, height: 900 };
const dpiProjects = [
    { name: "100dpi", deviceScaleFactor: 1 },
    { name: "125dpi", deviceScaleFactor: 1.25 },
    { name: "150dpi", deviceScaleFactor: 1.5 },
    { name: "200dpi", deviceScaleFactor: 2 },
];
const browserProjects = [
    { name: "edge", channel: "msedge" as const },
    { name: "chrome", channel: "chrome" as const },
];

export default defineConfig({
    testDir: "./specs/compatibility",
    fullyParallel: false,
    forbidOnly: Boolean(process.env.CI),
    retries: process.env.CI ? 1 : 0,
    workers: 1,
    reporter: process.env.CI
        ? [["list"], ["junit", { outputFile: "../../artifacts/ci-evidence/playwright/compatibility-results.xml" }]]
        : [["list"]],
    outputDir: "../../artifacts/ci-evidence/playwright/compatibility-test-results",
    use: {
        baseURL,
        trace: "retain-on-failure",
        screenshot: "only-on-failure",
        video: "retain-on-failure",
    },
    projects: browserProjects.flatMap((browser) =>
        dpiProjects.map((dpi) => ({
            name: `${browser.name}-${dpi.name}`,
            use: {
                ...devices["Desktop Chrome"],
                channel: browser.channel,
                viewport,
                deviceScaleFactor: dpi.deviceScaleFactor,
            },
        })),
    ),
    webServer: {
        command: "dotnet ../../artifacts/bin/Adm.Server.Host/Debug/net10.0/Adm.Server.Host.dll --Server:Port=5199",
        url: `${baseURL}/health/ready`,
        reuseExistingServer: !process.env.CI,
        timeout: 120_000,
    },
});
