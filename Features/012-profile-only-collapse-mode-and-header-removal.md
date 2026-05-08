# Feature 012 - Header Removal

## Goal
Remove the legacy fixed top header so the workspace becomes the first main layout block in the window.

## In Scope
- Removing the old title/helper banner above the workspace.
- Keeping visible workspace toast behavior aligned with Feature 013.

## Out Of Scope
- Pane collapse or expand behavior.
- Profile-only mode.
- Native window width persistence, restore rules, or fixed default window sizing.
- Any workspace view-swapping behavior, which is authoritative in Feature 014.

## Rules
- The top fixed header is not rendered.
- The workspace is the first main layout block in the window.
- Any visible workspace toast follows Feature 013 placement and overlay behavior.
- Feature 014 is authoritative for workspace view swapping.

## Acceptance Criteria
### Legacy header removed
Given the main window is rendered
When the top-level layout is shown
Then the old title/helper header banner is absent.
And any visible workspace toast follows Feature 013 placement and overlay behavior.
