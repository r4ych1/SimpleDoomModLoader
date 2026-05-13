# ModLoader Specification

## Product Baseline
ModLoader is a lightweight Windows-first desktop interface for managing Doom source-port launch inputs.

For each launch profile, users provide:
- One source-port input from an ordered source-port list.
- One IWAD input from an ordered IWAD list.
- Zero or more mod inputs from an ordered mod list.

Feature 008 makes saved profiles the only launchable unit while keeping Source Ports, IWADs, and Mods as shared library collections.
Feature 012 removes the legacy fixed top header.
Feature 013 replaces the old in-window message section with a shared top-centered toast overlay.
Feature 014 replaces the old pane-collapse model with a single shared workspace that swaps between `Profiles` and `File Library`, keeps `New Profile` in `Profiles` view, adds a `File Library` title in File Library view, moves the File Library back action to top-left above the selected-profile header, adds a no-selection selected-profile-header `Create Profile` action, removes the old pane-collapse model and fixed default window sizes, and sets the app window title to `Simple Doom Mod Loader`.

This repository follows feature-scoped delivery. Behavior is only guaranteed when specified in feature specs under `Features/`.

## Feature Index
- Feature 001: Drag-and-drop source-port, IWAD, and mod input lists.
  - Authoritative spec: `Features/001-drop-zones.md`.
- Feature 002: Full-border drop zones and selectable IWAD/Mod rows.
  - Shared-library drop zones use always-visible instructional card styling with drag-over highlight states.
  - Authoritative spec: `Features/002-border-drop-and-row-selection.md`.
- Feature 003: Config persistence and startup recovery.
  - Includes persisted selection state for IWAD and Mod rows.
  - Authoritative spec: `Features/003-config-persistence-and-recovery.md`.
- Feature 004: Fixed command preview footer and selection-synchronized mod ordering.
  - Adds generated launch-argument preview (`-iwad`, `-file`) using filename-only tokens and keeps no-profile mod ordering alphabetical.
  - Authoritative spec: `Features/004-fixed-command-preview-and-selection-order.md`.
- Feature 005: Fixed header and launch execution.
  - Defines launch execution using generated full-path `-iwad` / `-file` arguments from current selection state.
  - Feature 012 later removes the old fixed header presentation.
  - Authoritative spec: `Features/005-fixed-header-and-launch-execution.md`.
- Feature 006: Section collapse layout and row interaction states.
  - Adds whole-section collapse behavior plus distinct row hover and selected visuals.
  - Authoritative spec: `Features/006-row-actions-clear-all-and-row-states.md`.
- Feature 007: Source-port list parity and Mod `.zip` support.
  - Replaces single active source-port behavior with ordered source-port list behavior and expands Mod allowlist to include `.zip`.
  - Authoritative spec: `Features/007-source-port-list-and-mod-zip.md`.
- Feature 008: Selection-based profile workspace.
  - Adds saved profiles as the primary launch model, shared-library disabled-state behavior when no profile is selected, immediate profile auto-save through shared-library edits, and selected-profile header behavior in File Library view.
  - Feature 014 later becomes authoritative for workspace composition and view swapping.
  - Authoritative spec: `Features/008-profile-management.md`.
- Feature 009: Collapsible file library pane.
  - Historical feature superseded by Feature 014.
  - Authoritative historical note: `Features/009-file-library-pane-collapse.md`.
- Feature 010: Profile drag reordering.
  - Adds manual drag reordering for saved profile rows using a name-only floating ghost row and a single insertion marker.
  - Authoritative spec: `Features/010-profile-drag-reorder.md`.
- Feature 011: Icon-based action controls.
  - Replaces selected text-labeled UI actions with icon-only controls while preserving behavior.
  - Feature 014 later becomes authoritative for the workspace view-swap controls that expose `File Library` and `Profiles`.
  - Authoritative spec: `Features/011-icon-based-action-controls.md`.
- Feature 012: Header removal.
  - Removes the legacy fixed top header while leaving workspace swapping authoritative in Feature 014.
  - Authoritative spec: `Features/012-profile-only-collapse-mode-and-header-removal.md`.
- Feature 013: Toast message overlay.
  - Replaces the old in-window message section with a shared top-centered toast overlay that does not reserve layout space above the workspace.
  - Authoritative spec: `Features/013-toast-message-overlay.md`.
- Feature 014: Single-view profile/library workspace swap.
  - Replaces the two-pane workspace with one single shared workspace that shows either `Profiles` or `File Library`.
  - Creating a new profile keeps Profiles view active; double-clicking a profile opens File Library view.
  - File Library shows a title, uses a top-left back-to-profiles action above the selected-profile header, and shows selected-profile-header `Create Profile` when no profile is selected.
  - Persists the last active workspace view, keeps File Library accessible without a selected profile, and removes the old pane-collapse model and fixed default window sizes.
  - Authoritative spec: `Features/014-single-view-profile-library-swap.md`.
- Feature 015: Mod selection stability and manual drag reorder.
  - Replaces selection-synchronized Mod row ordering with stable shared-library ordering.
  - Adds manual Mod drag reorder with pointer-threshold drag, insertion marker, and drag ghost feedback.
  - Authoritative spec: `Features/015-mod-selection-stability-and-drag-reorder.md`.

## Current Authoritative Behavior
- Workspace model (Feature 014):
  - The app uses one single shared workspace that shows either `Profiles` or `File Library`.
- Launch model (Feature 008):
  - Saved profiles are the only launchable unit.
  - Source Ports, IWADs, and Mods remain shared library collections.
- Mod row ordering model (Feature 015):
  - Selecting or deselecting a Mod row does not reorder Mod rows.
  - Manual Mod drag reorder updates and persists shared-library Mod order.
- Feedback model (Feature 013):
  - The app uses one shared top-centered toast overlay above the workspace.
  - Only one toast is visible at a time.
  - Passive warning toasts and confirmation toasts are the supported feedback categories.
- Visibility model (Features 008 and 014):
  - Profile-row command preview and profile-row inline invalid-reason text are visible only in `Profiles` view.
  - Profile-row inline detail messaging (command preview and inline invalid-reason text) is additionally width-gated to window widths greater than `640 px`.

## Feature Precedence And Supersession
- Feature specs are authoritative in numeric order of delivery.
- Later features override earlier behavior only where replacement is explicit.
- Known supersessions:
  - Feature 009 is historical and superseded by Feature 014.
  - Legacy fixed-header behavior is superseded by Feature 012.
  - Legacy pane-collapse workspace behavior is superseded by Feature 014.
  - Feature 004 selection-synchronized Mod row ordering is superseded by Feature 015.
  - Feature 008 Mod ordering context is superseded by Feature 015 where Mod row ordering behavior is concerned.

## Persistence Contract Snapshot
- Current persisted model is defined by feature specs and represented through `LaunchInputsConfig`.
- The persisted contract includes shared library collections plus:
  - `Profiles`
  - nullable `SelectedProfileId`
  - `IsFileLibraryViewActive`
- Legacy `IsFileLibraryPaneCollapsed` and `LastExpandedWindowWidth` load-time compatibility handling is defined by Feature 014:
  - legacy fields are ignored on load and not re-saved.
- Field-level schema and migration rules remain authoritative in the feature specs (primarily Features 003, 008, and 014).

## Windows-First Path Semantics
- File-path handling is Windows-first unless a later feature explicitly broadens platform scope.
- Normalized absolute path identity and path comparisons are case-insensitive under Windows-first semantics.
- Surface-specific path rules and allowlists remain authoritative in the relevant feature specs.
