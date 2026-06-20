# Amendments to the Narrative Refactor Implementation Plan

These are targeted revisions to the implementation plan. **Keep the rest of the plan as-is.** Each amendment references the plan's sections (A–J) and states Issue / Change / Affects / Acceptance. Priority tiers tell you sequencing:

- **P0** — model/data-shape change; do this *before* coding fragments (fold into plan steps 1–4).
- **P1** — runtime correctness; must land in the vertical slice and its tests.
- **P2** — a decision to confirm or a claim to reframe.
- **P3** — cheap specification to remove ambiguity now; implementation can stay minimal.

---

## A1 (P0) — Decouple fact *scope* from *namespace*

**Issue.** `FactKeyDefinition` derives subject-arity from `_namespace` (World→no subject, Actor→instance, Faction→faction), and authoring subject tokens are a closed set `""/$self/$target/$faction`. This makes `world` facts global-only and cannot express entity-scoped world facts such as per-location `world.<locationId>.burned` — which the design's own cascade examples require (a village burns → an NPC who lived there dies). `namespace` is being overloaded as arity.

**Change.**
- Add an explicit `_scope` field to `FactKeyDefinition`: `enum FactScope { Global, PerActor, PerFaction, PerLocation }` (leave room to extend). `_namespace` remains a grouping/label; **arity comes from `_scope`**, not from namespace.
- Generalize the subject token from a fixed enum to an open mechanism: keep `$self/$target/$faction` as built-in context bindings, but allow an arbitrary `$<contextKey>` token (e.g. `$location`) that `SubjectResolver` resolves against the casting `ContextBag`. Contract: unknown token → fail-closed + warn.
- `FactKey { Ns; Subject; Key }` is unchanged (Subject is already an arbitrary string); this only opens the authoring + resolver side so `Subject` gets populated for scoped world facts.

**Affects.** §A `FactKeyDefinition`, authoring structs (`FactPredicateSerial._subjectToken`, `FactEffectSerial`); §B `SubjectResolver`, `FactKey`; §H `SubjectResolverTests`.

**Acceptance.** A `world.<locationId>.burned` fact can be authored, written via an effect using a `$location` subject, and read by another story's precondition resolving the same location. Extend `SubjectResolverTests` (or add `ScopedFactTests`) proving arbitrary context-key resolution and fail-closed on unknown tokens.

**Note.** The *slice* can still exercise only Global + PerActor + PerFaction. What matters is that the `_scope` field and open subject token exist now, so the data shape needs no migration later.

---

## B1 (P1) — Make the dialogue↔external suspension handshake explicit

**Issue.** Ink is synchronous. On `start-combat:` (and any async outcome) the runner must **suspend** — stop pumping Ink — until `ReportCombatResult` arrives, then perform exactly one `Continue()`. The plan describes the callback but not the suspended state, and `DialogueRunnerTagTests` has no case for it. Naively continuing after raising `OnCombatTriggered` is a race.

**Change.**
- Give `DialogueRunner` an explicit minimal state machine: `Running / AwaitingExternal / Ended`. On `start-combat:` / async tags, transition to `AwaitingExternal`, stop consuming Ink, and surface nothing further until `ReportCombatResult`/`ReportQuestResult` sets the write-back variable and calls a single `Resume()` (which performs the one `Continue()`).
- Define the rule for tags emitted *after* a suspending tag in the same `Continue()` batch: either require the suspending tag to be the last tag in its knot, or buffer/ignore trailing tags for that step — document whichever you choose.
- Guard against double-resume (`Resume()` with no pending await → no-op or throw; pick one and test it).

**Affects.** §B `DialogueSession`/runner control flow; §D tag bridge; §H `DialogueRunnerTagTests`.

**Acceptance.** Test "combat mid-dialogue": runner hits `start-combat:`, enters `AwaitingExternal`, assert no further Ink consumed; `ReportCombatResult(true)` sets `combat_won`, `Resume()` continues, Ink branches on `combat_won`. Plus the double-resume guard test.

---

## B2 (P1) — Snapshot the RNG *state*, not just the seed

**Issue.** `RunNarrativeSnapshot` stores the run seed only. If any procedural choice occurs after load (director `SelectNext`, casting fragment pick, name-pool draw), seed-only replay diverges from the uninterrupted run.

