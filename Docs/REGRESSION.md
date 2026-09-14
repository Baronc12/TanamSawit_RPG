# TanamSawit — Manual Regression Checklist

Run this checklist at the end of **every** implementation phase. All checks must pass.

---

## 1. Boot & Main Menu

- [ ] Launch SampleScene in Editor (or build exe)
- [ ] Game starts in MainMenu state (camera parked, player locked)
- [ ] World-space PLAY / SETTINGS / QUIT TextMesh buttons visible and clickable
- [ ] Hover effects on menu buttons work (outline/color change)
- [ ] Click PLAY (or press Space) → camera smooth-glides to player → gameplay begins
- [ ] Player spawns in **Area Kebun** (0, 0)

## 2. Player Movement & Camera

- [ ] WASD / Arrow Keys move the player in 8 directions
- [ ] 6-frame walk animation plays during movement
- [ ] Sprite flips (flipX) when changing horizontal direction
- [ ] Camera follows player smoothly (no jitter)
- [ ] Player stops at perimeter walls (Phase 3+ — before Phase 3, player walks through walls, note as known issue)

## 3. Area Transitions (4 Areas, 6 Portals)

- [ ] Walk to portal Kebun → Perumahan → fade-to-black → title card "MEMASUKI [AREA]" → teleport → fade-in
- [ ] Walk to portal Perumahan → Kota → transition works
- [ ] Walk to portal Kebun → Pabrik → transition works
- [ ] Walk to portal Perumahan → Kebun (return) → transition works
- [ ] Walk to portal Kota → Perumahan (return) → transition works
- [ ] Walk to portal Pabrik → Kebun (return) → transition works
- [ ] Player movement frozen during fade transition
- [ ] Camera snaps instantly on teleport (no smooth-damp drift)
- [ ] HUD location label updates to current area name

## 4. Facility Interaction [E]

- [ ] Approach each of the 11 facilities → floating "[E]" prompt appears
- [ ] Press [E] → correct modal opens for each facility type:
  - [ ] PapanLahan (Kebun) → buy land panel
  - [ ] MandorKebun (Kebun) → harvest/sell TBS panel
  - [ ] GudangTBS (Kebun) → TBS stock panel
  - [ ] MessPekerja (Perumahan) → recruit worker panel
  - [ ] KosKosan (Perumahan) → passive income panel
  - [ ] Bank (Kota) → bank loan panel
  - [ ] Pinjol (Kota) → pinjol loan panel
  - [ ] Rentenir (Perumahan) → rentenir loan panel
  - [ ] PabrikCPO (Pabrik) → CPO processing panel
  - [ ] YayasanCSR (Kota) → CSR foundation panel
  - [ ] BillboardSepupu (Kota) → rival status panel
- [ ] Player movement locked while modal is open
- [ ] ESC or Tab closes modal → movement unlocked

## 5. Top HUD & Pocket Tablet

- [ ] Top bar shows: date (day/month/year), area name, cash (green), net worth (color-coded), debt
- [ ] Speed buttons work: Pause (||), 1x, 2x, 4x — game speed changes
- [ ] Press [Tab] → Pocket Tablet opens
- [ ] Tablet tab "Keuangan" shows finance summary
- [ ] Tablet tab "Pekerja" shows worker list with stamina
- [ ] Tablet tab "Ekologi" shows karma level and land %
- [ ] Tablet tab "Rival" shows cousin net worth comparison
- [ ] Tablet tab "Simpan/Save" shows save/load buttons
- [ ] Press [Tab] or [ESC] → tablet closes

## 6. Economy & Tycoon Simulation

- [ ] Buy land plot → money decreases by Rp 30jt, land % increases by 2.5%
- [ ] Recruit worker → worker appears in list, hiring cost deducted
- [ ] Worker harvests TBS automatically (stamina drains)
- [ ] Worker rests when stamina depleted → stamina recovers
- [ ] Sell TBS → money increases
- [ ] Build factory → costs Rp 150jt (Phase 1B+ — before 1B, may show Rp 100jt from WorkerManager)
- [ ] Process TBS → CPO (5:1 ratio)
- [ ] Export CPO → money increases
- [ ] Factory multiplier (2.2x) applied to harvest income
- [ ] Worker capacity enforced (Phase 1C+ — recruit blocked at capacity, message "Kuota pekerja penuh!")

