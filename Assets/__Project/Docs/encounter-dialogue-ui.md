# Encounter Dialogue UI — Requirements & Design

> The Hades-style presentation layer for NPC encounters: a bottom-centre dialogue box with the
> speaker's portrait + name, the current line revealed **word by word** at a reading pace, choice
> **cards** (quest / attack / exit) that appear once the line finishes, and author-marked key words
> (`[[ ]]`) highlighted in both lines and cards. It is a presentation/feel layer over the unchanged
> conversation engine (`DialogueRunner`, facts, quests, combat — see `narrative-procedural.md`).
> Status: current as of 2026-07-07 (Track H — reward telegraph + several offers).
>
> This document describes the system **as implemented**. If code and this document disagree, this
> document is outdated and must be fixed. Planned behavior lives only in §6.
>
> ⚠ **Verification status (2026-07-07):** the Track-H additions (mystery reward slot, hover-inspect,
> several offers) are code-complete and edit-mode green, but the offer card's **on-screen feel is not
> yet play-tested** by the owner (tracked in ROADMAP "Quests").

---

## 1. Requirements

Product brief: `Docs/product-requirements/encounter-dialogue-ui.md` (Verified). Requirement ids
below mirror that brief.

### 1.1 Functional requirements

- **R1** A dialogue box is anchored bottom-centre for the whole conversation.
- **R2** It shows the NPC portrait and name plus the current line.
- **R3** The portrait comes from the NPC's archetype identity; a portrait-less NPC falls back to a
  neutral placeholder and the box still works (name + text).
- **R4** Each line is revealed **word by word** (whole words, never letter-by-letter) at a tunable,
  global reading speed.
- **R5** A tap while a line is revealing immediately completes that line.
- **R6** Choices do not appear/act until the current line has fully revealed; once it has, the choice
  cards appear **automatically** — there is no Continue button. Picking a quest/talk card shows the
  branch's closing reply, which a tap then dismisses to close the box.
- **R7** When choices are available they appear as cards centred above the box.
- **R8** The hand holds card kinds: **quest** (the offer), **attack** (when fighting is possible),
  **exit** (always). A story may present **several quest cards at once** (P1-9) — several resolutions
  of one situation, side by side — each labelled with its own offer.
- **R9** The quest card is labelled with the **job**: the quest's title + objective/summary, not just
  the reply text.
- **R10** A quest card that declares a reward shows a **mystery reward slot** (P1-6): the item is
  hidden as a `?`, the slot **glows by the declared tier**, and a chip is **tinted by the belonging
  colour** (race for a Part-Blank reward, reward-family for an artifact — resolved through
  `IBelongingTintCatalog`). The exact item is never shown. Attack/Leave cards render only their label.
- **R11** **Inspect on hover** (P1-6): hovering a quest card swaps its summary line for the fuller
  job detail (objectives + giver); the **reward stays hidden**. The face is terse at a glance.
- **R12** Author-marked key words — primarily NPC names — are highlighted wherever they appear, in
  the spoken line and on the cards.
- **R13** Highlighting is author-controlled: a key word is wrapped `[[like this]]`; the markers are
  never shown to the player; unmarked words render normally.

### 1.2 Non-functional requirements

- **N1** The keyword formatter is pure C# (no UnityEngine) and unit-tested; the presenter stays
  UnityEngine-free (it passes an archetype **id**, not a `Sprite`).
- **N2** All dependencies wired through Zenject; the view resolves the portrait via the already-bound
  `INpcArchetypeCatalog`.
- **N3** The conversation engine is unchanged — only presentation + the data the view reads.

---

## 2. Architecture

### 2.1 Layer map

```
Scripts/Narrative/Encounter/
  KeywordHighlightFormatter.cs   — pure C#: [[word]] -> <color> rich text (N1)
  EncounterCardViewData.cs       — view DTO (+ QuestTitle/Objective/Detail + reward telegraph)
  EncounterCardType.cs           — QuestOffer / Attack / Leave / Talk
  IEncounterCardHandView.cs      — view contract (+ SetPortrait)
  EncounterCardHandPresenter.cs  — pure-C# MVP presenter (composition + chrome + per-offer resolve)
Scripts/Narrative/View/
  EncounterCardHandView.cs       — MonoBehaviour: box, TMP reveal, portrait, tap; resolves belonging tint
  EncounterCardView.cs           — MonoBehaviour: one card (title + objective + mystery reward slot + inspect)
  {BelongingTintCatalog,IBelongingTintCatalog}.cs — belonging id -> authored colour (P0-3·b)
Scripts/Core/DI/NarrativeSliceInstaller.cs   — binds view + presenter + belonging tint catalog
```

This sits on top of §2.7 of `narrative-procedural.md` (the encounter card-hand). It keeps that
presenter/view seam and the runner unchanged; it adds the box chrome (portrait, word-by-word reveal),
the quest-card job text, and keyword highlighting.

