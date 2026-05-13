# Feature 006 - Section Collapse Layout and Row Interaction States

## Goal
Align row-level and section-level actions into a shared right-hand action-column pattern, move collapse controls inline with section labels, and provide distinct idle-unselected/hover/selected/selected+hover row visuals in light and dark themes.

Feature 011 later becomes authoritative for icon presentation of the row `Delete` actions and section collapse toggles while preserving the layout and behavior defined here.

## In Scope
- Row-level `Delete` button alignment for IWAD and Mod rows.
- Row-level `Delete` button alignment for Source Port, IWAD, and Mod rows.
- Section-level collapse / expand toggle for Source Port list.
- Section-level collapse / expand toggles for IWAD and Mod lists.
- Persisted collapse state for Source Port, IWAD, and Mod sections.
- Row visual states:
  - idle unselected outline
  - hover
  - selected
  - selected + hover
- Theme-aware row-state styling for both light and dark variants.

## Out Of Scope
- Changes to file allowlists, dedupe, or drop processing rules.
- Changes to selection semantics from prior features.
- Changes to command-preview argument format or launch behavior.

## Definitions
- Shared right-hand action column:
  - The single right-side action column used by row-level `Delete` buttons within a section.
  - The section header action group for that section must align to this same column, with `Collapse` / `Expand` inline to the right of the section label.
- Selected + hover state:
  - The visual state when a row is selected and currently pointer-hovered.
- Collapsed section:
  - A section state where only the section header row remains visible and the descriptor text, drop zone, and input rows are hidden.

## Rules
### Row-Level Delete Alignment
- Each Source Port / IWAD / Mod row `Delete` action is right-aligned to the section's shared right-hand action column.
- Row `Delete` actions remain vertically centered within each row.
- Existing removal behavior is unchanged.

### Section Collapse / Expand
- Source Port, IWAD, and Mod sections each provide a section-header toggle button.
- The toggle button exposes `Collapse` semantics when that section's rows are currently visible.
- The toggle button exposes `Expand` semantics when that section's rows are currently hidden.
- The toggle button appears inline with the section label on the same header row.
- Feature 011 later defines the visible icon used by that toggle.
- Activating the toggle hides or shows that section's body:
  - descriptive text
  - drop zone
  - input rows
- Collapse state is persisted independently for Source Port, IWAD, and Mod sections.
- Collapse state is shared-library UI state and is not tied to any selected profile.
- Configs that do not contain persisted collapse fields load with all three sections expanded.

### Row Interaction Visual States
- Active unselected rows show a subtle always-visible outline when not hovered and not selected.
- Hovering a non-selected row shows a visible hover state.
- Selecting a row shows a persistent selected state.
- Hovering a selected row shows an additional visual indication beyond selected-only state.
- Selected rows remain clearly selected while hovered.
- Idle unselected outline, hover, selected, and selected+hover states are visually distinct.
- Shared-library rows in `selection-disabled` state remain borderless while keeping their existing disabled treatment.
- Row-state styling is clearly distinguishable in both light and dark themes.

## Acceptance Criteria
### Shared action-column alignment
Given Source Port, IWAD, and Mod rows with row-level `Delete` actions
When the section is rendered
Then each `Delete` action is aligned to its section's shared right-hand action column.
And each `Delete` action remains vertically centered in its row.

### Section collapse toggle behavior and placement
Given any Source Port, IWAD, or Mod section is rendered
When the section header is displayed
Then that section shows the collapse / expand toggle inline with the section label.
And activating the toggle hides or shows that section's body.
And a collapsed section renders only the section header row.

### Persisted collapse state
Given a section has been collapsed or expanded
When the app persists state and restarts
Then each section restores its last saved collapsed or expanded state.
And configs without the new collapse fields start with all sections expanded.

### Row idle, hover, and selected visual distinctions
Given an IWAD or Mod row
When row is unselected, not hovered, and selection-enabled
Then an idle unselected outline visual is shown.
And when row is hovered and not selected
Then hover visual is shown.
And when row is selected and not hovered
Then selected visual is shown.
And when row is selected and hovered
Then selected+hover visual is shown and remains clearly selected.
And idle unselected outline, hover, selected, and selected+hover are visually distinguishable in light and dark themes.

### Disabled shared-library rows remain borderless
Given no profile is selected and shared-library rows are visible in disabled state
When a Source Port, IWAD, or Mod row is rendered and hovered
Then the row keeps borderless disabled styling.
And hover does not add outline or hover border feedback.

### Behavioral regression safety
Given existing add/remove/select workflows
When Feature 006 changes are applied
Then existing add/remove/select behaviors continue to function with prior deterministic rules.
