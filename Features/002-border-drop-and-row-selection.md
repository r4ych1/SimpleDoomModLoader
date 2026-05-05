# Feature 002 - Full-Border Drop Zones and Selectable IWAD/Mod Rows

## Goal
Expand the drop interaction hit area to each section border and add deterministic row selection behavior for IWAD and Mod lists.

## In Scope
- Drop zones accept drag/drop anywhere inside the section border for:
  - Source Port
  - IWAD
  - Mod
- IWAD row selection:
  - Exactly one row may be selected at a time.
  - Re-selecting the selected row deselects it.
- Mod row selection:
  - Multiple rows may be selected at the same time.
  - Re-selecting a selected row deselects only that row.
- Visual selected-row feedback for IWAD and Mod rows.
- In-memory state only.

## Out Of Scope
- Persistence.
- Launching source port.
- Multiple profiles.
- Keyboard-modifier selection semantics (`Ctrl`, `Shift`).

## Rules
### Full-Border Drop Interaction
- Drag-over and drop handlers for each section apply to the full area inside that section border, including empty interior space.
- Existing file validation, dedupe, and ordering rules from Feature 001 remain unchanged.
- Each drop zone renders a visible default target treatment before any drag begins:
  - rounded card/container styling
  - lightly tinted background
  - visible outlined border
  - clearly readable instructional text
  - an always-visible empty-state icon or badge treatment
- Each drop zone renders helper copy that includes:
  - `Drag and drop here`
  - `Drag files here or click to upload`
- Helper text remains visibly instructional rather than low-contrast decorative text.
- When a valid file payload is dragged over a drop zone, that zone enters a visible drag-over state:
  - border contrast and/or thickness increases
  - background becomes brighter or more tinted
  - the active drop target is visually clearer than its default state
- Each drop zone is also a clickable fallback target:
  - clicking the zone opens a multi-select file picker for that zone
  - the picker uses the same file-extension allowlist already defined by Feature 001 and later extension expansions
  - clickable fallback does not change drag/drop processing behavior
- Each clickable drop zone supports keyboard activation:
  - the zone can receive focus
  - `Enter` and `Space` trigger the same picker flow as click
- Each clickable drop zone provides an accessible label describing the drag-and-drop plus click-upload affordance.

### IWAD Selection
- Row click toggles selection for that row.
- If no IWAD row is selected, clicking a row selects it.
- If a different IWAD row is selected, clicking a row moves selection to the clicked row.
- If the clicked row is already selected, selection is cleared.

### Mod Selection
- Row click toggles selection for that row independently.
- Clicking an unselected Mod row adds it to selected rows.
- Clicking a selected Mod row removes it from selected rows.

### Remove Interaction
- Removing a selected IWAD row clears IWAD selection.
- Removing a selected Mod row removes that row from selected Mod rows.
- Removing a non-selected row does not change other selected rows.

## Acceptance Criteria
### Border drop capture
Given a pointer over empty interior area of a section border
When files are dropped
Then that section processes the drop with the same validation and ordering behavior defined in Feature 001.

### Visible idle affordance
Given any Source Port, IWAD, or Mod drop zone is rendered
When no drag is active
Then the zone still appears as an intentional upload target with visible instructional styling and helper text.

### Drag-over highlight state
Given a valid file payload is dragged over a drop zone
When the zone enters drag-over
Then that zone shows a stronger highlighted state than its default idle styling.
And when the drag leaves or the drop completes
Then the zone returns to its default idle styling.

### Click and keyboard fallback
Given a drop zone is focused or clicked
When the user activates it with click, `Enter`, or `Space`
Then that zone opens a multi-select file picker using the same file-type allowlist as its drag/drop rules.

### IWAD single-select toggle
Given at least two IWAD rows
When a row is selected and then a different row is selected
Then only the most recently selected row remains selected.
And when the selected row is selected again
Then no IWAD row is selected.

### Mod multi-select toggle
Given at least three Mod rows
When two different rows are selected
Then both rows remain selected.
And when one selected row is selected again
Then only that row is deselected.

### Selection consistency with remove
Given selected IWAD and Mod rows
When selected rows are removed
Then removed rows are no longer selected and unrelated selections remain unchanged.
