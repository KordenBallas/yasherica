# Arena — Parts Draft Screen (UI & Presentation) — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-06
> Level: product-owner (what & feel). The code track owns the technical "how".
> **Track G · G4.** Builds on: `arena-part-draft-and-catalog.md` (P4-5 — the draft *model* this
> screen presents) and `arena-mode-mvp.md`. Related: `vision.md` Pillar 4. Background:
> `/design/arena-mode.md`.

## Goal

Give the Arena parts draft (model already decided in **P4-5**) its **screen**: how a player sees
the shared board, takes a part, watches the denial land, and watches their **monster assemble**
before the fight. This is **presentation over an already-decided model** — it adds **no** new draft
rule, catalog logic, or economy. It stays inside the Arena **budget guard**
(`/design/arena-mode.md`): presentation only, Pillar 4 only, nothing that feeds back into Journey.

The draft is the first thing a match shows, so it sets the mode's whole first impression: a fast,
legible, competitive "grab-and-deny" table that ends with a monster you built and can read.

## What P4-5 already fixed (do not relitigate here)

The **model** is settled by `arena-part-draft-and-catalog.md`: one **shared board per match**
(common floor + a sample of the union of participants' catalogs, deterministic per seed), a
**snake-order** turn draft, **denial** (a part taken leaves the board for everyone), draft until
every player fills the standard Arena slots, then the unchanged MVP round runs. This brief presents
that model on screen; it does not change board composition, snake order, slot loadout, or the
catalog. Those stay P4-5 / code-track tuning.

## The one presentation decision this brief makes (owner-locked 2026-07-06)

**Arena parts show their kit openly.** On the draft board and on the assembling monster, a part
**displays the abilities it grants** (icons/labels the player can read while choosing) — Arena is
**pure tactics** (Pillar 4, "read the board"). The campaign's **hidden-trait** economy
(archetype colour + tier-glow only, exact kit concealed) is a *crafting-mystery* mechanic and
**deliberately does not apply to Arena**. Drafting blind would be anti-tactical. (This is the
intentional divergence from `medallion-socket-ui.md` / `mutation-choice-cards.md` concealment.)

## User stories

- As a player, when a match starts I see a **board of parts** laid out clearly by body slot, the
  **same board every other player sees**, and I can tell at a glance **what each part does** (its
  abilities) before I pick.
- As a player, I always know **whose turn it is** to pick and **when it's mine**, and I can see the
  **snake order** so I can plan around the wrap-around.
- As a player, when I take a part it **visibly moves onto my monster** and **leaves the board for
  everyone** — I see denial happen, mine and my opponents'.
- As a player, my **monster assembles in front of me** as I draft — a real **3D hero model** I can
  read — and I **also** see it as a clean **name + parts** list, so by the last pick I already
  understand the body I'm about to fight with.
- As a player, I see **what each opponent is assembling** as **their name + the parts they've
  drafted** (a clean text read, not a cluttered 3D model), so I can plan against their build.
- As a player, I can **click a part's 3D model** to read what it is and its ability, and **hover its
  ability icon** to open a preview that **describes the skill and shows my hero casting it** on a
  battlefield.
- As a player, if someone is slow or drops, the draft **does not hang** — a soft timer keeps it
  moving and fills a missed pick for me so the match still starts.
- As a player, when the draft ends I get a **clear "this is your monster" beat**, then drop into the
  fight under the unchanged Arena round rules.

## Functional requirements

### The board

1. **Shared board, laid out by slot.** Present the P4-5 board as a readable layout **grouped by body
   slot** (head / torso / arms / legs / tail / … per the Arena slot loadout), so a player can find
   the slot they still need to fill. Every client shows the **same** board (P4-5 determinism —
   presentation must not reorder it per client in a way that misleads).
2. **Each part reads at a glance:** its **3D part model** (clickable — see req 11), its **slot**, and
   its **granted abilities shown openly** (per the decision above). Enough to choose on tactics, not
   on mystery.
3. **A taken part is visibly removed** from the board for **all** players the moment it is drafted
   (denial made visible) — it does not linger as a dimmed duplicate that could be re-picked.

### Turn & order

4. **Whose-pick indicator.** Always show **whose turn it is** and **highlight the local player's own
   turn** unmistakably (it is my pick now vs. waiting on someone else).
5. **Snake order is legible.** Show the **draft order** (the seat sequence and that it snakes/wraps),
   so a player can anticipate when their next pick comes and weigh a denial.
6. **Remote picks are shown, not silent.** When another player drafts, the pick **animates** (part
   leaves the board → onto that player's side) so the table stays a shared, readable event.

### Picking & the assembling monster

7. **Pick = one clear interaction** that places the part **into its matching slot** on the local
   player's monster (consistent with P4-5 "parts drafted into their matching slot"). An
   already-filled slot / illegal pick is prevented or clearly rejected, not silently dropped.
8. **Own hero = a 3D model, assembling live.** As the local player drafts, their **hero assembles on
   screen as a real 3D model** (built from the character rig), updating on each pick — anatomy
   telegraphs the kit (Pillar 1/4).

### Reading the field — rosters, parts & abilities

9. **Dual readout for the local player.** Alongside the 3D model, the local player's monster is
   **also listed as hero name + its drafted parts** — the same clean text form used for opponents
   (req 10) — so the player reads their own build both ways.
10. **Opponent readout = name + parts list (owner call).** Each opponent is shown as **hero name +
    the set of body parts they have drafted so far** (updating as they pick), the **clean, legible
    text form** — chosen over an opponent 3D model, which reads more cluttered in FFA. **No opponent
    3D model is required.**
11. **Click a body part → info popover.** The **3D part models** — on the board **and** on the
    assembling hero — are **clickable**: a click opens a popover with that part's **info** and its
    **ability icon(s)**.
12. **Hover an ability icon → the ability-preview popover.** Hovering an ability icon opens a popover
    with the ability's **name + description** **plus a 3D model of the hero on a battlefield casting
    that ability's animation** (reuse the **D3 placeholder ability animations**,
    `combat-round-and-telegraph.md` R15–R18; a **passive** shows an idle pose). This
    **ability-preview popover is a shared grammar** with the mutation choice cards
    (`mutation-choice-cards.md`) — one preview mechanism, two surfaces.

### Flow & robustness

13. **Soft per-pick timer with auto-pick fallback.** Each pick has a **generous soft time limit**;
    if it elapses (slow or AFK player), the draft **auto-picks a legal viable part** for that seat
    and continues — a networked FFA draft must **never hang** on one player. (This is a friends-fun
    guard, not a competitive shot-clock; the exact duration is a tunable.)
14. **Draft-complete beat, then fight.** When every player has filled their slots, show a short
    **"your monster" confirmation beat**, then hand off to the **unchanged Arena MVP round** (hidden
    simultaneous commit → simultaneous resolve; last hero standing wins). The draft screen adds no
    step to the fight itself.
15. **Presentation only — determinism untouched.** The screen **reads** the P4-5 draft state; it
    must not alter board composition, pick legality, or resolution. Same seed + same catalogs + same
    picks → same assembled bodies as P4-5 guarantees.

## Acceptance criteria

- Starting an Arena match opens a **draft screen** showing the shared board **grouped by slot**, with
  **each part's abilities visible**.
- The **local player's turn** is unmistakable; the **snake order** is shown; **remote picks animate**.
- Taking a part **places it on the local monster and removes it from every player's board**.
- The local player's **monster assembles live as a 3D model** and is **also** shown as a **name +
  parts** readout; each **opponent** is shown as **name + drafted parts** (no opponent 3D model).
- **Clicking a body part's 3D model** opens its info + ability icon; **hovering an ability icon**
  opens the **ability-preview popover** — description **plus a 3D hero casting the ability animation**
  (idle pose for a passive) — the **same popover** used by the mutation choice cards.
- A **timed-out pick auto-fills** with a legal viable part and the draft continues — it never hangs.
- On completion, a **"your monster" beat** shows, then the match runs under the **unchanged MVP
  round rules**.
- Given the same seed/catalogs/picks, the assembled bodies match P4-5 exactly (the screen changes
  nothing but presentation).

## Out of scope / open points (do not build now)

- **The draft model itself** — board composition, snake order, slot loadout, the tasted-forms
  catalog, denial rules: all owned by **P4-5** and the code track; this brief only presents them.
- **Draft variants** (auction, blind pick, ban phase) and the **whose-pick / timer tuning numbers**
  — playtest detail.
- **Frame-changing parts in the draft** (skeleton-swap plans) — P4-5 keeps the first draft on the
  base body-plan; the preview presents base-frame monsters only.
- **Cosmetics, prestige, ranking, or any Journey feedback** — the budget guard holds (`arena-mode.md`).
- **Spectator/lobby polish, reconnect during draft** — unchanged from the MVP deferred list.
- **The in-fight seat camera / backdrop** (Track G · G1) and **dev console** (G2) — separate G rows.
- **Opponent 3D models on the draft screen** — the owner chose the **name + parts** readout for
  opponents; per-opponent 3D models are intentionally out.
- **Production ability VFX / animation clips** for the ability-preview popover — it **reuses the D3
  code-authored placeholder animations**; real clips + the always-on in-world live-hero preview stay
  **P5-12** (`ROADMAP.md`).
