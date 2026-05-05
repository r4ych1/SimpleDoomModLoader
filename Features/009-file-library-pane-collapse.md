# Feature 009 - Collapsible File Library Pane

## Goal
Wrap the shared file library in its own right-side workspace section and allow that section to collapse fully out of view so the left profile pane can expand into the freed space.

## In Scope
- Pane-level collapse and expand behavior for the right-side file library section.
- A dedicated file-library section header with title only while expanded.
- Persistent `IsFileLibraryPaneCollapsed` state across restart.
- Layout changes that let the left profile pane expand when the file library is collapsed.

## Out Of Scope
- Resizable splitters or drag-to-resize pane widths.
- Changes to profile creation, selection, rename, delete, validity, or launch rules.
- Changes to Source Port / IWAD / Mod inner section collapse behavior.

## Definitions
- File library pane:
  - The right-side shared library workspace section that contains the selected-profile header plus Source Port, IWAD, and Mod library controls.
- Collapsed file library state:
  - The hidden right-pane state that removes the file-library pane body and inter-pane gap from view.

## Rules
### Workspace Layout
- The right-side shared library is rendered inside its own bordered `File Library` section similar in structure to the left profile-management section.
- The file-library section header contains the `File Library` section title.
- The file-library collapse and expand action is invoked from the left profile-management header, using the `Expand` / `Collapse` control to the right of `New Profile`.
- The selected-profile header area remains inside the file-library pane body and keeps its existing behavior except where this feature explicitly updates pane-collapse interaction.
- When the file library is expanded:
  - the workspace uses the existing Feature 008 two-pane arrangement
  - the file library fills its default right-side size
- When the file library is collapsed:
  - the right file-library pane is not rendered
  - the spacer gap between the profile pane and file-library pane is not rendered
  - the left profile pane expands to fill the remaining workspace width
- Collapsing the file library does not move it to the left side and does not overlap the profile pane.
- The collapsed state does not show a right-edge strip, a secondary expand affordance, or a duplicate `File Library` label.

### Behavior Preservation
- File-library pane collapse does not change:
  - selected profile
  - current library selections
  - left-pane row launch availability
  - profile auto-save behavior
  - Source Port / IWAD / Mod inner section collapse state
- File-library pane state controls inline profile-row command preview visibility:
  - when the file library pane is collapsed, inline profile-row command preview may be visible subject to the shared Feature 008 window-width rule
  - when the file library pane is expanded, inline profile-row command preview is hidden for all profile rows
- Activating `New Profile` while the file library pane is collapsed is one exception:
  - the new profile is still created and selected using existing Feature 008 rules
  - the file library pane expands immediately so the selected-profile header becomes visible
  - the expanded pane state persists immediately
- Double-clicking a profile row while the file library pane is collapsed is the other exception:
  - the clicked profile ends selected using existing Feature 008 rules
  - current Source Port / IWAD / Mod selections hydrate from that profile
  - the file library pane expands immediately so the selected-profile header becomes visible
  - the expanded pane state persists immediately
- Double-clicking a profile row while the file library pane is expanded is the inverse exception:
  - the clicked profile ends selected using existing Feature 008 rules
  - current Source Port / IWAD / Mod selections hydrate from that profile
  - the file library pane collapses immediately
  - the collapsed pane state persists immediately
- Expanding the file library restores the full file-library pane body without resetting its inner section states.

### Persistence
- `LaunchInputsConfig` adds:
  - `IsFileLibraryPaneCollapsed`
- The file-library pane collapse state persists immediately after toggle.
- Saves that do not contain `IsFileLibraryPaneCollapsed` load with the file library expanded by default.

## Acceptance Criteria
### Collapse to hidden pane
Given the workspace is rendered with the file library expanded
When the file-library collapse control is activated
Then the file library pane is removed from view.
And the spacer gap between the panes is removed from view.
And the left profile pane expands to fill the remaining workspace width.

### Expand to default size
Given the file library is collapsed
When the expand control is activated
Then the file library returns to its default expanded size.
And the spacer gap between the panes returns.
And the selected-profile header area and shared library controls become visible again.
And inline profile-row command preview is hidden for all profile rows.

### New profile expands collapsed pane
Given the file library is collapsed
When `New Profile` is activated from the left profile-management header
Then the new profile is created and selected using Feature 008 rules.
And the file library returns to its default expanded size.
And the selected-profile header area becomes visible again.

### Double-clicked profile expands collapsed pane
Given the file library is collapsed
And a saved profile row exists
When that profile row is double-clicked
Then that profile is selected using Feature 008 rules.
And the file library returns to its default expanded size.
And the selected-profile header area becomes visible again.

### Double-clicked profile collapses expanded pane
Given the file library is expanded
And a saved profile row exists
When that profile row is double-clicked
Then that profile is selected using Feature 008 rules.
And the file library pane is removed from view.
And the spacer gap between the panes is removed from view.

### Collapse state persists
Given the user collapses or expands the file library
When config persistence occurs and the app restarts
Then the file-library pane restores the last saved collapsed state.

### Existing workspace behavior remains intact
Given any file-library pane state
When the user selects profiles, launches profiles from the left pane, edits library selections, or toggles inner Source Port / IWAD / Mod sections
Then existing Feature 008 profile, launch, and shared-library behavior remains unchanged.
