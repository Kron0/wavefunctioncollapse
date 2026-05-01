# Test Plan — Wave Function Collapse 4D City
**Date:** 2026-05-01  
**Scope:** All systems currently implemented in the main game scene. No automated test runner is available; all tests are manual in Unity Play Mode unless noted. Record data in a session log alongside this document.

---

## How to Run a Test Session

1. Open the 4D scene configured by FourDSetupWizard
2. Open the Unity **Console** (Clear on Play enabled), **Profiler** (CPU + Memory), and **Frame Debugger** if testing rendering
3. Keep a text log open alongside — record timestamps, readings, and anomalies as you go
4. Test in **Development Build** where possible; Editor play mode is acceptable but note GC behaviour differs

---

## 1. Startup & Scene Initialisation

### What to test
- The startup black fade covers the screen until the first chunk is built
- `GameState.StartupComplete` becomes true after fade completes (~6s max or first chunk)
- No NullReferenceExceptions on startup
- `MapBehaviour4D.Initialize()` runs without error
- `DistrictStyler` seeds `DistrictBiasProvider` before any chunk callback fires
- Player spawns at a valid position (not falling through the floor)

### Steps
1. Enter Play Mode. Watch Console for errors during first 10 seconds
2. Confirm black fade is visible immediately, then fades out
3. Confirm player is standing on geometry (not in freefall) when startup completes
4. Note time from Play to fade-out completion

### Data to record
- Time to first chunk built (seconds)
- Time to startup fade complete (seconds)
- Error/warning count in Console at T+10s
- Whether player spawned on solid ground: Y/N

---

## 2. WFC Map Generation

### What to test
- Chunks generate continuously as the player moves
- `CollapseFailedException4D` does not occur under normal movement
- Each chunk is seeded deterministically (same chunk address → same content every run)
- District style is stamped on chunks before collapse runs
- The background generation thread does not crash or produce race condition errors
- `GeneratedChunks` count increases over time

### Steps
1. Stand still for 30 seconds. Note `GeneratedChunks.Length` at 10s, 20s, 30s
2. Walk in a straight line for 60 seconds. Count any `CollapseFailedException` in Console
3. Return to start position. Visually verify the chunk at your origin looks identical to first run (restart Play Mode and compare)
4. Walk in all four cardinal directions to ~100m. Note if generation keeps up without stalling

### Data to record
- Chunks generated per minute (at rest, walking, running)
- `CollapseFailedException4D` count per session
- Any `Debug.LogError` from the generation thread
- Chunk generation lag (visible pop-in distance, eyeball estimate in blocks)

---

## 3. W-Layer System — Physics & Transitions

### What to test
- Pressing E raises `WPosition`, pressing Q lowers it
- `MapBehaviour4D.ActiveWLayer` only changes when `HasSolidGround` returns true for the target layer
- Colliders in the current W-layer are enabled; all other layers have colliders disabled
- `SnapToGround()` places player on the floor of the new layer after a valid transition
- If no solid ground exists in the target layer, `ClampWPosition` reverts W back
- `OnWLayerChanged` fires exactly once per successful layer change
- Rapid Q/E tapping does not cause the player to fall through floors or get stuck

### Steps
1. At W=0, press E slowly. Confirm player smoothly rises and snaps to W=1 floor
2. At W=1, press E into a layer that hasn't generated yet. Confirm W snaps back to 1
3. Rapidly alternate Q and E 10 times. Confirm no fall-through
4. Stand at a W-gate (cyan wall). Confirm colliders block at wrong W, open at correct W
5. Check `MapBehaviour4D.ActiveWLayer` in Inspector during transitions

### Data to record
- Successful layer transitions: count and whether snap-to-ground fired
- Failed transitions (no solid ground): count and whether clamp correctly reverted
- Fall-through incidents: count and reproduction steps
- `OnWLayerChanged` fire count vs expected transitions (should be 1:1)

---

## 4. TesseractProjection — W Rendering

### What to test
- Adjacent W-layers render at reduced alpha (ghost effect)
- Layers outside `WRenderRange` are hidden (`SetActive(false)`)
- `UpdateSlotPositions()` only runs when player moves >0.05m or W changes >0.02
- Scale and position of ghost layers is visually correct (no Z-fighting, no floating geometry)
- Changing `WRenderRange` in Inspector at runtime updates visibility correctly

