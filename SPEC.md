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
Feature 014 replaces the old pane-collapse model with a single shared workspace that swaps between `Profiles` and `File Library`, and removes the old pane-collapse model and fixed default window sizes.

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
  - Persists the last active workspace view, keeps File Library accessible without a selected profile, and removes the old pane-collapse model and fixed default window sizes.
  - Authoritative spec: `Features/014-single-view-profile-library-swap.md`.

## Scope Boundary For Feature 001
Feature 001 provides in-memory state management and UI interactions only. It does not include:
- Persistence to disk (provided later by Feature 003).
- Launch execution (provided later by Feature 005).
- Profile management.
- Recursive directory traversal.
