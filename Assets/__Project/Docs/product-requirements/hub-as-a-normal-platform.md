# The Hub Is Just Another Biome — One Platform, the World's Own Camera & Controls — Product Requirements

> Status: **Corrective brief** (discussed with the product owner, ready for the code track) · 2026-07-06
> Level: product-owner (what & feel). The code track owns the technical "how".
> Track O · O1 **rework**. Supersedes the **scene-assembly / camera / control** parts of
> `hub-staging-and-launch.md` (and its as-implemented `hub-staging.md`). The staging **domain** of
> that brief — the starting-part offer, the independent starting-biome pick, the cauldron voice, and
> the death-return — is **correct and unchanged**; this brief only fixes **how the Hub scene is built
> and controlled**.

## Why this brief exists (the diagnosis)

The Hub was built as a **parallel world**: it re-implements its own platform assembly, its own
camera, and its own hero movement, standing apart from the real world (the Area scene). The result
feels wrong — the camera angle and the hero's movement do not match a normal world platform, and the
scene "works badly".

The root cause to remove is the divergence itself. **The Hub is not a special place that
re-implements the world. The Hub is a normal biome that happens to have exactly one platform.** It
must be indistinguishable from a normal world platform in every way *except its style and its
content*.

## The core requirement (read this first — everything else serves it)

Standing on the Hub must **look and control exactly like standing on any normal world platform**:

- **Same camera** — identical angle, distance, framing, and follow behavior as a normal-world
  platform. No bespoke Hub camera; reuse the world's existing camera system as-is.
- **Same hero movement** — the hero walks the Hub with the **same controller, feel, input, and
  animation** as in the world. No bespoke Hub locomotion.
- **Same platform** — the hero stands on **one** ordinary world platform (the shared hex-surface
  platform: mesh, walkable colliders, perimeter walls). Not a custom mesh, not a re-implemented
  island.

The acceptance test for the whole fix: **if a player could not tell — from camera and controls
alone — whether they are on the Hub or on a normal world platform, the fix is correct.**

## What actually differs on the Hub (the only allowed differences)

1. **Style.** The platform wears the Hub's **own biome look** — its own ground material / dressing.
   Treat the Hub as a **"hub biome"**: authored like any other biome's appearance, not hard-coded.
   Muted junkyard character per the render-look bible.
2. **Content on the platform:**
   - **One NPC figure**, standing **slightly to the left of the platform's center as seen through the
     (world-matching) camera**. "Left" must be correct *for the shared camera's viewpoint* — the old
     divergence is exactly why this was wrong before, so verify it against the real camera, not a
     guessed axis.
   - **Three portals** on the platform — small interaction-radius spots.
3. **No combat, no enemies, no run streaming** on the Hub — it is a calm staging platform.

## User stories

- As a player, the Hub feels like **a place in my world** — same viewpoint, same walking — just a
  different-looking biome with one island, so nothing about moving around it feels off.
- As a player, I walk up to **one keeper NPC** and pressing **F** offers me **three body-part cards**
  to choose my starting part.
- As a player, I see **three labelled portals**; walking up to one shows **its biome's name on the F
  prompt**, and pressing **F** sends me into that biome.

## Functional requirements

### The Hub scene, camera & controls

1. **The Hub is one normal world platform.** It is built and behaves as a standard world platform;
   there is **no parallel Hub platform/camera/movement implementation** left in the codebase.
2. **World-matching camera.** The Hub uses the same camera setup as a normal world platform — same
   angle, framing, and follow — with no Hub-specific camera behavior.
3. **World-matching hero movement.** The hero moves on the Hub with the same controller, input, and
   animation as in the world.
4. **Hub-biome style.** The platform's look (ground material / dressing) is the Hub's own biome
   style, **authored as data**, distinct from the combat biomes — retuning the look is editing that
   data, not code.
5. **A calm platform.** No enemies, combat, or run-streaming run on the Hub.

### The keeper NPC — the starting-part offer

6. **One NPC figure** stands **slightly left of the platform center from the shared camera's view**.
7. **Walk-up + F.** Approaching the NPC shows an **[F] prompt with the NPC's name**; pressing **F**
   opens the **three body-part choice cards** (the existing shared card panel / starting-part offer —
   unchanged domain). Choosing a card commits the run's starting part exactly as today.

### The three portals — enter a biome

8. **Three portal spots** on the platform, each with a **small interaction radius** (a portal you
   walk onto/up to, not a platform-wide trigger).
9. **Labelled by biome.** Each portal is **visibly labelled with its destination biome's name**.
10. **Walk-up + F to launch.** Entering a portal's radius shows an **[F] prompt whose hint text is
    the biome's name** (e.g. `[F] Forest`); pressing **F launches the run into that biome** (the
    existing launch-into-homeland flow — unchanged domain).

## What must be preserved (do NOT regress)

- The starting-part offer (up to three tasted-pool cards + bare launch when nothing is tasted), the
  **independent** starting-biome pick, the **cauldron voice** reacting to the picks, and **death
  returning the player to the Hub** — all shipped and correct (`hub-staging-and-launch.md`). This
  brief does not touch that domain; it only re-hosts it on a proper world-style platform.
- **Journey → Hub → launch**, and the run-scoped (picks) vs meta-persistent (memory) split.

## Acceptance criteria

- On the Hub, **camera angle/framing and hero movement are identical to a normal world platform**;
  **no separate Hub camera or Hub locomotion remains** in the codebase.
- The Hub is **one ordinary world platform** carrying a distinct **hub-biome style** (authored
  appearance, not hard-coded).
- **One NPC** stands slightly **left of center from the shared camera's view**; **[F] → three part
  cards**; choosing installs the starting part.
- **Three portals**, each **labelled with its biome name**, each a **small-radius** walk-up spot;
  **[F] shows the biome name and launches the run into that biome**.
- **No enemies/combat/streaming** on the Hub.
- The starting-part offer, biome independence, cauldron voice, and **death-return-to-Hub** still work.

## Out of scope / open points (do not build now)

- Hub meta-progression, a recurring hub cast beyond the cauldron voice, digs beyond the three parts,
  a fourth starting biome, and final Hub art production — all unchanged from `hub-staging-and-launch.md`.
- The **exact reuse mechanism** (shared scene path vs shared systems) is the code track's call — the
  requirement is the *outcome* (indistinguishable camera & controls, one real platform), not a named
  approach.
