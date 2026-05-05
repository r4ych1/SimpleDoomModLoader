# Feature 008 - Selection-Based Profile Workspace

## Goal
Introduce saved launch profiles as the primary launch model by converting the current screen into a two-pane workspace: saved profiles on the left and the shared Source Port / IWAD / Mod library on the right.

Feature 009 later adds pane-level collapse for the right-side file library. Feature 008 remains authoritative for the expanded file-library workspace behavior.
Feature 010 later adds manual drag reordering for saved profile rows. Feature 008 remains authoritative for profile selection, launch, rename, delete, validity, and auto-save behavior outside explicit ordering rules introduced there.

## In Scope
- Saved profile list with single-select toggle behavior.
- Profile creation from current live library selections using generated default names from the left-pane profile-management header.
- Row-scoped launch actions for saved profiles.
- Inline selected-profile rename initiated from the right-pane selected-profile header.
- Profile delete with confirmation.
- Immediate profile auto-save when editing a selected profile through library selection changes.
- Persisted `Profiles` and `SelectedProfileId`.
- Profile validity computation based on required launch inputs plus library membership and file existence.
- Backward-compatible load of existing library lists without auto-migrating a profile from legacy selected fields.

## Out Of Scope
- Save / Save As workflow.
- Detached draft persistence across restart.
- Profile notes, tags, extra launch args, last-played metadata, or separate routes/tabs.
- Automatic selection of another profile after delete.
- Any prompt-based profile naming flow during creation.

## Definitions
- Shared library:
  - The persisted Source Port, IWAD, and Mod collections shown in the right pane.
- Profile:
  - A saved record with stable `Id`, unique display `Name`, one source-port path, one IWAD path, and ordered selected mod paths.
- Selected profile:
  - The nullable saved profile identified by `SelectedProfileId`.
- Detached library state:
  - Current library selections shown when no profile is selected. Detached state is temporary UI state only and is not restored on restart.
- Invalid profile:
  - A saved profile whose current references cannot produce valid launch arguments.

## Rules
### Workspace Layout
- The window becomes a two-pane workspace:
  - Left pane: profile management.
  - Right pane: the existing shared Source Port / IWAD / Mod library.
- The left profile pane remains pinned while the right file-library pane scrolls independently.
- The left profile list provides its own internal scrolling when saved profiles exceed available vertical space.
- The left pane begins with a profile-management header row that contains:
  - the `Profiles` label on the left
  - the `New Profile` action on the right
  - the file-library `Expand` / `Collapse` action to the right of `New Profile`
- `New Profile` and the file-library `Expand` / `Collapse` action are right-aligned within the profile-management header row and aligned with the `Profiles` label.
- The right pane begins with a selected-profile header area that contains:
  - the selected profile name or selected-profile rename input on the left
  - the `Rename` action on the right
- The footer command preview remains visible and reflects current library selections, even when no profile is selected.
- The existing top message/warning area is reused for rename validation and delete confirmation.

### Profile List And Selection
- Left pane shows:
  - saved profile rows
  - explicit row-level validity messaging
  - launch access
  - delete access
- Saved profile row ordering is the current profile-list display order.
- Feature 010 becomes authoritative for how profile row ordering is changed by drag reordering and persisted afterward.
- Profile rows use one shared right-side status-badge slot:
  - invalid rows show an `INVALID` badge in that slot and keep the invalid reason text under the profile name
  - valid rows show a `VALID` badge in that same slot
  - valid rows do not render a separate inline valid text line under the profile name
- Profile rows are single-select toggle rows:
  - Clicking an unselected row selects it.
  - Clicking a different selected row moves selection to that profile.
  - Clicking the selected row unselects it.
- Selecting a profile hydrates current Source Port / IWAD / Mod selections from that profile.
- Unselecting a profile:
  - sets `SelectedProfileId` to `null`
  - clears current Source Port / IWAD / Mod selections
  - leaves no launchable selected profile
- Double-clicking a profile row has no special behavior beyond the existing row-selection interaction model.

### Profile Launch
- Each profile row exposes a `Launch` action immediately to the left of `Delete`.
- Row `Launch` is available only while the row is in normal display mode.
- Row `Launch` is enabled only when that row's profile is valid.
- Activating `Launch` for a row:
  - selects that profile
  - hydrates current Source Port / IWAD / Mod selections from that profile
  - launches that profile through the existing launcher flow
- Row `Launch` does not require the profile to already be selected.
- Invalid profiles remain listed and selectable but their row `Launch` action is disabled.
- Valid profiles show an explicit `VALID` badge in the shared row status slot.

