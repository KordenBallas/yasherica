# Architecture & Extensibility Audit — Full-Project Review

**Track W.** A read-only, whole-project diagnostic pass: read the codebase and the data systems,
measure them against the project's own architectural constitution (`Assets/__Project/CLAUDE.md`),
and produce a **prioritised findings backlog** — design problems, coupling/bottlenecks, data systems
that are hard to extend through SO/config, dead weight to remove, and *hypotheses* worth profiling.

This is a **review**, not an implementation. It changes **no code, no assets, and no system docs** —
its only output is findings reports (and, in the final pass, deduped ROADMAP entries the owner
approves). It is deliberately split into **scoped passes** because "read everything and find every
problem" in one prompt overruns context and degrades into generic advice and hallucinated file
references. Run **one pass per session**.

Recommended model: **Fable 5** (correctness on a hard, cross-system read is exactly its lane). Runs
from the **code root** (`Assets/__Project`) so it reviews as the coding model, not the PO.

---

## Shared rules for every pass (read first)

1. **Read-only.** Produce a report. Do **not** edit code, assets, or docs. No refactor is applied
   here — the audit *proposes*, the owner disposes, and approved items become ROADMAP tasks.
2. **One pass per session.** Each pass below is a self-contained assignment scoped to fit context.
   Do not attempt several passes at once.
3. **Every finding cites `file:line`.** A finding with no concrete anchor is not a finding. If you
   cannot point at the code, do not report it. **Never invent a path** — verify it exists first.
4. **Measure against `CLAUDE.md`, not taste.** The project's rules (SOLID/KISS · MVP · Zenject DI ·
   Core has no `UnityEngine` · data-driven content · no God-MonoBehaviours · no service locators ·
   no magic numbers) are the rubric. Tie each finding to the specific rule it bends, or to a concrete
   felt cost — not to a stylistic preference.
5. **Prioritise every finding P0 / P1 / P2.**
   - **P0** — breaks a core rule or blocks data-only extension (a designer *must* touch code to add
     content); a correctness landmine; a cross-system coupling that will bite the next change.
   - **P1** — real, recurring friction or a rule bent (not broken); worth a scheduled refactor.
   - **P2** — cleanup / nice-to-have; extract-on-next-consumer.
