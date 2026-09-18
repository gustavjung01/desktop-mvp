# Sidebar visual refresh

## Scope
- Desktop sidebar only.
- Preserve current navigation groups, permissions, click handlers, loading/content behavior, and workspace routing.
- No backend, database, API-contract, or business-rule changes.

## Visual treatment
- Keep the existing green Desktop palette.
- Keep the existing saturated green surface with only restrained translucency; avoid the washed-out pale treatment.
- Use compact single-line navigation rows, a slim active indicator, and a nested submenu guide line for an office-desktop hierarchy.
- Hide the rail scrollbar chrome while preserving mouse-wheel scrolling and permission-driven menu visibility.
- Keep expanded navigation inline, and make the collapsed state a true 64px icon rail with right-side group flyouts; expanded width is 224px.
- Apply the treatment consistently in light and dark themes.

## Boundary
This change intentionally does not redesign workspace content and does not introduce new navigation/business actions.
