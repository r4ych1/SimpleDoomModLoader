# Feature 015 - Shared-Library Selection Stability and Manual Drag Reorder

## Goal
Stop shared-library row reordering during selection changes and add explicit pointer-driven drag reorder for Source Port, IWAD, and Mod lists.

## In Scope
- Preserve existing single-click selection behavior for Source Port, IWAD, and Mod rows.
- Keep Source Port, IWAD, and Mod row order stable while selecting or deselecting.
- Pointer-driven drag reorder with a movement threshold for Source Port, IWAD, and Mod lists.
- One floating drag ghost and one insertion marker for the currently active drag list.
- Immediate persistence of reordered shared-library Source Port, IWAD, and Mod order.
- Drag reorder availability both with and without a selected profile.

## Out Of Scope
- Cross-list movement between Source Port, IWAD, and Mod lists.
- Changes to profile drag reorder behavior.
- Keyboard-only reorder shortcuts, multi-row drag, or auto-scroll while dragging.
- Changes to launch argument generation semantics for selected Mod sequence.

## Definitions
- Stable shared-library row order:
  - The collection order used for row rendering in `SourcePorts`, `Iwads`, and `Mods`.
- Successful drop:
  - A drag release with a valid insertion target inside the active list bounds.
- Selection sequence:
  - Ordered `SelectedModPaths` sequence used by launch argument generation.

## Rules
### Selection behavior
- Single-click on Source Port, IWAD, or Mod rows changes selection only for that list.
- Selection and deselection do not reorder Source Port, IWAD, or Mod rows.
- Mod selection sequence semantics remain unchanged:
  - selecting appends to selected sequence
  - deselecting removes from selected sequence
- Launch preview and launch argument generation continue to use selected Source Port, selected IWAD, and selected Mod sequence semantics.

### Drag start
- Drag reorder starts only from the non-interactive body of a row in the active list.
- Row delete actions do not start drag.
- A small pointer-movement threshold is required before press becomes active drag.
- A real drag does not also toggle row selection.

### Drag visuals
- While drag is active for Source Port, IWAD, or Mod, the UI shows:
  - one floating drag ghost
  - one insertion marker
- Insertion marker uses hovered-row midpoint targeting:
  - pointer in top half inserts before the row
  - pointer in bottom half inserts after the row
- Insertion marker supports insertion at:
  - first position
  - positions between rows
  - last position

### Drop behavior
- Releasing with a valid insertion target reorders only the active list immediately.
- Reordered list order persists immediately as canonical shared-library order for that list.
- Cross-list drops are invalid:
  - Source Port reorders only within Source Port rows
  - IWAD reorders only within IWAD rows
  - Mod reorders only within Mod rows
- Reordering does not change selected/unselected membership for rows.
- Reordering Source Port or IWAD does not clear selected Source Port/IWAD path if the selected path remains present.
- Reordering Mod rows does not rewrite selected sequence order in `SelectedModPaths`.
- Dropping onto the current no-op position leaves order unchanged and does not persist.

### Cancel behavior
- Releasing outside active list bounds cancels reorder and keeps order unchanged.
- Ending drag without a valid insertion target keeps order unchanged.
- When drag ends, drag ghost and insertion marker are removed.

## Supersession
- This feature supersedes earlier selection-synchronized Mod row ordering behavior from Feature 004.
- This feature supersedes Feature 008 Mod ordering context where it described selected-first Mod row display ordering.
- All other selection sequence and profile persistence behavior from Features 004 and 008 remains unchanged.

## Acceptance Criteria
### Click does not reorder any shared-library list
Given Source Port, IWAD, and Mod rows exist
When a user clicks a row to select or deselect it
Then only selection state changes.
And row order remains unchanged for all three lists.

### Source Port drag reorder updates and persists
Given three or more Source Port rows exist
When a Source Port row is dragged to a valid insertion target and released
Then Source Port row order updates to the dropped position.
And the new shared-library Source Port order persists immediately.

### IWAD drag reorder updates and persists
Given three or more IWAD rows exist
When an IWAD row is dragged to a valid insertion target and released
Then IWAD row order updates to the dropped position.
And the new shared-library IWAD order persists immediately.

### Mod drag reorder updates and persists
Given three or more Mod rows exist
When a Mod row is dragged to a valid insertion target and released
Then Mod row order updates to the dropped position.
And the new shared-library Mod order persists immediately.

### Drag reorder keeps selection state
Given selected Source Port and IWAD rows and one or more selected Mod rows
When Source Port, IWAD, or Mod rows are drag-reordered
Then selected Source Port and IWAD stay selected by path identity if still present.
And selected/unselected Mod membership remains unchanged.
And launch-argument selected Mod sequence remains unchanged.

### Invalid cross-list drag is not applied
Given Source Port, IWAD, and Mod rows exist
When a drag operation does not resolve to a valid insertion target inside its active list
Then no list reorder is applied.
And no cross-list move is persisted.

### Drag cancel keeps order
Given one or more rows exist in a list
When drag starts and ends without a valid insertion target
Then list order remains unchanged.
And no reorder persistence occurs.

### Drag available without selected profile
Given no profile is selected and Source Port, IWAD, or Mod rows exist
When rows are drag-reordered to valid insertion targets
Then shared-library order updates and persists for the reordered list.
And selection state remains unavailable.
