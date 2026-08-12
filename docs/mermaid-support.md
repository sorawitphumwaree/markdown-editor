# Mermaid diagram support

Markdown Editor 1.0.1 uses Mermaid 11.16.0 and renders supported diagrams with
pure SVG labels. Pure SVG labels survive the application's strict sanitization
pipeline and remain readable in WebView2 and PDF output.

The support baseline is versioned with the Mermaid dependency. Run the browser
rendering suite and manually open
[`test-cases/mermaid-rendering.md`](test-cases/mermaid-rendering.md) whenever
Mermaid is upgraded.

## Supported and regression-tested

| Diagram family | Accepted declaration |
| --- | --- |
| Flowchart | `flowchart`, `graph` |
| Sequence | `sequenceDiagram` |
| Class | `classDiagram` |
| State | `stateDiagram`, `stateDiagram-v2` |
| Entity relationship | `erDiagram` |
| User journey | `journey` |
| Gantt | `gantt` |
| Pie | `pie` |
| Quadrant | `quadrantChart` |
| Requirement | `requirementDiagram` |
| Git graph | `gitGraph` |
| C4 | `C4Context` and the Mermaid C4 family |
| Mindmap | `mindmap` |
| Timeline | `timeline` |
| Sankey | `sankey-beta` |
| XY chart | `xychart`, `xychart-beta` |
| Block | `block`, `block-beta` |
| Packet | `packet`, `packet-beta` |
| Kanban | `kanban` |
| Architecture | `architecture-beta` |
| Radar | `radar-beta` |
| Treemap | `treemap`, `treemap-beta` |

Both state renderers have explicit regression cases because the original defect
was first observed in `stateDiagram-v2`.

## Not part of the product support baseline

Mermaid 11.16.0 also registers experimental, internal, or lightly documented
renderers for event modeling, swimlanes, Ishikawa, railroad grammars (generic,
EBNF, ABNF, and PEG), Venn, Wardley, and Cynefin diagrams. Markdown Editor does
not claim product support for those renderers in version 1.0.1. Their syntax and
output can change without a stable Mermaid compatibility contract.

ZenUML is an external Mermaid diagram integration and is not bundled by the
pinned dependency.

An unsupported diagram is left as its fenced source with an isolated
`Mermaid error` message if Mermaid rejects it. It must not prevent other
Markdown content or supported diagrams from rendering.

## Verification contract

For every supported fixture, the Edge browser suite checks that:

- Mermaid does not produce an isolated render error;
- sanitization leaves exactly one SVG;
- expected human-readable labels survive;
- the SVG has finite, non-zero rendered dimensions and a finite `viewBox`;
- visible drawing primitives remain after sanitization.

The manual Windows acceptance pass additionally checks Preview and Split modes,
scrolling, light-theme contrast, and PDF export clipping.
