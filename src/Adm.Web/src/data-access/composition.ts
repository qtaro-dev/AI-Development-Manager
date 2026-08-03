import { createHttpDataAccess } from "./http-adapter";
import type { DataAccessMode, DataAccessPort } from "./port";

export class DataAccessCompositionError extends Error {
    constructor(message: string) {
        super(message);
        this.name = "DataAccessCompositionError";
    }
}

export type DataAccessCompositionOptions = {
    readonly mode: DataAccessMode;
    readonly adapter?: DataAccessPort;
    readonly baseUrl?: string;
    readonly request?: typeof fetch;
};

export function composeDataAccess(
    options: DataAccessCompositionOptions,
): DataAccessPort {
    if (options.mode !== "local" && options.mode !== "server") {
        throw new DataAccessCompositionError(
            "実行モードが指定されていないため、DataAccessを構成できません。",
        );
    }

    if (options.adapter) {
        return options.adapter;
    }

    if (options.mode === "server" && options.baseUrl?.trim()) {
        return createHttpDataAccess(options.baseUrl, options.request);
    }

    throw new DataAccessCompositionError(
        "DataAccess Adapterが指定されていないため、安全に起動できません。",
    );
}
