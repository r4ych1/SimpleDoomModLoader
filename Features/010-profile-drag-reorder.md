# Feature 010 - Profile Drag Reordering

## Goal
Allow users to manually reorder saved profiles directly within the left-side profile-management list using drag interaction.

## In Scope
- Pointer-driven drag reordering within the left-side saved profile list.
- A floating ghost row that displays only the dragged profile name.
- A single insertion marker that shows where the dragged profile will be inserted.
- Immediate persistence of the new saved profile order after a successful drop.

## Out Of Scope
- Drag reordering for Source Port, IWAD, or Mod rows.
- Multi-row drag, keyboard reorder shortcuts, or auto-scroll while dragging.
- Changes to profile validity, selection, launch, rename, delete, or file-library behavior outside list ordering.

## Definitions
- Drag ghost:
  - A floating visual that follows the pointer during profile drag and displays only the dragged profile name.
- Insertion marker:
  - The single visual line that indicates the current before-or-after drop position within the saved profile list.
- Successful drop:
  - A completed drag release with a valid insertion target inside the saved profile list.

## Rules
### Drag Start
- Drag reordering starts only from the non-interactive body of a profile row in the left profile list.
- `Launch` and `Delete` buttons do not start drag.
- Valid and invalid profile rows are both draggable.
- A small pointer-movement threshold is required before a press becomes an active drag.
- A real drag does not also toggle profile row selection.

### Drag Visuals
- While a drag is active, the UI shows:
  - one floating drag ghost
  - one insertion marker
- The drag ghost renders only the dragged profile name.
- The drag ghost does not render:
  - validity badge
  - invalid reason text
  - `Launch`
  - `Delete`
- The insertion marker uses hovered-row midpoint logic:
  - pointer in the top half of a row targets insertion before that row
  - pointer in the bottom half of a row targets insertion after that row
- The insertion marker supports insertion at:
  - the first position
  - positions between rows
  - the last position

### Drop Behavior
- Releasing with a valid insertion target reorders the saved profile list immediately.
- Reordered profile order persists immediately and becomes the canonical saved profile order.
- Reordering preserves the moved profile's contents exactly.
- Reordering preserves `SelectedProfileId`.
- Reordering does not clear or recompute current hydrated library selections beyond what existing selected-profile behavior already provides.
- Dropping onto the current no-op position leaves order unchanged and does not persist a new state.

### Cancel Behavior
- Releasing outside the saved profile list cancels the drag and leaves order unchanged.
- Ending the drag without a valid insertion target leaves order unchanged.
- When drag ends, both the drag ghost and insertion marker are removed.

## Acceptance Criteria
### Reorder to first position
Given three or more saved profiles exist
When a later profile row is dragged above the first row and released
Then that profile becomes the first saved profile.
And the new order persists immediately.

### Reorder between rows
Given three or more saved profiles exist
When a profile row is dragged between two other rows using midpoint targeting and released
Then that profile is inserted at that midpoint position.
And the new order persists immediately.

### Reorder to last position
Given two or more saved profiles exist
When a profile row is dragged below the last row and released within the saved profile list
Then that profile becomes the last saved profile.
And the new order persists immediately.

### Selected profile remains selected
Given the currently selected profile is dragged to a new position
When the drag is released on a valid insertion target
Then `SelectedProfileId` remains that profile's `Id`.
And the current selected profile remains the selected profile after reorder.

### Drag cancel leaves order unchanged
Given one or more saved profiles exist
When a drag is started and released outside the saved profile list
Then the saved profile order remains unchanged.
And no reorder persistence occurs.

### Real drag does not toggle selection
Given a profile row is draggable
When the pointer movement exceeds the drag threshold and the row is dragged
Then the interaction is treated as drag reorder only.
And the row press does not also toggle selection from click behavior.
