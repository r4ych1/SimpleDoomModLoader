# Feature 008 - Selection-Based Profile Workspace

## Goal
Introduce saved launch profiles as the primary launch model while keeping Source Ports, IWADs, and Mods as shared library collections.

Feature 014 later becomes authoritative for the single shared workspace layout and view swapping. Feature 008 remains authoritative for profile behavior, shared-library editing rules, validity, launchability, and persistence unless a later feature explicitly replaces them.

## In Scope
- Saved profile list with single-select toggle behavior.
- Profile creation from empty launch inputs.
- Row-scoped launch actions for saved profiles.
- Selected-profile header content in File Library view.
- Immediate profile auto-save when editing a selected profile through shared-library selections.
- Profile rename, delete, validity, and shared-library disabled-state behavior.
- Persisted `Profiles` and `SelectedProfileId`.

## Out Of Scope
- Save / Save As workflow.
- Detached draft persistence across restart.
- Profile notes, tags, extra launch args, or last-played metadata.
- Automatic selection of a neighboring profile after delete.

## Definitions
- Shared library:
  - The persisted Source Port, IWAD, and Mod collections shown in File Library view.
- Profile:
  - A saved record with stable `Id`, unique display `Name`, one source-port path, one IWAD path, and ordered selected mod paths.
- Selected profile:
  - The nullable saved profile identified by `SelectedProfileId`.
- Invalid profile:
  - A saved profile whose current references cannot produce valid launch arguments.

## Rules
### Workspace behavior
- The app uses one single shared workspace whose layout is defined by Feature 014.
- Profiles view shows:
  - saved profile rows
  - row-scoped command preview text
  - row-scoped invalid-reason text when visible
  - row launch, rename, and delete actions
- File Library view shows:
  - the selected-profile header
  - the shared Source Port, IWAD, and Mod collections
- When no profile is selected, Source Port, IWAD, and Mod rows remain visible but are not selectable.
- Disabled shared-library rows render a disabled visual treatment while keeping row delete actions available.

### Profile list and selection
- Saved profile row ordering is the current profile-list display order.
- Feature 010 becomes authoritative for how profile row ordering changes through drag reorder.
- Profile rows are single-select toggle rows:
  - clicking an unselected row selects it
  - clicking a different selected row moves selection
  - clicking the selected row unselects it
- Selecting a profile hydrates current Source Port / IWAD / Mod selections from that profile.
- Unselecting a profile:
  - sets `SelectedProfileId` to `null`
  - clears current Source Port / IWAD / Mod selections
  - leaves no launchable selected profile
- Double-clicking a profile row is authoritative in Feature 014.

### Profile-row presentation
- Profile rows use one shared right-side status-badge slot:
  - invalid rows show `INVALID`
  - valid rows show `VALID`
- Profile names wrap within the row body and are capped at two rendered lines.
- Each profile row renders its command preview in the non-interactive text area under the profile name:
  - preview uses saved profile inputs, not live detached selections
  - preview token order is source-port filename, `-iwad`, IWAD filename, `-file`, ordered mod filenames
  - rows with no previewable tokens omit the preview line
- Profile-row command preview is visible only in Profiles view and only when the overall window width is greater than `768 px`.
- Invalid profile inline reason text is visible only in Profiles view.

### Profile creation
- The profile-creation action is always enabled.
- Creating a profile immediately creates a new saved profile with empty launch inputs:
  - no selected source port
  - no selected IWAD
  - no selected mods
- New profile name uses the first available `Profile N` positive integer sequence, filling gaps from deleted profiles.
- New profile creation:
  - generates a new stable `Id`
  - persists immediately
  - selects the new profile
- After the new profile becomes selected, current Source Port / IWAD / Mod selections hydrate from that profile and become cleared.
- Feature 014 is authoritative for which workspace view becomes visible after profile creation.

### Profile editing and rename
- When a profile is selected, editing Source Port / IWAD / Mod selections changes that selected profile immediately and persists after each change.
- Source Port, IWAD, and Mod row selection rules inherited from earlier features apply only while a profile is selected.
- Auto-save includes transitions into invalid state.
- There is no Save, Save As, dirty state, or unsaved-changes prompt.
- Each profile row exposes launch, rename, and delete actions in that order.
- Rename validity rules:
  - name is required
  - name cannot be empty or whitespace-only
  - name must be case-insensitively unique across all profiles
