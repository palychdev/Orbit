# Orbit interface

## Product

A small Windows utility for configuring a radial application switcher. The task is to add applications, place them in a predictable order and choose a global binding. The wheel itself is the runtime interaction, not a dashboard widget.

## Direction

Dark, quiet and compact, with the restraint of Apple utility interfaces. Keep native Windows window behavior and Segoe typography; do not imitate macOS traffic-light controls or distribute Apple fonts. Light appearance is a complete alternate theme, toggled from a sun/moon icon in the upper corner and applied to both configuration and wheel.

The interface has two short pages: applications and settings. No preview stage, decorative illustrations, promotional slogans or permanent instruction panels. Icons have tooltips and accessible labels. Show full paths on demand, rather than filling the primary list with them.

## Visual system

- Dark canvas #161617, surface #222224, text #F5F5F7, secondary #A1A1A6, separator #38383A.
- Light canvas #F5F5F7, surface #FFFFFF, text #1D1D1F, secondary #66666B, separator #DCDCE0.
- Blue is reserved for interaction focus and active controls; primary actions use foreground/background inversion.
- Typography: Segoe UI Variable Text / Segoe UI. Page headings 24, section labels and row titles 14, controls 13, supporting labels 12. Use weight and spacing rather than all-caps tracking.
- Spacing: 4, 8, 12, 16, 24, 32. Control radius 7–9; grouped surface radius 12. Avoid nested cards.
- Rows expose real app icons, sequential positions, names and concise file details. Selection stays stable in size. Actions support keyboard focus, disabled and hover states.
- The wheel uses neutral segmented surfaces, a strong selected-state boundary and a readable central label in both themes. Selection remains apparent without relying on hue.

## Verification

Render both pages and themes at normal and minimum sizes, empty lists, 12 entries and long names. Inspect the dialog, inline validation, keyboard states and radial labels separately. Preserve all input/focus fixes from v0.1.1. Run regression and Windows CI checks; state exactly which interactive tests were performed.

Research and reusable guidance: [skills/product-ui-craft](skills/product-ui-craft/SKILL.md).
