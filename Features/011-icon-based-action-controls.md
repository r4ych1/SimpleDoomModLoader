# Feature 011 - Icon-Based Action Controls

## Goal
Replace selected text-labeled UI action buttons with icon-based controls so the workspace stays compact and visually scannable without changing the underlying profile, library, rename, launch, delete, or collapse behavior, while also defining the shared Fluent icon used by the file-library scroll affordance.

## In Scope
- Icon-only presentation for selected profile-header, profile-row, shared-library-row, and section-toggle controls.
- Shared accessibility text for each icon-only control through tooltip and automation name metadata.
- Shared Fluent icon geometry resources stored in the window for reuse by all affected controls and the file-library scroll affordance indicator.

## Out Of Scope
- Changes to launch rules, profile validity, persistence, drag behavior, or rename behavior.
- Changes to the amber confirmation-message `Delete` and `Cancel` buttons.
- New keyboard shortcuts, context menus, or alternate action placements.

## Definitions
- Icon-only control:
  - A standard button that keeps its existing hit target and click behavior but renders only a Fluent icon as visible content.
- Shared accessibility text:
  - The action text exposed through tooltip and automation name metadata for icon-only controls.

## Rules
### Icon Mappings
- The left profile-management header `New Profile` action uses `add_square_regular`.
- The left profile-management header file-library toggle uses:
  - `folder_regular` while the file library pane is collapsed and the action text is `Expand File Library`
  - `folder_open_regular` while the file library pane is expanded and the action text is `Collapse File Library`
- The file-library scroll affordance indicator uses `arrow_down_regular`.
- Each profile-row `Launch` action uses `play_regular`.
- Each profile-row `Rename` action uses `edit_regular`.
- Each profile-row `Delete` action uses `delete_regular`.
- Each shared-library row `Delete` action uses `delete_regular`.
- Each Source Port / IWAD / Mod section toggle uses:
  - `chevron_up_regular` while that section is expanded and the action semantics are `Collapse`
  - `chevron_down_regular` while that section is collapsed and the action semantics are `Expand`

### Behavior Preservation
- Icon-only controls keep the same click handlers, enabled state, visibility rules, and layout slots used by the prior text-labeled buttons.
- Profile-row icon actions do not change drag-reorder behavior:
  - they do not start drag reorder
  - rename mode still hides the normal row actions
- Shared-library row icon actions do not change row selection or remove semantics.
- File-library and inner section toggles keep their existing collapse and expand behavior exactly.
- The file-library scroll affordance indicator is visual-only and does not introduce a new command, hit target, or keyboard behavior.

### Accessibility
- Every icon-only control exposes the same action text through:
  - tooltip text
  - automation name text
- The file-library pane toggle continues to expose `Expand File Library` or `Collapse File Library` according to its current state.
- Source Port / IWAD / Mod section toggles continue to expose `Expand` or `Collapse` according to their current state.
- Static icon-only actions expose these texts:
  - `New Profile`
  - `Launch`
  - `Rename`
  - `Delete`

### Visual Composition
- The icon-only conversion does not remove button chrome or reduce the effective hit target below the prior button treatment.
- Fluent icon geometries are defined as shared `StreamGeometry` resources and rendered through `PathIcon`.
- Icon color follows normal button foreground behavior rather than introducing custom per-action colors.
- The file-library scroll affordance indicator is anchored near the bottom-center of the expanded file-library pane, remains non-interactive, and uses low visual weight with semi-transparent presentation.

## Acceptance Criteria
### New profile uses icon-only control
Given the left profile-management header is rendered
When the header actions are visible
Then the `New Profile` action renders `add_square_regular` as icon-only content.
And that control exposes `New Profile` through tooltip and automation name text.
And activating it keeps existing Feature 008 profile-creation behavior unchanged.

### File-library pane toggle changes icon by state
Given the left profile-management header is rendered
When the file library pane is collapsed
Then the file-library toggle renders `folder_regular`.
And it exposes `Expand File Library` through tooltip and automation name text.
And when the file library pane is expanded
Then that same toggle renders `folder_open_regular`.
And it exposes `Collapse File Library` through tooltip and automation name text.

### Profile-row actions use icons without behavior change
Given a saved profile row is rendered in normal display mode
When its row actions are shown
Then `Launch`, `Rename`, and `Delete` render `play_regular`, `edit_regular`, and `delete_regular` respectively as icon-only controls.
And each control exposes its action text through tooltip and automation name text.
And existing launch enablement, rename entry, and delete-confirmation behavior remain unchanged.

### Shared-library delete actions use delete icon
Given a Source Port, IWAD, or Mod row is rendered
When its row action is shown
Then the row `Delete` action renders `delete_regular` as icon-only content.
And it exposes `Delete` through tooltip and automation name text.
And existing removal behavior remains unchanged.

### Section toggles use chevron icons by state
Given a Source Port, IWAD, or Mod section header is rendered
When that section is expanded
Then its toggle renders `chevron_up_regular`.
And it exposes `Collapse` through tooltip and automation name text.
And when that section is collapsed
Then its toggle renders `chevron_down_regular`.
And it exposes `Expand` through tooltip and automation name text.

### File-library scroll affordance uses downward chevron icon
Given the expanded file-library pane shows the hidden-scrollbar scroll affordance
When that affordance is visible
Then it renders `arrow_down_regular` as non-interactive icon-only content near the bottom-center of the pane.
And its visual treatment remains subtle and semi-transparent.
