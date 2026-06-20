# Amendments to the Narrative Refactor Plan — Wave 3

Waves 1 (A1, B1–B5, C1, C2, D1–D3) and 2 (W2-1…W2-6) are assumed integrated. These four are implementation-hygiene items, not model changes — none blocks starting the vertical slice; fold each into its relevant step. Same format and priority tiers (P0 = model change first; P1 = correctness; P2 = clarity; P3 = cheap cleanup).

---

## W3-1 (P1) — Define save behavior while a dialogue is suspended (`AwaitingExternal`)

**Issue.** `DialogueSessionSnapshot` reuses `InkStoryManager.SaveState/LoadState`, which captures Ink variable/flow state only. The runner's suspension state (`Running / AwaitingExternal / Ended`, B1) and the *pending-external descriptor* — which combat/quest the session is waiting on and the write-back var to set — are runner state, **not** Ink state. If a save lands while `AwaitingExternal` (e.g. combat is a separate scene the player can quit during), reload restores Ink but loses the "waiting for result" status and the session cannot resume.

**Change.** Pick one and document it in §G:
- **(a) Suspension is a non-savepoint (recommended this pass):** the save service refuses/defers a snapshot while any session is `AwaitingExternal`; assert it. Cheap, matches the common "no mid-combat save" rule.
- **(b) Persist the suspension:** extend `DialogueSessionSnapshot` with the runner state and a pending-external descriptor `{ kind: combat|quest, targetId, writeBackVar }`, so reload re-enters `AwaitingExternal` and re-arms the same resume path. Use only if the game must persist mid-excursion.

**Affects.** §B `DialogueSession`/runner (expose suspension state to the save guard or the snapshot); §G `DialogueSessionSnapshot` + `INarrativeSaveService`; §H save-while-suspended test.

**Acceptance.** (a) snapshotting while `AwaitingExternal` is rejected/deferred with a clear error (tested); or (b) save during `AwaitingExternal` → reload → session resumes into the same pending-external and `ReportCombatResult`/`Resume()` completes correctly.

---

## W3-2 (P1) — Move derived story-footprint computation out of the static mapper

**Issue.** The derived/advisory `StoryTemplate._effectFootprint` (W2-1) = union of the story's own effects + each slot's candidate-fragment footprints, resolved by slot `_requiredTags` against the fragment library. §H currently assigns this to `StoryTemplateMapperTests`, implying the static per-SO mapper computes it. But the SO→Core mappers are per-asset and intentionally have **no access to the whole fragment library** (e.g. `DialogueMapper` only extracts `TextAsset.text`). The union over tag-matched candidates needs the library, so it cannot live in the mapper.

**Change.** Keep `StoryTemplateMapper` pure (maps own fields; does **not** populate `_effectFootprint`). Compute the derived footprint in a component that holds the fragment libraries — the `CastingFactory` or a small director-side planning helper — lazily/at-cast or at install time, cached on the Core story-template data or a planning index. Relocate the union test from the mapper suite to `CastingFactoryTests` (or a `DirectorPlanningTests`). A slot with zero candidates is flagged there.

**Affects.** §B `StoryTemplateMapper` (stays pure), `CastingFactory`/planning helper (owns derivation); §A `StoryTemplate._effectFootprint` note (derived where the library is available, not in the mapper); §H move the union test; §J steps 5 (mapper) vs 7 (casting/director).

**Acceptance.** `StoryTemplateMapper` has no fragment-library dependency; the derived footprint is produced by the casting/planning component and equals union(own, candidate-fragment footprints); zero-candidate slots are flagged there.

---

## W3-3 (P2) — Footprints should be key-*shapes*, not effects

**Issue.** `DialogueDefinition._declaredFactWrites` and the derived `StoryTemplate._effectFootprint` are typed as `FactEffectSerial[]`, which carries `op` + `value`. A footprint is a set of permitted write **targets** (key shapes), not concrete effects; `op`/`value` are vestigial here and invite authoring confusion and shape-vs-effect mix-ups in the guard.

**Change.** Introduce a lightweight `FactKeyShape` (a.k.a. `FactWriteSpec`): `{ namespace, subjectToken, key, valueType }`, and use it for `_declaredFactWrites` and the derived footprint. The runtime guard compares writes against these shapes by (namespace + key + subject arity), as already specified. If a new type is unwanted, explicitly document that `op`/`value` are ignored for footprint purposes and have the SO validator hide/zero them.

**Affects.** §A `DialogueDefinition._declaredFactWrites`, `StoryTemplate._effectFootprint` (type), authoring-struct set; §B `FactEffectApplier` guard (shape comparison); §H footprint tests use shapes.

**Acceptance.** Footprint declarations carry no op/value semantics (dedicated shape type, or documented-and-validated-ignored); guard comparison is purely shape-based.

---

## W3-4 (P3) — Qualify the editor-time Ink-tag validator as best-effort

**Issue.** §B states the editor-time validator pre-checks each `DialogueDefinition`'s Ink `fact:` tags against `_declaredFactWrites` without qualification. Only literal/static tag text is statically determinable; tags whose content is computed inside Ink can't be verified at edit time. (The runtime fragment-footprint guard already backstops this.)

**Change.** Qualify as best-effort — the validator checks statically determinable `fact:` tags only ("where statically determinable"), consistent with the wording already used elsewhere; the runtime guard remains the authoritative enforcement.

**Affects.** §B editor-time validator wording; §H (validator test covers static tags only).

**Acceptance.** Validator flags literal out-of-footprint `fact:` tags at edit time; computed-tag cases are documented as runtime-only; no false failure on dynamic tags.

---

## Suggested integration order
1. **W3-2** at steps 5–7 (keep the mapper pure; derivation in casting/planning).
2. **W3-3** at steps 4–5 (shape type used by the guard and the SOs).
3. **W3-1** at step 11 (save/load boundary).
4. **W3-4** at step 4 (validator wording).
