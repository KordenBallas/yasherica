# AI Coding Rules for Unity Project (C#)

This document defines **mandatory rules** for AI-assisted development in this Unity project.
These rules are the **architectural constitution** of the project and must be followed at all times.

If a user request conflicts with these rules, **the rules override the request** unless an explicit
exception is approved in that conversation.

---

## 0. How AI must work (read first)

Before writing or changing any code, AI MUST produce a short plan:

1. **Restate the goal** in one or two sentences.
2. **Describe the proposed architecture** — which layer each new class lives in, its single
   responsibility, and the dependency flow (always inward).
3. **State the documentation impact**: which doc(s) in `Assets/__Project/Docs/` change, whether a
   new ScriptableObject type or content recipe is introduced, and the CHANGELOG / ROADMAP edits the
   change will carry. See §8 — this is not optional.

Then, when delivering code, AI MUST:

* Explain **why** this solution was chosen, not only what it does.
* Point out trade-offs and any rule tension.
* Suggest improvements, and add them to the ROADMAP per §8 rather than silently expanding scope.

If a request violates SOLID, KISS, MVP, or any rule here: propose a compliant alternative and
explain why the original is problematic — do not quietly comply.

**Effort calibration.** For a single-class or single-system change, a brief plan is enough. For any
change that crosses systems, alters a public contract, or introduces a new ScriptableObject type,
think hard first (`ultrathink`) and write the plan into the relevant doc before coding.

A change is **not complete** until its code, its tests, its system doc, the CHANGELOG, and (if
scope changed) the ROADMAP are all updated **in the same change**. Stale docs are treated as bugs.

---

## 1. Absolute Rules (Non‑Negotiable)

* MUST follow **SOLID** and **KISS** at all times.
* MUST use **Zenject** for dependency injection.
* MUST use **MVP (Model–View–Presenter)** as the default architectural pattern.
* MUST keep **MonoBehaviour classes thin** and logic‑free (adapters only).
* MUST prioritize **readability, testability, and maintainability** over speed of writing.
* MUST use **English** in all `.md` files and as the only language for code comments.
* MUST keep content extension **data‑driven**: new game content (abilities, artifacts, biomes,
  NPCs, stories, quests, body parts, …) is added by authoring ScriptableObjects + assets, **not**
  by writing code. If new content cannot be added without code, that is a design gap — record it
  in the ROADMAP (§8) and prefer fixing the data path over hard‑coding.

---

## 2. Architectural Overview

The project follows **Clean Architecture** with **MVP**. Dependencies always point inward.

* **Domain / Core** — pure C# logic, **no UnityEngine references**, business rules and domain models.
* **Application** — use cases, presenters, application services.
* **Infrastructure** — Unity-specific code: input, UI, audio, persistence, MonoBehaviour adapters.

Visual/gameplay-infrastructure systems where literal MVP does not apply (e.g. the character system)
still keep the spirit: a testable pure-C# domain, thin MonoBehaviours, constructor-injected plain
classes. The layering rule (Core has no UnityEngine reference; it is unit-testable) is never waived.

---

## 3. MVP Rules

**Model** — pure C# classes; domain state and rules; framework-agnostic; fully unit-testable.

**View** — implements a View interface; inherits MonoBehaviour; **no business logic**; handles only
Unity UI, visual updates, and forwarding user input to the Presenter.

**Presenter** — pure C# class; **NEVER** inherits MonoBehaviour; contains application logic; depends
only on interfaces; coordinates View and Model.

**Communication:** View → Presenter (events / method calls); Presenter → View (interface methods);
Presenter → Model (interfaces). **View never talks to Model directly.**

---

## 4. Dependency Injection (Zenject)

* Zenject MUST be used for all dependency management; all bindings live in **Installers**.
* Manual service locators are **FORBIDDEN**.
* Prefer **constructor injection** for pure C# classes; use field/property injection ONLY for
  MonoBehaviour adapters; never resolve dependencies manually at runtime.
* If a class has many dependencies, reconsider its responsibilities (likely an SRP violation).

---

## 5. Unity‑Specific Rules

* MonoBehaviours act only as **adapters**; business logic inside them is **FORBIDDEN**.
* Avoid logic inside `Update()`; prefer events, signals, coroutines, state machines.
* **Forbidden APIs:** `FindObjectOfType`, `GameObject.Find`, `GetComponent` inside `Update`.
  All references must be cached or injected.

---

## 6. Communication & Events

* Prefer **event-driven architecture**: C# events / `Action`s, or Zenject Signals.
* Avoid tight coupling; never reference concrete implementations across layers.
* **Input:** MUST use the Unity Input System, abstracted behind interfaces, forwarded to Presenters —
  never handled directly in Views.

---

## 7. ScriptableObject Usage (data-driven content)