### 2.2 Core domain types

| Type | Responsibility |
|---|---|
| `KeywordHighlightFormatter` | Static pure-C# `ToRichText(raw, hexColor)`: wraps each `[[word]]` in a TMP `<color=#hex>` span and strips the markers; unmarked text and an unterminated `[[` pass through verbatim; `[[]]` yields nothing. Used by both the line view and the card view so a key word reads the same everywhere. |
| `EncounterCardViewData` | View DTO. `QuestTitle` / `QuestObjective` / `QuestDetail` (quest cards only) plus the reward telegraph: `HasRewardTelegraph`, `RewardTier`, `BelongingId`. The rolled item is deliberately absent. |
| `EncounterCardHandPresenter` | Pushes per-encounter chrome once, resolves each choice's offer by its `offer-quest: <tag>` (P1-9; untagged falls back to the single/first offer), labels the card with the job + reward telegraph (first declared reward), builds the inspect detail (objectives + giver), and fires the DarkOffer bark once per encounter when the hand first shows the attack card or a dark-belonging offer. |
| `BelongingTintCatalog` / `IBelongingTintCatalog` | Belonging id → authored colour (race + reward-family merged); the hand view resolves the mystery-slot chip colour through it so the presenter stays UnityEngine-free. |

### 2.3 Runtime flow

1. The runner pumps a line → presenter `HandleLine`: `SetVisible(true)`, `EnsureEncounterChrome()`
   (once per encounter: `SetPortrait(runner.EncounterArchetypeId)`, and `SetSpeaker(runner.
   EncounterDisplayName)` unless a `#speaker:` tag already set one), **clears the hand** (no cards while
   a line types), then `ShowSituation(text)`.
2. The view formats the line through `KeywordHighlightFormatter`, sets it on the TMP text with
   `maxVisibleCharacters = 0`, and a coroutine steps `maxVisibleCharacters` to each **word boundary**
   at `_wordsPerSecond` (R4). TMP's `characterCount` excludes rich-text tags, so the `<color>` spans
   never shift the boundaries. A tap on `_tapArea` mid-reveal snaps the line to full (R5).
3. When the line has fully revealed the view raises `OnRevealCompleted` → presenter `HandleRevealCompleted`:
   a **pre-choice** line auto-advances (`runner.Continue()`) so its choices surface without a tap; a
   **closing reply** (after a quest/talk pick, guarded by `_closingReplyPending`) holds, waiting for the
   dismiss tap. There is no Continue button (R6).
4. At a decision point the runner fires `OnChoices` → presenter composes the hand (clearing
   `_closingReplyPending`). Each `QuestOffer` card resolves **its own** offer: a choice tagged
   `offer-quest: <tag>` labels with `runner.OfferedQuestByTag(tag)` (several offers, P1-9), an untagged
   choice with `runner.OfferedQuest` (the single/first slot) — read **before** the offer-quest tag mints
   the live instance. The card carries `DisplayName` (title) + `Summary` (objective) + the first declared
   reward's tier + belonging (the mystery telegraph). The view renders cards centred above the box; for
   each telegraphing card it resolves the belonging colour via `IBelongingTintCatalog`, sets the glow
   from the tier, and shows the hidden `?`. Quest cards run title + objective through the highlighter.
   After showing the hand the presenter fires the DarkOffer bark once (attack card or dark-belonging
   offer present).
5. Picking a quest/talk card sets `_closingReplyPending` then `runner.SelectChoice(...)`; the branch's
   trailing line (carrying its `offer-quest`/`fact` tags, applied as usual) is shown via `HandleLine` and
   typed out, then a tap raises `OnContinueRequested` → `runner.Continue()` → `END`. (Picking Leave ends
   immediately.)
6. `OnDialogueEnded` → presenter hides the box and resets the chrome guards + `_closingReplyPending` for
   the next encounter.

### 2.4 DI wiring

`NarrativeSliceInstaller` binds `IEncounterCardHandView` from
`Prefabs/UI/Encounter/EncounterCardHandView` and the `EncounterCardHandPresenter`
(`AsSingle().NonLazy()`). The view `[Inject]`s the already-bound `INpcArchetypeCatalog` (portrait) and
the Track-H `IBelongingTintCatalog` (mystery-slot chip colour). The presenter constructor-injects the
optional `ICauldronBarkService` (the DarkOffer bark; absent = no bark). `IBelongingTintCatalog` is bound
in the same installer from the loaded `RaceDefinition`s + `RewardFamilyDefinition`s.

### 2.5 Reading-speed / highlight tuning

`_wordsPerSecond` and `_keywordColor` are serialized fields on the `EncounterCardHandView` prefab —
the single global reading speed and the accent tint. They are not authored per line.