### Profile Creation
- `New Profile` is always enabled.
- `New Profile` uses the current live library selections at activation time:
  - zero or one selected source port
  - zero or one selected IWAD
  - zero or more selected Mods in current order
- Current selected Mods, if any, are copied into the new profile in current order.
- Clicking `New Profile` immediately creates a new saved profile from current library selections.
- New profile name uses the first available `Profile N` positive integer sequence, filling gaps from deleted profiles.
- New profile creation:
  - generates a new stable `Id`
  - persists immediately
  - selects the new profile
- Creating a new profile while the file library pane is collapsed expands the file library pane immediately and persists the expanded pane state.
- A newly created profile may start invalid and remain repairable through later shared-library edits.
- Creating a new profile while another profile is selected creates a second profile from the current library selections and does not overwrite the existing selected profile.

### Profile Editing And Rename
- When a profile is selected, editing Source Port / IWAD / Mod selections changes that selected profile immediately and persists after each change.
- Auto-save includes transitions into invalid state.
- There is no Save, Save As, dirty state, unsaved-changes prompt, or separate profile-name field.
- Each profile row exposes `Launch` and `Delete` actions in that order.
- Profile rename is available only through the selected-profile header in the right pane.
- The selected-profile header `Rename` action is visible but disabled when no profile is selected.
- Activating `Rename` opens inline rename mode in the selected-profile header by replacing the selected profile name text with a rename input.
- Rename commit behavior:
  - `Enter` saves if valid.
  - Clicking outside the rename input cancels rename and restores the prior saved name.
  - `Escape` cancels and restores the prior saved name.
- The outside click that cancels rename is consumed and does not also activate the clicked row, button, or other control.
- Rename validity rules:
  - name is required
  - name cannot be empty or whitespace-only
  - name must be case-insensitively unique across all profiles
- Invalid rename attempts:
  - do not change the saved name
  - keep rename mode open for that row
  - show a visible validation message in the message/warning area

### Delete Behavior
- Each profile row exposes delete access.
- Delete requires explicit confirmation in the message area before removal.
- Deleting an unselected profile removes only that saved profile.
- Deleting the selected profile:
  - removes that saved profile
  - sets `SelectedProfileId` to `null`
  - clears current Source Port / IWAD / Mod selections
- Deleting a profile does not automatically select a neighboring profile.

### Validity And Launch
- A valid profile requires:
  - exactly one source port
  - exactly one IWAD
  - zero or more mods
  - preserved mod order
  - every referenced path must exist on disk
  - every referenced path must still exist in the matching shared library collection
- Removing a library item that a profile references is allowed.
- Profiles affected by removed or missing library items remain saved and listed.
- Invalid profiles:
  - show an explicit invalid-state indication in the shared row status slot
  - keep the invalid reason text under the profile name
  - remain selectable
  - remain renameable
  - remain editable through the shared library
  - remain auto-saveable
  - are not launchable from their row action
- Valid profiles:
  - show an explicit `VALID` badge in the same shared row status slot used by invalid profiles
  - keep their row `Launch` action enabled
- Launch is available only through saved profile rows.
- No selected profile means no currently selected launchable profile, even if current detached library selections are otherwise launch-valid.

### Mod Ordering Context
- Mod row ordering is derived UI state and does not rewrite the shared library collection order during selection toggles.
- When no profile is selected:
  - Mod rows default to alphabetical filename order
  - selected Mods temporarily move to the top in detached selected sequence order
  - remaining unselected Mods stay in alphabetical filename order
  - detached selected-mod ordering is not restored on restart
- When a profile is selected:
  - selected Mods appear first in that profile's `SelectedModPaths` order
  - remaining unselected Mods appear afterward in alphabetical filename order
  - changing Mod selection persists only that selected profile's `SelectedModPaths`

### Persistence And Backward Compatibility
- `LaunchInputsConfig` adds:
  - `Profiles`
  - `SelectedProfileId`
- `ProfileConfig` contains:
  - `Id`
  - `Name`
  - `SourcePortPath`
  - `IwadPath`
  - `SelectedModPaths`
- Canonical Feature 008 saves persist:
  - shared library collections
  - saved profiles
  - nullable `SelectedProfileId`
- Canonical Feature 008 saves do not rely on top-level selected source-port / IWAD / mod fields.
- Backward compatibility requirements:
  - existing Source Ports, IWADs, and Mods are preserved from old configs
  - old selected source-port / IWAD / mod fields do not auto-create a saved profile
  - old configs with no profiles load with zero profiles
  - startup with no valid selected profile begins with no selected profile, cleared library selections, and Launch disabled
