# NPC Proximity Interaction — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-06-28
> Level: product-owner (what & feel). The code track owns the technical "how".

## Goal

Make approaching an NPC a deliberate, readable act. Today an encounter fires the instant the player
lands on a platform with an NPC — there is no way to walk up without committing, no sense of danger
by distance, and nothing on the NPC that says who is worth talking to. We replace that with
**proximity + a button press**: the player walks up to a friendly NPC and *chooses* to talk by
pressing **F**, while hostile NPCs start a fight on their own as the player gets close.

Floating markers above each NPC tell the player, from across the screen, who has something to offer
(`?`) and who is dangerous (`!`). The distances that drive both behaviours live in one config and can
be seen on screen while tuning.

This changes only **how** an encounter is triggered — what the dialogue or the battle does once it
starts is unchanged.

## User stories

- As a player, when I walk near a friendly/quest NPC, a clear **F** prompt appears beside them, and
  pressing **F** opens the conversation.
- As a player, I can pass close to an NPC and walk away **without triggering anything**, as long as I
  don't press the button (and they aren't hostile).
- As a player, when I enter a hostile NPC's space, the fight **starts on its own** — no prompt, no
  talking first.
- As a player, I can tell from across the screen who has a **quest** (`?`) and who will **attack**
  (`!`) before I ever get close.
- As a designer, I can change the interaction and aggro distances in one config and **see those
  circles in play mode** to tune them.

## Functional requirements

### Interaction trigger (replaces land-on-platform)
1. Landing on or standing on a platform **no longer** starts a dialogue or a battle. Encounters start
   only via the rules below. Platforms remain only as the spatial stage (e.g. the battle arena).
2. Each NPC has an **interaction radius** around it. While the player is inside it, an **F button
   prompt** appears near that NPC.
3. Pressing the interact button (**F**) while the prompt is shown starts that NPC's conversation —
   exactly the existing flow (same story, quests, and in-conversation combat triggers).
4. Leaving the interaction radius hides the prompt and starts nothing. If the player is inside more
   than one NPC's radius at once, the prompt targets the **nearest** eligible NPC.

### Aggro trigger (hostile NPCs)
5. A hostile NPC has an **aggro radius**. The moment the player crosses into it, the **battle starts
   immediately** — no F prompt, no dialogue.
6. Hostile NPCs are **not talkable** and show no F prompt. (A friendly NPC can still turn hostile
   mid-conversation through the existing in-dialogue combat trigger; that path is unchanged.)

### Intent markers
7. An NPC that currently offers a quest shows a floating **`?`** above its head.
8. A hostile NPC shows a floating **`!`** above its head.
9. Markers are **always visible** — not gated by the player's distance — and face the camera so they
   read from across the screen. A plain talkable NPC with no quest shows **no marker**; only the F
   prompt when the player is in range.
10. A marker reflects current state: once a quest is no longer on offer, the `?` clears.

### Config & tuning
11. The interaction radius and the aggro radius are **global config values** (a single config asset)
    that apply to all NPCs by default. Per-NPC tuning is out of scope for now.
12. A **dev debug overlay** (toggleable) draws the interaction and aggro circles on the ground around
    NPCs during play, visually distinct from each other, so distances can be tuned by eye. This is a
    development aid, **not** shipped player UX.

## Acceptance criteria (demo)

Using the existing demo NPCs:
- **Quest NPC (barn-victim):** a `?` floats above it, visible from across the room; walking into its
  interaction radius shows the **F** prompt; pressing **F** opens the dialogue. Walking past without
  pressing F triggers nothing.
- **Hostile NPC (raider):** a `!` floats above it; the **battle starts on its own** the instant the
  player crosses its aggro radius — no prompt or dialogue appears first.
- **Plain villager (no quest):** no floating marker; the F prompt appears in range; **F** opens the
  chat.
- **Debug overlay:** toggling it on shows two distinct circles (interaction vs aggro) around the
  relevant NPCs; changing the config values changes the on-screen circle sizes.

## Out of scope (deferred, do not build now)

- Per-archetype / per-NPC radius overrides — global values only for now.
- Player-facing art for the radii — the radii are a **debug overlay** only.
- Any change to what the dialogue or the battle does once it has started.
- Controller/gamepad prompt glyphs beyond the existing input binding, and marker art polish or
  animation.
