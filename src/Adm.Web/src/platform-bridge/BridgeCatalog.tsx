import { useState } from "react";
import { message } from "../messages/catalog";
import {
    BridgeError,
    getHostInfo,
    isHostBridgeAvailable,
    type HostInfo,
} from "./bridge";

export function BridgeCatalog() {
    const [hostInfo, setHostInfo] = useState<HostInfo | null>(null);
    const [error, setError] = useState<string | null>(null);
    const available = isHostBridgeAvailable();

    async function handleRequest() {
        setError(null);
        try {
            setHostInfo(await getHostInfo());
        } catch (reason) {
            setHostInfo(null);
            setError(
                reason instanceof BridgeError
                    ? reason.message
                    : message("bridge.rejected"),
            );
        }
    }

    return (
        <section
            className="foundation-card bridge-catalog"
            aria-labelledby="bridge-title"
        >
            <p className="eyebrow">{message("bridge.eyebrow")}</p>
            <h2 id="bridge-title">{message("bridge.title")}</h2>
            <p className="description">{message("bridge.description")}</p>
            <ul aria-label={message("bridge.allowedLabel")}>
                <li>{message("bridge.allowedGetHostInfo")}</li>
            </ul>
            <button type="button" onClick={handleRequest}>
                {message("bridge.checkHost")}
            </button>
            {!available && (
                <p role="status">{message("bridge.browserUnavailable")}</p>
            )}
            {hostInfo && (
                <p
                    role="status"
                    data-testid="bridge-host-info"
                >{`${hostInfo.applicationName} / ${hostInfo.runtime}`}</p>
            )}
            {error && <p role="alert">{error}</p>}
            <p className="bridge-security-note">
                {message("bridge.securityNote")}
            </p>
        </section>
    );
}
