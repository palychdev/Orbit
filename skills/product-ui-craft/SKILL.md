---
name: product-ui-craft
description: Design and refine functional app and web interfaces with deliberate visual hierarchy, product-specific styling, accessible interaction states, and rendered visual verification. Use for UI creation, redesign, and visual polish; not for standalone illustrations or unrelated backend work.
---

# Product UI Craft

Build an interface around the actual task and real content. A polished result is coherent and useful, not merely decorated. Read the project's DESIGN.md and current user preferences first; they outrank this skill's defaults.

## Establish the direction

- Identify the principal task, recurring controls, infrequent options, and content density. Inspect the working product before choosing a layout.
- Turn references into explicit decisions: hierarchy, type, spacing, surfaces, states, and interactions. Record the decisions in a short DESIGN.md so later edits preserve them.
- Use an appropriate primary source for the platform, and inspect real products with a comparable interaction model. A marketing landing page is not a settings utility.
- Prefer the user's requested visual direction. Do not silently convert a request for dark, compact software into a light editorial website, or add features as visual filler.

## Make the primary task obvious

- Order controls by task frequency. Keep rare options in a clearly named secondary area; do not hide essential controls behind unexplained icons.
- Every persistent block must support a real action, decision, or status. Remove decorative dashboards, invented statistics, redundant descriptions, and promotional slogans from utility flows.
- Use grouping and spacing before adding another card. Distinguish a real list, form, toolbar, and dialog instead of drawing the same rounded rectangle around everything.
- Show filenames, labels, counts and units where useful; expose full paths or secondary detail on demand. Never replace useful feedback with silent failure.

## Build a small visual system

- Define semantic theme tokens for canvas, surfaces, hover, selection, text, muted text, separators, accent, danger, and keyboard focus. Use the same tokens for dialogs and overlays.
- Choose typography for the platform, language coverage and density. System typography is often appropriate for desktop software. Do not ban or select a font solely because another agent commonly uses it.
- Use a restrained, documented type scale and spacing rhythm. Reserve strong contrast and solid fills for important actions and selected states.
- Use consistent icon geometry and optical sizing. Icons need accessible names and tooltips when their meaning is not obvious.
- Match corner radii to scale and containment. Avoid excessive floating cards, glow, gradients, glass effects, huge pill buttons, all-caps labels and hairline body text unless the brief provides a functional reason.
- Do not replace one generic template with another. Cream-and-terracotta editorial layouts and monochrome dashboards can be just as formulaic as purple gradients.

## Implement the full interaction

- Make hover, pressed, focused, selected, disabled, recording, empty and error states visually distinct without causing layout shifts.
- Preserve keyboard traversal, visible focus, Enter/Escape behavior and native window controls. Do not sacrifice resizing or accessibility for a screenshot.
- Prefer inline validation near the invalid field. Keep entered values when validation fails. Make destructive actions reversible where practical.
- In theme switching, update text, controls, native title bars, dialogs and floating UI together; persist the choice. Never depend on color alone for selection.
- Use motion only to explain a state change; honor reduced-animation settings. An animation is optional, not a quality requirement.

## Verify and refine

- Render the implemented interface, not a separate mockup. Inspect normal, minimum-size, long-content, empty and dense states, plus every supported theme.
- Check typography, alignment, clipping, icon fallbacks, vertical rhythm and hierarchy. Fix the three most visible weaknesses, render again, then reassess.
- Check text contrast against actual surfaces: aim for at least 4.5:1 for normal text and 3:1 for large text; visible focus and control boundaries also need sufficient contrast.
- Exercise the primary flow and error recovery. Automated tests should verify behavior, state, persistence and meaningful layout invariants, not exact prose or copied styling values.
- Treat screenshot QA, automated tests and real interaction tests as separate evidence. State what was actually checked and any remaining limitations.

For source summaries and reference selection, read [references/design-sources.md](references/design-sources.md). This skill is a reusable workflow, not a claim that the model has permanently trained itself. It grants no additional publishing or account permissions.
