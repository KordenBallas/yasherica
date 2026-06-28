# Encounter Dialogue UI — Product Requirements

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-06-28
> Level: product-owner (what & feel). The code track owns the technical "how".

## Goal

Make NPC conversations read and feel like Hades: a personable bottom-of-screen dialogue box
with the speaker's face and name, lines that "speak" themselves out at reading pace, and
clear, prize-like choice cards that float in once the NPC has finished talking. Key names
glow in the text so the player tracks who matters.

This is a presentation/feel upgrade to the existing conversation flow — the underlying
conversation logic, quests, and combat triggers are unchanged.

## User stories

- As a player, I see who is talking: their portrait and name sit in a dialogue box at the
  bottom-centre of the screen, so the speaker is always obvious.
- As a player, the NPC's line *types itself out* word by word at a comfortable reading speed,
  so it feels like they're speaking to me rather than dumping text.
- As a player, if I'm impatient I can tap to instantly finish the current line.
- As a player, once the line is finished, my options appear as cards centred just above the
  dialogue box, so I act only after I've heard what was said.
- As a player, I get up to three kinds of cards: a **quest** card, an **attack** card, and an
  **exit** card — and the quest card tells me what the job actually is before I commit.
- As a player, important names (the NPC, people they mention) stand out — highlighted — in both
  the spoken line and on the cards, so I notice who and what the story is about.

## Functional requirements

### Dialogue box (the speaker)
1. A dialogue box is anchored at the **bottom-centre** of the screen for the whole conversation.
2. It shows the **NPC portrait** and the **NPC name**, plus the current line of text.
3. The portrait comes from the NPC's own identity (each NPC kind already has a portrait image).
   If an NPC has no portrait, the box still works (name + text) and shows a neutral placeholder.

### Text reveal (typing)
4. Each line is revealed **word by word** (whole words appear, never letter-by-letter) at a
   comfortable, tunable **reading speed**.
5. A tap/click while a line is still revealing **immediately completes** that line.
6. Choices do **not** appear until the current line has fully finished revealing — the player
   always hears the full line before being asked to choose.

### Choice cards
7. When choices are available, they appear as cards **centred, above the dialogue box**.
8. The hand can contain up to three card kinds:
   - **Quest card** — the offer/talk option, carrying quest information (below).
   - **Attack card** — present when fighting this NPC is possible.
   - **Exit card** — always available to end the conversation.
9. The **quest card is labelled with the job**: it shows the quest's **title** and its
   **objective/summary** (what the player is being asked to do), not just a one-line choice.
10. Cards visually distinguish the three kinds at a glance (e.g. quest vs attack vs exit read
    differently). Per-tier *glow* on quest cards is **out of scope for now** (see Out of scope).

### Keyword highlighting
11. Designated **key words — primarily NPC names** — are **highlighted** (visually emphasised)
    wherever they appear, in both the spoken line and the card text.
12. Highlighting is **author-controlled**: writers mark a key word in the story script by
    wrapping it, e.g. `[[Garrick]]`. Marked words render highlighted; the markers themselves are
    never shown to the player. Unmarked words render normally.

## Content authoring rules (for writers)
- To highlight a key word, wrap it in double brackets in the line or choice text: `[[Mira]]`.
- A quest card automatically shows the title and objective of the quest attached to that
  encounter — writers don't re-type the objective into the choice text.
- Reading speed is a single global setting, not authored per line.

## Acceptance criteria (demo: barn-victim NPC)
- Walking into the NPC opens a bottom-centre box with their portrait and name.
- The line types out word by word; a marked name (e.g. `[[name]]`) appears highlighted.
- Tapping mid-line fills the rest of the line instantly.
- After the line finishes, cards appear centred above the box: a quest card showing the job's
  title + objective, an exit card, and an attack card when a fight is possible.
- A highlighted name on the quest card reads the same way as in the line.
- Picking a card behaves exactly as before (accept quest / fight / leave).

## Out of scope (deferred, do not build now)
- Per-tier **glow / rarity colour** on quest cards — waits on the crafting tier model.
- Multiple distinct quest cards from a single NPC — current design is one quest per encounter.
- Automatic name detection — highlighting is only via the author's `[[ ]]` markers.
- Emotion/portrait changes mid-conversation — one portrait per NPC for now.
