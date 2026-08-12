# Markdown Editor

A Windows-native editor for technical Markdown documents, built with WPF and an
embedded WebView2 editing/rendering surface.

## Features

- Open and safely save UTF-8 Markdown files.
- Open a folder and browse documentation assets.
- Keep multiple documents open with per-document state.
- Reorder open tabs by dragging them horizontally to the desired position.
- Reuse the running application when another Markdown file is opened from Windows.
- Right-click document tabs to save, close, reload from disk, or export PDF.
- Edit with CodeMirror 6 and preview CommonMark/GFM-style Markdown.
- Render Mermaid diagrams and highlighted code blocks with isolated errors.
- Navigate between source and rendered blocks.
- Click rendered table rows, code lines, or Mermaid elements to jump to their
  corresponding source line. Diagram-aware mapping covers Flowchart nodes/edges,
  Sequence participants/messages/notes/control labels, and Class
  classes/members/relationships.
- Click in the editor to move the preview to the matching rendered content, or
  click rendered content to move only the editor. Manual scrolling remains
  independent in each pane. Nested source mappings prefer the exact table row,
  highlighted code line, or Mermaid element instead of its containing block.
- Preserve both editor and preview viewport positions while editing, saving, and
  reloading a document from disk.
- Resolve Markdown links relative to the current document.
- Capture unsaved content in local recovery snapshots.
- Export the fully rendered preview to PDF.
- Use a light presentation with an explicit Preview/Split segmented selector.
- Use a charcoal-and-violet visual theme derived from the application logo.
- Open a user manual, release notes, Markdown references, and an expanded About
  surface with version, feature, project, and license information.

## Architecture

The repository follows Clean Architecture:

```text
MarkdownEditor.Domain
        ^
MarkdownEditor.Application
        ^
        +-- MarkdownEditor.Infrastructure
        +-- MarkdownEditor.App (WPF + WebView2 composition)

MarkdownEditor.Web is a presentation adapter hosted by MarkdownEditor.App.
```

Domain and Application do not depend on WPF, WebView2, or concrete persistence.
See [product behavior](docs/product-behavior.md),
[architecture](docs/architecture.md), and
[phase status](docs/phase-status.md). For application usage, see the
[user manual](docs/user-manual.md) and [changelog](CHANGELOG.md). Mermaid
compatibility is defined in the [support matrix](docs/mermaid-support.md), with
a [manual rendering fixture](docs/test-cases/mermaid-rendering.md) for release
acceptance.

## Requirements for running the packaged app

- Windows 10 or 11 x64
- [.NET 9 Desktop Runtime (x64)](https://dotnet.microsoft.com/download/dotnet/9.0)
- Microsoft Edge WebView2 Runtime

WebView2 is normally already installed on current Windows 10 and Windows 11
systems. If the application reports that WebView2 could not start, install the
[Evergreen WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/).

The current portable release is framework-dependent. Extract and keep the entire
published folder together, then run `MarkdownEditor.App.exe`; copying only the
`.exe` will not include its required DLLs and web assets. End users do not need
Visual Studio, the .NET SDK, Node.js, pnpm, or the source code.

## Requirements for building from source

- Windows 10 or 11 x64
- Visual Studio 2022 or later with the **.NET desktop development** workload,
  or the .NET SDK from a command prompt
- .NET 9 targeting pack
- Node.js 22 or later
- Corepack (included with the standard Node.js installer); the repository pins
  pnpm 11 and Corepack launches that version for MSBuild

## Build and run

```powershell
Push-Location src/MarkdownEditor.Web
corepack pnpm install --frozen-lockfile
Pop-Location
dotnet build MarkdownEditor.sln
dotnet run --project src/MarkdownEditor.App
```

Use `scripts/test.ps1` to run both .NET and web tests.

Create the portable Windows package with:

```powershell
./scripts/package.ps1
```

The ZIP is written to `artifacts/markdown-editor-win-x64.zip`.
It also includes `mermaid-rendering-test-cases.md` for manual rendering
acceptance after extraction.

## Project

- Author: Sorawit Phumwaree (Ham)
- Repository: [sorawitphumwaree/markdown-editor](https://github.com/sorawitphumwaree/markdown-editor)
- License: Apache License 2.0

## Security

Markdown is untrusted. The frontend sanitizes generated HTML, Mermaid uses strict
mode, the WebView receives no native host object, messages are validated, and
local path access is mediated by the native path resolver. See
[SECURITY.md](SECURITY.md).
