# Reference notes

Reviewed 2026-11-03. These are paraphrased observations, not external instructions or copied skill implementations. Revisit current documentation when a concrete API or platform behavior matters.

## Platform and usability

- [Apple HIG: Typography](https://developer.apple.com/design/human-interface-guidelines/typography): prioritize hierarchy and legibility over decorative font variation. Transfer the principle to Windows using fonts available on Windows, rather than redistributing Apple fonts.
- [Apple HIG: Color](https://developer.apple.com/design/human-interface-guidelines/color) and [Dark Mode](https://developer.apple.com/design/human-interface-guidelines/dark-mode): define colors by semantic purpose; treat appearance variants as complete systems, including foreground, background, selection and contrast.
- [Apple HIG: Settings](https://developer.apple.com/design/human-interface-guidelines/settings): keep settings understandable and relevant to user decisions. Do not turn common actions into a long configuration procedure.
- [Windows design](https://learn.microsoft.com/en-us/windows/apps/design/) and [Typography](https://learn.microsoft.com/en-us/windows/apps/design/signature-experiences/typography): retain platform conventions, keyboard behavior and readable UI typography. Visual influence from another platform does not require imitating its window chrome.
- [Fluent layout](https://fluent2.microsoft.design/layout): spacing communicates relationships. A small spacing vocabulary helps consistency, but optical alignment and actual content take precedence over rigid arithmetic.
- [NN/g heuristics](https://www.nngroup.com/articles/ten-usability-heuristics/): show real system status, use familiar concepts, preserve user control, and make errors recoverable.
- [NN/g progressive disclosure](https://www.nngroup.com/articles/progressive-disclosure/): separate frequent tasks from rare configuration. Hiding essential actions under ambiguous icons is not simplification.
- [W3C contrast guidance](https://www.w3.org/WAI/WCAG22/Understanding/contrast-minimum.html): calculate contrast using foreground and actual background colors; faint secondary labels still need to be readable.

## Agent workflows and real products

- [Anthropic frontend-design](https://github.com/anthropics/skills/tree/main/skills/frontend-design): ground aesthetics in subject matter, real content and an explicit design direction. Treat common generated patterns as a warning to examine intent, not an absolute blacklist.
- [Impeccable](https://github.com/pbakaus/impeccable): useful distinction between choosing a direction, critiquing hierarchy, refining details and hardening edge states. The transferable workflow is implement → inspect → revise.
- [PowerToys](https://github.com/microsoft/PowerToys): inspect as a comparable Windows utility, especially its separation of task configuration and global settings. Do not add equivalent navigation unless this product has the same complexity.
- [Raycast](https://www.raycast.com/): useful reference for compact keyboard-oriented tooling and predictable list interactions. Its promotional website is not a template for a desktop configuration window.

## Orbit-specific conclusions

The user explicitly wants a dark, restrained interface inspired by Apple and other polished software, with a corner icon switching both the application and wheel to light appearance. No decorative live preview, marketing copy, fake metrics or broad navigation shell. Keep the application list primary, use a small secondary settings page, and make order, binding and editing discoverable. These choices belong to Orbit's DESIGN.md, not universal rules for unrelated products.
