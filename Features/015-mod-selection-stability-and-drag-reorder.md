# Feature 015 - Mod Selection Stability and Manual Drag Reorder

## Goal
Stop Mod row reordering during selection changes and add explicit pointer-driven drag reorder for the shared Mod library list.

## In Scope
- Preserve single-click Mod selection and deselection behavior.
- Keep Mod row order stable while selecting or deselecting.
- Pointer-driven Mod drag reorder with a movement threshold.
- One floating drag ghost and one insertion marker during active drag.
- Immediate persistence of reordered shared-library Mod order.
- Mod drag reorder availability both with and without a selected profile.

## Out Of Scope
- Changes to Source Port or IWAD reorder behavior.
- Changes to profile drag reorder behavior.
- Keyboard-only reorder shortcuts, multi-row drag, or auto-scroll while dragging.
- Changes to launch argument generation semantics for selected Mod sequence.

## Definitions
- Stable Mod row order:
  - The shared-library `Mods` collection order used for Mod row rendering.
- Successful Mod drop:
  - A drag release with a valid insertion target inside the Mod list bounds.
- Selection sequence:
  - Ordered `SelectedModPaths` sequence used by launch argument generation.

## Rules
### Selection behavior
- Single-click on a Mod row toggles only that row's selected state.
- Selection and deselection do not reorder Mod rows.
- Selection sequence semantics remain unchanged:
  - selecting appends to selected sequence
  - deselecting removes from selected sequence
- Launch preview and launch argument generation continue to use selection sequence.

### Drag start
- Mod drag reorder starts only from the non-interactive body of a Mod row.
- Mod row delete action does not start drag.
- A small pointer-movement threshold is required before press becomes active drag.
- A real drag does not also toggle Mod selection.

### Drag visuals
- While Mod drag is active, the UI shows:
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
- Releasing with a valid insertion target reorders shared-library Mod rows immediately.
- Reordered Mod order persists immediately as canonical shared-library `Mods` order.
- Reordering does not change selected/unselected membership of Mod rows.
- Reordering does not rewrite selected sequence order in `SelectedModPaths`.
- Dropping onto the current no-op position leaves order unchanged and does not persist.

### Cancel behavior
- Releasing outside Mod list bounds cancels reorder and keeps order unchanged.
- Ending drag without a valid insertion target keeps order unchanged.
- When drag ends, drag ghost and insertion marker are removed.

## Supersession
- This feature supersedes earlier selection-synchronized Mod row ordering behavior from Feature 004.
- This feature supersedes Feature 008 Mod ordering context where it described selected-first Mod row display ordering.
- All other selection sequence and profile persistence behavior from Features 004 and 008 remains unchanged.

## Acceptance Criteria
### Click does not reorder
Given three or more Mod rows exist
When a user clicks a Mod row to select or deselect it
Then only selection state changes.
And Mod row order remains unchanged.

### Drag reorder to first
Given three or more Mod rows exist
When a later Mod row is dragged above the first Mod row and released
Then that Mod row becomes first.
And the new shared-library Mod order persists immediately.

### Drag reorder between rows
Given three or more Mod rows exist
When a Mod row is dragged to a midpoint between two rows and released
Then that Mod row is inserted at that midpoint position.
And the new shared-library Mod order persists immediately.

### Drag reorder to last
Given two or more Mod rows exist
When a Mod row is dragged below the last row and released in list bounds
Then that Mod row becomes last.
And the new shared-library Mod order persists immediately.

### Drag reorder keeps selection state
Given one or more selected Mod rows
When a selected or unselected Mod row is drag-reordered
Then selected/unselected membership remains unchanged.
And launch-argument selected sequence remains unchanged.

### Drag cancel keeps order
Given one or more Mod rows exist
When a Mod drag starts and ends without a valid insertion target
Then Mod row order remains unchanged.
And no reorder persistence occurs.

### Drag available without selected profile
Given no profile is selected and Mod rows exist
When a Mod row is drag-reordered to a valid insertion target
Then shared-library Mod order updates and persists.
And Mod selection state remains unavailable.
