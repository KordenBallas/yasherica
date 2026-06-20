# Amendments to the Narrative Refactor Plan — Wave 2

First-wave amendments (A1, B1–B5, C1, C2, D1–D3) are assumed already integrated into the updated plan. These are the remaining issues, same format: Issue / Change / Affects (plan sections A–J) / Acceptance. Priority tiers as before (P0 = model change, do before coding; P1 = slice correctness; P2 = decision/clarity; P3 = cheap cleanup).

---

## W2-1 (P0) — Re-anchor footprint enforcement to the *fragment*, not the *story*

**Issue.** The B3 runtime guard validates each play-time write against the **story's** `_effectFootprint`. But slots are filled by tag at cast time (R5), so the concrete dialogue/quest doing the writing is chosen at runtime and may write facts the story author never enumerated. Forcing every candidate fragment's writes into one static story footprint recouples the story to fragment internals and **defeats recombination** (add a second `[shakedown]` dialogue that writes a different fact → it trips the guard until you edit the story). Separately, quest/combat effects can fire **after** the `DialogueSession` ends, when there is no "active story" for the guard to consult.

**Change.**
- **Each writing fragment owns its footprint.** `DialogueDefinition._declaredFactWrites` becomes the *authoritative, complete* set of keys its Ink `fact:` tags may write (any `fact:` tag key not in the set is rejected). A quest's declared write set is the union of `_onCompleteEffects` + `_onFailEffects` + per-objective `_completionEffects` (no new field needed).
- **The runtime guard checks fragment-write ⊆ that-fragment's own footprint** — resolved from the effect's *origin*, not from a story. `DialogueSession` and `QuestInstance` each carry their fragment's footprint handle, so detached quest/combat effects validate correctly whenever they fire, with no "active story" dependency.
- **`StoryTemplate._effectFootprint` becomes derived/advisory**, used **only** by the director for planning/lookahead — never as the runtime write-gate. Compute it (editor-time or lazily) as the union of the story's own direct effects (if any) plus each slot's candidate fragments' footprints (slot `_requiredTags` resolved against the fragment library). Flag a slot with zero candidates.

**Affects.** §A `StoryTemplate._effectFootprint` (semantics → derived/advisory), `DialogueDefinition._declaredFactWrites` (→ authoritative + validated), `QuestDefinition` (effect lists = its footprint); §B `FactEffectApplier` (guard resolves footprint via effect origin), `DialogueSession`/`QuestInstance` (carry footprint handle); §H `FactEffectApplierTests`, `StoryTemplateMapperTests`; §J steps 4–5.

**Acceptance.**
- Two `[shakedown]` dialogues writing *different* facts both cast into the same story without tripping the guard and without editing the story.
- An Ink `fact:` tag writing a key absent from its `DialogueDefinition._declaredFactWrites` is rejected + warned.
- A quest completing *after* its dialogue session ended validates against the quest's own footprint (no active-story context needed).
- The derived `StoryTemplate._effectFootprint` equals union(own-effects, candidate-fragment footprints); a test asserts the applier never consults it.

---

## W2-2 (P1) — Optional-slot availability for dialogue tags

**Issue.** A Quest/Combat slot can be `_optional`. If no matching fragment is found, the casting has no bound quest/enemy — yet the bound dialogue's Ink may still emit `offer-quest:` / `start-combat:`. Ink has no way to know whether the optional slot was filled, and the behavior of a slot-dependent tag firing against an empty slot is undefined.

**Change.**
- `CastingFactory` records which optional slots were filled. The runner injects reserved boolean availability vars at **first load** (e.g. `quest_available`, `combat_available`) from that record, alongside declared vars; Ink branches defensively on them.
- Define empty-slot fallback: a slot-dependent tag firing with no bound fragment → fail-closed (skip the action + warn with story id + slot tag); for combat, perform no transition and set the write-back var (`combat_won`) to a safe value so Ink can continue.
- Treat the availability vars as a reserved set the runner always provides (document in `DialogueDefinition` conventions).

**Affects.** §B `CastingFactory` (filled-slot record), `DialogueSession`/`DialogueRunner` (inject availability vars + fail-closed handling); §D tag grammar (empty-slot rows for `offer-quest:`/`start-combat:`); §A `DialogueDefinition` (reserved vars note); §H `DialogueRunnerTagTests`, `CastingFactoryTests`.

**Acceptance.** With the combat slot unfilled, `combat_available=false` is injected; guarded Ink never calls `start-combat:`; a forced `start-combat:` against the empty slot is skipped + warned with no transition. Symmetric test for `offer-quest:`.

