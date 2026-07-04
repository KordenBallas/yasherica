# Independent Body-Plans & Skeleton-Swap Runtime — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-04
> Level: product-owner (what & feel). The code track owns the technical "how".
> Fuller design background — do not restate it: `design/characters/model.md` (the independent
> body-plans model) and `vision.md` Pillar 1 ("you build your own monster"). The **authoring
> contract** for a part (slot / size / sockets / skeleton family / rarity re-texture) is the
> sibling brief `part-authoring.md` — this brief is the **runtime** half.
> This is the roadmap item **P2-1**. It **supersedes** `Docs/character-system.md` R10/R3 (single
> fixed bone-superset, "a swap never touches the Animator").

## Goal

Let the hero take on **structurally different bodies** — not just a different appendage bolted onto
one humanoid frame, but a whole different **body plan** (a legless serpent, a quadruped, a heavy
flyer) — so its silhouette reads **visibly different from run to run** (Pillar 1).

Today every hero is variations of **one** frame: the rig carries a full bone superset (tail, wings,
ears all present at once) and a part swap only re-skins onto it; a part built for a different
skeleton is rejected as a validation error (`character-system.md` R3/R4/R10). That caps every hero
to "bipedal + optional tail/wings". This brief lifts that cap: a special part can **pull in its own
skeleton**, and the body **re-forms around it at runtime**.

The change is deliberately **rare**: ordinary parts (~80% of them) still ride one **base rig** and
swap cheaply exactly as today. A **body-plan change** happens only when a special, skeleton-changing
part is installed or removed — the uncommon case — and it is an honest, felt event, not a silent
re-skin.

## User stories

- As a player, installing a special part (a serpent-spine, a second pair of limbs) can **reshape my
  whole frame**, not just add a limb — my creature stops looking like "the same guy with a new tail"
  and becomes a **different animal**.
- As a player, when a new frame has **nowhere to attach** parts I'm wearing (my legs, once I become a
  legless serpent), the game **tells me which parts will come off before I commit**, and lets me back
  out — the reshaping is a real decision, not a surprise amputation.
- As a player, the parts shed by a body-plan change are **not destroyed** — they go **back to my
  inventory**, so reshaping my frame costs me the *slots*, not the parts themselves.
- As a player, ordinary part swaps (a new head, a new arm on the same frame) stay **instant and
  seamless** exactly as before — the heavy reshaping only happens on the rare frame-changing part.
- As a designer, I can introduce a **new body plan** as data (a new skeleton + parts authored for it)
  and mark a part as **frame-changing with a priority**, **without writing code**.

## Functional requirements

### The base frame and the exception
1. **A base body plan** carries the common case: most parts belong to it and swap on it cheaply and
   seamlessly (the shipped behaviour — no reshaping, no interruption).
2. **A skeleton-changing part** declares that it **governs the body plan** when equipped. Installing
   one makes **its** frame the governing frame; removing it returns governance to the base (or to the
   next-highest remaining frame-changing part).

### Resolving which frame governs
3. When several equipped parts each want to govern the frame, the **highest-priority** frame-changing
   part wins. The outcome is **deterministic** — the same equipped body always resolves to the same
   governing frame. Priority is **authored data** on the part.
4. A part authored for one body plan is **not assumed to fit** another. Cross-frame compatibility is
   **not** an authoring promise — plans are independent, not supersets of a shared core.

### Changing the frame
5. A **body-plan change** re-forms the body around the new governing frame so the character keeps
   **playing/animating correctly** on it (no T-pose, no broken pose). This is the rare, heavier event
   contrasted with an ordinary same-frame part swap.
6. When the new frame has **no attach point** for a currently-equipped part (legs on a legless
   serpent), that part is **removed from the body** as part of the change.
7. **Confirm before committing.** Before a body-plan change that would shed one or more equipped
   parts, the player is **shown which parts will come off** and must **confirm**; declining leaves the
   body unchanged (the frame-changing part is not installed). A frame change that sheds **nothing**
   needs no prompt.
8. **Shed parts return to the player's inventory** (they are not destroyed), available to re-install
   on any future frame that can carry them.

