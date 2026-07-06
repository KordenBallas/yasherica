# Cauldron View — Spatial Spine & Zone Rule — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Track F · 1/3 — foundational (gates F2 & F3, re-homes P1-3).**

## Goal

Give the inventory/cauldron view a **legible spatial layout** and **one consistent rule** for how an
artifact looks depending on *where it is*, so the whole screen reads as one coherent workbench instead of
a pot with a rack awkwardly bolted to its side.

Today the open inventory is a cauldron with artifacts as **bubbles** floating in it
(`inventory-subsystem.md` R10), a **crafting zone above the pot** (staging slots + result, R14–R17), and
the socketing **Blank Rack shoved to the left** of the cauldron (`mutation-subsystem.md` §2.3,
flagged as visually weak in `medallion-socket-ui.md`). There is no single principle tying the zones
together, and no rule for when an artifact wears a bubble.

This brief establishes the **vertical three-zone spine** and the **bubble = submerged** rule. It is
**layout + a presentation rule** over shipped systems; it does not change crafting, socketing, or what an
artifact/blank *is*.

## Reference image (shared Track F mood/layout board)

`references/cauldron-view-reference.png` (in this folder) is the product owner's target composition for
the **whole** cauldron view. It illustrates all three Track F briefs at once — treat it as the shared
board, not F1-only.

**What it reads as, mapped to this brief's zones:**

- **Top** — two source reagents (a green serpent, a purple lizard) feeding an octagonal medallion-result
  on beams of light = the **crafting zone**: staged inputs + the forming fusion result, all **bare**.
- **Centre** — a green brew holding six artifacts, **each inside its own bubble** = the **brew**, the
  `bubble = submerged` rule in action.
- **Bottom** — a **horizontal ribbon of six medallions**, each holding a body-part with gems around its
  rim = the **medallion ribbon** (re-homed rack, FR4); the medallion's own art is F4 /
  `medallion-socket-ui.md`.

**Normative (build to this):** the vertical three-zone stack, the zones' relative positions, one
artifact per bubble in the brew, the bare crafting/medallion zones, and the ribbon as a horizontal row
under the pot.

**NOT normative — mood only, owned elsewhere (do not build from the image):**

