# Changelog

All notable user-facing changes to Markdown Editor are documented here.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## [Unreleased]

### Fixed

- English-to-Thai translation results remain available when the optional
  part-of-speech lookup times out or returns an unusable response.

## [1.3.0] - 2026-08-22

### Added

- Per-diagram Mermaid controls for zooming in and out, resetting the view, and
  panning an enlarged diagram without moving the surrounding Preview content.

## [1.2.1] - 2026-08-17

### Added

- Selected words in rendered Preview content now expose the same contextual
  **Translate** action as words selected in the Split-view editor.

### Fixed

- Translation requests now carry their correlation identifier across the
  WebView bridge, so choosing **Translate** reliably opens the result popup.

## [1.2.0] - 2026-08-17

### Added

- Contextual English-to-Thai translation for a single selected editor word,
  opened from a right-click **Translate** action.
- An anchored, non-blocking result popup with the language pair, part of speech,
  concise Thai alternatives, and close, outside-click, and Escape dismissal.
- Persisted source and destination language settings. English to Thai is the
  supported pair in this initial translation release.

## [1.1.1] - 2026-08-17

### Fixed

- Repeated Mermaid syntax errors no longer accumulate hidden error diagrams in
  the preview or exported PDFs while a diagram is being edited.
- Correcting invalid Mermaid syntax now replaces the current error with the
  rendered diagram without retaining stale error output.

## [1.1.0] - 2026-08-12

### Added

- Horizontal pointer-drag reordering for document tabs, with a visible insertion
  indicator and support for dropping at either boundary.
- Runtime preservation of the reordered tab sequence across editing, saving,
  context-menu actions, keyboard traversal, and closing.

### Changed

- Reordering moves the existing document-tab instance so unsaved content, dirty
  state, view mode, cursor line, and editor and preview positions are preserved.

## [1.0.1] - 2026-08-05

### Added

- A versioned Mermaid support matrix covering 23 supported diagram families.
- A comprehensive Markdown acceptance document for Preview, Split, and PDF
  export checks.
- Edge-based rendering tests that exercise the real Mermaid and SVG
  sanitization pipeline.

### Fixed

- Mermaid labels, including state names in `stateDiagram` and
  `stateDiagram-v2`, now survive strict SVG sanitization by rendering as pure
  SVG text instead of HTML inside `foreignObject`.

## [1.0.0] - 2026-08-01

### Added

- Native Windows Markdown editing with Preview and Split modes.
- Multiple document tabs with per-document view and viewport state.
- CommonMark/GFM-style rendering, syntax-highlighted code, and Mermaid diagrams.
- Click navigation between source lines and rendered content, including detailed
  table, code-line, and Mermaid mappings.
- Folder Explorer and relative Markdown link navigation.
- Save, Save As, reload from disk, and safe modified-tab close handling.
- PDF export with render, font, and image readiness.
- Local recovery snapshots for unsaved edits.
- Single-instance file forwarding for Markdown files opened from Windows.
- Help references, user manual, product information, and release links.

### Fixed

- Forwarded files now become the active visible tab.
- Closing the final tab clears stale editor and preview content.
- Mermaid diagrams render reliably on the initial document load.
- Portable packages include the complete WebView frontend.

[1.3.0]: https://github.com/sorawitphumwaree/markdown-editor/releases/tag/v1.3.0
[1.2.1]: https://github.com/sorawitphumwaree/markdown-editor/releases/tag/v1.2.1
[1.2.0]: https://github.com/sorawitphumwaree/markdown-editor/releases/tag/v1.2.0
[1.1.1]: https://github.com/sorawitphumwaree/markdown-editor/releases/tag/v1.1.1
[1.1.0]: https://github.com/sorawitphumwaree/markdown-editor/releases/tag/v1.1.0
[1.0.1]: https://github.com/sorawitphumwaree/markdown-editor/releases/tag/v1.0.1
[1.0.0]: https://github.com/sorawitphumwaree/markdown-editor/releases/tag/v1.0.0
