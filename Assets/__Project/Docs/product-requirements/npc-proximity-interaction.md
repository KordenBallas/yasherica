# NPC Proximity Interaction — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-06-28
> Level: product-owner (what & feel). The code track owns the technical "how".

## Goal

Make approaching an NPC a deliberate, readable act. Today an encounter fires the instant the player
lands on a platform with an NPC — there is no way to walk up without committing, no sense of danger
by distance, and nothing on the NPC that says who is worth talking to. We replace that with
**proximity + a button press**: the player walks up to a friendly NPC and *chooses* to talk by
pressing **F**, while a hostile NPC starts a fight on its own as the player gets close.

Crucially, **hostility is not a separate kind of character** — it *emerges from the world's facts*.
An NPC turns hostile-on-approach when the current fact-state leaves it **no quest to offer the player
and it still has a fight behind it** (e.g. its quest line is closed to the player — the player reads
as the wrong people, or has broken trust with the faction — yet the NPC has an enemy to field). The
same character, under friendlier facts, would offer a quest and be talkable instead.

Floating markers above each NPC tell the player, from across the screen, who has something to offer
(`?`) and who is dangerous (`!`). The distances that drive both behaviours live in one config and can
be seen on screen while tuning.

This changes only **how** an encounter is triggered and how an NPC's current intent is read — what
the dialogue or the battle does once it starts is unchanged.

## User stories

- As a player, when I walk near an NPC that has a quest for me, a clear **F** prompt appears beside
  them, and pressing **F** opens the conversation.
- As a player, I can pass close to an NPC and walk away **without triggering anything**, as long as I
  don't press the button (and they aren't hostile).
- As a player, when I enter a hostile NPC's space, the fight **starts on its own** — no prompt, no
  talking first.
- As a player, I can tell from across the screen who has a **quest** (`?`) and who will **attack**
  (`!`) before I ever get close — and that danger reflects *my* standing: an NPC whose quest line I've
  closed off (wrong people / broken trust) and who has a fight behind it now reads as hostile.
- As a designer, I can change the interaction and aggro distances in one config and **see those
  circles in play mode** to tune them.

## Functional requirements

### NPC intent (derived from facts + content)
Every NPC, at the moment its encounter is placed, falls into exactly one of three intents, **derived**
from the current fact-state — there is no authored "hostile" flag on the character:
1. **Quest-bearer** — the facts let this NPC **offer the player a quest**. Shows a `?`; engaged by the
   F prompt.
2. **Hostile** — the facts leave this NPC with **no quest to offer** *and* it has **a fight available**
   (an enemy is associated with its encounter). Shows a `!`; engaged automatically by approach.
3. **Plain** — neither a quest to offer nor a fight available (an ordinary talkable NPC). No marker;
   engaged by the F prompt.

Intent is evaluated from the facts **as of when the encounter is placed**. If the same character would
offer a quest under different facts, it is a quest-bearer (or plain), **not** hostile.

### Interaction trigger (replaces land-on-platform)
4. Landing on or standing on a platform **no longer** starts a dialogue or a battle. Encounters start
   only via the rules below. Platforms remain only as the spatial stage (e.g. the battle arena).
5. A **quest-bearer** or **plain** NPC has an **interaction radius**. While the player is inside it, an
   **F button prompt** appears near that NPC.
6. Pressing the interact button (**F**) while the prompt is shown starts that NPC's conversation —
   exactly the existing flow (same story, quests, and in-conversation combat triggers).
7. Leaving the interaction radius hides the prompt and starts nothing. If the player is inside more
   than one eligible NPC's radius at once, the prompt targets the **nearest** one.

### Aggro trigger (hostile NPCs)
8. A **hostile** NPC has an **aggro radius**. The moment the player crosses into it, the **battle
   starts immediately** — no F prompt, no dialogue.
9. Hostile NPCs are **not talkable** and show no F prompt. (A talkable NPC can still turn to combat
   *mid-conversation* through the existing in-dialogue path; that is unchanged and is separate from
   approach-aggro.)

### Intent markers
10. A **quest-bearer** shows a floating **`?`** above its head; a **hostile** NPC shows a floating
    **`!`**; a **plain** NPC shows **no marker**.
11. Markers are **always visible** — not gated by the player's distance — and face the camera so they
    read from across the screen.
12. A marker reflects current intent: if the facts change what this NPC offers, the marker matches its
    current state (a quest no longer on offer clears the `?`; an NPC that becomes hostile shows `!`).

### Config & tuning
13. The interaction radius and the aggro radius are **global config values** (a single config asset)
    that apply to all NPCs by default. Per-NPC tuning is out of scope for now.
14. A **dev debug overlay** (toggleable) draws the interaction and aggro circles on the ground around
    NPCs during play, visually distinct from each other, so distances can be tuned by eye. This is a
    development aid, **not** shipped player UX.

## Content authoring rules (how hostility arises)
- Hostility is **never** set on a character directly. A designer creates a hostile-on-approach moment
  by authoring the world so that, under the relevant facts, an NPC's eligible encounter **offers no
  quest but does field an enemy**.
- The usual shape is a **fact-gated story variant**: the "open" variant offers a quest (plays when the
  player is in good standing / reads correctly); a sibling "closed" variant offers **no quest** and
  carries an **enemy** (plays when the player is gated out — wrong people, broken trust). When the
  closed variant is the eligible one, that NPC reads and behaves as hostile.
- An NPC that simply has no quest and no enemy stays **plain** (talkable, no marker). An NPC that has a
  quest stays a **quest-bearer** even if a fight is *also* possible from inside the conversation.

## Acceptance criteria (demo)
- **Quest-bearer (barn-victim):** a `?` floats above it, visible from across the room; walking into its
  interaction radius shows the **F** prompt; pressing **F** opens the dialogue. Walking past without
  pressing F triggers nothing.
- **Hostile by facts:** an NPC whose quest line is closed by the current facts *and* who has an enemy
  behind it shows a `!`; the **battle starts on its own** the instant the player crosses its aggro
  radius — no prompt or dialogue first. Under facts that re-open its quest line, the **same** NPC shows
  `?` and is talkable via **F** instead — demonstrating the intent is derived, not fixed.
- **Plain villager (no quest, no fight):** no floating marker; the F prompt appears in range; **F**
  opens the chat.
- **Debug overlay:** toggling it on shows two distinct circles (interaction vs aggro) around the
  relevant NPCs; changing the config values changes the on-screen circle sizes.

## Out of scope / open points (do not build now)

- Per-archetype / per-NPC radius overrides — global values only for now.
- Player-facing art for the radii — the radii are a **debug overlay** only.
- Any change to what the dialogue or the battle does once it has started.
- ~~Controller/gamepad prompt glyphs beyond the existing input binding~~ — **superseded by
  `cross-device-input-foundation.md`**: the prompt cue now follows the active input source
  (see `Docs/input-foundation.md`). Marker art polish/animation stays out of scope.
- **No-quest-but-talkable-with-optional-fight is not expressible.** Because hostility is derived from
  "no quest available **and** a fight available", an NPC that has no quest yet should remain talkable
  (with an *optional* fight) would be read as hostile and auto-engage. If such a case is ever needed,
  it requires an explicit signal and is deferred until then.
