# Feature 008 - Selection-Based Profile Workspace

## Goal
Introduce saved launch profiles as the primary launch model by converting the current screen into a two-pane workspace: saved profiles on the left and the shared Source Port / IWAD / Mod library on the right.

Feature 009 later adds pane-level collapse for the right-side file library. Feature 008 remains authoritative for the expanded file-library workspace behavior.
Feature 010 later adds manual drag reordering for saved profile rows. Feature 008 remains authoritative for profile selection, launch, rename, delete, validity, and auto-save behavior outside explicit ordering rules introduced there.
Feature 013 later becomes authoritative for shared toast mechanics, placement, and timing. Feature 008 remains authoritative for which profile workflows invoke those toasts.

## In Scope
- Saved profile list with single-select toggle behavior.
- Profile creation from empty launch inputs using generated default names from the left-pane profile-management header.
- Row-scoped launch actions for saved profiles.
- A fixed selected-profile header in the right pane.
- Profile delete with confirmation.
- Immediate profile auto-save when editing a selected profile through library selection changes.
- Persisted `Profiles` and `SelectedProfileId`.
- Profile validity computation based on required Source Port and IWAD launch inputs plus their library membership and file existence, while Mods remain non-blocking saved references.
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
- Invalid profile:
  - A saved profile whose current references cannot produce valid launch arguments.

## Rules
### Workspace Layout
- The window becomes a two-pane workspace:
  - Left pane: profile management.
  - Right pane: the existing shared Source Port / IWAD / Mod library.
- While the file library pane is expanded, the left profile pane uses a fixed width of `380 px`.
- The left profile pane remains pinned while the right file-library pane scrolls independently.
- The left profile list provides its own internal scrolling when saved profiles exceed available vertical space.
- When either pane's content exceeds available vertical space, the left profile list and the right file-library pane remain scrollable through mouse wheel, touchpad, keyboard, and equivalent platform scroll input while visible scrollbar chrome is not rendered in either pane.
- While the file library pane is expanded, the right pane shows a fixed selected-profile header above the independently scrolling shared library content.
- While the file library pane is expanded, the right file-library pane shows a subtle bottom-centered downward chevron affordance only when additional scrollable file-library content exists below the current viewport beneath the fixed selected-profile header.
- The file-library scroll affordance remains visible while additional file-library content still exists below the current viewport.
- The file-library scroll affordance fades out only when the user reaches the bottom of that scrollable library content or when the content fully fits within the viewport.
- The file-library scroll affordance is not shown when the file-library pane is collapsed or when the expanded scrollable library content fully fits within the viewport.
- The left profile list shows the same subtle bottom-centered downward chevron affordance only when additional profile rows exist below the current viewport.
- The profile-list scroll affordance remains visible while additional profile-list content still exists below the current viewport.
- The profile-list scroll affordance fades out only when the user reaches the bottom of the profile list or when the full profile list fits within the viewport.
- The profile-list scroll affordance is shown in both expanded two-pane mode and collapsed profile-only mode.
- The left pane begins with a profile-management header row that contains:
  - the `Profiles` label on the left
  - the profile-creation action on the right
  - the file-library collapse / expand action to the right of the profile-creation action
- The profile-creation action and the file-library collapse / expand action are right-aligned within the profile-management header row and aligned with the `Profiles` label.
- Feature 011 later becomes authoritative for the visible icon presentation of those two actions while preserving their placement and behavior.
- The right pane begins with a fixed selected-profile header area that contains:
  - the selected profile name on the left
  - selected-profile `Edit` and `Delete` actions on the right, inline with the name row
  - selected-profile status text below the name row
  - selected-profile command preview text below the status text when the selected profile has one or more previewable saved launch tokens
