# Arena — Per-Client Seat Camera & Oriented Backdrop — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> Track: **G — Arena / Multiplayer Polish** (item **G1**).
> Related: `arena-mode-mvp.md` (the mode + the deterministic round it must not disturb),
> `world-backdrop-and-elevation.md` / `world-backdrop-fill-demo.md` (the backdrop layer this orients),
> `render-look.md` (muted/hazed backdrop treatment), `vision.md` Pillar 4 ("fast chess" — read the board).

## Goal

In **Arena**, each client views the shared arena **from behind its own hero** — the local player's
hero sits at the **near edge** of the board and the player looks **across** the platform, and the
world backdrop sits **behind** the arena from that client's point of view. Every player reads the
board from their own familiar **seat**, like sitting at your own side of a table.

This is **purely client-side presentation**. The authoritative game state — hex coordinates, unit
positions, committed queues, and deterministic resolution — is **identical** on every client; only
the **local camera orientation and the backdrop** differ. Two players in the same match see the
**same board, rotated to their own seat**.

The reference is the *feeling* of Journey (thatgamecompany): your character is anchored where you
expect it, not dropped at a random angle — **not** a literal copy of that game's camera.

## User stories

- As an Arena player, when I spawn, **my hero is at the near (bottom) edge** and I look across the
  board — not from a random or shared angle that belongs to someone else's side.
- As a player, **the world/backdrop is behind the arena from my viewpoint**, on the side it should be.
- As a player, **my view stays put for the whole match** so I can read the queue and plan; it does
  **not** spin around as my hero moves.
- As a player, **my seat is mine**: another player in the same match sees the **same board rotated to
  their seat**, and we still fight on **one shared board**.

## Functional requirements

### Seat orientation
1. **Behind-my-hero framing.** Each client's camera is oriented so the **local player's hero starts
   at the near edge**, looking across the platform toward the far side.
2. **Static for the match** (owner: static seat, **not** a follow/chase camera). The seat orientation
   is set **once at spawn** from the local player's start position and **does not rotate as the hero
   moves**. A hex-tactics board needs a stable frame to read the committed queue and telegraphs
   (Pillar 4); a camera that chases the hero around the platform is disorienting.
3. **Distinct seats per player.** In a **2–4** player FFA on one platform, **each player gets their
   own near-edge orientation** (seats spread ~evenly around the rim) so each reads the board from
   their own side.
4. **Client-side only — game state untouched.** Orientation changes **nothing** about the shared
   state: hex coordinates, unit positions, turn order, committed queues, and the **deterministic
   resolution** are identical across host and every client (preserves `arena-mode-mvp.md` §10). The
   same board is merely **viewed from a different angle** per client.

### Backdrop
5. **Backdrop turns *with* the seat** (owner: rotate-with-seat, **not** a 360° surround). The camera
   and backdrop move together as **one local view rig**; the **arena, platform, and units do not
   move**. The backdrop therefore stays **behind the arena** from the local client's viewpoint.
6. **Reuse the existing backdrop treatment** — the same heavily hazed/desaturated distant low-poly
   backdrop the world already uses (`render-look.md` muted key). **No new backdrop art**; it is the
   same layer, oriented per client.

### Scope
7. **Arena only.** **Journey** (single-player) is **unchanged** — its camera is authored and there is
   a single player.

### Board interaction under the rotated view
8. All board interaction that depends on the camera — **hex hover/targeting highlight, ability
   ghosts, move-destination telegraph, unit facing** — must remain **correct under the rotated seat
   view** (the screen→hex mapping resolves from the local angle). *This is the same seam as the
   Arena hex-highlight defect (**G3**); verify the two together.*

## Acceptance criteria
- On spawn, each client's hero is at the **near edge**; the camera looks **across** the board.
- **2–4 clients** in one match each see the board from **their own seat**; the boards are the same
  game state, rotated.
- The **backdrop is behind the arena for every client** (never off to the side or in front).
- The camera **does not rotate** as the hero moves during the match.
- **Hover/targeting highlight lands on the correct hex** under the rotated view.
- **Same seed + same committed queues → identical resolution** on every client despite the different
  camera angles.

## Out of scope / deferred
- **Follow/chase camera** or path-following yaw — static seat only.
- **360° surround/skybox** backdrop — rotate-with-seat chosen.
- Any change to **Journey's** camera.
- Seat **re-assignment on player drop-out**, spectator-camera polish.
- **Zoom / pan controls, cinematic intro, camera juice.**
- PvP balance, netcode transport (**P4**), and the parts draft (**P4-5**).
