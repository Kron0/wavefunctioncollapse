# Test Plan — Wave Function Collapse 4D City
**Date:** 2026-05-01 (rev 2 — log-first)
**Scope:** All systems currently implemented. Primary verification method is `game_debug.log`; visual/audio checks are secondary. No automated test runner.

---

## Setup Before Any Session

### Enable verbose logging

1. In Inspector, on the **Player** GameObject:
   - `FourDController` → tick **VerboseLogging**
   - `LogToFile` → confirm **SaveToFile** is ticked
2. On the **Map4D** GameObject:
   - `MapBehaviour4D` → tick **VerboseLogging**
   - `GenerateMap4DNearPlayer` → tick **VerboseLogging**
3. Enter Play Mode.
4. Check Console for: `[LogToFile] Writing to: /path/to/game_debug.log`
5. Open `game_debug.log` in a terminal with `tail -f game_debug.log`

### Log file location

- **Editor (Windows/WSL2):** `<project root>/game_debug.log`
- **Build:** next to the executable in `<Company>/<Product>/game_debug.log`

### Grep shorthand used below

All grep commands assume you're in the project root and the game is running or has just been played:

```bash
grep "[Gen4D]"  game_debug.log
grep "[Map4D]"  game_debug.log
grep "[4D]"     game_debug.log
grep "ERR\|EXCP\|WARN" game_debug.log
```

---

## 1. Startup Sequence

**What the logs should show (in order):**

```
[LOG ] [LogToFile] Writing to: ...
[LOG ] [Gen4D] Queuing chunk Vector4Int(0, 0, 0, 0) ...
[LOG ] [Gen4D] Collapsing chunk Vector4Int(0, 0, 0, 0) ...
[LOG ] [Gen4D] Chunk Vector4Int(0, 0, 0, 0) collapsed. Total chunks: 1
[LOG ] [Gen4D] Background generation thread started
[LOG ] [4D] SnapToGround succeeded on attempt 1 at ... (BuiltSlots=N)
```

**Verify with:**
```bash
grep -E "SnapToGround|Background generation thread" game_debug.log
```

**What to check:**
- `SnapToGround succeeded` appears exactly once — no `failed` lines before it
- `BuiltSlots=` value is ≥30 when snap fires (confirming geometry existed)
- Thread start line appears before any `Thread iteration` lines
- No `EXCP` or `ERR` lines in the first 10 seconds of the log

**Data to record:**

| Check | Result |
|---|---|
| Time from session start to `SnapToGround succeeded` | |
| `BuiltSlots` value at first snap | |
| Number of `SnapToGround failed` lines (should be 0 on good run) | |
| Any ERR/EXCP in first 30 lines | |

---

## 2. WFC Generation — Rate and Coverage

**What the logs should show:**

```
[Gen4D] Queuing chunk Vector4Int(X, 0, Z, W) (dist=..., wRange=0..1)
[Gen4D] Collapsing chunk ...
[Gen4D] Chunk ... collapsed. Total chunks: N
[Gen4D] Thread iteration 20, chunks total: N
[Gen4D] Thread iteration 40, chunks total: N
```

**Verify with:**
```bash
grep "collapsed\. Total" game_debug.log | tail -20
grep "Thread iteration" game_debug.log
```

**What to check:**
- `wRange=` values never go negative or exceed `ceil(NumWLayers/ChunkSize)-1` (should be 0..1 for 6 layers, ChunkSize=4)
- `Total chunks` increases monotonically
- Thread iterations increase steadily (1 per ~50ms = ~20/s — so iteration 20 should appear within ~1s of thread start)
- No chunk address repeats in `Queuing chunk` lines (duplicates = generation bug)

**Data to record:**

| Check | Result |
|---|---|
| Chunks generated at 30s / 60s / 120s | |
| wRange values seen (should be 0..1 only) | |
| Any repeated chunk addresses | |
| Any `CollapseFailedException` in log | |

```bash
# Check for repeated chunk addresses
grep "Queuing chunk" game_debug.log | grep -oP 'Vector4Int\([^)]+\)' | sort | uniq -d
```

---

## 3. W-Layer Transitions — Physics

**What the logs should show on a successful transition:**

```
[Map4D] W layer change 0→1: HasSolidGround=True
[Map4D] W layer confirmed: 1
```

**On a failed transition (no solid ground in target layer):**

```
[Map4D] W layer change 1→2: HasSolidGround=False
[Map4D] W=2 has no solid ground, clamping to W=1
```

**On a snap failure after colliders enable (rare):**

```
[Map4D] W layer change 1→2: HasSolidGround=True
[Map4D] SnapToGround failed for W=2, reverting to W=1
```

