# Orbit interface

## Product

A small Windows utility for configuring a radial application switcher. The task is to add applications, place them in a predictable order and choose a global binding. The wheel itself is the runtime interaction, not a dashboard widget.

## Direction

Dark, quiet and compact, with the restraint of Apple utility interfaces. Keep native Windows window behavior and Segoe typography; do not imitate macOS traffic-light controls or distribute Apple fonts. Light appearance is a complete alternate theme, toggled from a sun/moon icon in the upper corner and applied to both configuration and wheel.

The interface has two short pages: applications and settings. No preview stage, decorative illustrations, promotional slogans or permanent instruction panels. Icons have tooltips and accessible labels. Show full paths on demand, rather than filling the primary list with them.

## Visual system

- Dark canvas #161617, surface #222224, text #F5F5F7, secondary #A1A1A6, separator #38383A.
- Light canvas #F5F5F7, surface #FFFFFF, text #1D1D1F, secondary #66666B, separator #DCDCE0.
- Muted bronze is reserved for interaction focus and active controls (#B6A579 dark / #776237 light); primary actions use foreground/background inversion. Focus outlines appear for keyboard traversal, not every mouse click.
- Typography: Segoe UI, Display formatting and grayscale antialiasing to avoid colored fringes on small text. Row titles 14, controls 13, supporting labels 12. Hover never fades or scales text.
- Spacing: 4, 8, 12, 16, 24, 32. Control radius 7–9; grouped surface radius 12. Avoid nested cards.
- Rows expose real app icons, sequential positions, names and concise file details. Selection stays stable in size. Actions support keyboard focus, disabled and hover states.
- The wheel uses neutral segmented surfaces, a strong selected-state boundary and a readable central label in both themes. Selection remains apparent without relying on hue.
- The utility window is fixed at 720 × 480 DIP. Its 44-DIP title bar exposes theme, minimize and close; there is no maximize control, including a disabled placeholder. Secondary dialogs use the same plain close icon in a custom caption. Theme icons use Bootstrap Icons filled sun/moon, with room inside the button. About shows the developer GitHub; icon attribution remains in README and distributed licenses.
- Use one 24-DIP horizontal grid for navigation, content and footer. List actions belong directly below the rows inside the list group. Rows are 56 DIP; show up to four complete rows, then scroll. Settings contain only binding, size, sensitivity and startup.
- Use licensed Freepik / Flaticon UIcons solid-rounded paths for interface controls and the aperture logo. Retain license and attribution in About and README. App icons come from Windows Shell, including installed packaged-app identities for Notepad and Calculator. Never replace a real application icon with a hand-drawn or generated substitute. Only an unavailable icon gets the generic apps fallback.
- Theme transitions animate solid brush colors for 170 ms, respect reduced animation settings, and release their animation clocks on completion.
- The wheel uses a single locally captured, blurred background region and subtle fixed tint. Release that image when it closes. Do not continuously capture the desktop or register Raw Input while hidden.
- Do not show a permanent success status. Keep actual errors, undo feedback and invalid input visible.

## Verification

Render both pages and themes at the actual fixed size, at 100% and 125% scale, with empty lists, 12 entries and long names. Check that short lists do not scroll and that list actions cannot collide with the footer. Inspect the dialog, inline validation, keyboard states and radial labels separately. Preserve all input/focus fixes from v0.1.1. Run regression and Windows CI checks; state exactly which interactive tests were performed.

Research and reusable guidance: [skills/product-ui-craft](skills/product-ui-craft/SKILL.md).