## 7. Loans & Credit

- [ ] Bank loan → money added, daily interest 0.05%/day
- [ ] Pinjol loan → money added, daily interest 2%/day compounding
- [ ] Rentenir loan → money added, daily interest 1%/day
- [ ] Pinjol daily forced cut (5% of debt) deducts from cash
- [ ] Missed pinjol payment → debt collector terror → worker stamina penalty
- [ ] Kos-kosan passive income → Rp 1.5jt/unit/month added
- [ ] Pay off loan → debt cleared

## 8. Ecology & Karma

- [ ] Land % at 75% → monkey theft event fires (InvasiMonyet)
- [ ] Land % at 85% → elephant event fires (SeranganGajah), factory damaged, Rp 15jt emergency charge
- [ ] Land % at 90% → tiger event fires (TerorMacan), worker killed
- [ ] Land % at 100% → GameOver (KiamatLongsor)
- [ ] Each karma event fires only once (one-shot triggers)
- [ ] Animal sprites/spawners appear (monkeys, elephants, tigers)
- [ ] Animals wander within radius

## 9. Rival & Endings

- [ ] Cousin net worth increases ~8%/year
- [ ] Evaluation year 2034 triggers ending check
- [ ] Good ending: player net worth > cousin net worth
- [ ] Bad ending: player net worth < cousin net worth
- [ ] Secret ending: land % at 100%
- [ ] Bankruptcy ending: net worth below −Rp 50jt
- [ ] Game Over banner shows with restart option

## 10. Save & Load

- [ ] Save game → file created in persistentDataPath
- [ ] Quit and relaunch → load game → all state restored
- [ ] Auto-save fires on month change
- [ ] Auto-save fires on application quit
- [ ] Calendar (date) restored correctly
- [ ] Money, net worth, land %, debts restored
- [ ] Workers restored with stamina
- [ ] Karma level and trigger flags restored
- [ ] Cousin net worth restored
- [ ] Delete save → save file removed

## 11. Settings

- [ ] Settings popup opens (gear icon or settings button)
- [ ] Volume slider adjusts AudioListener.volume
- [ ] Fullscreen/Windowed toggle works
- [ ] Settings persist across sessions (PlayerPrefs)

## 12. Time & Calendar

- [ ] Day advances every ~3 seconds at 1x speed
- [ ] 30 days per month, month changes
- [ ] 12 months per year, year changes
- [ ] Month names in Indonesian (Januari–Desember)
- [ ] Speed 2x → day advances ~1.5s
- [ ] Speed 4x → day advances ~0.75s
- [ ] Pause → time stops

---

## Tags & Layers Requirements (Phase 3+)

After Phase 3 (Physics & Collision), these Unity Tags & Layers must exist:

**Tags:**
- `Player` (already exists by default in Unity)

**Layers (add in Project Settings → Tags & Layers):**
- `World` (walls, buildings, solid colliders)
- `Player` (player character)
- `Portal` (area transition trigger zones)
- `Minimap` (Phase 6 — minimap icon sprites)

**Physics 2D Matrix (Edit → Project Settings → Physics 2D):**
- Player ↔ World: checked (collision)
- Player ↔ Portal: checked (trigger)
- Player ↔ Player: unchecked
- Minimap ↔ *: unchecked (minimap camera culling handles isolation)

---

## Phase Completion Sign-off

| Phase | Date | Tester | Result | Notes |
|-------|------|--------|--------|-------|
| P0: Baseline | | | | |
| P1: System consolidation | | | | |
| P2: Save v2 | | | | |
| P3: Physics & collision | | | | |
| P4: UI migration | | | | |
| P5: NPC + dialogue | | | | |
| P6: Visual/art upgrade | | | | |
