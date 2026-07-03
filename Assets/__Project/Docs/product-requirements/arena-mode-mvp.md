# Arena Mode (Multiplayer) — MVP — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-03
> Level: product-owner (what & feel). The code track owns the technical "how".
> Related: `vision.md` Pillar 4 (fast chess — read the queue, plan, act). Contrast the PvE round model
> in `combat-turn-intent-phase.md` (asymmetric); Arena is the **symmetric** form. Forward-looking design
> background: `/design/arena-mode.md`.

## Goal

Ship a **second, networked game mode** reachable from a main menu, without touching the single-player
campaign. From the menu the player picks **Journey** (the current game) or **Arena** (a networked
free-for-all fight on one platform). In an Arena, several players fight under the **same combat rules as
PvE**, with one change: the ability queue resolves **in the same round for everyone** — each player
plans their round **hidden**, everyone locks in, then all queues **resolve together**. Last hero
standing wins. For this MVP a **default hero** spawns for each player; hero/deck selection is later.

This is a **secondary combat mode** on Pillar 4 only — it deliberately has **no** narrative, mutation,
crafting, NPCs, or director. It reuses the existing combat and the early network scaffold; it does not
add a new progression or economy.

## User stories

- As a player, I **launch the game into a main menu** with two clear modes and pick one.
- As a player, I pick **Journey** and land in the current game exactly as before.
- As a player, I pick **Arena**, **host a match or join one by address**, and a few of us gather on one
  platform, each controlling a hero.
- As a player, **each round I plan my actions without seeing anyone else's plan**; when everyone has
  locked in, **all of our queued actions resolve together** in that round.
- As a player, if I read an opponent right and **step out of where their blow lands, it whiffs** — and
  if we both commit to the same spot, the game resolves it the **same way every time** for a given match.
- As a player, when I'm the **last hero standing, I win**; when I'm knocked out, I'm out of the fight.

## Functional requirements

### Main menu & mode flow
1. **The game boots into a main menu** presenting exactly two modes: **Journey** and **Arena**.
2. **Journey** leads to the **current combat/exploration scene as it is today** — unchanged behavior.
   *(A dedicated Hub the Journey button will eventually open is acknowledged as a future need, not built
   here — for the MVP, Journey goes straight to the current scene.)*
3. **Arena** leads to the **networked Arena scene** (host/join flow below).

### Arena session
4. **Free-for-all, several players** — support **2–4** players in one match on **one arena platform**.
5. **Host / join by direct connect** — one player **hosts**, others **join by address**. **No** lobby,
   matchmaking, ranking, or reconnect in this MVP.
6. **Default hero** — each player spawns the **same default hero** at a distinct start position. **No**
   hero/deck selection yet.

### The round — hidden simultaneous commit → simultaneous resolve
7. **Plan hidden.** Each round, every player builds their queued actions **without seeing any other
   player's queue**.
8. **Lock.** Each player commits their queue for the round.
9. **Resolve together.** Once all surviving players have locked in, **all committed queues resolve in
   the same round** — there is no "player A first, then player B" turn order between players.
10. **Deterministic.** Given the same match seed and the same committed queues, resolution produces the
    **same outcome every time** (preserves the existing reproducibility guarantee).
11. **Whiffs are real.** A committed action whose target has moved or is gone at resolve **fires as
    committed and whiffs** — it does **not** silently re-target to something better. Reading an opponent
    and dodging their committed blow is intended play, not a bug.
12. **Conflicts resolve by a deterministic rule.** Simultaneous conflicts (two heroes entering the same
    hex, mutual blows, an action landing where a unit just left) resolve **deterministically**. *(The
    exact edge rules are a combat-design detail for the code track; the requirement here is only that
    they are symmetric, deterministic, and allow whiffs.)*

### Rules parity & win condition
13. **Abilities are unchanged.** Every ability does exactly what it does in PvE; **only the round timing
    and commitment differ** (hidden + simultaneous instead of the PvE asymmetric plan→act→resolve).
14. **Last hero standing wins.** A player whose hero is defeated is **out** (spectate or leave); the
    match ends when one hero remains.

## Acceptance criteria
- Launching the game shows a **main menu with Journey and Arena**.
- **Journey** loads the current scene with **no behavioral change**.
- **Arena**: a host and **2–3 joiners connect by address** and each spawn a **default hero** on **one
  platform**.
- In a round, **no player can see another player's queue while planning**; after all lock in, **all
  queues resolve together**.
- **Same seed + same committed queues → identical resolution** across host and clients.
- A committed attack **whiffs** (does not re-target) when its target moved away.
- When only one hero remains, that player is declared the **winner**.

## Out of scope / open points (do not build now)
- **Deck of heroes / run-snapshot roster** and **character selection** — a separate later design pass;
  MVP is default-hero only. (Conflict with the campaign's death/reform frame is noted in
  `/design/arena-mode.md`.)
- **PvP-specific balance** and whether Arena bodies are PvE snapshots or a separately-tuned roster.
- **Matchmaking, lobby, ranking, reconnect, late-join.**
- **A dedicated Hub scene** for the Journey branch.
- **FFA variants** beyond last-hero-standing (rounds, teams, scoring), and **spectator polish**.
- **Exact simultaneous conflict / whiff edge rules** — combat-design/code detail, not fixed here.
