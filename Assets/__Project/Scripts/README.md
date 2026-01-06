# Yasherica - Design Documentation Index

**Project:** Yasherica - Action-Adventure Tactical Game  
**Status:** Active Development  
**Architecture:** Clean Architecture with MVP Pattern

---

## 📚 Documentation Overview

This folder contains **living design documents** that describe the entire game system. These documents are updated as the game evolves.

### Main Design Documents

1. **[GAME_DESIGN.md](GAME_DESIGN.md)** 🎮
   - Overall game concept and vision
   - Core mechanics overview
   - High-level system descriptions
   - Technical architecture principles
   - Complete implementation status tracker
   - Development roadmap

2. **[WORLD_DESIGN.md](WORLD_DESIGN.md)** 🗺️
   - Platform navigation system
   - Procedural area generation
   - Platform types and behaviors
   - Graph-based level structure
   - Generation pipeline details
   - Implementation status: **100% Complete**

3. **[COMBAT_DESIGN.md](COMBAT_DESIGN.md)** ⚔️
   - Turn-based tactical combat system
   - Hex-based positioning
   - Ability system with cooldowns
   - AI opponents (Random + Tactical)
   - Network multiplayer support
   - Implementation status: **100% Complete**

---

## 🎯 Quick Navigation

### By Topic

**Want to understand the game?** → Start with [GAME_DESIGN.md](GAME_DESIGN.md)

**Working on world/platforms?** → See [WORLD_DESIGN.md](WORLD_DESIGN.md)

**Working on combat?** → See [COMBAT_DESIGN.md](COMBAT_DESIGN.md)

**Need implementation details?** → Check subsystem docs:
- Combat: `Combat/FULL_SYSTEM_COMPLETE.md`
- Combat Quick Start: `Combat/QUICK_START_GUIDE.md`
- Area Generation: `AREA_GENERATOR_USAGE.md`

### By Role

**Game Designer:**
- Start with GAME_DESIGN.md for overview
- Review COMBAT_DESIGN.md for balance mechanics
- Check WORLD_DESIGN.md for level structure

**Programmer:**
- Read GAME_DESIGN.md → Technical Architecture section
- Refer to system-specific docs for implementation details
- Check CLAUDE.md for coding rules

**Artist/Designer:**
- WORLD_DESIGN.md for platform visualization needs
- COMBAT_DESIGN.md for UI/VFX requirements
- GAME_DESIGN.md for overall visual direction

---

## 📂 Project Structure

```
Scripts/
├── GAME_DESIGN.md           ⭐ Main design doc
├── WORLD_DESIGN.md          ⭐ World system design
├── COMBAT_DESIGN.md         ⭐ Combat system design
├── README.md                ← You are here
│
├── Combat/                  💯 Fully implemented
│   ├── Core/                # Pure C# combat logic
│   ├── TurnManagement/      # Turn order system
│   ├── Execution/           # Action validation & execution
│   ├── Player/              # Human, AI, Network players
│   ├── View/                # Combat visualization
│   ├── Networking/          # Netcode integration
│   ├── Integration/         # Combat-world integration
│   └── DI/                  # Zenject bindings
│
├── Platform/                💯 Fully implemented
│   ├── Model/               # Platform data
│   ├── StateMachine/        # State management
│   ├── Content/             # Platform content types
│   ├── Interfaces/          # Platform abstractions
│   └── View/                # Platform visualization
│
├── LevelGeneration/         💯 Fully implemented
│   ├── Area/                # Area generation
│   ├── Graph/               # Platform graph generation
│   ├── Scenario/            # Scenario definitions
│   └── Builder/             # Platform builders
│
├── Battlefield/             💯 Fully implemented
│   ├── Core/                # Hex coordinates, grid
│   ├── Grid/                # Grid implementations
│   └── View/                # Grid visualization
│
├── Character/               🟡 Partial (60%)
├── UI/                      🟡 Partial (40%)
└── Core/                    ✅ Utilities & DI
```

---

## 🔄 Living Documentation Process

These documents are **living** - they evolve with the codebase:

### When to Update

Update relevant design docs when you:
- ✅ Add new game systems or features
- ✅ Make significant architectural changes
- ✅ Complete implementation milestones
- ✅ Change core mechanics or rules
- ✅ Add new content types (abilities, platforms, etc.)