6. **Each finding carries:** *what · where (`file:line`) · which rule/cost · estimated refactor
   cost & blast-radius (files touched, tests affected, compiler-needed?) · recommended disposition
   (fix-now / ROADMAP / won't-fix, with a one-line why).*
7. **Do not re-file known debt.** Cross-reference the existing `[debt]`/`[arch]` items in
   `ROADMAP.md` and `ROADMAP-prioritized.md` (Tracks J/L/N, the P6 wave). If a finding is already
   tracked, **cite the ID and skip it** — unless it is materially worse than filed, in which case say
   so. The audit's value is the *unknown*, not a re-print of the backlog.
8. **Perf findings are hypotheses.** There is no profiler in the environment. Anything about
   allocations / hot paths / LINQ / `Update` cost is a **"needs profiling"** candidate, labelled as
   such — never asserted as a measured fact.
9. **Lean on the integrity tools.** `/content-graph`, `/balance-ledger`, and `/part-spec` already
   validate the narrative graph, world-balance, and rig contracts. Read their outputs; don't
   re-derive what they own.
10. **Output = one markdown report per pass**, written to `Assets/__Project/Docs/audits/`
    (create it): `audits/<pass-id>.md`. Reports are diagnostics, **not** system docs — they live
    apart from the as-implemented `Docs/` tree and are not bound by the `_TEMPLATE.md` shape.

---

## The passes

### W1 — Data-driven / SO extensibility audit
**Question:** for each content-bearing subsystem — artifacts, abilities, body-parts/mutation, loot,
quests, stories/director, biomes/platforms, races/passport, status-effects — *can a designer add one
new instance with data alone?* (CLAUDE.md §1, §7.)

- For each subsystem, trace the add-one-instance path and report the **"places you must touch"
  count**: SO asset(s) only (good) vs. SO + a code edit somewhere (a data-path gap → P0/P1).
- Flag every place content variety is **hard-coded** where it should be data: enums/switch
  statements over content kinds, `if type == …` ladders, literal ids in logic, archetype/biome/tier
  handling that a new value would silently miss.
- Flag **SOs holding logic** (beyond pure eligibility predicates over their own data — §7) and Core
  records that leak Unity types (§7 mapper rule).
- Confirm every SO type is **documented** (asset-menu path, fields, load/wiring, add-recipe) per §8;
  list undocumented or drifted SO types (hand off the drift detail to **W4**).
- **Deliverable metric:** a table of subsystem × add-instance-touch-count, worst offenders first.

### W2 — Architecture, coupling & duplication audit
**Question:** where does the dependency flow, layering, or MVP/DI discipline actually break, and
where is the *same job done twice*? (CLAUDE.md §2–§6, §11, §13–§14.)

- **Layering:** any `UnityEngine` reference reaching Core; any View talking to a Model directly;
  business logic inside a MonoBehaviour or a View (§3, §5, §14).
- **DI:** manual service-locator / `Container.Resolve` at runtime, `FindObjectOfType` /
  `GameObject.Find` / `GetComponent` in `Update` (§4, §5); classes with a dependency count that
  signals an SRP break.
- **Coupling / seams:** God-objects, cross-system concrete references that should be interfaces, the
  known dual-path hazards (e.g. legacy vs streaming director, PvE vs Arena vs draft combat paths) —
  where does one change force edits in N unrelated places?
- **Duplication / parallel implementations** (the highest-value smell of this pass): the *same
  responsibility implemented twice in divergent code paths* — e.g. two systems that each build
  platforms on their own scene instead of one shared path, or two flows for one concept (a normal vs.
  a "special" version) that drift apart. Report both **semantic duplication** (parallel
  implementations of one job — the costly kind that breeds inconsistency and double-maintenance) and
  **copy-paste blocks** (cf. the filed `WorldArtifactView`/`BubbleView` dup, P6-5). For each, name the
  **one approach they should share** and the reconvergence cost.
- **Patterns:** patterns used without justification, or a missing pattern where a switch-ladder
  should be Strategy/State (§13).
- Rank by **blast-radius**: a divergent parallel-implementation of one concept, or a seam coupling two
  big systems, outranks a local copy-paste or a lone smell.

### W3 — Dead, unreachable & orphaned code
**Question:** what can be deleted? Every dead path is a maintenance tax and a reader trap; shrinking
the surface is a real win. **This pass is a hybrid — read the model note.**

- **Enumerate candidates** with no live reference: unreferenced classes/methods/SO types, no-caller
  public APIs (cf. the filed dead platform-loot API + the duplicate `CombatInputModeManager`), dormant
  installers/paths (cf. legacy `EncounterDirector`/`RunDirector`), orphaned assets/prefabs/`.ink`.
- **Then adjudicate each candidate against Unity's indirection channels before calling it dead** — a
  symbol can be live through a path a C#-reference search misses:
  - `[SerializeField]` / inspector wiring — referenced from a `.prefab`/`.unity`/`.asset` by
    `fileID`+GUID, not from C#;
  - **Zenject** bindings — bound/resolved **by type**, so a class only ever `new`'d by the container
    reads as unreferenced;
  - `Resources.Load("…")` by string / `[CreateAssetMenu]` SO types — loaded or instantiated as
    assets, never `new`'d in code;
  - reflection · attributes · `[Inject]` · Ink external functions bound **by name** · editor-only code
    (`#if UNITY_EDITOR`, `Scripts/Editor/**`);
  - **test-only** usage — still referenced, but flag as **production-dead** if only tests touch it.
- A "dead" finding MUST state **which channels were checked and cleared**. A candidate that cannot be
  cleared is reported as *"suspected — needs a human confirm"*, **never** "delete".
- **Model note (this is the answer to "give it to a simpler model?").** The *enumeration* is
  mechanical — a cheaper model, or a Roslyn / IDE reachability sweep, can list candidates fine. The
  *adjudication* against the channels above needs care: a too-simple model will confidently flag every
  `[SerializeField]` class and every `[CreateAssetMenu]` SO as dead → **false-positive deletions, the
  dangerous kind**. Run it **hybrid**: cheap sweep for candidates, careful model (Fable/Opus) to clear
  them. Do **not** hand the whole pass to a simple model unsupervised.
- **Deliverable:** a deletion-candidate list, each tagged *confirmed-dead / production-dead /
  suspected*, with the liveness channels checked and (if confirmed) the safe delete order.

### W4 — Performance hypotheses (needs-profiling candidates)
**Question:** what *looks* like it could cost, worth a profiler session? (CLAUDE.md §12 — "optimise
only after profiling".) **Every item here is explicitly a hypothesis, not a verdict.**

- Runtime allocations / LINQ / boxing in plausibly hot paths (per-frame `Update`, combat resolve,
  streaming generation, dressing/placement planners).
- Repeated work that could be cached; `Resources.Load` patterns; per-frame re-evaluation
  (e.g. marker refresh) vs event-driven.
- Runtime skinned-mesh / instantiation costs already suspected (cf. P6-11).
- **Deliverable:** a ranked "profile these first" shortlist, each with *why suspected* and *what to
  measure* — so a later profiling session is targeted, not a fishing trip.

### W5 — Docs ↔ implementation drift
**Question:** where do the system docs lie? (CLAUDE.md §8 — "if code and a doc disagree, the doc is
wrong.")

- Compare each `Docs/*.md` system doc's requirements / SO reference / add-content recipe against the
  live code and assets; report contradictions, missing SO types, stale field lists, dead recipes.
- Cross-check the `ROADMAP.md` "Known limitations" vs. `ROADMAP-prioritized.md` for items that ship
  in one but stay open in the other.
- **Deliverable:** a drift list (doc · claim · reality · `file:line`), so a doc-refresh pass has an
  exact worklist. (The audit does not fix the docs — it lists the fixes.)

### W6 — Synthesis & prioritised refactor backlog
**Question:** what is the single, deduped, owner-ready picture? (Run **after** W1–W5.)

- Merge W1–W5 into **one** prioritised list, de-duplicated against each other **and** against the
  existing ROADMAP debt (rule 7).
- Group into **themes** (the initiative shape the ROADMAP uses), each with a recommended model and a
  rough cost, so the owner can green-light a theme as a track rather than triage line-items.
- Call out the **top 5 "do these first"** (highest cost-of-not-doing) explicitly.
- **Deliverable:** a synthesis report + a proposed set of ROADMAP rows (IDs left for the owner to
  assign) — the hand-off from "review" to "planned work".

---

## Consuming the report — triage workflow (not a blocking phase)

The audit **informs** the build queue; it does **not** pause it. Do **not** gate further work on
"clear every finding first" — a big-bang refactor before feature work is the trap this scoped,
read-only shape exists to avoid. The owner triages the W6 synthesis into three buckets, using each
finding's `disposition`:

1. **fix-first (narrow)** — only findings inside the **blast radius of the next 1–2 tracks about to be
   built** (e.g. before the MP Online-Robustness core, fix a duplication/coupling *on the arena/netcode
   path*; a P0 in an unrelated subsystem does **not** qualify). Building a new track on a duplicated /
   dead / tangled path in the same area is building on sand — that, and only that, is fixed up front.
2. **backlog** — every other P0/P1 → a ROADMAP row, scheduled opportunistically; many will **merge**
   with existing `[debt]` items (Tracks J/L/N, P6).
3. **won't-fix** — a valid outcome; findings must not balloon scope.

Two outputs, not one: besides the fix-first shortlist, W1–W3 may surface something that **re-orders the
Fable lane itself** — apply that before spending premium hours (this is why Track W runs at slot 0 of
the Fable lane).

**Each approved fix is its own normally-scoped task** (code + tests + docs + CHANGELOG per §0/§8), never
part of the audit, and on the **cheapest capable model**: dead-code deletions (W3) → Haiku/Sonnet quick
wins; duplication consolidation (W2) → Opus, opportunistic; **perf hypotheses (W4) are never fixed
blind — a profiling session precedes any change.**

---

## Out of scope

- **Applying any fix.** The audit proposes; a separate, normally-scoped task implements an approved
  finding (per the usual plan → code → tests → docs → CHANGELOG discipline).
- **Re-reviewing content balance / narrative-graph integrity** — owned by `/balance-ledger` and
  `/content-graph`; this audit reads their reports, it doesn't redo them.
- **Bikeshedding.** Style/naming nits below the P2 bar are noise; a finding must tie to a rule or a
  cost.
- **A ground-up rewrite recommendation.** The mandate is "what to refactor, in what order", grounded
  in the current architecture — not "start over".