- When no profile is selected, the selected-profile header actions are hidden.
- The Source Port, IWAD, and Mod library sections render in a scrollable region below that fixed selected-profile header.
- When no profile is selected, Source Port, IWAD, and Mod rows remain visible but are not selectable.
- When no profile is selected, Source Port, IWAD, and Mod rows render a disabled visual treatment that suppresses selection affordance while leaving row delete actions available.
- Rename validation, invalid-profile row launch feedback, and selected-profile delete confirmation use Feature 013 toast behavior.
- While the selected profile is invalid, the selected-profile status text in the right-pane header uses the same amber invalid text color used by left-pane inline invalid text.
- While the selected profile is valid or no profile is selected, the selected-profile status text in the right-pane header keeps the muted helper/status text color.
- While the file library pane is expanded, the Source Port, IWAD, and Mod drop zones inside that pane use the shared visible drop-zone affordance defined by Feature 002.
- Each expanded file-library drop zone provides a section-specific accessible label that describes the drag-and-drop affordance, for example `Source Port drop zone. Drag and drop files here.`

### Profile List And Selection
- Left pane shows:
  - saved profile rows
  - row-scoped command preview text
  - explicit row-level validity messaging
  - launch access
  - delete access
- Saved profile row ordering is the current profile-list display order.
- Feature 010 becomes authoritative for how profile row ordering is changed by drag reordering and persisted afterward.
- Profile rows use one shared right-side status-badge slot:
  - invalid rows show an `INVALID` badge in that slot and keep invalid-reason text available for the inline row invalid-reason area when that text is visible
  - valid rows show a `VALID` badge in that same slot
  - valid rows do not render a separate inline valid text line under the profile name
- Profile names in left-pane rows wrap within the row body and are capped at two rendered lines.
- Each profile row renders its command preview in the non-interactive text area under the profile name:
  - preview text uses wrapped display
  - preview text uses the profile's saved launch inputs rather than current detached selections
  - preview text uses filename-only tokens in this order: source-port filename, `-iwad`, IWAD filename, `-file`, ordered mod filenames
  - rows with no previewable tokens omit the preview line instead of showing an empty placeholder
  - invalid-reason text, when present, remains distinct from the preview text
- Inline profile-row command preview is responsive:
  - when the file library pane is collapsed and the overall window width is greater than `768 px`, row preview text is visible
  - when the file library pane is expanded, row preview text is hidden for all profile rows regardless of width
  - when the overall window width is less than or equal to `768 px`, row preview text is hidden for all profile rows even while the file library pane is collapsed
- Inline invalid-reason text is also responsive:
  - when the file library pane is expanded, inline invalid-reason text is hidden for all profile rows regardless of width
  - when the file library pane is collapsed, invalid rows show their inline invalid-reason text under the profile name
- Profile rows are single-select toggle rows:
  - Clicking an unselected row selects it.
  - Clicking a different selected row moves selection to that profile.
  - Clicking the selected row unselects it.
- Selecting a profile hydrates current Source Port / IWAD / Mod selections from that profile.
- Unselecting a profile:
  - sets `SelectedProfileId` to `null`
  - clears current Source Port / IWAD / Mod selections
  - leaves no launchable selected profile
  - leaves the shared file library visible but non-selectable until a profile is selected again
- While the file library pane is collapsed, double-clicking a profile row is a profile-open shortcut:
  - the clicked profile ends selected
  - current Source Port / IWAD / Mod selections hydrate from that profile
  - the file library pane expands immediately using Feature 009 behavior
  - the second click does not toggle the row back off
- While the file library pane is expanded, double-clicking a profile row is the inverse pane shortcut:
  - the clicked profile ends selected
  - current Source Port / IWAD / Mod selections hydrate from that profile
  - the file library pane collapses immediately using Feature 009 behavior
  - the second click does not toggle the row back off
- Profile-row double-click does not launch the profile, does not start rename, and does not change Feature 010 drag-reorder rules.

### Profile Launch
- Each profile row exposes a launch action immediately to the left of delete.
- Row launch is available only while the row is in normal display mode.
- Row launch remains clickable while the row is in normal display mode, even when that row's profile is invalid.
- Activating the launch action for a row:
  - selects that profile
  - hydrates current Source Port / IWAD / Mod selections from that profile
  - launches that profile through the existing launcher flow when that profile is valid
  - does not invoke launch when that profile is invalid
  - shows a Feature 013 passive warning toast with that profile's current invalid reason when that profile is invalid