### How to Update

1. **Make code changes** following architecture rules
2. **Update relevant design doc** sections:
   - Core Mechanics (if gameplay changed)
   - Implementation Status (mark as complete/in-progress)
   - Technical details (if architecture changed)
3. **Update version history** at bottom of doc
4. **Commit both code and docs** together

### Document Maintenance

- **GAME_DESIGN.md** - Updated on major milestones or roadmap changes
- **WORLD_DESIGN.md** - Updated when platform/generation systems change
- **COMBAT_DESIGN.md** - Updated when combat mechanics change

---

## 🏗️ Architecture Principles

All code follows **strict architectural rules** defined in `CLAUDE.md`:

### Core Principles

1. **Clean Architecture** - Dependency rules strictly enforced
2. **MVP Pattern** - MonoBehaviours are thin views, logic in presenters
3. **SOLID Principles** - Single Responsibility, Open/Closed, etc.
4. **Dependency Injection** - Zenject for all service bindings
5. **Immutability** - Core game state is immutable
6. **Type Safety** - No dictionaries, typed classes everywhere

### Layer Dependency Rules

```
Presentation → Application → Domain → Core
     ↓              ↓           ↓        ↓
  Unity      Controllers    Pure C#   Utils
  Input      Presenters     Models
  Views      Use Cases      Rules
```

**Rule:** Inner layers NEVER depend on outer layers!

---

## 🎮 Current Implementation Status

### ✅ Fully Implemented (100%)

- **Combat System** - Complete turn-based tactical combat
- **Platform System** - Navigation and state management
- **Level Generation** - Procedural area creation
- **Battlefield** - Hex grid system
- **Networking** - Multiplayer support (Netcode)
- **AI** - Random and tactical opponents

### 🟡 Partially Implemented

- **Character System** (60%) - Movement works, progression pending
- **UI System** (40%) - Combat UI done, world UI pending

### 🔴 Not Yet Implemented

- Core game loop (scene transitions, save/load)
- Content systems (items, loot, enemies)
- Polish (animations, VFX, audio)
- Advanced features (replays, editor, modding)

See [GAME_DESIGN.md](GAME_DESIGN.md) for detailed roadmap.

---

## 📖 Additional Resources

### Code Rules & Guidelines
- **CLAUDE.md** - AI coding rules and architecture constitution
- **ARCHITECTURE_ANALYSIS.md** - Architecture analysis
- **ARCHITECTURE_PROPOSAL.md** - Architecture proposals

### System-Specific Docs
- **Combat/FULL_SYSTEM_COMPLETE.md** - Complete combat implementation
- **Combat/QUICK_START_GUIDE.md** - How to use combat system
- **AREA_GENERATOR_USAGE.md** - How to use level generation

### Implementation Tracking
- **IMPLEMENTATION_STATUS.md** - Detailed status tracking
- **CHARACTER_MOVEMENT_FIX_REPORT.md** - Movement system fixes

---

## 🚀 Getting Started

### For New Team Members

1. Read [GAME_DESIGN.md](GAME_DESIGN.md) for overview
2. Read CLAUDE.md for coding rules (mandatory!)
3. Explore system-specific docs based on your area
4. Set up Unity project and required packages
5. Run existing test scenes to see systems in action

### For New Features

1. Check if feature fits existing systems
2. Read relevant design doc
3. Follow architecture rules in CLAUDE.md
4. Implement feature
5. Update design doc
6. Test and document

---

## 📝 Document Version History

| Document | Version | Last Updated | Status |
|----------|---------|--------------|--------|
| GAME_DESIGN.md | 1.0 | Jan 2026 | 🟢 Current |
| WORLD_DESIGN.md | 1.0 | Jan 2026 | 🟢 Current |
| COMBAT_DESIGN.md | 1.0 | Jan 2026 | 🟢 Current |

---

## 💡 Tips

- **Use Ctrl+F** to search within documents
- **Check "Implementation Status"** sections to see what's done
- **Look for code examples** in design docs
- **Follow cross-references** between documents
- **Update docs** when you change systems!

---

**Questions?** Check the relevant design document first, then ask the team!

**Found outdated info?** Update the document and commit the change!

**Need to add a system?** Create appropriate documentation following these templates!

