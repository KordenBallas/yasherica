# Product Requirements — Boss / Run Apex (Track Z)

> Status: **verified** — 2026-07-08
> Track: **Z** (`ROADMAP-prioritized.md`)
> Model: **Fable** (multi-phase boss decomposition · Vice pool system · Alliance summon · meta-deed
> gating — all cross-system and correctness-critical)
> Owner: **Hybrid** — code builds the systems; designer authors the Order's Seat Site SO, Vice SOs,
> the Lieutenant actor, and the Alliance Blanks per race.

---

## 1. Goal & Feel

The run has a real climax. The world the player has climbed through — backwater, courts, the Order's
seat — resolves in a confrontation at the **tyrant's throne**. This is the endpoint the whole
escalation ladder points toward.

The apex is **earned across many runs, not reached in one**. Early runs end at the Seat's outer
layers. Later runs reach deeper. The first time the Tyrant himself appears is a milestone a player
remembers.

The Tyrant fight is the **central mirror moment of the game**: he decomposes across the battle,
his corruptions breaking off one by one as separate fighting units, until nothing is left but a bare
nobody — the same hollow beast the player was at the start, at the junkyard. "This is what you
become if you walk the path of Conquest."

The Alliance path reverses the image without a word: the player arrives with racial allies while
the Tyrant stands alone. He accumulates his vices outward and weakens; the player accumulates
allies inward and holds. The board reads the theme.

---

## 2. The Order's Seat — the Site

The Order's Seat is a **unique `apex`-family Site** in the Sites vocabulary:

- **Unique**: one instance per world; it is always the run's terminal Site.
- **Footprint**: a cluster of platforms — the largest Site in the game. The designer authors the
  appearance kit (throne architecture, Order iconography, approach path).
- **Escalation gate**: only reachable after the run's `run_escalation_tier` reaches the authored
  threshold (tier 3). The Site does not appear in earlier stretches.
- **Capacity recipe concept**: a guaranteed **anchor** (the deepest unlocked encounter for this
  player's meta-progress) on the throne platform, with optional **fill** (lesser guards/sentinels)
  on the approach platforms. The anchor is always the run's final encounter.

---

## 3. The Three-Tier Progression Ladder

The Order's Seat offers three escalating encounter tiers. Which tier the player reaches in a given
run is determined by **meta-deeds** persisted in `meta.json`:

| Tier | Encounter | Unlock condition |
|---|---|---|
| **1** | **Vice (solo)** — one Vice drawn from the pool | Always available once escalation gate is met |
| **2** | **Lieutenant** | `world.vices_defeated_count ≥ 2` (authored threshold) |
| **3** | **The Tyrant** | `world.lieutenant_defeated = true` |

The player reaches the deepest tier their deeds have unlocked. In any given run, only one tier is
the anchor encounter — the player does not fight through all three in sequence within one run.

### 3.1 Vices (Tier 1)

A Vice is a **standalone boss-level enemy** embodying one of the Tyrant's corruptions. It is
powerful enough to be a satisfying run-ender in its own right. Fighting a Vice solo (Tier 1) is
how the player first learns each Vice's kit — a knowledge that pays off when that Vice later
appears inside the Tyrant fight.

**Pool selection for Tier 1:** one Vice is drawn from the base pool each run (weighted, variable
between runs). Heat-pool Vices enter the eligible set when the run's current Heat meets their
`min_heat` threshold.

**Demo Vice set (MVP — two base, one Heat placeholder):**

| Name | Theme | `min_heat` |
|---|---|---|
| **Gluttony** | The cauldron's seduction taken to its end — insatiable devouring, no restraint; aggressive and consuming; many maws | `0` |
| **Paranoia** | Total isolation as calcified self-protection; mistrust made flesh; defensive, withdraws, punishes approach | `0` |
| **Carnage** *(placeholder)* | Killing as identity; violence that has forgotten any purpose beyond itself | `2` |

The Vice roster is designed to grow over time. Full vice content — names, lore, kits — is a
separate narrative/content pass, deliberately not locked in this brief.

### 3.2 The Lieutenant (Tier 2)

A **personal, named recurring actor** — the Tyrant's right hand. The Director may have placed this
character in earlier runs as a recurring actor, so the player may recognise them when they appear
at the Order's Seat.

The Lieutenant is a powerful authored enemy with a defined combat kit and a Director actor identity.
Their specific design (name, lore, kit) is a separate narrative content pass. This brief establishes
the **slot** in the ladder and the **meta-deed** that unlocks it.

### 3.3 The Tyrant (Tier 3)

The run's true climax. The Tyrant fight begins as a single encounter; it grows as he breaks apart.

**Decomposition mechanic:**

1. The fight opens with the Tyrant as the sole active unit.
2. At each authored HP threshold (3–5 thresholds; authored on the Tyrant encounter SO), a **Vice
   spawns out of him** and joins the fight as a separate active unit on the hex board.
3. The spawned Vices are drawn from a **per-run selection** of 2–3 Vices from the pool (base +
   Heat-eligible), chosen at run-start — the player does not know which ones until they appear.
4. The Vices that appear in the Tyrant fight are the **same enemy types** the player may have
   fought as standalone Tier 1 bosses. Familiarity is intentional.
5. Spawned Vices have **reduced HP** compared to their Tier 1 solo version (a per-Vice authored
   ratio; tunable in playtest).
6. After all HP thresholds are crossed: the Tyrant's **final form** — a bare, ability-less unit,
   without kit or parts. A hollow nobody. A mirror.

**Vice pool expansion:** after the player defeats the Tyrant for the first time, new Vice SOs
authored by the designer (tied to narrative/lore) enter the pool and can appear in both Tier 1
runs and future Tyrant fights.

---

## 4. Alliance Summon — the Racial Ally Mechanic

### 4.1 How the ability is acquired

Each race has a **quest chain**. The chain begins with simpler racial quests and unlocks
progressively deeper branches. Completing a race's **Alliance branch** (the deepest branch in
its chain) rewards a special **Alliance Blank** — a distinct blank type within the existing
Part-Blank system.

