# Feature 014 - Single-View Profile/Library Workspace Swap

## Goal
Replace the two-pane workspace with one shared workspace that shows either `Profiles` or `File Library`.

## In Scope
- Replace pane collapse with full-view swapping between `Profiles` and `File Library`.
- Move the File Library back-to-profiles action to a top-left position above the selected-profile header.
- Keep the selected-profile header fixed at the top of File Library view.
- Add a selected-profile-header `Create Profile` action that is visible only when no profile is selected.
- Remove persisted pane-collapse and remembered-width state.
- Remove app-defined fixed default window sizes.
- Persist the last active workspace view.

## Out Of Scope
- Changes to profile validity rules.
- Changes to launch execution rules.
- Changes to drag reorder rules.
- Changes to section-collapse behavior inside the file library.

## Definitions
- Profiles view:
  - The full-width workspace view that shows the profile-management header, profile list, profile-row actions, drag reorder, and profile-list scroll affordance.
- File Library view:
  - The full-width workspace view that shows the selected-profile header plus the shared Source Port, IWAD, and Mod library controls.

## Rules
### Workspace composition
- The workspace shows one full-width view at a time:
  - `Profiles`
  - `File Library`
- The Profiles view contains:
  - the `Profiles` label
  - the `New Profile` action
  - the `File Library` view-swap action
  - the saved profile list
- The File Library view contains:
  - a top-left `Profiles` back action above the selected-profile header
  - the selected-profile header
  - the shared Source Port, IWAD, and Mod library controls
- The File Library view exposes a top-left `Profiles` back action above the selected-profile header.
- The selected-profile header remains fixed while the file-library content scrolls beneath it.

### View-swap behavior
- Activating the Profiles-header `File Library` action opens File Library view.
- Activating the top-left File-Library `Profiles` back action opens Profiles view.
- The last active workspace view persists immediately after change.
- Saves that do not contain view state load into Profiles view by default.
- Switching views cancels any active rename session.

### Profile interactions
- Creating a new profile selects it and keeps Profiles view active.
- Double-clicking a profile row selects that profile and opens File Library view.
- Single-click profile selection behavior remains unchanged.
- Opening File Library view does not require a selected profile.
- When no profile is selected, Source Port, IWAD, and Mod rows remain visible but are not selectable.
- Disabled shared-library rows keep their delete actions available.

### Header behavior
- The File Library header shows the selected profile name while no header rename is active.
- The File Library header `Edit` action opens rename inline in that header.
- The File Library header `Delete` action keeps the existing delete-confirmation flow.
- The selected-profile header `Edit` action opens rename inline in that header.
- When no profile is selected, the selected-profile header hides `Edit` and `Delete`.
- When no profile is selected, the selected-profile header shows a `Create Profile` action inline to the right of the selected-profile label.
- Activating the selected-profile-header `Create Profile` action creates and selects a new profile using Feature 008 profile-creation rules while keeping File Library view visible.
- The selected-profile status text and selected-profile command preview keep their existing meaning.

### Visibility rules carried forward
- Profile-row command preview is visible only in Profiles view and only when the overall window width is greater than `768 px`.
- Profile-row inline invalid-reason text is visible only in Profiles view.
- File Library view hides profile-row command preview and inline invalid-reason text.
- Profile-list and file-library scroll affordances remain visual-only and only show for the currently visible view.

### Persistence
- `LaunchInputsConfig` replaces `IsFileLibraryPaneCollapsed` with `IsFileLibraryViewActive`.
- `LaunchInputsConfig` no longer persists `LastExpandedWindowWidth`.
- Legacy `IsFileLibraryPaneCollapsed` and `LastExpandedWindowWidth` fields are ignored on load and are not re-saved.

## Acceptance Criteria
### Profiles view swaps to File Library
Given the Profiles view is visible
When the Profiles-header view-swap action is activated
Then File Library view becomes visible.
And the new active view persists immediately.

### File Library view swaps to Profiles
Given File Library view is visible
When the top-left File-Library `Profiles` back action is activated
Then Profiles view becomes visible.
And the new active view persists immediately.

### New profile keeps Profiles view active
Given Profiles view is visible
When the user creates a new profile
Then the new profile is created and selected using Feature 008 rules.
And Profiles view remains visible.

### Double-clicked profile opens File Library view
Given Profiles view is visible and a saved profile row exists
When that profile row is double-clicked
Then that profile is selected using Feature 008 rules.
And File Library view becomes visible immediately.

### File Library remains accessible without a selected profile
Given no profile is selected
When File Library view is opened
Then Source Port, IWAD, and Mod rows remain visible.
And those rows are not selectable.
And their delete actions remain available.

### Header edit renames in place
Given File Library view is visible and a profile is selected
When the selected-profile header `Edit` action is activated
Then the header replaces the selected-profile name display with a rename input.
And the existing rename validation and toast behavior remain unchanged.

### Header create profile is available when no profile is selected
Given File Library view is visible and no profile is selected
When the selected-profile header is shown
Then a `Create Profile` action is visible inline to the right of the selected-profile label.
And `Edit` and `Delete` are hidden.
And when the `Create Profile` action is activated
Then a new profile is created and selected using Feature 008 rules.
And File Library view remains visible.