### First shippable scope
9. Ship the **full runtime machinery** — governing-frame resolution, the re-forming of the body on a
   frame change, and the confirm-and-shed flow — proven end-to-end with **two** demonstrator special
   frames chosen to exercise the machinery from both sides and to make every requirement testable:
   - a **legless serpent** — a frame that **removes** attach points (it orphans **both legs** → the
     cleanest test of the shed/return-to-inventory flow), and the sharpest silhouette break from base;
   - a **spider-legged** frame — a frame that **adds** attach points (a radial cluster of many thin
     legs where the base's two legs were), testing the opposite direction: new attach points appear
     and the base's Leg-L/Leg-R parts, having nowhere to sit on the radial frame, are shed.
   Two coexisting frames also make **priority resolution (FR3) testable with real content**: equip a
   serpent-puller and a spider-puller at once and the **higher-priority** frame must govern
   deterministically while the loser's incompatible parts shed. The system **must not be hard-coded**
   to "base + serpent + spider" — further plans (quadruped, flyer) are authored later on the same
   machinery, as data.
10. **The two demonstrator frames are code-track placeholder content, not blocked on production art.**
    The serpent and spider frames (their rig/skeleton + placeholder parts that pull them) are authored
    by the **code track** as test frames, so P2-1 can be built and proven without waiting on the
    designer's production meshes. Production-quality frame meshes come later (roadmap P5-6), authored to
    `part-authoring.md`.

### Robustness
11. **Determinism.** Given the same equipped parts (and the same install/remove action), the governing
    frame, the set of shed parts, and the resulting body are always the same.

## Content authoring rules (for the designer)
- Ordinary parts declare the **base** body plan and need nothing new — they ride the base rig.
- A **new body plan** is authored as data: a new skeleton (frame) plus parts built for it, following
  the authoring contract (`part-authoring.md`).
- A part that should **reshape the frame** is marked **skeleton-changing** and given a **priority**
  (higher wins when two frame-changers are equipped at once).
- **The two first demonstrator frames — legless serpent and spider-legged — are authored by the code
  track as placeholder test frames** (see FR10), not by the designer; the designer's job on this axis
  is the later **production** frame meshes (P5-6) to this same contract.
- Adding a body plan or a frame-changing part is **data only** — no code change.

## Acceptance criteria
- Installing a **serpent** frame-changing part on a legged hero **reshapes the body** to the serpent
  plan and the character continues to **animate correctly** on the new frame.
- That install first **shows the legs will be shed and asks to confirm**; **declining** leaves the
  hero legged and un-changed; **confirming** removes the legs, and the **legs are found back in the
  inventory** afterward.
- Installing a **spider-legged** frame-changing part likewise reshapes the body to the spider plan, the
  character animates correctly on the added radial legs, and the base **Leg-L/Leg-R parts are shed to
  inventory** (same confirm-and-return flow) since they have nowhere to sit on the radial frame.
- An **ordinary part swap on the same frame** stays **instant and seamless** (no confirm, no reshape)
  — unchanged from today.
- With a **serpent-puller and a spider-puller equipped at once**, the **higher-priority** frame governs
  — **deterministically** and repeatably — and the loser's incompatible parts shed.
- **Adding a new body plan / frame-changing part is data-only**; the same equipped body always yields
  the same governing frame and the same shed set.

## Out of scope / open points (do not build now)
- **Production-quality frame meshes** — the serpent and spider ship as **code-track placeholder** frames
  (FR10); the polished production meshes for any body plan are the designer's later art pass (P5-6),
  authored to `part-authoring.md`. This brief needs the runtime plus the *two* placeholder plans to
  prove it, not a library of finished art.
- **More than the two demonstrator plans** (quadruped, winged flyer, further extra-limb frames) —
  authored later on this same machinery; only serpent + spider ship first.
- **Per-race body plans / signature race silhouettes.** The three starting races (Ibex / Lizard / Fox,
  see `race-roster-and-passport.md`) are all **base-frame** beastfolk — they need **no** special
  skeletons. Frame-changing plans come from the monstrous mutation side, not the passport races.
- **Passport reading of a radically different frame.** No special rule is needed: acceptance is a
  **count of race-tagged parts worn** (`race-roster-and-passport.md`), so a frame that can carry fewer
  parts simply reads as fewer markers — the finite-slot economy already **is** the passport tension.
  Left as an explicit non-requirement, not an open design question.
- **Per-rarity texture/material variants of one mesh** (common green vs legendary re-texture) — a
  visual axis noted in `design/characters/model.md` and `part-authoring.md` FR8–9; a separate, smaller
  presentation item, not part of this runtime brief.
- **Procedural part-mesh generation tooling** — the pipeline that authors parts to the contract is a
  production/tooling track (roadmap P5-4 / P5-6), not this brief.
- **Cross-run persistence** of the current frame / shed parts — rides the general save/load work
  (P2-2), not here.
