# Feature 013 - Toast Message Overlay

## Goal
Replace the old in-window message/banner section with a shared toast system for workspace warning and confirmation feedback.

Feature 013 is the authoritative source for toast behavior. Earlier features remain authoritative only for when their own workflows invoke toast feedback.

## In Scope
- Replacing the old message/banner section with a toast overlay above the workspace.
- Shared passive warning-toast behavior for config-load warnings, launch failure, rename validation, and invalid-profile row launch.
- Shared confirmation-toast behavior for selected-profile delete confirmation.
- Shared toast replacement and invalid-launch suppression rules.

## Out Of Scope
- New notification categories beyond passive warning toasts and confirmation toasts.
- Persistence, window-sizing, or profile data-model changes.
- Changing the underlying launch, rename, delete, or profile-validity rules outside how feedback is presented.

## Definitions
- Toast overlay:
  - A single in-window feedback surface rendered above the workspace without reserving layout space.
- Passive warning toast:
  - A non-blocking warning toast with no action buttons that dismisses automatically after `5` seconds.
- Confirmation toast:
  - A toast with text `Delete` and `Cancel` actions that stays visible until acted on or replaced.

## Rules
### Shared Presentation
- The old fixed-layout message/banner section is not rendered.
- The toast host is anchored to the top-center of the window above the workspace content.
- Toasts render as an overlay and do not push the workspace layout downward.
- Only one toast is visible at a time.
- Showing a new toast replaces the current visible toast unless the invalid-launch duplicate-suppression rule keeps the existing toast active.

### Passive Warning Toasts
- Config-load warnings use a passive warning toast.
- Launch failure uses a passive warning toast.
- Rename validation failure uses a passive warning toast.
- Invalid-profile row launch uses a passive warning toast.
- Passive warning toasts remain non-blocking while visible.
- Passive warning toasts auto-dismiss after `5` seconds.

### Confirmation Toasts
- Selected-profile delete confirmation uses a confirmation toast.
- Confirmation toasts expose text `Delete` and `Cancel` actions.
- Confirmation toasts do not auto-dismiss.

### Invalid-Profile Launch Feedback
- Invalid profile row launch remains clickable while the row is in normal display mode.
- Activating invalid profile row launch:
  - selects that profile
  - hydrates current Source Port / IWAD / Mod selections from that profile
  - does not invoke launch
  - shows the profile's current invalid reason in a passive warning toast
- If the same invalid-profile warning toast is already visible for the same invalid reason, repeated row-launch clicks do not replace or restart that toast.
- If that toast has been dismissed or the invalid reason has changed, the next invalid row-launch shows the current invalid reason in a new passive warning toast.

## Acceptance Criteria
### Config-load warning uses passive warning toast
Given config load completes with a warning
When the main window is shown
Then a passive warning toast is visible with that warning text.

### Launch failure uses passive warning toast
Given launch is triggered and process start fails
When failure is raised by launcher
Then a passive warning toast is shown with the failure message.
And the app remains interactive.

### Rename validation uses passive warning toast
Given a profile row is in rename mode
When the entered name is empty, whitespace-only, or duplicates another profile name case-insensitively
Then the saved name remains unchanged.
And rename mode stays open.
And a passive warning toast is shown.

### Selected-profile delete uses confirmation toast
Given a selected profile exists
When the selected-profile delete action is activated
Then a confirmation toast is shown for that selected profile.
And the toast exposes text `Delete` and `Cancel` actions.

### Delete confirmation toast clears on confirm or cancel
Given a selected-profile delete confirmation toast is visible
When `Delete` is activated
Then the toast clears.
And the profile is removed.
And when `Cancel` is activated instead
Then the toast clears without deleting the profile.

### Invalid profile row launch shows warning without launching
Given an invalid saved profile row exists
When that row's launch action is activated
Then that profile becomes selected.
And current Source Port / IWAD / Mod selections hydrate from that profile.
And launch does not execute.
And a passive warning toast shows that profile's current invalid reason.

### Repeated invalid row launch does not restart same visible warning
Given an invalid saved profile row exists
And its current invalid-launch passive warning toast is already visible
When that row's launch action is activated again and the invalid reason is unchanged
Then launch still does not execute.
And the existing passive warning toast remains visible without being replaced or restarted.

### Invalid row launch shows updated warning after reason change
Given an invalid saved profile row exists
And its current invalid-launch passive warning toast has been dismissed or the invalid reason has changed
When that row's launch action is activated
Then a passive warning toast shows that row's current invalid reason.

### Toast overlay stays top-centered across workspace modes
Given a toast is visible
When the file library pane is expanded or collapsed
Then the toast remains top-centered above the workspace.
And it remains an overlay that does not push the workspace layout downward.