**Change.** Pick one and document it:
- (a) **Recommended:** wrap RNG in a counter-based, serializable PRNG (e.g. a PCG/splitmix-style generator exposing its state/counter) behind the existing seeded-random helper, and include that state in `RunNarrativeSnapshot`. (`System.Random` isn't cleanly serializable, hence the wrapper.)
- (b) Enforce and document the invariant "no procedural generation after load until a fresh, separately-seeded sub-stream is created," relying on reloaded concrete state (castings/quests/facts) for everything pre-load.

**Affects.** §B `RunDirector` and any RNG consumer; §E seeded-random helper; §G `RunNarrativeSnapshot`.

**Acceptance.** Determinism test: run to step N, save, reload, continue; assert the subsequent draws (next director selection / casting pick) match the uninterrupted run.

---

## B3 (P1) — Enforce effect-footprint conformance

**Issue.** R7 trusts that a story's runtime fact writes ⊆ its declared `_effectFootprint`. An Ink `fact:` tag can write outside the footprint silently; once the director does any pacing/lookahead it will plan on a false footprint.

**Change.**
- Single runtime chokepoint: before `FactEffectApplier.Apply` runs a play-time write, verify the target key is within the active casting story's `_effectFootprint`. Violation → fail-closed (skip write) + warn with story id + key (dev builds may throw).
- Compare *resolved key shapes* (namespace + key + subject arity), not concrete subject ids — `actor.$self.hostile` in the footprint must match a write to `actor.<instanceId>.hostile`.
- Add a best-effort editor-time validator scanning `DialogueDefinition._declaredFactWrites` (and, where feasible, Ink `fact:` tags) against the owning/eligible `StoryTemplate._effectFootprint`. The runtime guard is the backstop.

**Affects.** §A `StoryTemplate._effectFootprint`, `DialogueDefinition._declaredFactWrites`; §B `FactEffectApplier`; §D runner; §H `FactEffectApplierTests`.

**Acceptance.** An out-of-footprint `fact:` write is rejected, store unchanged, warning recorded (FakeLogger). In-footprint token writes resolve and apply.

---

## B4 (P1) — Pin presence-vs-default semantics

**Issue.** `Has`/`Exists` (presence) and `GetOrDefault` (value-with-default) interact subtly: a never-set fact with default `false` satisfies `Eq false` but fails `Exists`. This is a classic QBN footgun and is currently unspecified.

**Change.** Lock the contract in the doc and tests:
- Comparison ops (`Eq/NotEq/Gt/...`) operate on `GetOrDefault` (unset → default participates).
- `Exists/NotExists` operate on `Has` (presence only; default ignored).
- Decide and document the `Remove` interaction: after `Remove`, `Eq default` becomes true (via default) while `Exists` is false — confirm intended.

**Affects.** §B `IFactStore`, `PreconditionEvaluator`; §H `PreconditionEvaluatorTests`.

**Acceptance.** Parametric matrix per value type: {set, unset, removed} × {Eq-default, Eq-nondefault, Exists, NotExists}.

---

## B5 (P1) — Validate op×type in the applier

**Issue.** `FactEffectOp.{Set,Add,Toggle,Remove}` are not all valid for every `FactValueType` (e.g. `Add` on Bool/String, `Toggle` on Int).

**Change.** Define the legal op×type matrix. `FactEffectApplier` fails closed + warns on illegal pairs. Optionally surface it via SO validation so an illegal pair can't be authored/saved.

**Affects.** §B `FactEffectApplier`; §A authoring-struct validation; §H `FactEffectApplierTests`.

**Acceptance.** Each illegal pair → no-op + warning; legal pairs covered.

---

## C1 (P2 — confirm a decision) — Dual-Ink channel collapse

**Issue.** Legacy ran two Ink channels (NPC character Ink + story Ink) merged by `CompositeDialoguePresenter`; the new model uses one `DialogueSession` per casting and retires the merge. If the character channel provided ambient/idle/bark lines layered over story content, that capability is dropped.

**Decision needed.** Confirm the ambient/character channel is not needed. If it **is** needed, model it explicitly — an optional secondary ambient `DialogueDefinition` reference on the casting, or move barks to a separate non-narrative system — rather than overloading the single story session.

**Affects.** §A `DialogueDefinition`; §B `Casting`; §I migration row `CompositeDialoguePresenter → DialogueRunner`.

**Action.** Add a one-line resolution to the plan. If "keep," put the secondary-channel design in the design-doc deferred section with a ROADMAP item.

---

## C2 (P2 — reframe a claim) — R8 is labelled, not realized, this pass

**Issue.** `_threadId` is a string label this pass; first-class threads are deferred. `CrossThreadEligibilityTests` proves **R7** (cross-storylet coupling via facts); it only *labels* R8.

**Change.** Reframe the test and §F acceptance wording to: "R7 cross-storylet eligibility proof (threads as labels; first-class R8 deferred)." Keep the test. Add an explicit ROADMAP line: "R8 first-class `Thread` (id + stage) + director thread-balancing."

**Affects.** §F acceptance wording; §H test name/description; §J deferred list (align wording).

---

## D1 (P3 — specify now) — Casting fragment tie-break

**Issue.** When multiple fragments match a slot's `_requiredTags`, the selection policy is unspecified → nondeterministic under seeded runs.

**Change.** Gather matches in a stable deterministic order (by id), then choose via the seeded stream; document it. Per-fragment match weight can be a later ROADMAP item.

**Affects.** §B `CastingFactory`; §H `CastingFactoryTests` (add "multiple matches → deterministic seeded pick").

---

## D2 (P3 — specify now) — Recurring-actor identity policy

**Issue.** R12 carry-over depends on reusing the same `NpcInstance`/`InstanceId`, but the rule for when an actor is "the same" vs freshly instanced is undefined.

**Change.** State the contract even if minimal: re-casting an archetype yields a **new** instance by default; reuse requires an explicit run-local actor reference (a stable id in the story/casting that the director resolves to an existing instance). The slice can ship default-new; fixing the contract keeps R12 unambiguous later.

**Affects.** §B `NpcInstance`/`CastingFactory`; design-doc R12 section; ROADMAP.

---

## D3 (P3 — optional) — Guard typed-ref ↔ registry drift

**Issue.** Hand-curated `WorldFacts`/`ActorFacts`/`FactionFacts` refs can drift from the SO registry on rename. The startup assert catches *missing* refs but not a silent rename mismatch.

**Change.** Add a small editor validator flagging registry keys with no matching `FactKeyRef` and vice versa, or schedule codegen as a later upgrade (rename-safe). Low priority.

**Affects.** §B typed-accessor layer; §H a consistency test.

---

## Suggested integration order
1. **A1** into plan steps 1–4 (changes fact data shape before fragments exist).
2. **B4, B5, B3** into the evaluator/applier step (4) and their suites.
3. **B1** into the DialogueRunner step (8).
4. **B2** into the save/load boundary step (11) and the seeded-random helper (step 7/E).
5. **D1, D2** into the casting/director steps (6–7).
6. **C1, C2** resolved in the design doc + ROADMAP (step 12); no code impact beyond wording.
7. **D3** optional, anytime.