- Invalid rename attempts:
  - do not change the saved name
  - keep rename mode open
  - show a Feature 013 passive warning toast
- The selected-profile header `Edit` action starts rename inside the File Library view header.

### Delete behavior
- Each profile row exposes delete access.
- The selected-profile header exposes delete access only while a profile is selected.
- Activating selected-profile header delete requests delete confirmation through the Feature 013 confirmation toast.
- Deleting an unselected profile removes only that saved profile.
- Deleting the selected profile:
  - removes that saved profile
  - sets `SelectedProfileId` to `null`
  - clears current Source Port / IWAD / Mod selections
- Deleting a profile does not automatically select a neighboring profile.

### Validity and launch
- A valid profile requires:
  - exactly one source port
  - exactly one IWAD
  - zero or more mods
  - preserved mod order
  - source-port path exists on disk and remains in the Source Port library
  - IWAD path exists on disk and remains in the IWAD library
- Removing a referenced Mod from the shared library does not invalidate the profile.
- Missing referenced Mod files on disk do not invalidate the profile.
- Invalid profiles:
  - remain saved and listed
  - remain selectable, renameable, editable, auto-saveable, and row-launch-clickable
  - do not actually launch
  - show their current invalid reason in a Feature 013 passive warning toast when row launch is activated
- Valid profiles show `Selected profile is ready to launch.` in the selected-profile status text when selected.
- Launch is available only through saved profile rows.

### Selected-profile header
- File Library view shows the selected-profile header at the top of that view.
- The header shows:
  - selected profile name, or header rename input while header rename is active
  - `Edit` and `Delete` actions only while a profile is selected
  - status text below the name row
  - saved command preview below the status text when one or more previewable saved tokens exist
- The selected-profile status text uses the shared amber invalid color only while the selected profile is invalid.
- The selected-profile command preview uses saved profile inputs rather than current live selections.

### Mod ordering context
- When no profile is selected:
  - Mod rows default to alphabetical filename order
  - attempted selection input does not change ordering or selection state
- When a profile is selected:
  - selected mods appear first in that profile's saved order
  - remaining unselected mods appear afterward in alphabetical filename order

### Persistence and backward compatibility
- `LaunchInputsConfig` adds:
  - `Profiles`
  - `SelectedProfileId`
- `ProfileConfig` contains:
  - `Id`
  - `Name`
  - `SourcePortPath`
  - `IwadPath`
  - `SelectedModPaths`
- Canonical saves persist shared library collections, saved profiles, and nullable `SelectedProfileId`.
- Old selected source-port / IWAD / mod fields do not auto-create a profile.
- Startup with no valid selected profile begins with no selected profile, cleared library selections, and Launch disabled.

## Acceptance Criteria
### Create profile from current selections
Given any current live library selection state
When the profile-creation action is activated
Then a saved profile is created immediately with empty launch inputs.
And the profile name uses the first available `Profile N`.
And the new profile receives a new stable `Id`.
And the new profile becomes the selected profile.
And current Source Port / IWAD / Mod selections become cleared after hydration from that new profile.

### Shared library is non-selectable without a profile
Given no profile is selected and shared Source Port, IWAD, and Mod rows exist
When selection input is applied to any of those rows
Then current Source Port / IWAD / Mod selections remain unchanged.
And no profile is auto-created or auto-selected.

### Explicit rename action
Given a saved profile exists
When the row rename action is activated on that profile row
Then inline rename mode opens inside that same profile row.
And `Enter` saves a valid unique non-empty name.
And outside click or `Escape` restores the previous saved name.

### Selected-profile header edit uses header rename
Given File Library view is visible and a profile is selected
When the selected-profile header `Edit` action is activated
Then rename mode opens in that header instead of in a profile row.

### Invalid profile launch shows warning toast without launching
Given an invalid saved profile row exists
When the row launch action is activated for that row
Then that row's profile becomes the selected profile.
And launch does not execute for that profile.
And a Feature 013 passive warning toast shows that profile's current invalid reason.
