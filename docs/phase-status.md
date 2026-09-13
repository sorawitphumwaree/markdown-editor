# Phase Status

Reviewed 2026-08-12.

This repository contains a buildable implementation across all planned technical
areas. It is an engineering baseline, not yet a claim that every v1.0 acceptance
criterion is release-qualified.

| Phase | Implemented baseline | Remaining acceptance work |
|---|---|---|
| 1 Foundation | Solution, DI, logging, WPF, WebView2, Vite, versioned JSON bridge, .NET 9 SDK/CI alignment | Startup benchmark and protocol contract tests |
| 2 Document slice | Open, edit, preview, safe save, Save As, modified marker, highlighted Preview/Split selector, launch-mode behavior, document-tab context actions, standard document/tab shortcuts, horizontal tab drag/drop, logo-derived native/web theme | None |
| 3 Rendering | GFM plugins, DOMPurify, Shiki, light-only UI, full-width Preview layout, detached stale-render rejection by document/version/generation | Golden-document visual suite and bundle-size optimization |
| 4 Mermaid | SVG render, strict mode, per-diagram errors, themes, collision-free concurrent render IDs | Zoom/pan controls and broader rendering tests |
| 5 Synchronization | Token source ranges, row-level table mapping, line-level fenced-code mapping, diagram-aware Flowchart/Sequence/Class mapping, directional click navigation, independent manual scrolling, viewport persistence across edit/save/reload | Additional Mermaid diagram families, large-document stability, and feedback-loop tests |
| 6 Workspace/tabs | Multiple document state, runtime tab reordering, relative and same-document fragment links, standard new/open/save/save-as/tab hotkeys, save/close/export/reload tab menu, single-instance file forwarding | Create/rename/delete, reopen closed tab, cross-restart tab-order persistence, native relative-image serving |
| 7 Reliability | Temporary-file replacement, serialized per-document recovery writes, JSON settings/recovery stores, debounced watcher | Wire recovery restore/session/settings UI and conflict-resolution dialogs |
| 8 PDF | WebView2 print path, paginated print CSS, Mermaid/Shiki/font/image readiness, request-correlated `export.ready` | Expose page options and qualify representative long documents |
| 9 Release | Tests, docs, user manual, changelog, in-app documentation links, CI, portable ZIP workflow, policies | Playwright end-to-end suite, signing, installer choice, Windows 10/11 qualification |

## Verification

- `pnpm typecheck`: passed.
- `pnpm test`: 17 tests passed.
- `pnpm build`: passed.
- `dotnet build MarkdownEditor.sln --configuration Release`: passed with zero
  warnings and zero errors.
- `dotnet test`: 14 tests passed using `DOTNET_ROLL_FORWARD=Major` because the
  development machine has .NET 10 but not the .NET 9 runtime.

## Release blockers

1. Install the .NET 9 Desktop Runtime for direct local execution.
2. Complete the acceptance work above.
3. Run manual and automated qualification on Windows 10 and Windows 11.
4. Decide installer/signing identity before public distribution.

## Review findings addressed on 2026-07-31

- Moved document persistence and recovery sequencing into an Application-layer
  workflow instead of coordinating ports directly in the WPF view model.
- Prevented stale asynchronous renders from replacing newer preview content or
  restoring obsolete viewport positions.
- Serialized recovery writes per document and remove snapshots when Undo returns
  to saved content.
- Fixed same-document fragment links and expanded path/filesystem regression
  coverage, including Thai and Unicode round trips.
- Aligned `global.json`, GitHub Actions, Visual Studio builds, and repository
  scripts around .NET 9 plus Corepack-managed pnpm.

## Review findings addressed on 2026-08-01

- Made forwarded file opens select the new tab and clear the WebView when the
  final tab closes.
- Prevented concurrent initial Mermaid renders from reusing diagram IDs or
  allowing a superseded render generation to update the preview.
- Corrected repository-root PowerShell commands so Corepack discovers the pinned
  pnpm project version, and pinned CI to the same pnpm 11.9.0 release.
- Declared generated WebView assets as normal build and publish content so the
  portable ZIP includes `Web/index.html` and its bundled assets.
- Added the user manual and changelog, expanded About with version/features and
  project links, and applied a restrained charcoal/violet logo palette across
  native controls, preview content, editor states, and Mermaid diagrams.
