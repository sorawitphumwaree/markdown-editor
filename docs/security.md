# Security Design

- Markdown HTML is sanitized by DOMPurify.
- Script, iframe, object, and embed elements are forbidden.
- Mermaid uses strict security mode.
- The WebView receives no native host objects.
- Local links are resolved and classified by the native path resolver.
- External links are opened by Windows, outside the privileged WebView.
- Unsupported absolute URI schemes, including `file:`, are rejected.
- Message envelopes and protocol versions are validated.
- Contextual translation sends only the explicitly selected word to
  `api.mymemory.translated.net` and `api.dictionaryapi.dev`. Surrounding document
  content, paths, and document identifiers are not sent to either service.
- Full document content is excluded from application logs.
- Saves replace the destination from a temporary file in the same directory.

Remote images are not loaded automatically by a privileged native file API.
