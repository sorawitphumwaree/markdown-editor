export interface AppMessage<TPayload = unknown> {
  type: string;
  protocolVersion: number;
  requestId?: string;
  documentId?: string;
  version?: number;
  payload: TPayload;
}

export interface DocumentPayload {
  content: string;
  filePath: string;
  viewMode: "preview" | "split";
  cursorLine: number;
  editorScroll: number;
  previewScroll: number;
}

export type RenderState =
  | "idle"
  | "rendering-markdown"
  | "rendering-mermaid"
  | "highlighting-code"
  | "loading-assets"
  | "ready"
  | "failed";

declare global {
  interface Window {
    chrome?: {
      webview?: {
        postMessage(message: unknown): void;
        addEventListener(type: "message", listener: (event: MessageEvent) => void): void;
      };
    };
  }
}