When the Alliance Blank is **unsealed**, it installs as a **body part** whose active ability is
`Summon [Race] Ally`.

**This part occupies a body slot.** This is the intended design. Choosing the Alliance path means
spending a slot that could hold a combat part — the same tradeoff as the passport model (belonging
costs slots, power costs belonging). A player can carry up to one Alliance part per race; three
races = up to three potential Alliance slots.

The Alliance Blank is **meta-progress-gated**: a player who has not completed enough racial quests
in prior runs will not have access to the Alliance branch. The alliance builds across runs.

### 4.2 How it works in combat

- The Summon ability is used as a standard ability on the player's turn (same ability queue and
  turn structure as all other abilities).
- On activation: a **racial ally unit** spawns on the hex board adjacent to the player.
- The racial ally is a **playable unit under the player's full control**: the player queues its
  moves and abilities each round; it appears in the turn-order strip.
- The ally has its own authored anatomy and kit (a racial ally unit SO authored per race; the ally
  reads anatomically as a member of that race, consistent with Pillar 4).
- Duration and cooldown: authored on the ability SO (e.g., N rounds of presence, or until
  defeated; one use per combat).

### 4.3 The contrast on the final board

The board of the Tyrant fight reads the game's two endings without narrative text:

- **Conquest path** (no Alliance parts equipped): the player is alone; the Tyrant is alone; the
  board grows from 1v1 to 1v(Tyrant + vices) as he decomposes outward. Loneliness meeting
  loneliness.
- **Alliance path**: the player arrives with summoned allies; the Tyrant decomposes alone. As he
  breaks apart, the player holds or grows. The Tyrant ends isolated with his vices; the player ends
  with friends.

**Ending determination:** which ending the player receives is based on whether at least one
Alliance summon part was equipped and used in the Tyrant fight. This is written as a run fact and
read by the ending narrative beat (the beats themselves are a separate out-of-scope content pass).

---

## 5. Meta-Progression Deeds

Two new deed types persisted in `meta.json`:

