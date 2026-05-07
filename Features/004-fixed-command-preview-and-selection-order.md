# Feature 004 - Fixed Command Preview Footer and Selection-Synchronized Mod Ordering

## Goal
Provide a fixed footer that shows a live generated launch-argument preview and keep displayed Mod ordering synchronized with selected Mod load order without rewriting shared-library persistence.

## In Scope
- Fixed, non-scrolling footer preview text area.
- Preview content includes generated arguments only:
  - `-iwad`
  - `-file`
- Deterministic filename rendering for preview arguments:
  - Render filename tokens only (`Path.GetFileName`) from selected IWAD/Mod paths.
  - Quote a filename only when it contains spaces.
- Wrapped preview text behavior:
  - Preview text wraps within footer bounds to avoid horizontal overflow.
- Mod display ordering synchronized to selection sequence:
  - Selected Mods appear first in the same order as selection sequence.
  - Unselected Mods remain after selected Mods in alphabetical filename order.
- Detached-state default Mod ordering:
  - When no profile is selected, Mod rows default to alphabetical filename order.
- Profile-backed selected-mod persistence:
  - `ProfileConfig.SelectedModPaths` is the persisted authority for per-profile selected-mod order.
- Deselection compaction:
  - Removing a Mod from selection removes it from selected sequence and keeps remaining selected sequence order stable.
- Immediate persistence when selection changes a selected profile's mod references.

## Out Of Scope
- Launch execution.
- Source-port executable prefix in preview text.
- Any keyboard modifier semantics (`Ctrl`, `Shift`) beyond existing behavior.
- Changes to extension allowlists or drop processing rules from prior features.

## Definitions
- Preview arguments:
  - Generated command-line argument string that excludes executable path.
- Selection sequence:
  - Ordered list of currently selected Mod paths based on the sequence of successful selections.
- Selection-synchronized mod ordering:
  - Mod row order used by the UI where selected Mods appear first in selection sequence, followed by remaining Mods in alphabetical filename order.

## Rules
### Footer Preview Rendering
- Footer preview is visible while main content scrolls.
- Preview text wraps to additional lines within the preview area when needed.
- Preview updates immediately after all relevant state changes:
  - IWAD selection toggle.
  - Mod selection toggle.
  - Operations that remove selected IWAD/Mod rows.
  - Startup sanitation/recovery that changes relevant selection state.
- If no IWAD is selected and no Mods are selected, preview is an empty string.

### `-iwad` Argument
- Include `-iwad <path>` only when an IWAD row is currently selected.
- Selected IWAD contributes exactly one path.

### `-file` Argument
- Include `-file <path1> <path2> ...` only when one or more Mod rows are currently selected.
- Path order in `-file` matches selected Mod sequence exactly.

### Argument Segment Ordering
- Preview argument segments always follow this order:
  1. `-iwad` segment (if present)
  2. `-file` segment (if present)

### Filename Rendering and Quoting
- Preview tokens use filename-only values extracted from selected paths.
- Filenames with spaces are quoted using double quotes.
- Filenames without spaces are not quoted.

### Mod Selection Ordering Behavior
- Selecting an unselected Mod appends that Mod path to the end of selected sequence.
- Deselecting a selected Mod removes only that Mod path from selected sequence.
- After every Mod selection toggle, Mod row order is rebuilt as:
  - all selected Mod paths in selected sequence order
  - then all non-selected Mod paths sorted by filename case-insensitively, with full path as deterministic tie-breaker
- When no profile is selected:
  - Mod rows remain in alphabetical filename order
  - profile-scoped selection changes are unavailable
- When a profile is selected:
  - selected-mod order persists through that profile's `SelectedModPaths`
  - selection toggles do not rewrite top-level shared-library `Mods` ordering

### Persistence
- After Mod selection toggles, persist immediately.
- Persisted `Mods` ordering remains the shared library collection order.
- Persisted per-profile selected-mod order is stored in `ProfileConfig.SelectedModPaths`.

## Acceptance Criteria
### Empty preview
Given no selected IWAD row and no selected Mod rows
When the footer preview is shown
Then preview text is empty.

### IWAD-only preview
Given one selected IWAD row and no selected Mod rows
When preview is generated
Then preview contains only `-iwad <iwadPath>`.

### Mod-only preview
Given no selected IWAD row and selected Mod rows
When preview is generated
Then preview contains only `-file <modFileName...>` in selected sequence order.

### Combined preview ordering
Given one selected IWAD row and selected Mod rows
When preview is generated
Then preview is ordered as `-iwad <iwadFileName> -file <modFileName...>`.

### Filename quoting
Given selected IWAD/Mod paths where at least one selected filename contains spaces
When preview is generated
Then only filenames containing spaces are double-quoted.

### Wrapped preview layout
Given preview content long enough to exceed available horizontal footer width
When preview is displayed
Then text wraps within the preview area instead of overflowing horizontally.

### Selection-synchronized Mod list ordering
Given at least three Mod rows
When rows are selected in a specific sequence
Then Mod row order moves selected rows to the top in that same sequence.
And non-selected rows remain afterward in alphabetical filename order.

### Deselection compaction
Given multiple selected Mod rows
When one selected Mod row is deselected
Then that row is removed from selected sequence.
And remaining selected Mod order is unchanged.
And Mod list remains continuous without gaps.

### No-profile Mod ordering stays alphabetical
Given no profile is selected and Mod rows exist
When Mod rows are rendered and selection input is attempted
Then Mod rows remain in alphabetical filename order.
And no Mod selection is applied.
And persisted config does not store any no-profile selected Mod ordering for restart.

### Profile-backed selected-mod persistence
Given a selected profile with Mod rows
When Mod selection changes
Then that profile's `SelectedModPaths` persists the selected sequence.
And top-level shared-library `Mods` ordering is unchanged.
