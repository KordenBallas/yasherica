# Multiple & Competing Offers — the Two Offer Shapes

> Status: **Verified** (discussed with the product owner, ready for the code track) · 2026-07-05
> Level: product-owner (what & feel). The code track owns the technical "how".
> Design background: `design/narrative/npc-encounter-cards.md` §3 (several cards on one NPC) and
> `design/narrative/quest-as-reward.md` §4 (the cross-actor moral fork). Covers ROADMAP **P1-9**
> (several offers per NPC) **+ P1-8** (competing / mutually-exclusive offers). Both obey the same
> **same-tier / different-currency** rule; this brief draws the line between the two shapes so they
> aren't conflated. Uses the shipped threads (`director-threads-and-continuity.md`, P2-3).

## Goal

Give the quest economy its **two ways to offer a choice**, and keep them **distinct**:

- **Shape A — several offers on one NPC, at one moment.** A single situation presents **several
  resolution cards at once** (several ways to solve *the same* trouble).
- **Shape B — a competing, mutually-exclusive fork across two actors, separated in time.** You commit
  to one offer now; a **mutually-exclusive** offer arrives **a couple platforms later at a different
  NPC**, on a shared-actor thread.

Both make the choice read as **whose side / which identity**, never "better loot vs worse loot": the
offers are the **same tier** (same glow) and pay in **different currency** (power/combat vs
access/passport). No dominant pick → Pillar 1 holds.

## User stories

- As a player, when an NPC has **several ways** I could solve their trouble, I see **several offer
  cards together**, all the **same tier**, differing by **belonging colour** — I pick the currency /
  approach I want, not the biggest number.
- As a player, sometimes I take one job, and **later, elsewhere**, someone offers me the **opposite**
  job — and taking it is **no longer possible** once I've committed to the first (or committing to the
  second **fails** the first). The choice was about **whose side I'm on**, and the world remembers.
- As a player, I never see the victim's offer and the robber's offer **side by side** as a menu — the
  moral fork unfolds **over the run**, so committing actually costs something.

## Functional requirements

### Shape A — several offers on one NPC (P1-9)
1. A single NPC/storylet can carry **several quest-offer cards** shown **together** in the encounter
   hand — several distinct resolutions of the **same** situation (a new authoring shape; today a story
   carries one offer).
2. These simultaneous offers are the **same tier** (same glow) and pay in **different belongings**
   (power/combat vs access/passport) — "different currency, not more"; there is **no dominant pick**.
3. Picking one is the resolution of that situation; the others are **not also taken** (they were
   alternative ways to solve the one trouble, not extra rewards).

### Shape B — competing / mutually-exclusive fork (P1-8)
4. A competing fork is **two offers on a shared-actor thread, separated in time** — offer A at one
   NPC now; the **mutually-exclusive** offer B **a couple platforms later** at a *different* NPC (the
   consequence clock + thread placement from the director). They are **never shown side by side**.
5. The two offers are the **same tier** (both glow) and pay in **different currency** — so tier can't
   drive it; the decision reads as **whose side / which facts remain**.
6. **Mutual exclusion is enforced on facts/threads**, using the shipped mechanism: committing to one
   writes facts that make the other's premise **impossible**, so the opposing thread **fails on
   fact-conflict** (the same mechanism as thread fail in P2-3) — a state change + indicator, **no
   authored closing storylet**. The player **commits to A before B appears**.
7. **The cauldron tempts the dark side of the fork** — presenting the power/combat (Monster-lean)
   offer is a **bark slot** (content in `cauldron-voice-barks.md`); the pull is delivered in fiction,
   never by a thumb on the loot scale.

### The rule both shapes share
8. **Same tier, different currency.** Neither shape may be balanced by raising one offer's tier or by
   compensating with combat corpse-loot (that channel is orthogonal — `quest-as-reward.md` §4). The
   fork's stakes are **facts, threads, and doors**, never loot-EV.

## Content authoring rules (for the designer)
- **Shape A:** author the several resolutions on **one** storylet/NPC, all at the **same tier**, each
  with a **different belonging** — so the hand reads as "pick your currency", not "pick the best".
- **Shape B:** author the two offers as **two storylets on a shared actor/thread**, same tier,
  opposed **facts** (join X / close X). Do **not** author an A→B link — the director places B later off
  the thread; the opposed facts do the mutual exclusion.
- Keep the distinction honest: **same-NPC, one moment = Shape A**; **two actors, separated in time =
  Shape B**. Don't collapse a moral fork into a same-moment menu (it removes the cost of committing).

## Acceptance criteria
- **Shape A:** an authored NPC with several resolutions shows **multiple offer cards together**, all
  the **same glow (tier)**, differing by **belonging colour**; picking one resolves the situation and
  the others don't also pay out.
- **Shape B:** taking offer A, then travelling on, surfaces the **mutually-exclusive** offer B at a
  **different** NPC later; **committing to B fails A's thread on fact-conflict** (indicator only, no
  closing beat), and vice-versa — the two are never simultaneously completable.
- Both shapes present **same-tier** offers distinguished only by **belonging** (currency), with no
  tier/loot advantage on either side.
- The dark-side offer of a Shape-B fork triggers a **cauldron bark slot** (placeholder acceptable).

## Out of scope / open points (do not build now)
- **The rolled reward** behind each offer (tier + belonging + payload kind) — separate,
  `quest-reward-rolled.md`.
- **The offer-card visual** — separate, `quest-offer-card.md`.
- **The attack card** as the Monster resolution of an actor — separate,
  `attack-card-monster-verb.md` (an attack can be one arm of a Shape-B fork, but its wiring lives
  there).
- **Cauldron bark content** — separate, `cauldron-voice-barks.md`.
- **New director placement machinery** — threads + continuity shipped in P2-3; this brief authors on
  top of them, it does not extend them. Any OR-composed precondition need rides P3-5 (out of scope).
- **A player-facing view of the live fork/threads** — that is the quest-log/saga readout
  (`quest-log-and-saga.md`, P1-11).
