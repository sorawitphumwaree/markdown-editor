# Source Mapping

`markdown-it` token maps are copied to rendered block attributes:

```html
<p data-source-start="42" data-source-end="45">...</p>
```

Editor clicks select the narrowest rendered range containing the source line.
This prevents a broad table, code fence, or Mermaid wrapper from hiding a more
precise mapped child. If no range contains the line, the nearest preceding block
is used.

Preview clicks select the first line of the nearest mapped rendered element.
Manual scrolling is independent; cross-pane movement occurs only on clicks.
Link clicks are handled separately and never trigger source navigation.

Detailed mappings currently include table rows, highlighted fenced-code lines,
and Mermaid Flowchart, Sequence, and Class elements. Other Mermaid families fall
back to diagram-level mapping.