**Verify with:**
```bash
grep "W layer change\|confirmed\|clamping\|SnapToGround failed" game_debug.log
```

**Test protocol:**
1. Press E once slowly — expect `change 0→1: HasSolidGround=True` then `confirmed: 1`
2. Press E rapidly into ungenerated layers — expect `HasSolidGround=False` + `clamping` for each attempt
3. Press Q from layer 0 — should produce NO log lines (WPosition clamped at 0 before rounding to -1)
4. Press E from layer 5 (NumWLayers=6) — should produce NO log lines (clamped at 5)
5. Rapid Q/E spam for 5 seconds — count transitions vs confirmations in log

**Data to record:**

| Check | Result |
|---|---|
| Successful transitions: count | |
| `HasSolidGround=False` clamps: count | |
| `SnapToGround failed` reversions: count | |
| Any transition below W=0 or above W=5 | |
| Q at W=0 produces no change log: Y/N | |
| E at W=5 produces no change log: Y/N | |

```bash
# Count each outcome
grep -c "HasSolidGround=True"  game_debug.log
grep -c "HasSolidGround=False" game_debug.log
grep -c "confirmed"            game_debug.log
grep -c "clamping to W"        game_debug.log
grep -c "reverting to W"       game_debug.log
```

---

## 4. Freefall / Physics Startup

**What a clean startup looks like:**

```
[4D] SnapToGround succeeded on attempt 1 at (0.0, 3.1, 0.0) (BuiltSlots=47)
```

**What a problematic startup looks like:**

```
[4D] SnapToGround failed (attempt 1), BuiltSlots=31, retrying in 0.5s
[4D] SnapToGround failed (attempt 2), BuiltSlots=38, retrying in 0.5s
[4D] SnapToGround succeeded on attempt 3 at (0.0, 3.1, 0.0) (BuiltSlots=52)
```

**Verify with:**
```bash
grep "SnapToGround" game_debug.log
```

**What to check:**
- If `failed` lines appear, the `BuiltSlots` count at each attempt — should be increasing
- Player Y coordinate in the success line should be positive (player landed on geometry, not at origin)
- If `succeeded` never appears: generation or collider issue — check for ERR lines

**Data to record:**

| Check | Result |
|---|---|
| Number of failed attempts before success | |
| Player Y position on success | |
| BuiltSlots at moment of success | |
| Time from session start to success (count log lines before it) | |

---

## 5. Generation Errors — Quick Error Sweep

Run this after any play session to see all problems at once:

```bash
grep "ERR\|EXCP\|WARN\|exception\|Exception" game_debug.log
```

**Expected warnings (safe to ignore):**
- `[WARN] Trying to collapse already collapsed 4D slot` — rare backtrack artifact, benign if <10/session
- `[WARN] Hit range limit` — WFC boundary, benign if rare

**Unexpected errors (investigate immediately):**
- Any `[ERR ]` or `[EXCP]` line
- `NullReferenceException`
- `ArgumentException`
- `InvalidOperationException` (thread safety violation)

**Data to record:**

| Error type | Count per session |
|---|---|
| NullReferenceException | |
| ArgumentException (incl. Submit button) | |
| InvalidOperationException | |
| CollapseFailedException4D | |
| Any other EXCP/ERR | |

---

## 6. W-Layer Clamp — Boundary Check

The player should never be in a layer outside [0, NumWLayers-1].

**Verify with:**
```bash
# Extract all "W layer confirmed" values and check range
grep "confirmed" game_debug.log | grep -oP 'confirmed: \K\d+'
```

All values should be in `[0, 5]` for a 6-layer setup. Any value outside this range is a bug.

```bash
# Check for any negative W or W > 5
grep "W layer change" game_debug.log | grep -oP '→\K-?\d+' | sort -n | head -3
grep "W layer change" game_debug.log | grep -oP '→\K-?\d+' | sort -rn | head -3
```

Both commands should return values in [0, 5] only.

---

## 7. HUD Player Reference

If the HUD W-display is not showing (but minimap or other elements are), the `player` reference is null.

**Verify with:**
```bash
grep "player\|Player\|FourDController\|WLayerHUD" game_debug.log | head -10
```

There should be no error about player being null. If the HUD works, the W-number and layer dots will respond to transitions logged in section 3.

Visual check: after a `W layer confirmed: 1` log line, confirm the `W1` big number updates on screen.

---

## 8. District & Atmosphere

No verbose logs currently from DistrictBiasProvider or ChunkAtmosphere4D — visual + fog checks.