### Steps
1. Look at a tall building from W=0. Press E to W=1 and confirm W=0 is now ghost-rendered
2. Set `WRenderRange = 1` in Inspector. Confirm only ±1 layers are visible
3. Stand still for 30 seconds. Open Profiler — `UpdateSlotPositions` should not spike every frame
4. Walk quickly. Confirm ghost layers reposition smoothly without pop-in

### Data to record
- Alpha of adjacent W-layer slots (should be <1, >0) — eyeball test
- `UpdateSlotPositions` ms cost per frame (from Profiler, CPU Deep Profile)
- Any visible Z-fighting or geometry misalignment: Y/N

---

## 5. Player Controller — FourDController

### What to test
- XZ movement responds to input axes with inertia (no instant stop)
- Run multiplier (`Run` axis) increases speed correctly
- Jump fires once per press; double-jump is blocked (`jumpLocked`)
- Jetpack (`Jetpack` axis) raises player when held
- Gravity applies when in air; grounded check via SphereCast
- Camera look (mouse + gamepad) is responsive and clamped at ±90° vertical
- W-axis has inertia (`WAcceleration` slows/ramps the velocity)
- `physicsEnabled` delay on startup prevents physics before world is ready

### Steps
1. Walk forward and release — confirm glide then stop (inertia)
2. Press jump while airborne — confirm second jump blocked
3. Walk into a wall — confirm no wall-climbing or jitter
4. Press Q/E — confirm `WPosition` changes smoothly with inertia, not instant
5. Move camera to extreme up/down — confirm clamp at ±90°

### Data to record
- Input-to-movement lag: subjective (good/acceptable/bad)
- Any physics jitter or wall-clip incidents: count + steps to reproduce
- Jump height (blocks): eyeball
- W-axis inertia feel: subjective

---

## 6. District & Sub-District System

### What to test
- `DistrictBiasProvider.seeds` and `subSeeds` are populated before first chunk generates
- `GetChunkStyle()` returns a valid style (0–7) for any chunk coordinate
- `GetSubDistrictType()` returns consistent results for the same position
- `GetSubDistrictBlend()` returns `t` between 0 and 1; `near` and `far` are distinct near a boundary
- Voronoi regions are visually apparent — architectural character differs between large districts
- Sub-district boundaries produce smooth atmospheric transitions (not instant)

### Steps
1. Open a fresh session. Add a debug log temporarily that prints `GetChunkStyle` for each generated chunk — confirm all values 0–7
2. Walk slowly across the city. Note when the neighbourhood card slides in — the boundary crossing should feel like a transition, not a teleport
3. At a sub-district boundary, check Console for any errors from `GetSubDistrictBlend`
4. Compare fog colour on either side of a clear boundary: visually distinct? Y/N

### Data to record
- Distinct chunk styles observed across 20 chunks (should see variety)
- Neighbourhood card trigger count per 5 minutes of walking
- Any `GetSubDistrictBlend` errors: Y/N
- Fog colour transition: gradual/abrupt/absent

---

## 7. ChunkAtmosphere4D — Fog, Ambient, Beat

### What to test
- `RenderSettings.fog` is enabled after first chunk
- Fog colour changes as player moves between sub-districts
- `fogDensity` varies between areas (FoundryYards should be denser than SaltWorks)
- The ambient beat (brightness pulse on sub-district boundary crossing) fires and decays within ~0.4s
- No fog or ambient changes occur while `GameState.IsPaused`
- Point lights spawn in chunks with the correct theme colour

### Steps
1. Walk from a known low-density district (SaltWorks/FernMarket) to a high-density one (FoundryYards/EmberLanes). Watch fog density change
2. Cross a sub-district boundary: watch for a brief ambient brightening
3. Open Pause Menu during fog transition — fog should freeze while paused
4. In a new chunk, find the point lights — confirm colour matches the sub-district theme (e.g. amber in ClockmakerQuarter)

