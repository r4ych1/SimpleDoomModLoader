# ModLoader Specification

## Product Baseline
ModLoader is a lightweight Windows-first desktop interface for managing Doom source-port launch inputs.

For each launch profile, users provide:
- One source-port input from an ordered source-port list.
- One IWAD input from an ordered IWAD list.
- Zero or more mod inputs from an ordered mod list.

Feature 005 added launch execution for the pre-profile single-selection workflow.
Feature 008 makes saved profiles the only launchable unit while keeping Source Ports, IWADs, and Mods as shared library collections.
Feature 012 removes the legacy fixed top header and turns collapsed file-library mode into a profile-only window mode that shrinks the native window to the profile-management section.

This repository follows feature-scoped delivery. Behavior is only guaranteed when specified in feature specs under `Features/`.

## Feature Index
- Feature 001: Drag-and-drop source-port, IWAD, and mod input lists.
  - Authoritative spec: `Features/001-drop-zones.md`.
- Feature 002: Full-border drop zones and selectable IWAD/Mod rows.
  - Current shared-library drop zones use always-visible instructional card styling and drag-over highlight states while preserving Feature 001 validation and ordering rules.
  - Authoritative spec: `Features/002-border-drop-and-row-selection.md`.
- Feature 003: Config persistence and startup recovery.
  - Includes persisted selection state for IWAD and Mod rows.
  - Authoritative spec: `Features/003-config-persistence-and-recovery.md`.
- Feature 004: Fixed command preview footer and selection-synchronized mod ordering.
  - Adds generated launch-argument preview (`-iwad`, `-file`) using filenames-only with wrapped footer display, and derives Mod display ordering from current selected-mod sequence instead of persisting shared-library reorders.
  - Authoritative spec: `Features/004-fixed-command-preview-and-selection-order.md`.
- Feature 005: Fixed header and launch execution.
  - Defines launch execution using generated full-path `-iwad` / `-file` arguments from current selection state.
  - The fixed top header introduced there is later removed by Feature 012.
  - Later profile-based features are authoritative for where launch is triggered in the UI.
  - Authoritative spec: `Features/005-fixed-header-and-launch-execution.md`.
- Feature 006: Section collapse layout and row interaction states.
  - Aligns Source Port/IWAD/Mod section headers with inline collapse actions and row-level `Delete` actions using shared right-hand action-column layout patterns.
  - Adds whole-section collapse behavior plus distinct row `hover`, `selected`, and `selected+hover` visuals for light/dark themes.
  - Authoritative spec: `Features/006-row-actions-clear-all-and-row-states.md`.
- Feature 007: Source-port list parity and Mod `.zip` support.
  - Replaces single active source-port behavior with ordered source-port list behavior and single-select source-port row state.
  - Expands Mod allowlist to include `.zip`.
  - Updates command preview to include selected source-port filename before `-iwad` / `-file` segments.
  - Authoritative spec: `Features/007-source-port-list-and-mod-zip.md`.
- Feature 008: Selection-based profile workspace.
  - Adds saved profiles as the primary launch model in a two-pane workspace: a pinned profile list on the left and independently scrolling shared file library on the right, with a profile-creation action in the left profile-management header.
  - Profiles persist source port, IWAD, and ordered mod references to the shared library, are the only launchable unit, and require one source port plus one IWAD only for profile validity.
  - The expanded shared file-library pane renders Source Port, IWAD, and Mod drop zones as visibly interactive drag-and-drop targets with visible instructional text, section-specific accessible labels, and drag-over highlight.
  - While the expanded shared file-library pane has additional content below the current viewport, it shows a subtle bottom-centered downward chevron affordance that remains visible until the bottom of the scrollable content is reached.
  - While the left profile list has additional content below the current viewport, it shows the same subtle bottom-centered downward chevron affordance in both expanded two-pane mode and collapsed profile-only mode.
  - Each saved profile row renders its own wrapped filename-only command preview inside the profile cell, and that preview appears only while the file library is collapsed and the window width remains above the Feature 008 minimum threshold.
  - While a profile is selected in the expanded file library, the selected-profile header stays fixed at the top of the file-library pane, keeps the profile name, shows right-aligned `Edit` and `Delete` actions on the same row, shows status text with the shared amber invalid color when invalid, and renders its own wrapped filename-only command preview from the saved profile inputs.
  - While the file library is expanded, the left `Profiles` pane uses a fixed `380 px` width, profile names wrap up to two lines, and inline invalid-reason text stays hidden while shared status badges remain visible.
  - The left profile list and the right file-library pane remain scrollable when their content overflows, while visible scrollbar chrome stays hidden for both panes.
  - Profile rows expose launch, rename, and delete actions plus a shared row-status badge treatment for valid and invalid states, while profile rename is handled inline on the selected row and outside-click rename exit cancels rather than saves.
  - Profile edits auto-save immediately through file-library selection changes; row launch actions select and run that specific valid saved profile.
  - Authoritative spec: `Features/008-profile-management.md`.
- Feature 009: Collapsible file library pane.
  - Wraps the shared file library in its own right-side section with a pane-level collapse control that lives in the left profile-management header.
  - Collapsing the file library removes the right pane from view and collapses the pane gap while the left profile pane remains as the only workspace pane in view.
  - Double-clicking a profile row acts as a pane shortcut: it expands the file library while collapsed and collapses it while expanded, while keeping the clicked profile selected.
  - File-library pane collapse state persists across restart without changing profile, launch, or inner library-section behavior, and the pane does not render a visible `File Library` title while expanded.
  - Authoritative spec: `Features/009-file-library-pane-collapse.md`.
- Feature 010: Profile drag reordering.
  - Adds manual drag reordering to the left-side profile-management list using a name-only floating ghost row and a single insertion marker.
  - Persisted profile order becomes user-managed list order while keeping profile selection, launch, rename, delete, and validity rules unchanged.
  - Authoritative spec: `Features/010-profile-drag-reorder.md`.
- Feature 011: Icon-based action controls.
  - Replaces selected text-labeled UI actions with Fluent icon-only controls while preserving profile creation, launch, rename, delete, remove, and pane-collapse behavior, and adds the shared Fluent `arrow_down_regular` geometry for the file-library scroll affordance.
  - Shared-library Source Port, IWAD, and Mod row delete icons expose `Delete`, while the file-library pane toggle exposes `File Library` and the selected-profile header exposes `Edit` and `Delete`.
  - Keeps destructive confirmation in the message banner text-based while exposing icon-only actions through tooltip and automation name text.
  - Authoritative spec: `Features/011-icon-based-action-controls.md`.
- Feature 012: Profile-only collapse mode and header removal.
  - Removes the legacy fixed top header while preserving the existing message banner and two-pane workspace structure.
  - Makes collapsed file-library mode a true profile-only window mode that shrinks the native window to the profile-management section and restores the remembered expanded width when reopened.
  - Persists the last expanded normal-window width for future restore from collapsed mode, while keeping the existing `768 px` row-preview threshold unchanged.
  - Authoritative spec: `Features/012-profile-only-collapse-mode-and-header-removal.md`.

## Scope Boundary For Feature 001
Feature 001 provides in-memory state management and UI interactions only. It does not include:
- Persistence to disk (provided later by Feature 003).
- Launch execution (provided later by Feature 005).
- Profile management.
- Recursive directory traversal.
