# Amendments to the Narrative Refactor Plan — Wave 4

Waves 1–3 are assumed integrated. One item; not a data-shape change and not a blocker — a semantic tightening of the footprint guard. Same format and tiers (P2 = correctness/clarity refinement).

---

## W4-1 (P2) — Footprint guard compares by *unresolved subject token*, not just arity

**Issue.** The footprint guard (B3/W2-1) validates a play-time write against the fragment's declared `FactKeyShape` footprint by **namespace + key + subject *arity***. Arity can't distinguish per-entity tokens that share it: `$self` and `$target` are both per-actor, so a footprint declaring `actor.$self.hostile` also permits a write routed through `actor.$target.hostile` — a different actor's fact. For a permission gate that's too loose; a fragment can write the wrong subject's fact undetected.

**Change.** Compare by **namespace + key + the unresolved subject token** — the token the `fact:` tag carries *before* `SubjectResolver` turns it into a concrete id — not merely arity. `actor.$self.hostile` then permits a `$self` write but **not** a `$target` write to the same per-actor fact. Cheap to implement: the `fact:` tag is already parsed with its token, so keep that token alongside the resolved key and compare it literally against the shape's `_subjectToken`. Global (`""`) and arbitrary context keys (`$location`, …) compare by literal token equality too. This also strengthens the W3-4 editor validator (it can now check token + key, not just key). If a fragment legitimately writes several subjects, its footprint simply enumerates each token it uses (bounded and author-known).

**Affects.** §A `FactKeyShape` note; §B `FactEffectApplier` "Footprint conformance" sentence; §H `FactEffectApplierTests`.

**Exact edits.**
- §A `FactKeyShape` bullet — replace
  *"the guard compares writes against shapes by namespace + key + subject arity."*
  with
  *"the guard compares writes against shapes by namespace + key + the unresolved subject token (not merely arity)."*
- §B `FactEffectApplier` "Footprint conformance" — replace
  *"Shapes compare by namespace + key + subject *arity* (e.g. `actor.$self.hostile` matches a write to `actor.<instanceId>.hostile`); `op`/`value` play no part in the comparison."*
  with
  *"Shapes compare by namespace + key + the **unresolved subject token** (`$self`, `$target`, `$location`, `""`, …) — i.e. the token the `fact:` tag carries before `SubjectResolver` turns it into a concrete id — so `actor.$self.hostile` permits a `$self` write but **not** a `$target` write to the same per-actor fact; `op`/`value` play no part in the comparison."*
- §H `FactEffectApplierTests` — add a case: *a `$self`-declared footprint permits a `$self` write but rejects a `$target` write to the same per-actor fact (token, not arity).*

**Acceptance.** A write whose origin token is `$self` passes against a `$self` shape; a write whose origin token is `$target` fails against a `$self`-only footprint for the same per-actor key; global and `$location` tokens match by literal token equality. Slice unaffected (uses only `$self` and `""`).

**Integration.** Step 4 (the `FactEffectApplier` guard), alongside the existing B3/W2-1/W3-3 work.
