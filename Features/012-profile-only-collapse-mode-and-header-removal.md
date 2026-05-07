# Feature 012 - Profile-Only Collapse Mode and Header Removal

## Goal
Remove the legacy fixed top header and make collapsed file-library mode a true profile-only window mode.

## In Scope
- Removing the old fixed top header banner from the main window.
- Preserving the existing toast overlay and workspace content below it.
- Shrinking the native window to the profile-management section when the file library collapses in a normal window state.
- Restoring the remembered expanded normal-window width when the file library expands again.
- Applying the same width behavior to pane-toggle, double-click pane-shortcut, startup-collapsed, and new-profile auto-expand flows.
- Persisting the last expanded normal-window width for future restore from collapsed mode.

## Out Of Scope
- Changing profile creation, selection, launch, rename, delete, validity, drag reorder, or shared-library editing rules.
- Changing the existing `768 px` profile-row preview threshold.
- Persisting maximized state, window height, or window position.
- Adding resizable splitters, manual profile-only width presets, or alternate expand affordances.

## Definitions
- Legacy fixed header:
  - The removed top banner that previously rendered title and helper copy above the workspace.
- Profile-only window mode:
  - The collapsed file-library presentation where the right pane is hidden and the native window shrinks to the profile-management section shell.
- Remembered expanded normal-window width:
  - The last persisted window width captured while the file library is expanded and the window state is normal.

## Rules
### Header Removal
- The top fixed header is not rendered.
- Toast messages render as an overlay and do not reserve layout space above the workspace.
- The toast overlay is anchored to the top-center of the window above the workspace content.
- The workspace remains the first main layout block in the window.

### Profile-Only Collapse Mode
- Collapsing the file library keeps the existing Feature 009 pane rules:
  - the right file-library pane is not rendered
  - the spacer gap is not rendered
  - the left profile pane remains as the only workspace pane in view
- When the window state is normal and the file library collapses:
  - the native window shrinks to the profile-management section shell
  - that collapsed target width is based on the profile-management pane shell rather than long profile names, previews, or message text
- When the file library expands again from collapsed mode in a normal window state:
  - the native window restores the remembered expanded normal-window width
  - if no remembered width exists, the restored expanded width falls back to `1180`
- Creating a new profile while collapsed still expands the file library immediately using Feature 008 behavior, and that expand flow also restores the remembered expanded width in a normal window state.
- Double-click profile-row pane shortcuts still follow Feature 008 and Feature 009 selection rules, and they also apply the same shrink/restore width behavior in a normal window state.

### Maximized Window Behavior
- If the window is maximized, collapsing or expanding the file library does not change the native window state or width immediately.
- While maximized, only the pane layout state changes.
- When the user returns the window to normal state, the current pane state immediately applies its matching normal-state width behavior:
  - collapsed uses the profile-only width
  - expanded restores the remembered expanded normal-window width

### Persistence
- `LaunchInputsConfig` adds:
  - `LastExpandedWindowWidth`
- `LastExpandedWindowWidth` is updated only when:
  - the file library is expanded
  - the window state is normal
  - a valid positive width is available
- Collapsed-mode width changes do not overwrite `LastExpandedWindowWidth`.
- Saves that do not contain `LastExpandedWindowWidth` fall back to the expanded default width of `1180`.
- If startup loads `IsFileLibraryPaneCollapsed = true` and the window opens in normal state, the app starts in the shrunk profile-only mode.

## Acceptance Criteria
### Legacy header removed
Given the main window is rendered
When the top-level layout is shown
Then the old title/helper header banner is absent.
And toast messages remain available when needed without pushing the workspace down.
And visible toast messages are aligned to the top-center of the window above the workspace content.

### Toast overlay stays top-centered across workspace modes
Given a toast message is visible
When the file library pane is expanded or collapsed
Then the toast remains aligned to the top-center of the window.
And it remains an overlay above the workspace content.

### Collapse shrinks to profile-only window mode
Given the file library is expanded and the window state is normal
When the file-library collapse control is activated
Then the file library pane is removed from view.
And the spacer gap is removed from view.
And the native window shrinks to the profile-management section shell.

### Expand restores remembered width
Given the file library is collapsed in normal window state
And a remembered expanded normal-window width exists
When the file library is expanded
Then the native window restores the remembered expanded normal-window width.
And the selected-profile header area and shared library controls become visible again.

### Expand falls back to default width
Given the file library is collapsed in normal window state
And no remembered expanded normal-window width exists
When the file library is expanded
Then the native window restores to `1180`.

### Collapsed startup opens in profile-only mode
Given saved config contains `IsFileLibraryPaneCollapsed = true`
When the app starts in normal window state
Then the file library pane starts hidden.
And the native window starts in the shrunk profile-only mode.

### Maximized collapse defers native resize
Given the window is maximized and the file library is expanded
When the file-library collapse control is activated
Then the pane layout changes to collapsed state.
And the native window remains maximized.
And when the user later returns the window to normal state
Then the native window shrinks to the profile-only width.

### Remembered width updates only from expanded normal state
Given the file library is expanded and the window state is normal
When the user resizes the window
Then `LastExpandedWindowWidth` updates to that width.
And given the file library is collapsed or the window is maximized
When width changes for those states occur
Then `LastExpandedWindowWidth` does not update from those changes.
