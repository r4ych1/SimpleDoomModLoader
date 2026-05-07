# Feature 005 - Fixed Header and Launch Execution

## Goal
Define deterministic full-path launch execution behavior.

Feature 005 originally introduced a fixed top header shell for the app. Feature 012 later removes that header, so Feature 005 is now authoritative only for launch execution and failure feedback behavior.

## In Scope
- Launch command generation using full paths:
  - `-iwad <selectedIwadFullPath>`
  - Optional `-file <selectedModFullPath...>` in selected Mod sequence order.
- Launch execution through a testable process-launch abstraction.
- Non-blocking warning toast when launch fails.

## Out Of Scope
- Any change to footer command preview format from Feature 004.
- Changing selection semantics from prior features.
- Profile management.
- Current window header or pane layout behavior.
- The final placement of launch controls in later profile-based workspace features.
- Retry workflows or modal error dialogs.

## Definitions
- Launch readiness:
  - Selected source-port path is present.
  - Selected IWAD path is present.
- Ordered Mods:
  - The existing selected Mod sequence order.
- Launch arguments:
  - Argument tokens passed to process start, using full normalized absolute paths.

## Rules
### Launch Gating
- Before profile-based features, launch requires one selected source-port row and one selected IWAD.
- Later profile-based features may replace that UI gate while reusing the same launch argument construction and failure feedback rules.

### Launch Argument Construction
- Executable path is the selected source-port row full path.
- Always include exactly one IWAD segment when launching:
  - `-iwad <selectedIwadFullPath>`
- Include `-file` only when one or more Mods are selected:
  - `-file <selectedMod1FullPath> <selectedMod2FullPath> ...`
- Selected Mod argument order must match selected Mod sequence exactly.

### Launch Failure Feedback
- If launch execution fails, show a non-blocking warning toast in the window.
- Warning toasts auto-dismiss after `5` seconds.
- Failure warning does not block further interaction.

### Preview Compatibility
- Feature 004 preview behavior remains unchanged:
  - Preview continues to render filename-only tokens with existing quoting rules.
- Launch execution uses full paths independently from preview rendering.

## Acceptance Criteria
### Launch gating
Given no selected source-port row or no selected IWAD
When UI is rendered
Then launch is not ready.
And given one source-port row is selected and one IWAD is selected
Then launch is ready.

### Launch command uses full paths
Given source port, selected IWAD, and selected Mods
When Launch is triggered
Then process start receives executable as source-port full path.
And arguments include `-iwad <selectedIwadFullPath>`.
And arguments include `-file` with selected Mod full paths in selected sequence order.

### Launch without selected Mods
Given source port and selected IWAD with no selected Mods
When Launch is triggered
Then arguments include `-iwad <selectedIwadFullPath>` only.
And no `-file` segment is included.

### Non-blocking failure warning
Given launch is triggered and process start fails
When failure is raised by launcher
Then warning toast is shown with a failure message.
And the app remains interactive.