- Row launch does not require the profile to already be selected.
- Invalid profiles remain listed, selectable, and row-launch-clickable while still blocked from actual launch execution.
- Valid profiles show an explicit `VALID` badge in the shared row status slot.

### Profile Creation
- The profile-creation action is always enabled.
- Activating the profile-creation action immediately creates a new saved profile with empty launch inputs:
  - no selected source port
  - no selected IWAD
  - no selected Mods
- New profile name uses the first available `Profile N` positive integer sequence, filling gaps from deleted profiles.
- New profile creation:
  - generates a new stable `Id`
  - persists immediately
  - selects the new profile
- After the new empty profile becomes selected, current Source Port / IWAD / Mod selections hydrate from that profile and become cleared.
- Creating a new profile while the file library pane is collapsed expands the file library pane immediately and persists the expanded pane state.
- A newly created profile may start invalid and remain repairable through later shared-library edits.
- Creating a new profile while another profile is selected creates a second empty profile and does not overwrite the existing selected profile.

### Profile Editing And Rename
- When a profile is selected, editing Source Port / IWAD / Mod selections changes that selected profile immediately and persists after each change.
- Source Port, IWAD, and Mod row selection rules inherited from earlier features apply only while a profile is selected.
- Auto-save includes transitions into invalid state.
- There is no Save, Save As, dirty state, unsaved-changes prompt, or separate profile-name field.
- Each profile row exposes launch and delete actions in that order.
- Each profile row exposes launch, rename, and delete actions in that order.
- Feature 011 later becomes authoritative for the visible icon presentation of those row actions while preserving their order and behavior.
- Profile rename is available only through the profile row rename action.
- The selected-profile header `Edit` action reuses that same rename flow:
  - it is available only while a profile is selected
  - activating it starts inline rename mode in the selected profile row inside the left pane
  - the right-pane selected-profile header remains display-only and does not render a rename input
- Activating a row rename action:
  - selects that profile
  - hydrates current Source Port / IWAD / Mod selections from that profile
  - opens inline rename mode inside that same profile row by replacing the row name text with a rename input
- Rename commit behavior:
  - `Enter` saves if valid.
  - Clicking outside the rename input cancels rename and restores the prior saved name.
  - `Escape` cancels and restores the prior saved name.
- The outside click that cancels rename is consumed and does not also activate the clicked row, button, or other control.
- While a profile row is in rename mode:
  - its normal row action buttons are not rendered
  - the row does not start drag reorder from the rename editor or rename-mode body
- Rename validity rules:
  - name is required
  - name cannot be empty or whitespace-only
  - name must be case-insensitively unique across all profiles
- Invalid rename attempts:
  - do not change the saved name
  - keep rename mode open for that row
  - show a Feature 013 passive warning toast

### Delete Behavior
- Each profile row exposes delete access.
- The selected-profile header exposes delete access only while a profile is selected.
- Activating selected-profile header delete requests delete confirmation for that selected profile through the Feature 013 confirmation toast.
- Delete requires explicit confirmation inside that Feature 013 confirmation toast before removal.
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
  - source-port path must exist on disk
  - source-port path must still exist in the Source Port shared library collection
  - IWAD path must exist on disk
  - IWAD path must still exist in the IWAD shared library collection
- Removing a library item that a profile references is allowed.
- Removing a referenced Source Port or IWAD from its shared library invalidates the profile.
- Removing a referenced Mod from the shared library does not invalidate the profile.
- Profiles affected by removed or missing library items remain saved and listed.
- Missing referenced Mod files on disk do not invalidate the profile.
- Saved mod references remain preserved for preview text and launch argument construction even when those Mod paths are stale.
- Invalid profiles:
  - show an explicit invalid-state indication in the shared row status slot
  - keep the invalid reason text under the profile name
  - show the full invalid reason in the selected-profile status text when selected
  - remain selectable
  - remain renameable
  - remain editable through the shared library
  - remain auto-saveable
  - keep their row launch action clickable in normal display mode
  - do not launch when their row launch action is activated
  - show their current invalid reason in a Feature 013 passive warning toast when their row launch action is activated
