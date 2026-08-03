# Tab Reordering

## Behavioral contract

- A horizontal pointer drag starts only after the system drag threshold is
  crossed.
- The tab strip shows an insertion indicator between tabs while dragging.
- Dropping before the first tab or after the last tab is supported.
- Dropping beside the tab's original position is a no-op.
- Moving a tab preserves the selected document, unsaved content, dirty marker,
  view mode, cursor line, and editor and preview scroll positions.
- Tab selection, keyboard traversal, context-menu actions, and closing continue
  to follow the visible order.
- The reordered position remains stable for the lifetime of the application
  process. Restoring order after restart is outside this feature.

## Design

The WPF tab strip remains responsible for pointer capture, hit testing, and the
drop indicator. It translates the pointer position into an insertion slot from
zero through the number of open tabs.

`TabReorder` converts that insertion slot into the destination index used by the
presentation collection. Keeping this index policy independent of WPF makes
left, right, boundary, and no-op moves directly testable.

The existing `DocumentTabViewModel` instance is moved rather than recreated.
Document identity and all mutable document state therefore remain attached to
the same object.

## Acceptance coverage

Automated tests cover index translation, boundary positions, no-op positions,
and invalid inputs. Presentation tests and manual qualification will cover the
WPF pointer interaction, indicator placement, state preservation, context-menu
actions, selection, closing, and keyboard traversal.
