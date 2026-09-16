# Tanam Sawit — Product Requirements Document (PRD)
## Upgrade & Modernization Plan

**Document Version:** 1.0  
**Date:** September 16, 2026  
**Prepared by:** Hermes Agent (Codebase Analysis)  
**Project Location:** `C:\Users\rafin\My project (1)\`

---

## 1. Executive Summary

**Tanam Sawit** is a 2D palm oil plantation tycoon simulation game built in Unity 6 (6000.6.0f1) with URP 2D. The player manages a palm oil business across 4 areas (Kebun/Plantation, Perumahan/Housing, Kota/City, Pabrik/Factory), balancing economic growth against ecological karma consequences, competing against a rival cousin for inheritance.

The codebase is functional and well-architected with clean event-driven patterns, but requires modernization in several key areas: **rendering fidelity**, **UI/UX polish**, **content depth**, **audio**, **performance optimization**, and **platform readiness**.

---

## 2. Current State Analysis

### 2.1 Tech Stack

| Component | Current Version | Status |
|---|---|---|
| Unity Editor | 6000.6.0f1 (Unity 6) | ✅ Latest |
| Render Pipeline | URP 2D (com.unity.render-pipelines.universal 17.6.0) | ✅ Current |
| Input System | com.unity.inputsystem 1.20.0 (dual: old + new) | ⚠️ Mixed |
| TextMeshPro | Imported | ✅ Available |
| 2D Animation | com.unity.2d.animation 16.0.0 | ✅ Available |
| 2D Tilemap | com.unity.2d.tilemap + extras 9.0.0 | ✅ Available |
| Aseprite Importer | com.unity.2d.aseprite 6.0.0 | ✅ Available |
| AI Inference | com.unity.ai.inference 2.6.1 | ✅ Available |
| Visual Scripting | com.unity.visualscripting 1.9.12 | ✅ Available |

### 2.2 Architecture Quality: GOOD

**Strengths:**
- **Clean Singleton pattern** — GameManager, EconomyManager, TimeManager, etc. all use proper singleton with DontDestroyOnLoad
- **Event-driven Observer pattern** — OnMoneyChanged, OnDayPassed, OnLandPercentageChanged, etc. keep systems decoupled
- **Save/Load v2 system** — Multi-slot JSON save with versioning and legacy migration
- **Namespace organization** — TanamSawit.Core, .Managers, .UI, .NPC, .Workers, .Buildings, .Environment, .SaveSystem, .Player
- **DefaultExecutionOrder attributes** — Proper script execution ordering
- **XML documentation** — All public methods have Indonesian-language doc comments

**Weaknesses:**
- Some managers use `FindGameObjectWithTag("Player")` and `FindFirstObjectByType<>()` at runtime — could use cached references
- Worker system has dual state (WorkerController mono + serializable Worker data) that can drift out of sync
- ModernTycoonHUD still contains 707 lines of legacy IMGUI code with strangler flags (SHOW_TOPBAR=false, etc.) — dead code

### 2.3 Codebase Statistics

| Category | Files | Est. Lines |
|---|---|---|
| Core Systems | 3 | ~510 |
| Managers | 9 | ~2,450 |
| Player | 2 | ~490 |
| NPC + Dialogue | 4 | ~330 |
| Workers | 2 | ~480 |
| Buildings | 2 | ~410 |
| Environment | 6 | ~1,750 |
| UI (uGUI) | 8 | ~1,950 |
| UI (IMGUI legacy) | 2 | ~1,000 |
| Save System | 3 | ~650 |
| Editor Tools | 2 | ~450 |
| **Total** | **~45** | **~10,500+** |

---

## 3. Detailed Upgrade Plan

### Phase 1: Visual & Rendering Upgrade (Week 1-2)
**Goal:** Replace primitive colored squares with proper 2D art assets and lighting

#### 1.1 Sprite & Tile Assets
- [ ] Create/commission pixel art sprite sheets for:
  - Player character (8-direction walk cycle, idle, run animations)
  - Worker NPCs (unique sprites per role: mandor, pemanen, operator)
  - Animals (monkey, elephant, tiger — for karma events)
  - Buildings (11 facility types with unique designs)
  - Ground tiles (grass, road, factory floor, tileable)
  - Trees/palm vegetation (decorative + functional)
  - UI icons (money, stamina, karma, time, settings)
- [ ] Set up Tile Palette with rule tiles for ground variation (3+ shades per area)
- [ ] Configure Aseprite import pipeline for animated sprites

#### 1.2 URP 2D Lighting
- [ ] Enable URP 2D Renderer Data with:
  - Global Light for ambient day/night color grading
  - Point Lights for buildings at night (warm glow for kos-kosan, factory, bank)
  - 2D Shadow Caster components on buildings/player for dynamic shadows
- [ ] Replace DayNightCycle's screen-space overlay with volume-based color grading:
  - Post-processing Volume profiles per time-of-day
  - Color Adjustments (saturation, contrast, hue shift)
  - Bloom for nighttime light sources
- [ ] Add emission maps for building windows/signs visible at night

#### 1.3 Visual Effects
- [ ] Particle systems for:
  - Harvest animation (palm fruit falling)
  - Factory smoke/processing sparks
  - Monkey invasion (banana peels, chaos)
  - Elephant dust cloud
  - Rain during karma events
- [ ] Screen shake for elephant attack / landslide events

### Phase 2: UI/UX Modernization (Week 2-3)
**Goal:** Complete the IMGUI→uGUI migration and add polish

#### 2.1 Remove Legacy IMGUI
- [ ] Delete ModernTycoonHUD.cs (707 lines) after verifying all features migrated to uGUI
- [ ] Delete TycoonHUD.cs (legacy debug HUD)
- [ ] Remove `OnGUI()` methods from AreaTransitionManager (use uGUI fade panel instead)
- [ ] Audit all files for `using UnityEngine.IMGUI` or `GUI.*` references

#### 2.2 uGUI Panel Completion
- [ ] TopBarUI — Add animated cash counter (count up/down on transactions)
- [ ] NotificationTickerUI — Add slide-in/out animation, queue system for overlapping events
- [ ] FacilityModalUI — Add icons, progress bars, confirmation dialogs for purchases
- [ ] GameOverUI — Add ending illustration, stats summary, "Play Again" / "Main Menu" buttons
- [ ] DialogueUI — Add typewriter text effect, speaker portrait, choice hover animation
- [ ] MinimapUI — Render actual world view (ground colors, building positions, NPC dots)
- [ ] SettingsPanel — Add volume sliders, quality selector, keybind display
- [ ] MainMenu — Animated background, game logo, version number, credits button

#### 2.3 Responsive Layout
- [ ] Verify all panels scale correctly at 1280×720, 1920×1080, 2560×1440, 3840×2160
- [ ] Safe area handling for ultra-wide and 4:3 aspect ratios
- [ ] CanvasScaler reference resolution audit

### Phase 3: Content & Gameplay Depth (Week 3-5)
**Goal:** Expand mechanics, balance economy, add replayability

#### 3.1 Economy Balancing
- [ ] Data balance pass using spreadsheet model:
  - Starting money, land cost, worker salary, TBS/CPO prices
  - Ensure 10-year path to victory is achievable but challenging
  - Model worst-case (all karma events + pinjol) for bankruptcy threshold
- [ ] Add market price fluctuation:
  - TBS/CPO prices vary ±20% based on random events
  - News ticker explains price changes ("Harga CPO naik karena permintaan Eropa meningkat")
- [ ] Add land valuation tiers (prime land near kebun costs more)

#### 3.2 Worker System Expansion
- [ ] Worker skills progression (gain XP → level up → harvest bonus)
- [ ] Worker happiness/moral system (affects productivity, can quit if unhappy)
- [ ] Worker housing quality affects stamina recovery rate
- [ ] Worker types specialization:
  - Harvester (faster TBS collection)
  - Operator (factory efficiency bonus)
  - Guard (reduces monkey theft chance)
  - Scout (detects elephant early, avoids factory damage)

#### 3.3 New Facilities
- [ ] **Pohon Sawit Nursery** — Replant trees, reduce karma accumulation rate
- [ ] **R&D Lab** — Research upgrades (better seeds, faster harvest, disease resistance)
- [ ] **Koperasi** — Worker-owned cooperative, passive income share but lower profit margin
- [ ] **Tourist Lodges** — Eco-tourism passive income, only if karma < 75%

#### 3.4 Event System Expansion
- [ ] Random events beyond karma:
  - Disease outbreak (affects TBS yield)
  - Government subsidy (one-time cash injection)
  - Worker strike (if happiness too low)
  - Fire (factory damage, emergency cost)
  - Heavy rain (floods, delayed harvest)
  - Good harvest season (bonus yield)
- [ ] Event queue with priority (karma events override random events)
- [ ] Event log/history accessible from Tablet

#### 3.5 Dialogue & NPC Depth
- [ ] Branching dialogue trees with memory (NPCs remember past choices)
- [ ] Quest system (NPC gives tasks → rewards)
- [ ] Relationship meter per NPC (gift/help → unlock better rates)
- [ ] Dynamic dialogue based on game state (e.g., mandor comments on karma level)
- [ ] Localization support framework (ID → EN toggle, string tables)

### Phase 4: Audio System (Week 4-5)
**Goal:** Add immersive audio layer

#### 4.1 Music
- [ ] Dynamic soundtrack that shifts with time-of-day:
  - Morning: cheerful acoustic guitar
  - Day: upbeat gamelan/orchestral
  - Evening: relaxed ambient
  - Night: quiet, contemplative
- [ ] Area-specific music variants
- [ ] Tension music during karma events
- [ ] Victory/defeat stingers

#### 4.2 Sound Effects
- [ ] Footsteps (grass, road, factory floor variants)
- [ ] Coin/chime for money transactions
- [ ] Chopping sound for harvest
- [ ] Factory processing hum
- [ ] Animal sounds (monkey screech, elephant trumpet, tiger roar)
- [ ] UI click/hover sounds
- [ ] Portal transition whoosh
- [ ] Day/night ambience (birds, crickets, wind)

#### 4.3 Audio Implementation
- [ ] AudioManager singleton with music/SFX buses
- [ ] Mixer groups with exposed volume parameters
- [ ] Object pooling for SFX (avoid instantiation overhead)
- [ ] Spatial audio for ambient sounds (if using AudioSource2D)

### Phase 5: Performance Optimization (Week 5-6)
**Goal:** Ensure smooth 60 FPS on mid-range hardware

#### 5.1 Rendering
- [ ] Tilemap chunking for large worlds (only render visible chunks)
- [ ] Sprite atlas packing (reduce draw calls from ~50 to <15)
- [ ] GPU instancing for repeated elements (trees, workers)
- [ ] LOD system for minimap (simplified representation)
- [ ] Profiler analysis: target <10ms CPU frame time for game logic

#### 5.2 Code Optimization
- [ ] Replace runtime `FindGameObjectWithTag("Player")` with cached reference
- [ ] Replace `FindFirstObjectByType<>()` with cached singletons
- [ ] Object pooling for WorkerController instances
- [ ] Object pooling for UI buttons (FacilityModalUI already pools — extend pattern)
- [ ] Move heavy Update() logic to coroutines or timed intervals
- [ ] Reduce LINQ allocations in hot paths (TabletUI.RefreshContent uses LINQ)

#### 5.3 Memory
- [ ] Addressable Asset System for art/audio (load on demand, not all at boot)
- [ ] Texture compression (ASTC for mobile, DXT for desktop)
- [ ] Audio compression (Vorbis for music, ADPCM for SFX)
- [ ] Scene streaming for 4 areas (load/unload area assets on transition)

### Phase 6: Input & Platform (Week 6-7)
**Goal:** Multi-platform input support and build pipeline

#### 6.1 Input System Unification
- [ ] Fully migrate to new Input System (remove all `Input.GetKey` fallbacks)
- [ ] Create InputActions asset with:
  - Gameplay action map (WASD, E, Tab, Space, Esc)
  - Menu action map (arrows, enter, escape)
  - UI action map (navigation, submit, cancel)
- [ ] Add gamepad support (Xbox/PlayStation controller)
- [ ] Add touch input for mobile (virtual joystick, tap-to-interact)

#### 6.2 Build Targets
- [ ] Windows (primary) — already working
- [ ] macOS (test Metal compatibility)
- [ ] Linux (Vulkan test)
- [ ] WebGL (reduce texture size, test browser performance)
- [ ] Android/iOS (if URP 2D mobile-ready, add touch controls)

#### 6.3 Quality Settings
- [ ] Low/Medium/High/Ultra presets:
  - Disable shadows on Low
  - Reduce particle count on Medium
  - Disable post-processing on Low
- [ ] Resolution scaling (render scale 0.5x–1.0x)

### Phase 7: Testing & QA (Week 7-8)
**Goal:** Comprehensive test coverage and bug fixing

#### 7.1 Automated Tests
- [ ] Unit tests for EconomyManager (money/land/net worth calculations)
- [ ] Unit tests for TimeManager (day/month/year rollover)
- [ ] Unit tests for LoanManager (interest calculation, terror trigger)
- [ ] Unit tests for SaveManager (serialize/deserialize roundtrip)
- [ ] Integration test: Full game loop (menu → play → karma event → ending)
- [ ] Playmode test: Load save → verify all managers restored

#### 7.2 Manual QA
- [ ] Full regression pass using existing REGRESSION.md checklist (192 lines)
- [ ] Edge case testing:
  - Save during karma event transition
  - Load save with 100% land
  - Spend all money → bankruptcy path
  - Max workers → recruit blocked
  - All 3 loans active simultaneously
- [ ] Localization string audit (all UI text uses string tables)
- [ ] Accessibility: colorblind mode (karma level uses icons, not just color)

#### 7.3 Performance Testing
- [ ] Profile densest scene (Kota with all facilities + NPCs)
- [ ] Memory allocation heatmap (target <1 KB/frame in steady state)
- [ ] Load time target: <3 seconds to main menu, <2 seconds area transition
- [ ] Battery test for mobile (if applicable)

---

## 4. Technical Debt to Address

| Priority | Item | Impact | Effort |
|---|---|---|---|
| 🔴 High | Remove legacy IMGUI code (ModernTycoonHUD.cs, TycoonHUD.cs) | Reduces build size, eliminates confusion | 4h |
| 🔴 High | Cache Player reference globally (avoid FindWithTag) | Prevents null ref in async save/load | 2h |
| 🟡 Medium | Unify Input System (remove old Input fallbacks) | Cleaner code, gamepad support ready | 4h |
| 🟡 Medium | Replace LINQ in Update loops (TabletUI) | Reduces GC allocations | 2h |
| 🟡 Medium | Sync Worker/WorkerController state properly | Prevents data desync bugs | 4h |
| 🟢 Low | Add [SerializeField] validation (range checks, null checks) | Editor-time error prevention | 3h |
| 🟢 Low | Add assembly definition files (.asmdef) | Faster compilation, clearer dependencies | 2h |
| 🟢 Low | Migrate PlayerPrefs to proper settings save | Consistency with game save system | 2h |

---

## 5. Architecture Decisions

### 5.1 Keep: Event-Driven Pattern
The current observer pattern (C# events) works well. All manager-to-UI communication should continue using events. Do NOT switch to a message bus or mediator unless the project grows significantly.

### 5.2 Keep: Singleton Pattern
Singletons are appropriate for this scale. The existing pattern (private setter, DontDestroyOnLoad, duplicate destruction) is correct. Do NOT introduce DI containers.

### 5.3 Adopt: Object Pooling
For frequently created/destroyed objects (workers, particles, UI buttons), implement a simple generic `ObjectPool<T>` class. Unity 6 has built-in pooling — use `UnityEngine.Pool.ObjectPool<T>`.

### 5.4 Adopt: Addressables
For all art and audio assets, use the Addressable Asset System. This enables:
- Reduced initial build size
- Dynamic loading/unloading
- Future DLC/content updates
- Better memory management

### 5.5 Consider: Mod Support (Future)
Design facility and event data to be data-driven (ScriptableObjects or JSON) so that modders can add new facilities, events, or dialogue without code changes.

---

## 6. Success Metrics

| Metric | Current | Target |
|---|---|---|
| Frame rate (Kota area) | Unknown (unmeasured) | 60 FPS stable |
| Draw calls | ~30-50 | <20 |
| Build size | ~200 MB (estimated) | <150 MB |
| Time to main menu | Unknown | <3 seconds |
| UI panels (uGUI) | 6 of 10 | 10 of 10 complete |
| Legacy IMGUI code | ~1,000 lines | 0 lines |
| Unit test coverage | 0% | >60% (core systems) |
| Audio assets | 0 | 30+ SFX, 4+ music tracks |

---

## 7. Risks & Mitigations

| Risk | Probability | Impact | Mitigation |
|---|---|---|---|
| Art asset creation bottleneck | High | Delays visual upgrade | Use placeholder assets from Unity Asset Store / Kenney.nl |
| Scope creep (feature bloat) | Medium | Delays release | Strict phase gating — finish Phase 1 before Phase 2 |
| Save breaking changes | Medium | Corrupts user saves | Version save data, write migration tests |
| Performance regression | Low | Poor user experience | Profile after each phase, set FPS budgets |
| Input System migration bugs | Low | Controls break | Dual-input mode during transition period |

---

## 8. Open Questions for Stakeholder

1. **Art Style Direction:** Pixel art (16x16 or 32x32), hand-drawn vector, or painterly? Budget for asset creation?
2. **Audio Budget:** Original composition vs. royalty-free assets?
3. **Target Platforms:** Windows only, or also macOS/Linux/WebGL/Mobile?
4. **Language:** Indonesian only, or bilingual (ID/EN)?
5. **Monetization:** Free, paid, or in-app purchases? Affects build pipeline and platform choice.
6. **Scope:** Is this a hobby project with no deadline, or is there a target release date?
7. **Mod Support:** Is community modding a goal? Affects data architecture decisions.

---

## 9. Recommended Immediate Actions (Next 7 Days)

1. ✅ **Delete legacy IMGUI code** — ModernTycoonHUD.cs and TycoonHUD.cs can be safely removed since all features are migrated to uGUI
2. ✅ **Cache Player reference** — Add a static `Player.Instance` reference in OpeningGameplayPlayerController.Awake()
3. ✅ **Profile current performance** — Run Unity Profiler on the Kota scene to establish baseline FPS, draw calls, memory
4. ✅ **Decide art direction** — This gates all Phase 1 work
5. ✅ **Set up Addressables** — Even with placeholders, establish the pipeline early
6. ✅ **Create test save files** — Generate saves at different game states (day 1, mid-game, pre-ending) for QA

---

## 10. Appendix: File Inventory

### Core Systems
- `Assets/Scripts/Core/GameManager.cs` — State machine (MainMenu, Playing, Paused, GameOver)
- `Assets/Scripts/Core/MainMenuSystem.cs` — Menu camera transition to gameplay
- `Assets/Scripts/Core/PrototypeAutoBootstrapper.cs` — Runtime manager initialization
- `Assets/Scripts/Core/InputEdgeDetection.cs` — Input edge detection helper

### Managers
- `Assets/Scripts/Managers/EconomyManager.cs` — Money, land %, net worth, bankruptcy
- `Assets/Scripts/Managers/TimeManager.cs` — Calendar, game speed, day/month/year events
- `Assets/Scripts/Managers/WorkerManager.cs` — Worker recruitment, TBS/CPO stocks, factory delegation
- `Assets/Scripts/Managers/LoanManager.cs` — 3 loan types, boarding house passive income, pinjol terror
- `Assets/Scripts/Managers/BuildingManager.cs` — Land purchase, factory, foundation (CSR)
- `Assets/Scripts/Managers/EnvironmentalKarmaManager.cs` — 75/85/90/100% karma events
- `Assets/Scripts/Managers/RivalManager.cs` — Cousin AI, 10-year evaluation, endings
- `Assets/Scripts/Managers/SettingsManager.cs` — PlayerPrefs volume/fullscreen
- `Assets/Scripts/Managers/GameManager.cs` (in Managers folder) — Duplicate? Verify

### Player
- `Assets/Scripts/Player/OpeningGameplayPlayerController.cs` — 8-dir movement, walk animation, teleport
- `Assets/Scripts/Player/SmoothFollowCamera2D.cs` — Camera follow with bounds

### NPC & Dialogue
- `Assets/Scripts/NPC/NPCInteractable.cs` — Interaction trigger + [E] prompt
- `Assets/Scripts/NPC/NPCIdentity.cs` — NPC data (name, role, color)
- `Assets/Scripts/NPC/DialogueRunner.cs` — Stateless dialogue state machine
- `Assets/Scripts/NPC/DialogueAsset.cs` — ScriptableObject dialogue tree
- `Assets/Scripts/NPC/DialogueActionRouter.cs` — Routes actionId to game systems

### Workers
- `Assets/Scripts/Workers/WorkerController.cs` — Runtime worker behavior, harvest cycle
- `Assets/Scripts/Workers/WorkerData.cs` — ScriptableObject worker type definition

### Buildings
- `Assets/Scripts/Buildings/BuildingManager.cs` — Factory, foundation, land capacity
- `Assets/Scripts/Buildings/BuildingUI.cs` — Building button interactions

### Environment
- `Assets/Scripts/Environment/AreaTransitionManager.cs` — Fade transition between 4 areas
- `Assets/Scripts/Environment/AreaPortalTrigger.cs` — Portal trigger collider
- `Assets/Scripts/Environment/DayNightCycle.cs` — Screen overlay day/night tint
- `Assets/Scripts/Environment/WorldBuildingBuilder.cs` — Procedural 4-area world builder
- `Assets/Scripts/Environment/TilemapAreaBuilder.cs` — Tilemap ground generation
- `Assets/Scripts/Environment/GridManager.cs` — Grid coordinate helpers
- `Assets/Scripts/Environment/InteractableFacility.cs` — Facility interaction trigger
- `Assets/Scripts/Environment/EcologySpawner.cs` — Animal spawn on karma events
- `Assets/Scripts/Environment/AnimalWanderer.cs` — Animal movement AI

### UI (uGUI — current)
- `Assets/Scripts/UI/UIRoot.cs` — Canvas root, helpers, modal management, theme colors
- `Assets/Scripts/UI/UIManager.cs` — Legacy IMGUI-ish data binding (uses TextMeshPro)
- `Assets/Scripts/UI/ModernTycoonHUD.cs` — 🏚️ LEGACY IMGUI — DELETE
- `Assets/Scripts/UI/TycoonHUD.cs` — 🏚️ LEGACY IMGUI — DELETE
- `Assets/Scripts/UI/TopBarUI.cs` — Date/money/net worth/debt display
- `Assets/Scripts/UI/NotificationTickerUI.js` — Scrolling notification banner
- `Assets/Scripts/UI/TabletUI.cs` — 5-tab pocket tablet (finance, workers, ecology, rival, save)
- `Assets/Scripts/UI/FacilityModalUI.cs` — Reusable modal for 11 facility types
- `Assets/Scripts/UI/FacilityModalDefinitions.cs` — Facility data definitions
- `Assets/Scripts/UI/DialogueUI.cs` — Dialogue panel with choices
- `Assets/Scripts/UI/MainMenuController.cs` — Play/Settings/Quit buttons
- `Assets/Scripts/UI/GameOverUI.cs` — Ending display
- `Assets/Scripts/UI/MinimapUI.cs` — Minimap render

### Save System
- `Assets/Scripts/SaveSystem/SaveManager.cs` — Multi-slot JSON save with migration
- `Assets/Scripts/SaveSystem/SaveData.cs` — Serializable save data structure
- `Assets/Scripts/SaveSystem/SaveSlotSummary.cs` — Slot metadata for UI

### Editor Tools
- `Assets/Scripts/Editor/TanamSawitSetupEditor.cs` — Menu item: setup scene
- `Assets/Scripts/Editor/UnifiedSceneBuilder.cs` — 1-click world builder

---

*End of PRD — Total analysis time: ~15 minutes across 45+ source files*
