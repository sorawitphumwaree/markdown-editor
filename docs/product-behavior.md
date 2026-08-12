# Product Behavior

Markdown Editor is a Windows desktop reader and editor for local technical
Markdown. It favors predictable document handling and accurate navigation over
general-purpose IDE features.

## Launch behavior

- Starting the application directly creates an untitled document in Split mode.
- Opening a Markdown file from Windows opens that file in Preview mode.
- If the application is already running, later Markdown files open as tabs in
  the existing window.
- Opening a file that is already open activates its existing tab.

## Reading and editing

- Preview mode uses the full document area.
- Split mode shows CodeMirror on the left and the rendered document on the right.
- Editing updates the preview after a short debounce.
- Asynchronous rendering is versioned. A stale render cannot replace newer
  content or restore obsolete viewport positions.
- Each tab owns its content, modified state, cursor line, editor viewport,
  preview viewport, and view mode.
- Tabs can be reordered with a horizontal pointer drag. Moving a tab preserves
  its identity and all per-document state. The visible order controls keyboard
  traversal, context actions, and closing for the rest of the process lifetime.

## Cross-pane navigation

Navigation is deliberate rather than continuously scroll-coupled:

- Clicking an editor line moves only the preview to the most precise matching
  rendered element.
- Clicking a rendered element moves only the editor to its mapped source line.
- Manually scrolling either pane does not move the other pane.
- Editing, saving, and reloading preserve both viewport positions.
- Exact child mappings are preferred over broad containers for table rows,
  highlighted code lines, and supported Mermaid elements.

## File safety

- Save uses a temporary file in the destination directory before replacing the
  target.
- Modified tabs are marked and require confirmation before close.
- Reload from disk requires confirmation before discarding local changes.
- Unsaved edits create local recovery snapshots. Automatic recovery restore is
  not yet exposed in the UI.
- Relative links are resolved from the current document directory. Paths outside
  an open workspace are rejected.

## Keyboard commands

| Shortcut | Command |
|---|---|
| `Ctrl+N` or `Ctrl+T` | New tab |
| `Ctrl+O` | Open Markdown file |
| `Ctrl+S` | Save |
| `Ctrl+Shift+S` | Save As |
| `Ctrl+W` | Close active tab |
| `Ctrl+Tab` | Next tab |
| `Ctrl+Shift+Tab` | Previous tab |

## Current product boundary

The current baseline does not yet claim v1.0 release qualification. Remaining
work is tracked in [phase-status.md](phase-status.md), including recovery restore,
external-change conflict UX, relative image serving, complete Explorer commands,
end-to-end tests, and signed distribution.