- Valid profiles:
  - show an explicit `VALID` badge in the same shared row status slot used by invalid profiles
  - show `Selected profile is ready to launch.` in the selected-profile status text when selected
  - keep their row launch action enabled
- The selected-profile header command preview:
  - uses the selected profile's saved launch inputs rather than current hydrated live selections
  - uses filename-only tokens in this order: source-port filename, `-iwad`, IWAD filename, `-file`, ordered mod filenames
  - wraps within the selected-profile header card
  - remains visible for invalid selected profiles when one or more previewable saved tokens exist
  - is omitted when no profile is selected or when the selected profile has no previewable saved tokens
- Launch is available only through saved profile rows.
- No selected profile means no currently selected launchable profile and no selectable shared-library launch inputs.

### Mod Ordering Context
- Mod row ordering is derived UI state and does not rewrite the shared library collection order during selection toggles.
- When no profile is selected:
  - Mod rows default to alphabetical filename order
  - attempted selection input does not change selection state
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
- On startup sanitation:
  - sanitize shared library collections as existing features require
  - preserve broken profiles
  - recompute profile validity from sanitized Source Port and IWAD library membership and file existence while leaving saved Mod references non-blocking

## Acceptance Criteria
### Create profile from current selections
Given any current live library selection state
When the profile-creation action is activated
Then a saved profile is created immediately with empty launch inputs.
And the profile name uses the first available `Profile N`.
And the new profile receives a new stable `Id`.
And the new profile becomes the selected profile.
And current Source Port / IWAD / Mod selections become cleared after hydration from that new profile.
And if the file library pane was collapsed, it becomes expanded.

### Select and unselect profile
Given a saved profile exists
When its row is selected
Then current Source Port / IWAD / Mod selections hydrate from that profile.
And when the selected row is selected again
Then `SelectedProfileId` becomes `null`.
And current Source Port / IWAD / Mod selections are cleared.
And no profile remains selected for launch.

### Double-click opens collapsed file library for a profile
Given the file library pane is collapsed and a saved profile row exists
When that profile row is double-clicked
Then that profile ends selected.
And current Source Port / IWAD / Mod selections hydrate from that profile.
And the file library pane expands using Feature 009 behavior.
And the double-click does not launch the profile.

### Launch profile from row action
Given a valid saved profile row exists and a different profile or no profile is currently selected
When the row launch action is activated for that row
Then that row's profile becomes the selected profile.
And current Source Port / IWAD / Mod selections hydrate from that profile.
And launch executes for that profile's saved Source Port, IWAD, and ordered Mods.

### Edit selected profile through library
Given a saved profile is selected
When Source Port / IWAD / Mod selections change in the shared library
Then the selected profile persists those changes immediately.
And those changes may place the profile into or out of invalid state.

### Shared library is non-selectable without a profile
Given no profile is selected and shared Source Port, IWAD, and Mod rows exist
When selection input is applied to any of those rows
Then current Source Port / IWAD / Mod selections remain unchanged.
And no profile is auto-created or auto-selected.
And the visible row ordering remains unchanged.

### Shared library rows show disabled treatment without a profile
Given no profile is selected and shared Source Port, IWAD, and Mod rows exist
When the shared library is rendered
Then those rows remain visible.
And those rows render a disabled visual treatment.
And their row delete actions remain available.

### Explicit rename action
Given a selected profile exists
When the row rename action is activated on that profile row
Then inline rename mode opens inside that same profile row for that selected profile.
And `Enter` saves a valid unique non-empty name.
And outside click or `Escape` restores the previous saved name.
And the outside click does not also activate another control.

### New profile placement
Given the workspace is rendered
When the left profile-management header is displayed
Then the profile-creation action appears in that header.
And the profile-creation action is right-aligned and aligned with the `Profiles` label.
And the file-library collapse / expand action appears to the right of the profile-creation action.
And the right selected-profile header does not render a second profile-creation action.

