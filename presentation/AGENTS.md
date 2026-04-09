# AGENTS.md

- This repo is a Slidev deck, not an app. The main entrypoint is `slides.md`; `components/` holds reusable Vue components and `pages/` holds imported slide fragments.
- The presentation is for developers about Event Driven Design. Keep copy tight and prefer diagrams, icons, screenshots, and visual examples over dense text.
- `slides.md` uses Slidev frontmatter and `---` separators. Treat it as the source of truth for theme, slide structure, and imports like `src: ./pages/imported-slides.md`.
- Use `bun`, not npm/yarn/pnpm. The useful commands are `bun install`, `bun run dev`, `bun run build`, and `bun run export`; there is no repo-local lint or test script in `package.json`.
- `.npmrc` enables `shamefully-hoist=true` and `auto-install-peers=true`; keep those settings in mind if dependency resolution looks odd.
- Do not edit generated/build output: `dist/`, `.remote-assets`, `.vite-inspect`, and `components.d.ts` are ignored.