**Visual checks:**
- Fog colour shifts as you move between named neighbourhoods
- Neighbourhood card slides in — name should match the sub-district character (e.g. amber/warm = ClockmakerQuarter)
- Brief ambient brightness pulse visible on crossing a boundary

**Cross-check generation log with visual:**
```bash
# See which W=0 chunks were generated (creatures only spawn at W=0)
grep "Queuing chunk" game_debug.log | grep "0, 0)$"
```

---

## 9. Collectibles, Landmarks, W-Gates

No verbose logs from placers — check via log error sweep (section 5) and visual inspection.

**Quick sanity via grep:**
```bash
# Any placer crashes?
grep "LandmarkPlacer\|CollectiblePlacer\|WGatePlacer" game_debug.log
```

Should return nothing (no errors from those components). If lines appear, they're unexpected.

**Visual checks (unchanged from previous plan):**

| System | Check | Pass/Fail |
|---|---|---|
| Landmarks | Orange beacon visible from ~20m | |
| Landmarks | HUD arrow appears after first beacon | |
| Collectibles | Glowing sphere on rooftop | |
| Collectibles | Approach wrong W — no collect | |
| Collectibles | Approach correct W — sphere destroyed | |
| W-Gates | Cyan wall visible | |
| W-Gates | Blocked at wrong W | |
| W-Gates | Open at correct W | |

---

## 10. Performance Baseline

Take these readings in a fresh session (VerboseLogging ON for startup, then turn it OFF before the perf baseline to avoid log-write overhead skewing GC).

To turn off mid-session: untick `VerboseLogging` on all three components in the Inspector. `LogToFile` can stay on — it flushes per-line but the overhead is negligible at low-log volume.

### Measurements

| State | FPS avg | FPS min | GC Alloc/frame | Total Memory |
|---|---|---|---|---|
| Startup (first 10s) | | | | |
| Still, 5 chunks generated | | | | |
| Still, 20 chunks generated | | | | |
| Walking | | | | |
| Running | | | | |
| Rapid W switching | | | | |
| After 10 min | | | | |
| After 30 min | | | | |

### Memory leak check

```bash
# After a 30-min session, count how many total chunks generated vs early in session
grep -c "collapsed\. Total" game_debug.log
grep "Total chunks:" game_debug.log | tail -5
```

Growing chunk count but stable FPS is fine. Growing chunk count AND growing memory could indicate built slot GOs not being culled.

---

## 11. Edge Cases

| Scenario | Log to check | Expected | Pass/Fail |
|---|---|---|---|
| Press Q at W=0 | `grep "W layer change 0→-1"` | Empty (no log) | |
| Press E at W=5 | `grep "W layer change 5→6"` | Empty (no log) | |
| Rapid Q/E spam | `grep "confirmed\|clamping"` count ratio | ≈50/50 on unbuilt layers | |
| Enter Play before any chunks | `grep "SnapToGround succeeded"` | Appears eventually, not immediately | |
| Pause during generation | `grep "Thread iteration"` | Continues incrementing while paused | |
| 30 min session | `grep "EXCP\|ERR"` | 0 new lines after minute 2 | |
| Return to origin chunk | Visual | Same layout as first visit | |

---

## Session Log Template

```
Date:
Tester:
Machine: (CPU, RAM, GPU, Unity version, OS)
NumWLayers setting:
VerboseLogging: ON / OFF

--- LOG SWEEP RESULTS ---
Total ERR/EXCP lines:
NullReferenceException count:
ArgumentException count (incl. Submit):
CollapseFailedException4D count:
SnapToGround failed attempts before success:
Player Y at snap success:
BuiltSlots at snap success:
W values seen outside [0,5]: (from section 6 grep)
Repeated chunk addresses: (from section 2 grep)
wRange values outside 0..1: Y/N

--- GENERATION STATS ---
Chunks at 30s / 60s / 120s:
Thread iterations visible at 60s:

--- W TRANSITION STATS ---
Successful transitions:
HasSolidGround=False clamps:
SnapToGround reversions:

--- PERF (steady state, VerboseLogging OFF) ---
FPS avg:
FPS min:
GC Alloc/frame:
Memory at 5min / 15min / 30min:
Top 3 CPU functions:

--- VISUAL CHECKS ---
Player spawns on ground: Y/N
HUD all elements visible: Y/N
Fog transitions visible: Y/N
Neighbourhood card fires: Y/N
Landmark arrow appears: Y/N
Collectible collection works: Y/N
W-gate blocks/opens correctly: Y/N
Creature audio audible: Y/N

--- ANOMALIES ---
(list with log timestamp and reproduction steps)
```
