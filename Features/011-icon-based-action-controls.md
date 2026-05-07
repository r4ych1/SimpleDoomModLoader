# Feature 011 - Icon-Based Action Controls

## Goal
Use icon-only controls for compact workspace actions without changing their underlying behavior.

Feature 014 later becomes authoritative for which workspace-view swap controls are shown and where they are placed.

## In Scope
- Icon-only presentation for profile-header, selected-profile-header, profile-row, shared-library-row, and section-toggle actions.
- Tooltip and automation-name text for every icon-only control.
- Shared Fluent icon geometry resources used by those controls and the scroll affordances.

## Out Of Scope
- Launch-rule changes.
- Profile validity changes.
- Toast-behavior changes beyond keeping confirmation actions text-based.

## Rules
### Icon mappings
- The Profiles-header `New Profile` action uses `add_square_regular`.
- The Profiles-header view-swap action uses `folder_open_regular`.
- The File-Library-header view-swap action uses `folder_regular`.
- The selected-profile header `Edit` action uses `edit_regular`.
- The selected-profile header `Delete` action uses `delete_regular`.
- Each profile-row `Launch` action uses `play_regular`.
- Each profile-row `Rename` action uses `edit_regular`.
- Each profile-row `Delete` action uses `delete_regular`.
- Each shared-library row `Delete` action uses `delete_regular`.
- The Source Port / IWAD / Mod section toggles use `chevron_up_regular` while expanded and `chevron_down_regular` while collapsed.
- The profile-list and file-library scroll affordances use `arrow_down_regular`.

### Accessibility
- Every icon-only control exposes the same action text through tooltip and automation name text.
- Profiles-header view-swap action exposes `File Library`.
- File-Library-header view-swap action exposes `Profiles`.
- Static icon-only actions expose:
  - `New Profile`
  - `Edit`
  - `Launch`
  - `Rename`
  - `Delete`
- Section toggles expose `Expand` or `Collapse` according to current state.

### Behavior preservation
- Icon-only controls keep the same click handlers, visibility rules, and layout slots used by their corresponding actions.
- The profile-list and file-library scroll affordances remain visual-only and do not introduce a new command, hit target, or keyboard behavior.
- Delete confirmation actions remain text-based inside the Feature 013 confirmation toast.

### Visual composition
- Icon-only conversion does not reduce the effective hit target below the prior button treatment.
- Scroll affordances stay anchored near the bottom-center of their visible view.
- Scroll affordances keep the shared visible opacity of `0.72`.

## Acceptance Criteria
### View-swap controls expose destination labels
Given the Profiles view is rendered
When the Profiles-header swap control is shown
Then it renders `folder_open_regular`.
And it exposes `File Library` through tooltip and automation name text.
And given the File Library view is rendered
When the File-Library-header swap control is shown
Then it renders `folder_regular`.
And it exposes `Profiles` through tooltip and automation name text.

### Selected-profile header actions use icons
Given the File Library header is rendered while a profile is selected
When its actions are shown
Then `Edit` and `Delete` render `edit_regular` and `delete_regular`.
And each exposes its action text through tooltip and automation name text.

### Profile-row actions use icons
Given a saved profile row is rendered in normal display mode
When its row actions are shown
Then `Launch`, `Rename`, and `Delete` render `play_regular`, `edit_regular`, and `delete_regular`.
And each exposes its action text through tooltip and automation name text.

### Scroll affordances use the shared arrow icon
Given the profile list or file library shows a hidden-scrollbar affordance
When that affordance is visible
Then it renders `arrow_down_regular` near the bottom-center of the visible view.
And it remains non-interactive with shared visible opacity `0.72`.