### Rename placement
Given the workspace is rendered
When saved profile rows are displayed
Then each profile row renders a row-level rename action between the row launch and delete actions.
And the right-pane selected-profile header renders an `Edit` action only while a profile is selected.
And the right-pane selected-profile header does not render a rename input.

### Selected-profile header action placement
Given the file library pane is expanded and a profile is selected
When the selected-profile header is rendered
Then the selected profile name appears on the left side of the name row.
And `Edit` and `Delete` actions appear inline on the right side of that same row.
And when no profile is selected
Then those selected-profile header actions are hidden.

### Rename validation
Given a profile is in rename mode
When the entered name is empty, whitespace-only, or duplicates another profile name case-insensitively
Then the saved name remains unchanged.
And rename mode stays open.
And a Feature 013 passive warning toast is shown.

### Pinned workspace panes
Given the workspace is rendered
When the file library content exceeds available vertical space
Then the right pane scrolls independently.
And the left profile pane remains pinned.
And the selected-profile header remains fixed at the top of the right pane.
And the Source Port / IWAD / Mod library content scrolls beneath that fixed header.
And when the profile list exceeds available height, the profile list scrolls within the left pane.

### Profile list scrolls without visible scrollbar chrome
Given the workspace is rendered and saved profiles exceed the available left-pane height
When the user scrolls the profile list with mouse wheel, touchpad, keyboard, or equivalent platform scroll input
Then the left profile list scrolls within the left pane.
And visible scrollbar chrome is not rendered for that profile list.

### File-library pane scrolls without visible scrollbar chrome
Given the workspace is rendered and file-library content exceeds the available right-pane height
When the user scrolls the file-library pane with mouse wheel, touchpad, keyboard, or equivalent platform scroll input
Then the right file-library pane scrolls independently.
And visible scrollbar chrome is not rendered for that file-library pane.

### File-library pane shows scroll affordance until bottom when more content remains below
Given the file library pane is expanded and its content exceeds the available right-pane height
When the scrollable file-library content below the fixed selected-profile header is rendered
Then a subtle bottom-centered downward chevron affordance is visible for that pane.
And when the user scrolls down while additional content still exists below the viewport
Then the affordance remains visible.
And when the user reaches the bottom of that scrollable content
Then the affordance fades out.
And when the user scrolls back up while additional content still exists below the viewport
Then the affordance becomes visible again.
And when the expanded file-library content fully fits within the viewport or the file-library pane is collapsed
Then the affordance is not shown.

### Profile list shows scroll affordance until bottom when more content remains below
Given the profile list exceeds the available left-pane height
When the profile list is rendered in expanded two-pane mode or collapsed profile-only mode
Then a subtle bottom-centered downward chevron affordance is visible for that pane.
And when the user scrolls down while additional profile rows still exist below the viewport
Then the affordance remains visible.
And when the user reaches the bottom of the profile list
Then the affordance fades out.
And when the user scrolls back up while additional profile rows still exist below the viewport
Then the affordance becomes visible again.
And when the full profile list fits within the viewport
Then the affordance is not shown.

### Shared-library drop zones stay visibly interactive
Given the file library pane is expanded
When the Source Port, IWAD, and Mod sections are rendered
Then each section shows a visibly interactive drop zone with visible instructional text before any drag begins.
And each zone supports drag-over highlight and a section-specific accessible label.

### Delete selected profile
Given a selected profile exists
When delete is confirmed for that profile
Then the profile is removed from saved profiles.
And `SelectedProfileId` becomes `null`.
And current Source Port / IWAD / Mod selections are cleared.
And no neighboring profile is auto-selected.

### Selected-profile header delete reuses existing confirmation flow
Given a selected profile exists
When the selected-profile header delete action is activated
Then delete confirmation is requested in the Feature 013 confirmation toast for that selected profile.
And no second delete workflow is introduced in the right pane.

### Invalid profile remains repairable
Given a saved profile references a Source Port or IWAD library item that is removed or a required Source Port or IWAD file path that no longer exists
When validity is recomputed
Then the profile remains saved and listed.
And it is marked invalid with an explicit reason.
And its row launch action remains clickable in normal display mode.
And changing library selections while it is selected can repair it and restore launchability.