- Detached no-profile library selections created during runtime are not restored on restart.
- On startup sanitation:
  - sanitize shared library collections as existing features require
  - preserve broken profiles
  - recompute profile validity from sanitized library membership and file existence

## Acceptance Criteria
### Create profile from current selections
Given any current live library selection state
When `New Profile` is activated
Then a saved profile is created immediately from the current selections.
And the profile name uses the first available `Profile N`.
And the new profile receives a new stable `Id`.
And the new profile becomes the selected profile.
And if the file library pane was collapsed, it becomes expanded.

### Select and unselect profile
Given a saved profile exists
When its row is selected
Then current Source Port / IWAD / Mod selections hydrate from that profile.
And when the selected row is selected again
Then `SelectedProfileId` becomes `null`.
And current Source Port / IWAD / Mod selections are cleared.
And no profile remains selected for launch.

### Launch profile from row action
Given a valid saved profile row exists and a different profile or no profile is currently selected
When `Launch` is activated for that row
Then that row's profile becomes the selected profile.
And current Source Port / IWAD / Mod selections hydrate from that profile.
And launch executes for that profile's saved Source Port, IWAD, and ordered Mods.

### Edit selected profile through library
Given a saved profile is selected
When Source Port / IWAD / Mod selections change in the shared library
Then the selected profile persists those changes immediately.
And those changes may place the profile into or out of invalid state.

### Explicit rename action
Given a selected profile exists
When `Rename` is activated in the selected-profile header
Then inline rename mode opens in the selected-profile header for that selected profile.
And `Enter` saves a valid unique non-empty name.
And outside click or `Escape` restores the previous saved name.
And the outside click does not also activate another control.

### New profile placement
Given the workspace is rendered
When the left profile-management header is displayed
Then `New Profile` appears in that header.
And `New Profile` is right-aligned and aligned with the `Profiles` label.
And the file-library `Expand` / `Collapse` action appears to the right of `New Profile`.
And the right selected-profile header does not render a second `New Profile` action.

### Rename placement
Given the workspace is rendered
When the right-pane selected-profile header is displayed
Then `Rename` appears in that header.
And `Rename` is right-aligned and aligned with the selected profile name.
And profile rows do not render a row-level `Rename` action or row-level rename input.

### Rename disabled with no selection
Given no profile is selected
When the selected-profile header is displayed
Then the `Rename` action remains visible.
And the `Rename` action is disabled.

### Rename validation
Given a profile is in rename mode
When the entered name is empty, whitespace-only, or duplicates another profile name case-insensitively
Then the saved name remains unchanged.
And rename mode stays open.
And a visible validation message is shown in the message area.

### Pinned workspace panes
Given the workspace is rendered
When the file library content exceeds available vertical space
Then the right pane scrolls independently.
And the left profile pane remains pinned.
And when the profile list exceeds available height, the profile list scrolls within the left pane.

### Delete selected profile
Given a selected profile exists
When delete is confirmed for that profile
Then the profile is removed from saved profiles.
And `SelectedProfileId` becomes `null`.
And current Source Port / IWAD / Mod selections are cleared.
And no neighboring profile is auto-selected.

### Invalid profile remains repairable
Given a saved profile references a library item that is removed or a file path that no longer exists
When validity is recomputed
Then the profile remains saved and listed.
And it is marked invalid with an explicit reason.
And its row `Launch` action is disabled.
And changing library selections while it is selected can repair it and restore launchability.

### Valid profile shows explicit valid badge
Given a saved profile has exactly one source port, exactly one IWAD, and all referenced paths still exist in the matching shared library collections
When validity is recomputed
Then the profile row shows an explicit `VALID` badge in the same status slot used by invalid profiles.
And the profile row does not render a separate inline valid text line under the profile name.
And its row `Launch` action remains enabled.

### Mod ordering by profile context
Given at least three Mod rows and one or more profiles
When no profile is selected
Then Mod rows start in alphabetical filename order.
And when Mods are selected, selected Mods move to the top in detached selected sequence order without changing restart state.
And when a saved profile is selected, Mod rows are ordered by that profile's selected sequence first and alphabetical remainder second.

### Legacy startup without profiles
Given an old config containing shared library lists and old top-level selected fields but no profiles
When the app starts under Feature 008
Then the shared library lists are preserved.
And zero profiles are loaded.
And no profile is selected.
And current library selections start cleared.
And no profile is launchable until a saved profile exists.
