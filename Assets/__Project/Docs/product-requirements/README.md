# Product Requirements — Verified Briefs

This folder holds **product-owner requirement briefs**: feature specs written at a
product-owner level (user stories, what the feature does, how it should feel, acceptance
criteria, content/authoring rules, out-of-scope) **after** they have been discussed and
**verified** with the product owner. Each brief is the source of *intent* handed to the code
track for implementation.

## What these are (and aren't)

- These describe **intended** behavior, before it is built. They are the **exception** to the
  rest of `Assets/__Project/Docs/`, which documents systems **as implemented**.
- They are **not** the `/design` tree. `/design` holds forward-looking *design intent* (world,
  lore, narrative, crafting). A product-requirements brief is the concrete, verified hand-off
  for one change — the "what to build now", not the long-term design vision.
- They are **product-owner level, not technical**: no architecture, class names, or
  implementation steps. The code track derives the technical "how".

## Lifecycle

1. The product owner and Claude discuss a need and verify the requirement.
2. The agreed requirement is committed here as one brief (English, product-owner level).
3. The code track implements it; the **as-implemented** behavior is then documented in the
   relevant system doc per `Assets/__Project/CLAUDE.md §8`.
4. The brief stays as the original record of intent — it is not rewritten to match the code.

## Briefs

| Brief | Scope |
|---|---|
| [Encounter Dialogue UI](encounter-dialogue-ui.md) | Hades-style bottom-centre dialogue box (NPC portrait + name), word-by-word text reveal at reading speed, choice cards (quest / attack / exit) that surface after the line, quest card carries title + objective, author-marked keyword highlighting |
| [NPC Proximity Interaction](npc-proximity-interaction.md) | Replace land-on-platform triggering with proximity: walk into an NPC's interaction radius → **F** prompt → dialogue. NPC intent is **derived from facts** (quest-bearer `?` / hostile `!` / plain) — hostility emerges when the facts leave an NPC no quest to offer *and* it has an enemy behind it (auto-battle on entering the aggro radius), not from an authored flag. Always-visible markers; global radius config with a toggleable dev debug overlay |
| [World Content Density](world-content-density.md) | Stop the director flooding the world with quests. A **rare, breathing world**: four content kinds (empty/traversal, simple loot, ambient aggressive monsters, rare NPC quests) instead of quest-or-void; quests rare (~1 per ~10 platforms), monsters standalone and flat-difficulty, loot low-tier, empty space as deliberate budgeted breath. Designer tunes the whole mix from SO config; deterministic. Content-driven platform size/landscape is a separate later pass |
| [Crafting & Mutation — Socketed Blanks](crafting-mutation-socketed-blanks.md) | Replace "feed the cauldron and hope" with **building the organ**. Two item categories — **Part-Blanks** (skull/tail/wings/…: carry form + species/passport marker + sockets) vs **Artifacts** (function only, no species tag; raw→crafted quality gradient). Two stations: the **cauldron** fuses artifacts (no-failure, signature+emergent), a separate **operating table** sockets artifacts into a blank and **unseals** a **menu of variant mutations** (commit-on-unseal). Puzzle = readable direction + hidden menu + slot interaction (anti-recipe-table). Scarce **Blank Rack** = multi-track tension; traits hidden (archetype color + tier glow; cauldron-voice hints). **Retires the shipped feeding/digestion flow.** Card visuals + biome bias are separate briefs |
| [Mutation Choice Cards](mutation-choice-cards.md) | The **unseal variant menu** of Socketed Blanks, as a hand of cards. **Front visual-first**: body part pictured centre + ability icons beneath (no stat blocks). **Flip** (corner button) → the replaced part + its abilities (before→after on demand). **Hover an ability icon** → description tooltip; **hover the part** → mini-model popover of the hero wearing the new part. Shared card grammar (glow = potency, colour = belonging); crafting traits stay hidden (only abilities shown); picking **commits**. Full live-hero preview + quest-card visuals are separate |
| [Platform — Hex Surface & Shape](platform-hex-surface-and-shape.md) | Make a platform's top **surface = its combat grid** (one source of truth), instead of fitting a hex grid into a random blob at combat time. Surface **composed of whole hex cells**, tiling **muted in traversal / crisp in combat**; **organic decorative rim** beyond the last full cell (natural island silhouette); **per-content-kind size profiles** (Empty/Loot/Combat/NPC) with a **battlefield minimum** for combat platforms, replacing random sizing; biome features **occupy whole cells** (obstacles align). Biome *styling* + multi-platform Site footprints are separate briefs |
| [World Sites & Landscape](world-sites-and-landscape.md) | Content shapes the world (builds on Content Density). Two axes **Biome × Site**; content-first — a settlement-scale beat pulls a **Site** (camp/village/city) or a **Landmark** (ruin/lair) into being, a **cluster of platforms** that reads as one place (connected islands + dressing), while most of the world stays **Wild**. Each site fills its footprint by a **capacity recipe** (anchor + weighted fill + connective) over one shared **base·flavor content vocabulary** (Empty/Loot/Combat/NPC × open flavors; quest & hostility derived). Built to stay **extensible/maintainable** (data-authored sites/flavors, family-default + override, additive attributes). Beta backlog + apex throne + landscape art deferred |