---

## 3. ScriptableObject Reference

This UI owns **no new ScriptableObject type**. It reads existing data:

- **Portrait** — `NpcArchetype._portrait` (`Sprite`), asset menu `Create → Narrative → Actors →
  Archetype`; resolved by id via `INpcArchetypeCatalog`. See `narrative-procedural.md` §3.
- **Quest title / objective / reward** — `QuestDefinition._displayName` + `_summary` + the declared
  `_rewards` (tier + belonging + payload kind), asset menu `Create → Narrative → Quests → Quest`. See
  `quest-subsystem.md` §3.
- **Belonging colour** — `RaceDefinition._belongingColor` (Part-Blank rewards) and
  `RewardFamilyDefinition._belongingColor` (artifact rewards), merged by `BelongingTintCatalog`. See
  `quest-subsystem.md` §3 / `races-passport.md`.

---

## 4. Adding Content

No code changes for any of the below.

### Highlight a key word in a line or choice
1. In the story's `.ink` (and the compiled `.json`, edited in lockstep — no inklecate in repo), wrap
   the word in double brackets: `[[Garrick]]`. The markers are stripped at render; the word shows in
   the accent tint.
2. The same syntax works in choice text and in a quest's `_summary` / objective `_description`, so a
   name reads the same on the card as in the line.

### Give an NPC a portrait
1. Assign a `Sprite` to the archetype's **Portrait** field (`NpcArchetype`). It appears in the box for
   any encounter cast from that archetype. Leave it empty to use the neutral placeholder.

### Label the quest card
1. The quest card automatically shows the attached quest's **title** (`_displayName`) and
   **objective/summary** (`_summary`) — do not re-type the objective into the choice text.

**Authoring constraints / gotchas:** edit `.ink` and `.json` together (the runtime loads the compiled
`.json`). A `# card: attack` tag must still follow **shown** (non-bracketed) choice text (see
`narrative-procedural.md` §2.7). Hand-authored `.asset` / `.json` must be BOM-less UTF-8.

---

## 5. Tests

Edit-mode suites in `Assets/__Project/Tests/EditMode/`:

- `KeywordHighlightFormatterTests` — single/multiple marks, no marks, `[[]]`, unterminated `[[`,
  null/empty color, leading `#` on the color, null/empty input; exact rich-text output and that the
  markers are stripped.
- `EncounterCardHandPresenterTests` — a `QuestOffer` card carries the offered quest's `DisplayName` /
  `Summary` (and Leave carries none); `SetPortrait` is forwarded the casting's archetype id and the
  speaker falls back to the NPC's display name; existing card-composition + routing assertions stay
  green.
- `MultipleOffersTests` (P1-9) — a two-slot casting shows both offers each labelled with its own
  quest, same tier / different belonging telegraph; the `offer-quest: <tag>` choice resolves the right
  offer; picking one mints only that quest; the untagged legacy single-offer still resolves. *(Lives
  with the quest subsystem; exercises this presenter.)*

The **mystery reward slot** (glow/tint/`?`), the **hover-inspect**, and the belonging colour are
authored on `EncounterCardView.prefab` (reward-slot child, glow + belonging-chip `Image`s) and
resolved through `IBelongingTintCatalog` — verified **in play mode** (pending the gameplay pass), not
by edit-mode tests.

Verified manually in the editor (the demo barn-victim encounter): bottom-centre box + portrait/name,
word-by-word reveal with no cards while the line types, tap-to-complete, cards then appearing
automatically above (no Continue button) with a quest card showing title + summary, picking it shows
the closing reply and a tap closes the box, the `[[Налётчики]]` key word tinted the same in the line
and on the card, and the portrait-less placeholder fallback. (Demo story text is authored in Russian;
`[[ ]]` keyword markers carry over.)

---

## 6. Known limitations / open points

- ⚠ **Gameplay-untested (2026-07-07)** — the mystery reward slot, hover-inspect, and several-offers
  layout are not yet confirmed in play mode (ROADMAP "Quests").
- **The card telegraphs the first declared reward only** — a multi-reward quest still reads as one
  mystery slot (terse by design).
- **The mystery slot is a placeholder shader treatment** — glow is a stepped alpha on a filled circle,
  the belonging a flat chip; the ornate frame ornament / real glow shader / reveal animation are a
  later render-look pass (`design/art/`).
- **No automatic name detection** — highlighting is only via the author's `[[ ]]` markers.
- **One portrait per NPC** — no emotion/portrait changes mid-conversation.
- **Prefab visuals verified in-editor** — the box layout, TMP text components, portrait `Image`, tap
  catcher, and card-anchor placement are authored in `EncounterCardHandView.prefab` /
  `EncounterCardView.prefab` (all serialized references wired); the on-screen look/feel is confirmed in
  play mode rather than by edit-mode tests.
