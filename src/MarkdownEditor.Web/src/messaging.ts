import type { AppMessage } from "./types";

const protocolVersion = 1;

export function post<T>(
  type: string,
  documentId: string | undefined,
  version: number | undefined,
  payload: T
): void {
  window.chrome?.webview?.postMessage({
    type,
    protocolVersion,
    documentId,
    version,
    payload
  } satisfies AppMessage<T>);
}

export function listen(handler: (message: AppMessage) => void): void {
  window.chrome?.webview?.addEventListener("message", event => {
    const message = event.data as AppMessage;
    if (message?.protocolVersion === protocolVersion && typeof message.type === "string")
      handler(message);
  });
}

