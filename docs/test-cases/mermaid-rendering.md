# Mermaid rendering acceptance cases

Use this document for the manual Windows acceptance pass. Open it in Markdown
Editor, inspect every numbered diagram in Preview and Split modes, then export
the full document to PDF and check that labels, lines, markers, and diagram
boundaries are not clipped.

Expected result: every section below displays one labeled diagram and no
`Mermaid error` block.

## 01 Flowchart

```mermaid
flowchart LR
    Source[Markdown source] --> Renderer{Render safely?}
    Renderer -->|Yes| Preview[Visible preview]
    Renderer -->|No| Error[Isolated error]
```

## 02 Sequence

```mermaid
sequenceDiagram
    participant Host as WPF Host
    participant Web as WebView Renderer
    Host->>Web: Load document
    Web-->>Host: Render completed
    Note over Host,Web: Labels must remain visible
```

## 03 Class

```mermaid
classDiagram
    class Document {
        +String Content
        +Render()
    }
    class Preview {
        +ShowSvg()
    }
    Document --> Preview : renders
```

## 04 State v2

```mermaid
stateDiagram-v2
    [*] --> Still
    Still --> Moving
    Moving --> Still
    Moving --> Crash
    Crash --> [*]
```

## 05 State legacy

```mermaid
stateDiagram
    [*] --> Draft
    Draft --> Reviewed
    Reviewed --> Released
    Released --> [*]
```

## 06 Entity relationship

```mermaid
erDiagram
    DOCUMENT ||--o{ DIAGRAM : contains
    DOCUMENT {
        string title
    }
    DIAGRAM {
        string type
        boolean valid
    }
```

## 07 User journey

```mermaid
journey
    title Mermaid acceptance journey
    section Verify
      Open fixture: 5: Ham
      Inspect labels: 5: Ham
      Export PDF: 4: Ham
```

## 08 Gantt

```mermaid
gantt
    title Rendering verification
    dateFormat YYYY-MM-DD
    section Fix
    Reproduce defect :done, 2026-08-05, 1d
    Verify diagrams :active, 2026-08-06, 1d
```

## 09 Pie

```mermaid
pie showData
    title Rendering coverage
    "Automated" : 70
    "Manual" : 30
```

## 10 Quadrant

```mermaid
quadrantChart
    title Rendering confidence
    x-axis Low coverage --> High coverage
    y-axis Low confidence --> High confidence
    State diagram: [0.85, 0.90]
    PDF export: [0.65, 0.70]
```

## 11 Requirement

```mermaid
requirementDiagram
    requirement visible_labels {
        id: REQ_1
        text: Labels remain visible after sanitization
        risk: high
        verifymethod: test
    }
    element renderer {
        type: software
        docref: renderer.ts
    }
    renderer - satisfies -> visible_labels
```

## 12 Git graph

```mermaid
gitGraph
    commit id: "reproduce"
    branch fix
    checkout fix
    commit id: "repair"
    checkout main
    merge fix
    commit id: "release"
```

## 13 C4 context

```mermaid
C4Context
    title Markdown Editor rendering context
    Person(user, "User", "Reviews technical Markdown")
    System(editor, "Markdown Editor", "Renders and exports documents")
    Rel(user, editor, "Opens documents")
```

## 14 Mindmap

```mermaid
mindmap
  root((Mermaid))
    Rendering
      SVG labels
      Geometry
    Acceptance
      WebView2
      PDF export
```

## 15 Timeline

```mermaid
timeline
    title Mermaid rendering fix
    Reproduce : Missing labels
    Repair : Pure SVG labels
    Verify : Browser matrix
    Release : Version 1.0.1
```

## 16 Sankey

```mermaid
sankey-beta
Markdown,Renderer,10
Renderer,Preview,8
Renderer,Error,2
```

## 17 XY chart

```mermaid
xychart-beta
    title "Rendering checks"
    x-axis ["Flow", "State", "Class", "Export"]
    y-axis "Passed" 0 --> 10
    bar [10, 10, 10, 10]
    line [8, 10, 9, 8]
```

## 18 Block

```mermaid
block-beta
    columns 3
    source["Markdown source"] --> render["Mermaid renderer"]
    render --> preview["Sanitized preview"]
```

## 19 Packet

```mermaid
packet
    0-7: "Header"
    8-15: "Type"
    16-31: "Payload length"
```

## 20 Kanban

```mermaid
kanban
    todo[Todo]
        reproduce[Reproduce defect]
    doing[In progress]
        verify[Verify all diagrams]
    done[Done]
        issue[Create issue]
```

## 21 Architecture

```mermaid
architecture-beta
    service host(server)[WPF Host]
    service renderer(server)[Web Renderer]
    service preview(disk)[Preview]
    host:R -- L:renderer
    renderer:R -- L:preview
```

## 22 Radar

```mermaid
radar-beta
    axis parse["Parse"], sanitize["Sanitize"], labels["Labels"], export["Export"]
    curve current["Version 1.0.1"]{9, 9, 10, 8}
    max 10
    min 0
```

## 23 Treemap

```mermaid
treemap-beta
    "Rendering"
        "Automated tests": 60
        "Manual WebView2": 25
        "PDF export": 15
```