- The **cave/organic backdrop** → stomach-interior backdrop, **P5-5** (not this diorama's cave).
- The **ornate pot mesh** and rune-carved feet → the "prettier cauldron mesh" art item (**F6**); this
  brief assumes the current mesh.
- The **liquid surface, glow, and fill level** → **F2** (`cauldron-liquid-and-fullness.md`). The image's
  **fully-submerged bubbles under a translucent surface are normative** (F2 FR2 — the pot fills with bubbles
  bottom-up, the waterline rises above them); only the surface *look/shader* is F2's to detail.
- **Exact artifact icons, medallion art, palette, and the six-count** → illustrative placeholders; real
  content is authored and variable (not fixed at six).

## The spatial spine (top → bottom)

One vertical reading of the beast's interior:

- **Top — the crafting zone.** Staging slots + the fusion result hover **above the brew** (unchanged
  position). Artifacts here are **bare** (no bubble) — "in hand, above the surface".
- **Centre — the brew (cauldron).** Artifacts held in the inventory are **suspended in the liquid** and
  wear a **bubble**. This is the pot's interior.
- **Bottom — the medallion ribbon.** The socketed Part-Blanks (the operating table) sit as a **horizontal
  ribbon of medallions under the cauldron**, re-homed from the left rack. Blanks/medallions are **bare**
  (they are on the table, not in the brew).

So the eye travels **incubating organs (bottom) → brewing reagents (centre, in liquid) → crafting &
result (top)**.

## The one rule — bubble = "in the brew"

- An artifact wears a **bubble only while it is suspended in the cauldron liquid**.
- **Everywhere else it is bare**: in a staging/result slot above the pot, and while socketed into a
  medallion below it.
- The bubble therefore doubles as a **state signal** — "this artifact is currently in the inventory pot"
  vs "this one is staged / socketed". The transitions become self-explanatory: drag a bare artifact down
  into the brew → it **gains a bubble** as it enters the liquid; pull one up to a staging slot or into a
  medallion socket → the **bubble pops**.

## User stories

- As a player, I read the pot view top-to-bottom as one bench: **stuff I'm crafting up top, my brew in
  the middle, the organs I'm building along the bottom**.
- As a player, an artifact in a **bubble** always means "this is in my pot"; when I pull it out to craft or
  socket it, the **bubble is gone** — I never confuse a staged item for a pot item.
- As a player, the blanks I'm socketing sit in a **clear ribbon beneath the cauldron**, not off to one
  awkward side.
- As a designer, I author nothing new — the zones and the bubble rule are presentation over the existing
  inventory, crafting, and socketing content.

## Functional requirements

1. **Three-zone vertical layout.** The view is composed as **crafting zone (top) · brew (centre) ·
   medallion ribbon (bottom)**, all reading as one continuous interior.
2. **Bubble = submerged.** An artifact is rendered **inside a bubble if and only if it is suspended in the
   cauldron liquid**. Staged artifacts, the fusion result, and artifacts socketed into a medallion are
   rendered **bare** (no bubble).
3. **Legible transitions.** Moving an artifact **into** the brew makes it **gain a bubble**; moving it
   **out** (to a staging slot or a medallion socket) **removes** the bubble. The change is visible, not
   instantaneous-teleport, so the player reads the state change.
4. **Medallion ribbon under the cauldron.** The socketed Part-Blanks are presented as a **horizontal
   ribbon of medallions positioned beneath the cauldron**, replacing the current left-of-cauldron rack.
   This is the **deliberate placement** `medallion-socket-ui.md` FR7 asks for; the medallion's own
   presentation (part centred, sockets as rim gems, rim = progress, confirm-before-unseal) is that brief's
   job and is unchanged.
5. **Ribbon anchors to the frame, not the pot.** The ribbon (and the crafting zone) hold a **stable
   screen position** independent of the brew's fill level, so they never jump as the pot's liquid line
   changes (see the fullness brief, F2).
6. **No behaviour change.** Crafting (stage → merge → result), socketing (drop artifact into a blank
   socket → ripen → unseal), and open/close orchestration keep their current behaviour; only the artifact's
   *appearance by zone* and the rack's *position* change.

## Content authoring rules (for the designer)

- None new. The spine and the bubble rule are pure presentation over existing artifact, blank, and recipe
  content.

## Acceptance criteria

- The open inventory reads as three stacked zones: crafting (top), brew (centre), medallion ribbon
  (bottom).
- An artifact in the pot shows a bubble; the same artifact, once staged or socketed, shows **no** bubble;
  dragging it across the liquid line visibly adds/removes the bubble.
- Part-Blank medallions appear in a horizontal ribbon **under** the cauldron, not to its left.
- The ribbon and crafting zone stay put on screen regardless of the brew's fill level.
- No change to crafting/socketing outcomes or to the open/close flow.

## Out of scope / open points (do not build now)

- **The liquid surface, the "bubbles pierce the surface" read, and the fullness = fill-level cue** — the
  **F2** brief (`cauldron-liquid-and-fullness.md`).
- **Stable per-artifact bubble placement, drop-in physics, and the continuous result flow** — the **F3**
  brief (`cauldron-brew-layout-and-physics.md`).
- **The medallion's internal presentation** (rim/gems/progress/confirm) — `medallion-socket-ui.md`
  (P1-3), which this brief only **re-homes**.
- **The stomach-interior backdrop** behind the diorama — P5-5 / `render-look.md` §5.
- **The upgraded cauldron mesh** ("make the model prettier") — the Track F art item.
- Exact ribbon art, spacing, and scroll/overflow behaviour when many blanks are incubated — tech-art /
  code-track's call.
- Implementation specifics (View/Presenter wiring) — the code track's call.