---

## W2-3 (P1) — Disambiguate `HostileFallback` (casting) from `actor.<id>.hostile` (fact)

**Issue.** Hostility appears twice — `Casting.HostileFallback` and the fact `actor.<id>.hostile` (in the slice footprint). Two representations of "hostile" risk the dual-source-of-truth anti-pattern R3 is meant to remove.

**Change.** Define them as distinct, non-overlapping concepts, and prefer eliminating the stored field:
- **Static (cast-time):** "is the combat branch available in this casting." This is fully derivable from *whether the combat slot was filled* (see W2-2) — so **drop `HostileFallback` and derive `CombatAllowed` from the filled combat slot** rather than storing it. (If kept for any reason, rename to `CombatAllowed`/`CanTurnHostile` and make it read-only after cast.)
- **Dynamic (runtime):** `actor.<id>.hostile` fact — "is this actor currently hostile," written **only** by the runtime dialogue/combat branch, read by later stories for recurring-actor carry-over (R12).
- Single-writer rule for the fact; the static availability value is never written at runtime.

**Affects.** §B `Casting` (field removed or renamed read-only), fact usage; §A footprint example; §F slice asset description; doc R3/R12.

**Acceptance.** Doc states the two cleanly; the `actor.hostile` fact is written only by the runtime branch (test); if derived, "combat available" equals "combat slot filled" with no separate stored value.

---

## W2-4 (P1) — First-load injection vs reload restoration of dialogue variables

**Issue.** `DialogueSession` injects `_declaredVariables` from the `ContextBag` on load. Ink already serializes its own variables in saved state, so re-injecting on a mid-session **reload** overwrites play-time-modified values with their initial casting values.

**Change.** Split the paths and track which one a session is on:
- **Fresh start** → inject declared vars + availability vars (W2-2) from the `ContextBag`.
- **Restore from snapshot** (`LoadState`) → do **not** re-inject Ink-bound vars; trust the serialized Ink state.
- Only non-Ink external context the runner itself needs may be re-resolved on both paths; Ink variables are injected on fresh start only.

**Affects.** §B `DialogueSession` (start vs restore paths); §G `DialogueSessionSnapshot`; §H reload test.

**Acceptance.** Set a dialogue var mid-session, snapshot, restore → the var keeps its play-time value (not the casting default); a fresh start injects casting defaults.

---

## W2-5 (P3) — Stable ordering where store iteration feeds decisions

**Issue.** `FactStore` backs on `Dictionary<FactKey,FactValue>`; iteration order isn't guaranteed. Any logic that iterates the store to drive a decision (selection, hashing, snapshot bytes) can drift non-deterministically.

**Change.** Give `FactKey` a stable comparer (Ns, Subject, Key). `FactStore.Snapshot()` emits in that stable order so snapshots are reproducible/diffable. Pure key lookups are unaffected; ensure no procedural decision depends on raw dictionary order.

**Affects.** §B `FactKey` (comparer), `FactStore.Snapshot()`; §G snapshot.

**Acceptance.** `Snapshot()` is stable across runs for equal logical state; no selection/RNG path reads raw dictionary order.

---

## W2-6 (P3) — Slice completeness (so it actually casts/validates)

**Issue.** Two gaps block the slice from casting/validating as written: the example `EnemyDefinition enemy_bandit_brute` has no tag to satisfy the combat slot's `_requiredTags [bandit]`; and the slice `FactKeyDefinition` assets don't specify `_scope`.

**Change.**
- Give `enemy_bandit_brute` an enemy tag set including `bandit` (confirm/add the tag field on `EnemyDefinition`).
- Set `_scope` on every slice fact: `world.pass_blocked`, `world.pass_cleared` → `Global`; `actor.hostile` → `PerActor`; `faction.reputation` → `PerFaction`.

**Affects.** §F slice asset list; §A `EnemyDefinition` (tag field).

**Acceptance.** `CastingFactory` fills the combat slot from `enemy_bandit_brute` by tag match; all slice facts resolve with correct arity.

---

## Suggested integration order
1. **W2-1** into steps 4–5 (applier guard + fragment SOs) — before the guard and footprint semantics harden.
2. **W2-3** into step 6 (casting) — it may *remove* a field; settle before casting tests cement.
3. **W2-2** into steps 6–8 (casting factory + runner).
4. **W2-4** into steps 8 + 11 (runner + save boundary).
5. **W2-5** into steps 1/7 (FactKey comparer + snapshot).
6. **W2-6** into step 10 (author slice assets).