### Data to record
- `RenderSettings.fogDensity` value in at least 3 distinct districts (note them)
- `RenderSettings.fogColor` values at 3 positions
- Beat visible on crossing: Y/N
- Beat duration (eyeball, should be <0.5s)
- Point light colour match to theme: Y/N

---

## 8. Collectibles (Artifacts)

### What to test
- `CollectiblePlacer.TotalPlaced` increases as chunks generate
- Artifacts only spawn on top-layer (y == MapHeight−1) walkable slots
- Approaching an artifact at the **wrong** W-layer does not collect it
- Approaching at the **correct** W-layer collects it: `TotalCollected` increments
- `CollectibleItem.OnCollected` fires → `WLayerHUD` notification slides in
- `OnSynthesisComplete` fires when `TotalCollected == TotalPlaced` (all collected)
- Collected items are destroyed (no lingering object)

### Steps
1. After 2 minutes of generation, note `TotalPlaced` in HUD. Should be >0
2. Find a collectible (glowing sphere on a rooftop)
3. Approach at the wrong W-layer — confirm no collection
4. Switch to correct W-layer — collect and confirm notification appears and `TotalCollected` increments
5. Confirm sphere is destroyed after collection

### Data to record
- `TotalPlaced` after 5 minutes of generation
- `TotalCollected` / `TotalPlaced` ratio after 10 minutes of play
- Spawn density: roughly 1 collectible per how many rooftop slots (eyeball)
- False positive collections (wrong W-layer triggering): count
- Missed collections (correct W-layer, in range, no trigger): count

---

## 9. Landmarks

### What to test
- ~1 in 8 chunks has a landmark beacon (orange light + cylinder)
- `LandmarkPlacer.LandmarkWorldPositions` populates as generation runs
- HUD landmark arrow appears once at least one landmark exists
- Arrow direction is correct (points toward nearest landmark)
- Distance reading in HUD is approximately accurate
- Beacons are visible from ~20+ units away

### Steps
1. After 3 minutes of generation, note `LandmarkCount`
2. Find a landmark beacon visually. Confirm orange point light and cylinder mesh present
3. Walk toward it — confirm distance in HUD decreases
4. Walk away — confirm arrow reorients
5. Stand behind a building — arrow should still point toward landmark even if offscreen

### Data to record
- `LandmarkCount` after 5 minutes of generation (expected: 1 per ~8 chunks generated)
- HUD arrow appears: Y/N
- Arrow accuracy: correct direction Y/N
- Beacon visible from 20m: Y/N

---

## 10. W-Gates

### What to test
- Cyan-emissive visual hint appears on gated slots
- At the **wrong** W-layer: colliders block passage through the gated opening
- At the **correct** (`wGateLayer`) W-layer: colliders are disabled, passage is open
- `WGateBlocker` subscribes to `OnWLayerChanged` and responds immediately
- No gates placed on slots without walkable faces
- Gate layer is assigned as `slot.Position.w + 1` (opens one layer above)

### Steps
1. Find a cyan-tinted wall (may take a few minutes of exploration)
2. Try to walk through at current W-layer — should be blocked
3. Note the `wGateLayer` value (one above current W), switch to that layer, retry
4. Confirm passage is now open
5. Switch back — confirm wall reinstates

### Data to record
- Gates found per 10 minutes of exploration
- Correct block at wrong W: Y/N
- Correct open at right W: Y/N
- Delay between layer change and gate response: eyeball (should be instant)
- Any gates that never open or never close: count + steps

---

## 11. HUD — WLayerHUD

### What to test
- Startup black fade: covers full screen, fades cleanly
- W position text updates continuously and matches `FourDController.WPosition`
- `W{layer}` big number updates on layer change with punch animation
- Layer dots: correct layer highlighted; inactive dots dim
- Progress bar: fills as W moves between integers
- Artifact counter: `found / total` matches statics
- Landmark section: hidden with no landmarks; appears and shows arrow when ≥1 exist
- Neighbourhood card: slides in on sub-district change, holds ~3s, slides out
- Transition flash: fires on layer change, correct colour (cyan up, magenta down), fades in 0.3s
- Artifact notification: slides in on collection, shows correct count