### Invalid profile launch shows warning toast without launching
Given an invalid saved profile row exists
When the row launch action is activated for that row
Then that row's profile becomes the selected profile.
And current Source Port / IWAD / Mod selections hydrate from that profile.
And launch does not execute for that profile.
And a Feature 013 passive warning toast shows that profile's current invalid reason.

### Removed or missing mod does not block launch
Given a saved profile has valid saved Source Port and IWAD references and one or more saved Mod references
When a saved Mod is removed from the shared Mod library or its file path no longer exists on disk
Then the profile remains saved and listed.
And the profile still recomputes as valid.
And its row launch action remains enabled.
And its saved Mod references remain preserved for preview text and launch argument construction.

### Valid profile shows explicit valid badge
Given a saved profile has exactly one source port, exactly one IWAD, and its saved Source Port and IWAD paths still exist in their matching shared library collections
When validity is recomputed
Then the profile row shows an explicit `VALID` badge in the same status slot used by invalid profiles.
And the profile row does not render a separate inline valid text line under the profile name.
And its row launch action remains enabled.

### Profile row preview placement and wrapping
Given a saved profile row has one or more previewable launch tokens
When the file library pane is collapsed and the profile list is rendered at a window width greater than `768 px`
Then that row shows its filename-only command preview directly under the profile name.
And the preview text wraps within the profile row body.
And the preview text does not render in a separate footer bar.

### Profile row preview hides while file library is expanded
Given one or more saved profile rows have previewable launch tokens
When the file library pane is expanded
Then inline command preview is hidden for all profile rows regardless of width.
And profile name, validity messaging, badges, and row actions remain visible.

### Invalid reason text hides while file library is expanded
Given an invalid saved profile row exists
When the file library pane is expanded
Then the row keeps its `INVALID` badge.
And inline invalid-reason text is hidden for that row.
And the selected-profile header in the right pane still shows the selected profile's full status text.

### Selected-profile header shows invalid amber status and saved command preview
Given a saved profile is selected in the expanded file library
When the selected-profile header is rendered
Then the header shows the selected profile name.
And the status text uses the shared amber invalid text color only when that selected profile is invalid.
And the header shows a wrapped filename-only command preview from that selected profile's saved launch inputs when one or more previewable saved tokens exist.
And the header omits the preview line when no selected profile exists or no previewable saved tokens exist.

### Selected-profile header stays fixed while library content scrolls
Given the file library pane is expanded and its library content exceeds the available right-pane height
When the user scrolls the Source Port / IWAD / Mod library content
Then the selected-profile header remains fixed at the top of the right pane.
And the scroll position change affects only the library content below that header.

### Double-click collapses expanded file library for a profile
Given the file library pane is expanded and a saved profile row exists
When that profile row is double-clicked
Then that profile ends selected.
And current Source Port / IWAD / Mod selections hydrate from that profile.
And the file library pane collapses using Feature 009 behavior.
And the double-click does not unselect that profile.

### Profile row preview hides at minimum width while collapsed
Given one or more saved profile rows have previewable launch tokens
When the file library pane is collapsed and the overall window width is less than or equal to `768 px`
Then inline command preview is hidden for all profile rows.
And profile name, validity messaging, badges, and row actions remain visible.

### Profile names wrap in the expanded profile pane
Given a saved profile row has a long name
When the file library pane is expanded and the profile list is rendered
Then the left profile pane uses a fixed width of `380 px`.
And the profile name wraps within the row body.
And the rendered profile name is capped at two lines.

### Mod ordering by profile context
Given at least three Mod rows and one or more profiles
When no profile is selected
Then Mod rows start in alphabetical filename order.
And attempted Mod selection does not change that ordering.
And when a saved profile is selected, Mod rows are ordered by that profile's selected sequence first and alphabetical remainder second.

### Legacy startup without profiles
Given an old config containing shared library lists and old top-level selected fields but no profiles
When the app starts under Feature 008
Then the shared library lists are preserved.
And zero profiles are loaded.
And no profile is selected.
And current library selections start cleared.
And no profile is launchable until a saved profile exists.
