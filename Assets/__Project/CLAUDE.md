# AI Coding Rules for Unity Project (C#)

This document defines **mandatory rules** for AI-assisted development in this Unity project.
These rules represent the **architectural constitution** of the project and must be followed at all times.

If a user request conflicts with these rules, **the rules override the request** unless an explicit exception is approved.

---

## 1. Absolute Rules (Non‑Negotiable)

* MUST follow **SOLID** and **KISS** principles at all times
* MUST use **Zenject** for dependency injection
* MUST use **MVP (Model–View–Presenter)** as the default architectural pattern
* MUST keep **MonoBehaviour classes thin** and logic‑free
* MUST prioritize **readability, testability, and maintainability** over speed of writing
* MUST use English in .md files and as the only language for comments

---

## 2. Architectural Overview

The project follows **Clean Architecture** with **MVP**:

### Layers

* **Domain / Core**

  * Pure C# logic
  * No UnityEngine references
  * Business rules and domain models

* **Application**

  * Use cases
  * Presenters
  * Application services

* **Infrastructure**

  * Unity-specific code
  * Input, UI, Audio, Persistence
  * MonoBehaviour adapters

> Dependencies must always point inward.

---

## 3. MVP Rules

### Model

* Pure C# classes
* Contains domain state and rules
* Framework-agnostic
* Fully unit-testable

### View

* Implements a **View interface**
* Inherits from MonoBehaviour
* Contains **NO business logic**
* Handles only:

  * Unity UI
  * Visual updates
  * Forwarding user input to Presenter

### Presenter

* Pure C# class
* NEVER inherits from MonoBehaviour
* Contains application logic
* Depends only on **interfaces**
* Coordinates View and Model

### Communication Rules

* View → Presenter (events / method calls)
* Presenter → View (interface methods)
* Presenter → Model (interfaces)
* View NEVER talks to Model directly

---

## 4. Dependency Injection (Zenject)

* Zenject MUST be used for all dependency management
* All bindings must be declared in **Installers**
* Manual service locators are FORBIDDEN

### Injection Rules

* Prefer **constructor injection** for pure C# classes
* Use field/property injection ONLY for MonoBehaviour adapters
* Never resolve dependencies manually at runtime

> If a class has many dependencies, reconsider its responsibilities (SRP violation).

---

## 5. Unity‑Specific Rules

* MonoBehaviours act only as **adapters**
* Business logic inside MonoBehaviours is FORBIDDEN

### Update Usage

* Avoid logic inside `Update()`
* Prefer:

  * Events
  * Signals
  * Coroutines
  * State machines

### Forbidden Unity APIs

* FindObjectOfType
* GameObject.Find
* GetComponent inside Update

All required references must be cached or injected.

---

## 6. Communication & Events

* Prefer **event-driven architecture**

* Use:

  * C# events / Actions
  * Zenject Signals

* Avoid tight coupling between systems

* Never reference concrete implementations across layers

### Input Handling

* MUST use **Unity Input System**
* Input handling must be abstracted behind interfaces
* Input events must be forwarded to **Presenters**, never handled directly in Views

---

## 7. ScriptableObject Usage

* ScriptableObject is allowed ONLY for:

  * Configuration
  * Static data

* Logic inside ScriptableObject is FORBIDDEN

---

## 8. Error Handling & Debugging

* Implement error handling using `try-catch` blocks where appropriate

  * Especially for file I/O, persistence, and network operations

* Use a **custom Utils/Logger abstraction** for logging

  * Logger may internally use `Debug.Log`, `Debug.LogWarning`, `Debug.LogError`
  * Direct usage of `Debug.*` outside infrastructure layer is discouraged

* Implement meaningful custom error messages

* Use debug visualizations (e.g. Gizmos, debug overlays) where they improve development experience

---

## 9. Testability Rules

* All business logic MUST be testable without Unity
* No static state in domain or application layers
* No hidden dependencies

> If logic is hard to test, it must be refactored.

---

## 9. Code Quality Standards

### Structure

* One class per file
* File name MUST match class name
* Namespaces must reflect architectural layer and context

### Methods & Classes

* Each class has one responsibility
* Methods must be small and focused

### Naming

* Use intention‑revealing names
* Avoid abbreviations
* Avoid vague names

### Constants

* Magic numbers are FORBIDDEN
* Use constants or configuration objects

### Comments

* Comment **WHY**, not WHAT
* Do not comment obvious code

---

## 10. Performance Rules

* Avoid runtime allocations in hot paths
* Avoid LINQ in performance‑critical code
* Optimize only after profiling confirms a problem

---

## 11. Design Patterns

Patterns are allowed ONLY if they reduce complexity.

Preferred patterns:

* MVP
* State
* Strategy
* Observer
* Factory

Patterns used without justification are considered violations.

---

## 12. Explicit Anti‑Patterns (Forbidden)

* God MonoBehaviours
* Static service managers
* Global state
* Hidden dependencies
* Tight coupling between systems
* Business logic in Views

---

## 13. AI Meta‑Rules

Before writing code, AI MUST:

1. Briefly describe the proposed architecture
2. Explain responsibilities of each class
3. Describe dependency flow

When providing code, AI MUST:

* Explain WHY this solution was chosen
* Point out trade‑offs
* Suggest improvements if applicable

If a request violates SOLID, KISS, MVP, or these rules:

* Propose a compliant alternative
* Explain why the original request is problematic

---

## 15. Context7

Always use Context7 MCP when I need library/API documentation, code generation, setup or configuration steps without me having to explicitly ask.

## 16. Final Principle

This is a **production‑quality Unity project**.

All code must be written as if:

* It will be maintained long‑term
* It will be worked on by a team
* It will require extension and testing

Short‑term hacks are NOT acceptable.