### Steps
1. Read through every HUD element on startup and confirm presence
2. Collect an artifact — confirm notification appears with correct count
3. Change W-layer — confirm big number punches, flash appears
4. Walk between sub-districts — confirm neighbourhood card shows the correct name
5. Open/close pause menu — confirm HUD is still correct after resume
6. Look up/down/sideways — confirm all HUD elements remain in correct screen positions

### Data to record
- Any HUD element missing on startup: list
- Notification timing: approximate slide-in duration (should be ~0.22s)
- Neighbourhood card timing: hold duration (should be ~3s)
- Flash colour correct (cyan for E, magenta for Q): Y/N
- Any HUD elements misaligned at non-16:9 aspect ratios: Y/N

---

## 12. Decorations, Human Details & Artwork

### What to test
- `DecorationPlacer4D` spawns props in newly generated chunks
- `HumanDetailPlacer` spawns street furniture, laundry, shoes, etc.
- `ArtworkPlacer` places artwork frames on interior walls
- None of these spawn floating or clipping severely into geometry
- Decorations appear only in W=0 (or whatever layer they're constrained to)

### Steps
1. Walk through 5 newly generated chunks. Note which decoration types appear
2. Check for floating props (hovering above ground) or severe geometry intersection
3. Walk through an interior space — confirm artwork frames on walls where expected

### Data to record
- Decoration types spotted: list
- Floating/clipping incidents: count + location
- Artwork visible in interiors: Y/N

---

## 13. Clocks

### What to test
- `ClockHandPlacer` places clock hand components on clock prototype slots
- `ClockFace` animates: hour and minute hands rotate to represent current time
- Clocks visible in buildings tick in real time (minute hand moves visibly over several minutes)
- No clocks spawn with hands at the wrong position on startup

### Steps
1. Find a clock in a building (look for `ClockFace` prototype)
2. Note the time shown vs system time — should match approximately
3. Wait 2 minutes — confirm minute hand has moved
4. Check multiple clocks across different chunks — confirm they all show the same time

### Data to record
- Clock time accuracy vs system time (seconds off)
- Clock hands visible and moving: Y/N
- Clocks found per 5 minutes of exploration: count

---

## 14. Trees

### What to test
- `TreePlacer` places trees in park/open areas
- Trees are visually distinct from buildings (no geometry overlap)
- Tree variety: different shapes/sizes visible
- Trees do not pop in from directly below or cause physics issues

### Steps
1. Walk through a park district (high `isWalkable` chunk). Count trees per chunk
2. Walk under a tree — confirm no collision weirdness
3. Look for tree variety — at least 2–3 distinct silhouettes

### Data to record
- Trees per park chunk (rough count)
- Variety of tree shapes: count distinct types
- Collision issues: Y/N

---

## 15. Creature Audio (ChunkAtmosphere4D)

### What to test
- Birds audible overhead in chunks with `BirdCount > 0`
- Cats/dogs/mice audible at street level in appropriate districts
- Sounds fire at randomised intervals (not all at once on chunk load)
- Audio pauses when `GameState.IsPaused`
- Spatial rolloff: sounds fade with distance (walk away, volume decreases)
- No audio sources leak after long play sessions (check object count)

### Steps
1. Stand in ClockmakerQuarter for 60 seconds. Listen for cats and birds
2. Walk to FoundryYards. Listen for dogs
3. Pause the game — confirm all creature audio stops
4. Resume — confirm audio resumes
5. Walk 50m away from a cat sound source — confirm volume reduces to near zero

### Data to record
- Creature audio heard per district: list species heard vs expected from theme
- Pause behaviour: audio stops Y/N, resumes Y/N
- Spatial falloff working: Y/N
- Any stuck/looping audio: Y/N

---

## 16. Pause Menu

### What to test
- Escape key opens pause menu; `GameState.IsPaused = true`
- Escape again (or Resume button) closes it; `GameState.IsPaused = false`
- Player input is blocked while paused
- Generation thread continues while paused (chunks still build)
- Pause menu UI renders correctly over the game world
- Cursor behaviour (locked in play, free in pause)

### Steps
1. Open and close pause menu 5 times rapidly — confirm no state corruption
2. While paused, attempt to move — confirm player is stationary
3. Open pause while mid-air — confirm player doesn't resume falling until unpaused

### Data to record
- Pause toggle errors: count
- Input bleed (movement while paused): Y/N
- Cursor behaviour correct: Y/N

---

## 17. Performance Baseline

This is the most important data to establish before further changes — it gives a baseline to detect regressions.

### Profiler setup
- CPU: Deep Profile off (too expensive); use standard Profiler with markers
- Memory: Simple mode, watch `Total Reserved`, `GC Alloc per frame`
- Target platforms: Editor on current dev machine; note machine specs alongside data

### Measurements to take (record at each state)

| State | FPS avg | FPS min | GC Alloc/frame | Total Memory |
|---|---|---|---|---|
| Startup (first 10s) | | | | |
| Standing still, 1 chunk generated | | | | |
| Standing still, 10 chunks generated | | | | |
| Walking normally | | | | |
| Running | | | | |
| Rapid W-layer switching (Q/E spam) | | | | |
| After 10 minutes of play | | | | |
| After 30 minutes of play | | | | |

### Steps
1. Enter play mode. Sample Profiler at each state above for 10 seconds
2. Note the **three most expensive** functions in CPU view at steady state
3. Check `Total Reserved Memory` at 5min, 15min, 30min — should not grow unboundedly (memory leak check)
4. Watch `GC Alloc per frame` at steady state — ideally near zero outside of chunk generation bursts

### Specific systems to check for cost
- `UpdateSlotPositions()` — should only run on player movement, not every frame
- `ChunkAtmosphere4D.Update()` — runs every frame, should be lightweight (pure math + lerp)
- `CollectibleItem.Update()` — runs on every live collectible; watch count × cost
- `WLayerHUD.Update()` — Canvas rebuild cost

### Data to record
- All values from the table above
- Top 3 CPU functions at steady state (from Profiler)
- Memory growth over 30 minutes: flat/slow growth/fast growth
- Any >16ms frame spikes and their cause (from Profiler)

---

## 18. Thread Safety

### What to test
- Background generation thread (`generatorThread`) does not access Unity API
- `generatedChunks` HashSet is always accessed under `lock(this.generatedChunks)`
- `completedChunks4D` ConcurrentQueue is the only bridge between threads
- No `InvalidOperationException` from dictionary modification during iteration

### Steps
1. Run for 15 minutes with the Console set to show All messages
2. Filter Console for "thread", "concurrent", "InvalidOperation" — should be zero
3. If possible, enable Unity's **Thread Sanitizer** in Player Settings for a test build

### Data to record
- Thread-related exceptions: count
- Any "collection was modified" errors: count

---

## 19. Edge Cases & Regression Checks

These are specific scenarios that have historically caused issues in procedural systems:

| Scenario | Expected behaviour | Pass/Fail |
|---|---|---|
| Stand at chunk boundary (X or Z near multiple of ChunkSize × BLOCK_SIZE) | No visual seam, generation continues | |
| Walk out of generation Range (>30m from last generated chunk) | Generation resumes when you return | |
| Rapidly switch W-layers 20 times in 10 seconds | No fall-through, correct layer at rest | |
| Collect all visible collectibles in an area | `TotalCollected` correct, no negative values | |
| Open pause menu immediately on startup (before startup complete) | No errors, startup continues | |
| Run for 30 minutes without stopping | No memory leak, FPS stable | |
| Generate >50 chunks | No generation slowdown, no crash | |
| Return to a previously generated chunk after 20 minutes | Chunk still present, decorations intact | |

---

## Session Log Template

Copy this for each test session:

```
Date:
Tester:
Machine specs (RAM, GPU, Unity version):
Scene / configuration:

--- READINGS ---
Startup time to fade: 
Errors at T+10s: 
Chunks/min at steady state: 
FPS avg (steady state): 
FPS min (worst observed): 
GC Alloc/frame (steady): 
Memory at 5min / 15min / 30min: 
CollapseFailedException count: 
Fall-through incidents: 
Gate correct open/close: Y/N
Collectible false positives: 
Neighbourhood card firing correctly: Y/N
Creature audio by district: 

--- ANOMALIES ---
(list any unexpected behaviour with timestamp and reproduction steps)

--- TOP CPU FUNCTIONS (steady state) ---
1. 
2. 
3. 
```