* ScriptableObject is allowed ONLY for **configuration and static data**. Logic inside a
  ScriptableObject is **FORBIDDEN** (eligibility helpers may be pure predicates over their own data).
* ScriptableObjects are the project's **content extension surface**. Every content type a designer
  can add must be expressible as one SO asset (plus referenced assets such as an Ink JSON, icon,
  prefab, or mesh).
* SO → Core conversion happens through an explicit mapper at install/generation time (the only
  bridge from the Data layer into Core). Core records never hold Unity types.
* **Every SO type MUST be documented** in its system's doc (§8): asset-menu path, every field with
  its meaning, the `Resources/` load path or installer wiring, and the step-by-step recipe to add a
  new instance. A new SO type without doc coverage is an incomplete change.

---

## 8. Documentation Discipline (Docs / CHANGELOG / ROADMAP)

All project documentation lives in **`Assets/__Project/Docs/`**. It is the maintained source of
truth: **if code and a doc disagree, the doc is wrong and must be fixed in the same change.**

### 8.1 System docs

* **One `.md` per system or subsystem.** New system → new doc, added to `Docs/README.md`.
* Every system doc MUST follow `Docs/_TEMPLATE.md`, which mandates, in order:
  requirements → architecture → **ScriptableObject Reference** → **Adding Content** → tests →
  known limitations / open points.
* The **ScriptableObject Reference** section lists every SO type the system owns: asset-menu path,
  field-by-field table, load/wiring convention.
* The **Adding Content** section is an **asset-only, step-by-step recipe** for each content type the
  system exposes (e.g. "Add an ability", "Add an artifact", "Add a biome", "Add an NPC",
  "Add a story / quest", "Add a body part"). A designer must be able to follow it without reading code.
* Docs describe systems **as implemented**. Planned/unimplemented behavior appears ONLY under a
  clearly marked "Known limitations / open points" or "Planned design (NOT implemented)" heading.

### 8.2 CHANGELOG

* Single file: `Assets/__Project/Docs/CHANGELOG.md`, reverse-chronological, "Keep a Changelog" style
  (grouped Added / Changed / Fixed / Removed under a dated, system-tagged entry).
* **Every functional change appends an entry in the same change as the code.** Reference the
  affected system and, where useful, the requirement id (e.g. `Loot R7`).

### 8.3 ROADMAP

* Single file: `Assets/__Project/Docs/ROADMAP.md`, checkbox items grouped by system plus a backlog.
* AI MUST update the ROADMAP whenever:
  * the user states a **new requirement** → add it as an unchecked item under the right system;
  * AI itself **proposes an enhancement, refactor, or notices a limitation** → add it (do not just
    mention it in chat and forget it);
  * an item is **implemented** → check it off and move the substance into the CHANGELOG.
* A system doc's "Known limitations / open points" entries and the ROADMAP must stay consistent:
  fixing a limitation removes it from both the doc and the roadmap and adds a CHANGELOG line.

---

## 9. Error Handling & Debugging

* Use `try-catch` where appropriate, especially for file I/O, persistence, and network operations.
* Log through the **custom Utils/Logger abstraction** (`IGameLogger`). Direct `Debug.*` outside the
  infrastructure layer is discouraged; domain/application code using `Debug.*` is a rule violation
  and belongs on the ROADMAP until fixed.
* Write meaningful, custom error messages. Use debug visualizations (Gizmos, overlays) where they
  improve the development experience.

---

## 10. Testability

* All business logic MUST be testable without Unity (edit-mode NUnit in `Assets/__Project/Tests/`).
* No static state and no hidden dependencies in domain or application layers.
* If logic is hard to test, refactor it.

---

## 11. Code Quality

* One class per file; file name matches class name; namespaces reflect architectural layer + context.
* One responsibility per class; small, focused methods.
* Intention-revealing names; no abbreviations or vague names.
* **Magic numbers FORBIDDEN** — use constants or configuration objects.
* Comment **WHY**, not WHAT; do not comment obvious code.

---

## 12. Performance

* Avoid runtime allocations in hot paths; avoid LINQ in performance-critical code.
* Optimize only after profiling confirms a problem.

---

## 13. Design Patterns

Patterns are allowed ONLY when they reduce complexity. Preferred: MVP, State, Strategy, Observer,
Factory. A pattern used without justification is a violation.

---

## 14. Explicit Anti‑Patterns (Forbidden)

God MonoBehaviours · static service managers · global state · hidden dependencies · tight coupling
between systems · business logic in Views.

---

## 15. Tooling

* Always use the **Context7 MCP** when library/API documentation, code generation, or setup/config
  steps are needed — without being asked explicitly.

---

## 16. Final Principle

This is a **production-quality Unity project**. Write all code as if it will be maintained
long-term, extended, and tested. Short-term hacks are not acceptable — and neither is undocumented
work (§8).
