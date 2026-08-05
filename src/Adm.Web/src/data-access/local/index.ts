export { LocalChannelClient, createWebView2LocalTransport } from "./client";
export { createLocalDataAccess } from "./adapter";
export type {
    LocalChannelRequestOptions,
    LocalChannelTransport,
} from "./client";
export type { LocalDataAccessPort } from "./adapter";
export {
    LOCAL_CHANNEL_VERSION,
    MAX_LOCAL_CHANNEL_MESSAGE_BYTES,
    LocalChannelProtocolError,
    parseMessage,
    serializeRequest,
} from "./protocol";
export type {
    LocalChannelError,
    LocalChannelMessage,
    LocalChannelPayload,
    LocalChannelRequest,
    LocalChannelResponse,
} from "./protocol";