| Fact key | Written when | Threshold / value |
|---|---|---|
| `world.vices_defeated_count` | Incremented each time the player defeats a Vice at Tier 1 (each unique Vice counted once — repeat defeats of the same Vice do not increment) | Lieutenant unlocks at designer-authored threshold (default `2`) |
| `world.lieutenant_defeated` | Set `true` on first Lieutenant defeat | Tyrant unlocks |

Both thresholds are authored values — adjustable without code changes.

---

## 6. Heat Integration

- Each Vice SO carries a `min_heat` field (same pattern as `min_tier` on stories and enemies):
  `min_heat: 0` = base pool; `min_heat: N` = Heat-pool, only eligible when run Heat ≥ N.
- The Tyrant's Vice selection for the fight filters the full pool by the run's current Heat.
- Defeating the Tyrant triggers the `world.heat_high_water` update (the seam already exists from
  Track Y — `heat-ascension.md`).

---

## 7. Authoring — Adding Content

### Adding a Vice

1. Create a Vice SO under `Resources/Enemies/Vices/` (the SO type and schema is code-track's
   responsibility).
2. Author: display name, combat parts/kit references, solo HP, in-Tyrant HP ratio, `min_heat`.
3. A `min_heat: 0` Vice enters the base pool immediately and is eligible for both Tier 1 runs and
   Tyrant decomposition. No additional wiring required.

### Adding the Lieutenant

1. Author a named Director actor SO with the Lieutenant's identity.
2. Author their combat encounter (kit, HP, difficulty profile).
3. The Lieutenant actor can be placed by the Director in earlier runs as a recurring character
   (optional — gives the player recognition before the Order's Seat).

### Adding a racial Alliance Blank

1. Author a special `PartBlankDefinition` flagged as an alliance blank, referencing the target race.
2. Author the `Summon [Race] Ally` ability it unseals into (duration, cooldown, spawn behavior).
3. Author the racial ally unit SO (anatomy, kit — should read as a rank-and-file member of that
   race, not a unique hero).
4. Wire the Alliance Blank as the terminal reward of the race's Alliance quest branch.

---

## 8. Acceptance Criteria

- [ ] The Order's Seat appears as a Site at the end of runs that have reached escalation tier 3
- [ ] A player with no meta-deeds faces a Tier 1 Vice (base pool) as the final encounter
- [ ] A Vice defeated at Tier 1 increments `world.vices_defeated_count` (unique Vices only)
- [ ] After the deed threshold is met, the Lieutenant appears as the final encounter on subsequent runs
- [ ] `world.lieutenant_defeated` is written on first Lieutenant defeat; subsequent runs show the Tyrant
- [ ] The Tyrant fight begins with one unit; Vice units spawn at authored HP thresholds
- [ ] Spawned Vices are recognisable as the same enemy type the player may have fought at Tier 1
- [ ] The Tyrant's final form (post all thresholds) is a bare, ability-less unit
- [ ] The per-run Vice selection for the Tyrant fight varies between runs; Heat-pool Vices appear only at sufficient Heat
- [ ] An Alliance Blank unseals into a body part occupying one body slot
- [ ] A player with an Alliance part can activate the Summon ability in combat; the ally is controllable on the hex board
- [ ] The ally appears in the turn-order strip and the player queues its moves/abilities
- [ ] A player without Alliance parts faces the Tyrant fight with no summoned allies
- [ ] Ending fact (alliance vs. conquest) is written correctly at fight end
- [ ] `world.heat_high_water` updates on Tyrant defeat

---

## 9. Out of Scope

- Full Vice roster beyond the 3 demo entries — separate narrative/lore design pass
- The Lieutenant's specific identity, name, lore, and combat kit — separate content pass
- Racial ally unit designs (anatomy, kit per race) — authored per race in a content pass
- The specific racial quest chains that unlock Alliance branches — designed in a narrative pass
- Conquest ending narrative beat ("you replace the tyrant") — separate scene/story pass
- Alliance ending narrative beat — same
- HP values, pool weights, scaling numbers, tuning — playtest
- Per-path local apexes (distinct biome bosses per race-path) — deferred (`world/overview.md` §11)
- Truth ending — deferred
- Order's Seat appearance kit and dressing — designer-authored; no code dependency
