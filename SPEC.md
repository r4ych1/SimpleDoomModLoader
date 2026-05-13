# ModLoader Specification

## Product Baseline
ModLoader is a lightweight Windows-first desktop interface for managing Doom source-port launch inputs.

Saved profiles are the only launchable unit. Source Ports, IWADs, and Mods are shared ordered library collections used by profiles.

This repository follows feature-scoped delivery. Behavior is only guaranteed when specified in feature specs under `Features/`.

## Feature Index
- Feature 001: Drag-and-drop source-port, IWAD, and mod input lists. Authoritative spec: `Features/001-drop-zones.md`.
- Feature 002: Full-border drop zones and selectable IWAD/Mod rows. Authoritative spec: `Features/002-border-drop-and-row-selection.md`.
- Feature 003: Config persistence and startup recovery. Authoritative spec: `Features/003-config-persistence-and-recovery.md`.
- Feature 004: Fixed command preview footer and selection-synchronized mod ordering. Authoritative spec: `Features/004-fixed-command-preview-and-selection-order.md`.
- Feature 005: Fixed header and launch execution; fixed-header presentation superseded by Feature 012. Authoritative spec: `Features/005-fixed-header-and-launch-execution.md`.
- Feature 006: Section collapse layout and row interaction states. Authoritative spec: `Features/006-row-actions-clear-all-and-row-states.md`.
- Feature 007: Source-port list parity and Mod `.zip` support. Authoritative spec: `Features/007-source-port-list-and-mod-zip.md`.
- Feature 008: Selection-based profile workspace; workspace composition and view swapping superseded by Feature 014. Authoritative spec: `Features/008-profile-management.md`.
- Feature 009: Collapsible file library pane; historical feature superseded by Feature 014. Authoritative spec: `Features/009-file-library-pane-collapse.md`.
- Feature 010: Profile drag reordering. Authoritative spec: `Features/010-profile-drag-reorder.md`.
- Feature 011: Icon-based action controls; workspace view-swap controls superseded by Feature 014. Authoritative spec: `Features/011-icon-based-action-controls.md`.
- Feature 012: Header removal. Authoritative spec: `Features/012-profile-only-collapse-mode-and-header-removal.md`.
- Feature 013: Toast message overlay. Authoritative spec: `Features/013-toast-message-overlay.md`.
- Feature 014: Single-view profile/library workspace swap. Authoritative spec: `Features/014-single-view-profile-library-swap.md`.
- Feature 015: Shared-library selection stability and manual drag reorder. Authoritative spec: `Features/015-mod-selection-stability-and-drag-reorder.md`.

## Current Authoritative Behavior
- Workspace model (Feature 014):
  - The app uses one single shared workspace that shows either `Profiles` or `File Library`.
- Launch model (Feature 008):
  - Saved profiles are the only launchable unit.
  - Source Ports, IWADs, and Mods remain shared library collections.
  - Selected-profile status text uses themed green `#10b981` while the selected profile is valid and shared amber `#f59e0b` while invalid.
- Shared-library row ordering model (Feature 015):
  - Selecting or deselecting Source Port, IWAD, or Mod rows does not reorder rows.
  - Manual drag reorder updates and persists shared-library Source Port, IWAD, and Mod order.
- Feedback model (Feature 013):
  - The app uses one shared top-centered toast overlay above the workspace.
  - Only one toast is visible at a time.
  - Passive warning toasts and confirmation toasts are the supported feedback categories.
- Profile rename interaction model (Feature 008):
  - Row and header rename modes save on `Enter` or outside click when the name is valid.
  - `Escape` restores the previous saved name.
  - Invalid rename attempts keep rename mode open and show a passive warning toast.
- Visibility model (Features 008 and 014):
  - Profile-row command preview and profile-row inline invalid-reason text are visible only in `Profiles` view.
  - Profile-row inline detail messaging (command preview and inline invalid-reason text) is additionally width-gated to window widths greater than `640 px`.
  - Profiles-view helper subtitle text is visible only in `Profiles` view and only when the overall window width is greater than `640 px`.
  - File-Library-view helper subtitle text is visible only in `File Library` view and only when the overall window width is greater than `640 px`.
- Persistence model (Features 003, 008, and 014):
  - Current persisted state is represented through `LaunchInputsConfig`.
  - Persisted state includes shared library collections, saved profiles, nullable `SelectedProfileId`, and `IsFileLibraryViewActive`.
  - Legacy `IsFileLibraryPaneCollapsed` and `LastExpandedWindowWidth` fields are ignored on load and not re-saved.
  - Field-level schema and migration rules remain authoritative in the feature specs.
- Path model:
  - File-path handling is Windows-first unless a later feature explicitly broadens platform scope.
  - Normalized absolute path identity and path comparisons are case-insensitive under Windows-first semantics.
  - Surface-specific path rules and allowlists remain authoritative in the relevant feature specs.

## Conflict Resolution
- Feature specs are authoritative in delivery order.
- Later features override earlier behavior only where replacement is explicit.
- Known supersessions:
  - Feature 009 is historical and superseded by Feature 014.
  - Legacy fixed-header behavior is superseded by Feature 012.
  - Legacy pane-collapse workspace behavior is superseded by Feature 014.
  - Feature 004 selection-synchronized Mod row ordering is superseded by Feature 015.
  - Feature 008 Mod ordering context is superseded by Feature 015 where Mod row ordering behavior is concerned.
