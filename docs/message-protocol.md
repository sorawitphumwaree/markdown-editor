# Message Protocol

Every WPF/WebView2 message uses this envelope:

```json
{
  "type": "document.load",
  "protocolVersion": 1,
  "requestId": null,
  "documentId": "00000000-0000-0000-0000-000000000000",
  "version": 4,
  "payload": {}
}
```

Unknown types and incompatible protocol versions are ignored and logged. Document
content is sent only inside the private WebView2 process boundary and is not logged.

Important messages include `web.ready`, `document.load`, `document.clear`, `document.changed`,
`editor.cursorChanged`, `editor.scrollChanged`, `preview.scrollChanged`,
`preview.linkClicked`, `preview.renderCompleted`, `preview.renderFailed`,
`view.setMode`, `view.setContentsVisibility`, `application.shortcut`, `export.prepare`, `export.ready`, and
`export.complete`.

Document messages carry both a document ID and monotonically increasing version.
Render work commits only while that pair and the local render generation remain
current. Viewport messages are stored per document and restored when its tab is
activated. `document.clear` removes stale editor and preview content after the
final tab closes.

`view.setContentsVisibility` carries `{ "visible": true | false }` from the host.
It applies to the application rather than a particular document. The host sends
the saved per-user preference after `web.ready` and saves each toolbar toggle
before sending the updated visibility.
