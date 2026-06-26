# ColonyZero

A 2D incremental / idle game built in Unity 6 where you establish a colony on a barren planet by managing three resources, constructing buildings, and eventually prestiging into **New Colony+** for permanent production bonuses.

---

## Gameplay Loop

1. **Click the planet** to mine minerals manually.
2. **Build structures** that passively generate Minerals, Energy, and Oxygen every second.
3. **Grow your population** — colonists increase click yield and are produced by certain buildings.
4. **Meet the win condition** — own at least one of each of the 6 buildings and reach 5 colonists.
5. **Prestige (New Colony+)** — reset your colony in exchange for a permanent +10% production multiplier, then do it all again faster.
6. **Come back later** — offline earnings calculate up to 8 hours of passive income while the game is closed.

---

## Tech Stack

| Tool | Version |
|---|---|
| Unity | 6000.4.10f1 (Unity 6) |
| Render Pipeline | Universal Render Pipeline (URP) 17.4.0 |
| UI Text | TextMesh Pro (via Unity UI 2.0.0) |
| Input | Unity Input System 1.19.0 |
| 2D Tooling | 2D Sprite, Animation 14.0.4, PSD Importer 13.0.3 |
| IDE | VS Code / JetBrains Rider |
| Version Control | Git + GitHub |

No third-party assets or paid plugins. All audio is generated procedurally at runtime.

---

## Project Structure

```
Assets/
├── Prefabs/                  # Reusable UI prefabs (building buttons, floating text)
├── Scenes/
│   └── Game.unity            # Single-scene architecture
├── ScriptableObjects/
│   └── Buildings/            # One .asset file per building type
│       ├── MiningRig.asset
│       ├── SolarPanel.asset
│       ├── Biodome.asset
│       ├── HabitatModule.asset
│       ├── ResearchLab.asset
│       └── OrbitalLauncher.asset
├── Scripts/
│   ├── Audio/
│   │   └── AudioManager.cs   # Procedural audio synthesis
│   ├── Buildings/
│   │   ├── BuildingData.cs   # ScriptableObject definition
│   │   └── BuildingManager.cs
│   ├── Core/
│   │   ├── GameManager.cs    # Central game state + all game logic
│   │   ├── ResourceManager.cs# Periodic auto-save trigger
│   │   └── SaveManager.cs    # PlayerPrefs persistence layer
│   └── UI/
│       ├── UIManager.cs      # HUD updates + planet click handler
│       ├── BuildingPanel.cs  # Spawns and owns building buttons
│       ├── BuildingButton.cs # Per-building buy button logic
│       ├── PrestigePanel.cs  # New Colony+ prestige popup
│       └── OfflineEarningsPanel.cs # Offline income summary popup
└── Settings/
    ├── UniversalRP.asset
    └── Renderer2D.asset
```

---

## Architecture

### Singleton Managers

The game uses a lightweight singleton pattern across its three managers — `GameManager`, `SaveManager`, and `AudioManager`. Each self-registers in `Awake` and exposes a static `Instance`. This avoids the overhead of Unity's service-locator patterns while keeping the codebase readable at this scope.

`GameManager` is the central hub: it owns all runtime state (resources, owned buildings, prestige level) and is the only object other systems write to. UI and audio systems only read from it or call well-defined methods on it.

### Data-Driven Buildings (ScriptableObjects)

Each building is a `BuildingData` ScriptableObject with fields for cost, cost scaling, and per-tick production rates for all three resources plus population. This means adding a new building type requires zero code — create an asset, fill in the numbers, and drop it into the `BuildingPanel`'s array in the Inspector.

Cost scaling uses exponential growth:
```csharp
public int GetCurrentCost(int owned)
    => Mathf.RoundToInt(baseCost * Mathf.Pow(costScaling, owned));
```

The default `costScaling` of `1.15` means each successive copy of a building costs 15% more than the last, creating a natural progression wall.

### Passive Income (Coroutine)

Rather than running production calculations in `Update` every frame, a single coroutine ticks once per second and iterates over owned buildings:

```csharp
private IEnumerator PassiveIncome()
{
    while (true)
    {
        yield return new WaitForSeconds(1f);
        float mult = ProductionMultiplier;
        foreach (var kvp in _owned)
        {
            // apply per-building rates × count × prestige multiplier
        }
    }
}
```

This is more predictable than frame-rate-dependent accumulation and makes the per-tick values in the Inspector directly meaningful ("this building earns 2 Minerals per second").

### Prestige System (New Colony+)

When the win condition is met (`_owned.Count >= 6 && Colonists >= 5`), a prestige panel appears. Prestiging:

- Increments `PrestigeLevel`
- Resets Minerals, Energy, Oxygen, Colonists, and all owned buildings
- Grants a permanent `ProductionMultiplier = 1 + PrestigeLevel × 0.10f`

The multiplier is applied to every source of passive income and planet clicks, so each prestige cycle is meaningfully faster. The prestige level persists across sessions via `PlayerPrefs`.

### Offline Earnings

On quit or app pause, `SaveManager` writes a UTC Unix timestamp to `PlayerPrefs`. On the next launch, `GameManager.Awake` reads the timestamp, clamps the elapsed time to 8 hours, and runs the same per-building production formula over that duration before starting the live coroutine:

```
offlineEarnings = Σ (building.ratePerTick × owned × multiplier × elapsedSeconds)
```

The result is applied to the player's resources and summarised in a popup so the player can see exactly what they earned. Buildings are also now persisted between sessions (saved by name, loaded back into the ownership dictionary), which is a prerequisite for offline earnings to be meaningful.

### Procedural Audio

`AudioManager` generates all sound effects at runtime using sine-wave synthesis with exponential decay — no audio files are shipped with the project:

```csharp
samples[i] = Mathf.Sin(2 * Mathf.PI * frequency * t) * Mathf.Exp(-5f * t);
```

Three tones are generated on `Awake` (click: 440 Hz, purchase: 587 Hz, milestone: 880 Hz) and played back via `AudioSource.PlayOneShot`. This keeps the project lightweight and taught me how Unity's audio pipeline works at the sample level.

### Save System

Persistence uses `PlayerPrefs` — intentionally simple for a single-scene idle game. `SaveManager` serialises:

- Float resources (Minerals, Energy, Oxygen, Colonists)
- Integer prestige level
- Per-building owned counts (keyed as `bld_<buildingName>`)
- Last save UTC timestamp (for offline earnings)

Auto-save triggers every 30 seconds via `ResourceManager.Update`, on prestige, and on application quit/pause.

---

## Systems Reference

| System | File(s) | Key Mechanic |
|---|---|---|
| Resources | `GameManager.cs` | Three resources (Minerals, Energy, Oxygen) + population |
| Buildings | `BuildingData.cs`, `BuildingManager.cs` | ScriptableObject per building, exponential cost scaling |
| Passive Income | `GameManager.cs` | 1-second coroutine, multiplied by prestige bonus |
| Planet Clicking | `UIManager.cs` | Click yield scales with colonist count × prestige multiplier |
| Win Condition | `GameManager.cs` | 1 of each of 6 buildings + 5 colonists |
| Prestige | `GameManager.cs`, `PrestigePanel.cs` | Reset + stacking +10% production multiplier |
| Offline Earnings | `GameManager.cs`, `OfflineEarningsPanel.cs` | UTC timestamp diff, capped at 8 hours |
| Save / Load | `SaveManager.cs` | PlayerPrefs, auto-save every 30s + on quit |
| Audio | `AudioManager.cs` | Procedural sine-wave synthesis, no audio files |

---

## What I Learned

**Coroutines over Update for game logic.** Running a 1-second production tick in a coroutine rather than accumulating `Time.deltaTime` in `Update` made the code simpler and the per-tick numbers in the Inspector directly interpretable. Reserving `Update` for UI and input kept frame-rate concerns separate from game logic.

**ScriptableObjects as data containers.** Defining buildings as assets rather than prefabs or code made it trivial to balance the game — tweak a number in the Inspector, hit Play, no recompile. It also enforced a clean boundary between data and behaviour that I'll carry into future projects.

**The offline earnings problem is really a save problem.** I initially had no building persistence between sessions — which meant offline earnings had nothing to calculate from. Building the offline system forced me to properly serialise and deserialise the ownership dictionary, making the save system substantially more complete as a side effect.

**Exponential cost scaling creates engagement.** The `Mathf.Pow(costScaling, owned)` formula is simple to write but generates a satisfying difficulty curve. Tuning `costScaling` from 1.10 to 1.20 dramatically changes game pacing, which highlighted how much design work lives in constants rather than code.

**Procedural audio is more accessible than it looks.** Generating a sine wave with an exponential decay envelope in Unity is about 10 lines of code. The result sounds like a real UI tone and removes any dependency on external audio assets, which simplified the project considerably.

---

## Getting Started

1. Clone the repository
2. Open in **Unity 6** (6000.x LTS)
3. Open `Assets/Scenes/Game.unity`
4. Hit Play

No additional setup required — all packages are listed in `Packages/manifest.json` and will be resolved automatically by the Package Manager.

---

*Built by Chrysl Sheckina*
