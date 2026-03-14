# MMA Simulator

A realistic tick-based MMA fight simulation engine built with C# .NET 10.
Pick two fighters from the current UFC roster (March 2026), watch every punch, takedown,
and submission attempt narrated in real time, and get a detailed scorecard at the end.

---

## Table of Contents

1. [Getting Started](#getting-started)
2. [Project Structure](#project-structure)
3. [Architecture Overview](#architecture-overview)
4. [Simulation Algorithms](#simulation-algorithms)
   - [Tick Loop](#tick-loop)
   - [Striking Engine](#striking-engine)
   - [KO and TKO Model](#ko-and-tko-model)
   - [Grappling Engine](#grappling-engine)
   - [Stamina Engine](#stamina-engine)
   - [Judge Scoring Engine](#judge-scoring-engine)
5. [Fighter Attributes](#fighter-attributes)
6. [Fighter Roster (March 2026)](#fighter-roster-march-2026)
7. [Cross-Division Superfights](#cross-division-superfights)
8. [Deterministic Replays](#deterministic-replays)
9. [Running Tests](#running-tests)
10. [Extending the Simulator](#extending-the-simulator)

---

## Getting Started

**Prerequisites:** .NET 10 SDK

```bash
# Build everything
dotnet build MmaSimulator.slnx

# Run the console app
dotnet run --project src/MmaSimulator.Console

# Run all tests
dotnet test MmaSimulator.slnx
```

On launch you will see:

```
 __  __ __  __   _      ___  _             _      _
|  \/  |  \/  | /_\    / __|| |  _ __  _ _| |__ _| |_ ___  _ _
| |\/| | |\/| |/ _ \   \__ \| | | '  \| | | / _` |  _/ _ \| '_|
|_|  |_|_|  |_/_/ \_\  |___/|_| |_|_|_|_,_|_\__,_|\__\___/|_|

[ Simulate a Fight ]
[ View Roster      ]
[ About            ]
[ Exit             ]
```

Select **Simulate a Fight**, choose a weight class, pick two fighters, and confirm to start.
You can also enable a **cross-division superfight** after selecting Fighter A to pit
fighters from different weight classes against each other.

---

## Project Structure

```
mma-simulator/
├── MmaSimulator.slnx                    ← .NET 10 solution file
├── Directory.Build.props                ← Shared MSBuild properties
├── Directory.Packages.props             ← Central NuGet version management
│
├── src/
│   ├── MmaSimulator.Core/               ← Domain models, enums, interfaces (no deps)
│   │   ├── Enums/                       ← WeightClass, FightingStyle, FightPosition, …
│   │   ├── Interfaces/                  ← IFightSimulator, IStrikingEngine, …
│   │   ├── Models/                      ← Fighter, FighterState, Round, FightResult, …
│   │   └── ValueObjects/                ← StrikingStats, GrapplingStats, PhysicalStats, …
│   │
│   ├── MmaSimulator.Simulation/         ← Simulation algorithms (references Core)
│   │   ├── Data/                        ← FighterData (roster), FighterRepository
│   │   ├── DependencyInjection/         ← AddSimulationServices() extension
│   │   ├── Engines/                     ← StrikingEngine, GrapplingEngine, StaminaEngine, JudgeScoringEngine
│   │   ├── Narration/                   ← NarrationBuilder, NarrationTemplates
│   │   ├── Providers/                   ← RandomProvider, SeededRandomProvider
│   │   └── Simulators/                  ← FightSimulator, RoundSimulator
│   │
│   └── MmaSimulator.Console/            ← Console UI (references Core + Simulation)
│       ├── Flows/                       ← MainMenuFlow, FightFlow
│       ├── UI/                          ← FighterSelectionView, FighterStatsView, FightSimulationView, FightResultView
│       └── DependencyInjection/         ← AddConsoleServices() extension
│
└── tests/
    ├── MmaSimulator.Core.Tests/          ← 9 unit tests for models and value objects
    ├── MmaSimulator.Simulation.Tests/    ← 21 unit tests for engines and simulators
    └── MmaSimulator.Integration.Tests/  ← 20 integration tests (full fight pipeline)
```

---

## Architecture Overview

The project follows a clean-layer architecture with strict dependency rules:

```
Console  →  Simulation  →  Core
                ↑
           (no back-refs)
```

- **Core** is a pure domain layer with no external dependencies.
  All interfaces, models, enums, and value objects live here.
- **Simulation** implements the interfaces from Core.
  It depends only on Core + Microsoft.Extensions.DependencyInjection.
- **Console** depends on both, provides the terminal UI via Spectre.Console.

### Dependency Injection

Register everything via the provided extension methods:

```csharp
var services = new ServiceCollection()
    .AddSimulationServices(randomSeed: 42)   // optional seed for deterministic runs
    .AddConsoleServices()
    .BuildServiceProvider();
```

`AddSimulationServices` registers all engines, simulators, narration helpers, and the fighter
repository. When a `randomSeed` is provided it registers a `SeededRandomProvider`; otherwise
a live `RandomProvider` is used.

---

## Simulation Algorithms

### Tick Loop

Each round runs for **300 ticks** (1 tick ≈ 1 second of fight time = 5 minutes per round).

On every tick:
1. A random actor is chosen (50/50 each fighter).
2. The actor's **current fight position** dispatches to the appropriate action resolver.
3. Probabilities (deliberately low) reflect that most ticks represent circling,
   feinting, and recovery rather than continuous action.
4. If an action produces a `FightEvent`, stamina is drained accordingly.
5. After a knockdown, a TKO probability check fires immediately.
6. After a submission lock, the round ends.

Between rounds, both fighters recover stamina proportional to their Cardio stat,
age, and accumulated body damage.

### Striking Engine

**Action probabilities (per actor-tick, Standing position):**

| Position      | Strike prob | Notes                              |
|---------------|------------|------------------------------------|
| Standing      | 17%        | ~25 sig strikes/round for elite strikers |
| Clinch        | 12%        | Shorter, heavier shots             |
| G&P Top       | 6%         | Elbows and ground punches          |
| Mount Top     | 5%         | Dominant position, high damage     |
| Back Control  | 2.5%       | Rear naked choke setup area        |

**Strike resolution pipeline:**

```
accuracy_roll → miss?
    ↓ (hit)
defense_roll → blocked? (40% damage, no KO check)
    ↓ (clean)
damage = (Power/100) × typeMultiplier × positionBonus ± 10% jitter
    ↓
KO/stun check (head strikes only)
```

**Strike power multipliers:**

| Strike Type       | Multiplier |
|-------------------|-----------|
| Head Kick         | 1.40×     |
| Elbow             | 1.15×     |
| Spinning Back Kick| 1.20×     |
| Overhand          | 1.10×     |
| Hook              | 1.00×     |
| Cross             | 0.90×     |
| Uppercut          | 0.85×     |
| Roundhouse        | 0.95×     |
| Body Shot         | 0.70× (body damage only) |
| Front Kick        | 0.65×     |
| Jab               | 0.40× (probing tool) |

### KO and TKO Model

The knockout system uses an **exponential accumulation model** designed to produce
realistic finish rates by weight class and matchup type.

**KO threshold formula:**
```
normalizedDamage = accumulatedHeadDamage / (20 × chinDurability)
koThreshold = (1 − e^(−normalizedDamage × dangerMultiplier)) × (1 − toughness × 0.3)
KO probability per strike = koThreshold × 0.35
Stun probability per strike = koThreshold × 0.55
```

**KO danger multipliers by weight class:**

| Division           | Base Multiplier |
|--------------------|----------------|
| Heavyweight (265)  | 2.0×           |
| Light Heavyweight  | 1.5×           |
| Middleweight       | 1.2×           |
| Welterweight       | 1.0×           |
| Lightweight        | 0.85×          |
| Featherweight      | 0.75×          |
| Bantamweight       | 0.65×          |
| Flyweight          | 0.55×          |

**Cross-division weight advantage:** every 30 lbs of weight difference adds 33% to the
base multiplier (capped at 3× bonus). A 240-lb Jon Jones hitting a 125-lb flyweight reaches
a danger multiplier of **8.0×** — producing a near-certain KO within 5–10 clean strikes.

**TKO after knockdown:** probability is evaluated immediately each time a knockdown occurs.

```
finisherMult = 1.5 (Kickboxer/Striker/MuayThai)  |  1.2 (Boxer/MMAFighter)  |  1.0 (grappler)
tkoProb = finisherMult × (0.25 + knockdownsThisFight × 0.35) × (1 − toughness × 0.65) × (1 − stamina × 0.3)
```

For example, Alex Pereira (Kickboxer) scoring a first knockdown on Jiri Prochazka:
~34% immediate TKO probability. Second knockdown: ~54%. This explains why their real fights
ended quickly.

### Grappling Engine

**Takedown probabilities (per actor-tick, Standing):**

| Fighting Style   | Base Prob/tick | ~Attempts per round |
|------------------|---------------|---------------------|
| Wrestler         | 0.022         | 6–8                 |
| Judoka           | 0.018         | 5–7                 |
| MMAFighter       | 0.012         | 3–5                 |
| BJJ Practitioner | 0.010         | 2–4                 |
| MuayThai         | 0.005         | 1–2                 |
| Kickboxer        | 0.004         | 0–1                 |
| Boxer / Striker  | 0.003         | 0–1                 |

Probabilities are further scaled by `TakedownAccuracy / 70.0` and current stamina.
Clinch takedowns are 3× more likely than standing attempts.

**Submission attempt multipliers by style:**

| Style            | Multiplier |
|------------------|-----------|
| BJJ Practitioner | 3.5×      |
| Judoka           | 2.0×      |
| Wrestler         | 1.4×      |
| MMAFighter       | 1.2×      |
| All others       | 0.5×      |

**Escape from ground:** base 1.0% per tick, scaled by `TakedownDefense / 70.0` and
current stamina → average ~100 seconds on the ground per takedown (realistic).
Back control is the hardest position to escape (0.4% base).

### Stamina Engine

**Drain table (stamina cost per action):**

| Action                | Drain  |
|-----------------------|--------|
| Strike thrown         | 0.003  |
| Strike missed         | 0.004  |
| Takedown attempt      | 0.018  |
| Submission attempt    | 0.012  |
| Knockdown/Stun        | 0.008  |

**Between-round recovery:**
```
recovery = 0.25 + (cardio − 70) / 200  −  max(0, age − 30) / 300  −  bodyDamage × 0.1
```
Elite cardio (100) with no damage: ~35% recovery. Older fighters with body damage: ~15%.

### Judge Scoring Engine

Three judges score each completed round using a 10-point must system.

**Criteria weights per judge:**

| Criteria    | Judge 1 | Judge 2 | Judge 3 |
|-------------|---------|---------|---------|
| Striking    | 40%     | 45%     | 35%     |
| Grappling   | 35%     | 30%     | 40%     |
| Aggression  | 15%     | 10%     | 15%     |
| Control     | 10%     | 15%     | 10%     |

A 10-8 round is awarded when a knockdown occurred AND the round was dominated.
After all rounds, each judge's totals determine Unanimous Decision, Split Decision,
or Draw.

---

## Fighter Attributes

Each fighter is defined by four immutable value-object groups.

### PhysicalStats

| Attribute      | Notes                              |
|----------------|------------------------------------|
| HeightCm       | Affects kick range and reach       |
| WeightLbs      | Used in KO danger multiplier       |
| ReachCm        | ±10% accuracy modifier             |
| Age            | Affects stamina recovery rate      |

### StrikingStats (0–100)

| Attribute       | Effect in Simulation                          |
|-----------------|-----------------------------------------------|
| Accuracy        | Base hit probability                          |
| Power           | Base damage per strike                        |
| Speed           | Volume of strikes (not yet applied to ticks)  |
| Defense         | Probability of blocking a strike              |
| ChinDurability  | Resists KO accumulation (higher = safer)      |
| BodyDurability  | Resists body shot accumulation                |

### GrapplingStats (0–100)

| Attribute         | Effect in Simulation                     |
|-------------------|------------------------------------------|
| TakedownAccuracy  | Scales takedown attempt probability      |
| TakedownDefense   | Scales escape probability from the floor |
| SubmissionOffense | Scales submission lock probability       |
| SubmissionDefense | Resists submission locks                 |
| GroundControl     | Affects guard pass and position advancement |
| Clinchwork        | Used in clinch position resolution       |

### AthleticStats (0–100)

| Attribute | Effect in Simulation                            |
|-----------|-------------------------------------------------|
| Stamina   | Starting stamina value modifier                 |
| Strength  | Bonus to takedown and clinch success rates      |
| Agility   | Contributes to evasion (future use)             |
| Cardio    | Between-round recovery rate                     |
| Toughness | Resists TKO stoppages after knockdowns          |

### FightingStyle

Primary style governs:
- Takedown attempt frequency
- Submission attempt multiplier
- TKO finisher multiplier

| Style            | Archetype examples                      |
|------------------|-----------------------------------------|
| Wrestler         | Islam Makhachev, Merab Dvalishvili      |
| BJJPractitioner  | Charles Oliveira, Alexandre Pantoja     |
| Kickboxer        | **Alex Pereira** (Glory champion), Adesanya |
| MuayThai         | Valentina Shevchenko, Marina Rodriguez  |
| Boxer            | Ilia Topuria, Max Holloway              |
| Striker          | Jiri Prochazka, Leon Edwards            |
| MMAFighter       | Jon Jones, Dricus du Plessis, Aspinall  |
| Judoka           | (future use)                            |

> **Note on Pereira:** Alex Pereira is correctly classified as `Kickboxer` — he is a
> two-time Glory Kickboxing World Champion, not a Muay Thai fighter.
> Valentina Shevchenko is `MuayThai` — she holds multiple Muay Thai world titles.

---

## Fighter Roster (March 2026)

The built-in roster contains **38 fighters** across 10 UFC divisions.

### Men's Divisions

| Division          | Fighters (excerpt)                              |
|-------------------|-------------------------------------------------|
| Flyweight (125)   | Alexandre Pantoja, Brandon Royval, Steve Erceg, Amir Albazi |
| Bantamweight (135)| Merab Dvalishvili, Sean O'Malley, Umar Nurmagomedov, Cory Sandhagen |
| Featherweight (145)| Ilia Topuria, Max Holloway, Brian Ortega, Alexander Volkanovski |
| Lightweight (155) | Islam Makhachev, Charles Oliveira, Arman Tsarukyan, Dustin Poirier |
| Welterweight (170)| Belal Muhammad, Leon Edwards, Shavkat Rakhmonov, Jack Della Maddalena |
| Middleweight (185)| Dricus du Plessis, Israel Adesanya, Khamzat Chimaev, Sean Strickland |
| Light Heavyweight (205)| Alex Pereira, Jiri Prochazka, Magomed Ankalaev, Khalil Rountree |
| Heavyweight (265) | Jon Jones, Tom Aspinall, Ciryl Gane, Sergei Pavlovich |

### Women's Divisions

| Division              | Fighters                                        |
|-----------------------|-------------------------------------------------|
| Womens Strawweight    | Zhang Weili, Tatiana Suarez, Marina Rodriguez   |
| Womens Flyweight      | Alexa Grasso, Valentina Shevchenko, Manon Fiorot|

---

## Cross-Division Superfights

After selecting Fighter A, you will be prompted:

```
Open Fighter B selection to all weight classes (cross-division superfight)? [y/n]
```

If you answer **yes**, Fighter B is selected from the full 38-fighter roster.

The simulation then applies a **weight-differential KO danger multiplier** automatically.
For example:

| Matchup                           | Weight diff | Extra KO multiplier |
|-----------------------------------|-------------|---------------------|
| Pereira (205) vs O'Malley (135)   | 70 lbs      | +2.3×               |
| Jones (240) vs Pantoja (125)      | 115 lbs     | +3.8× (capped)      |
| Chimaev (185) vs Suarez (115)     | 70 lbs      | +2.3×               |

A well-calibrated superfight between a heavyweight and a flyweight will almost certainly
end in Round 1 via KO — Jones or Aspinall would land a single clean cross and that would
typically be enough within 10 strikes.

---

## Deterministic Replays

Pass a `RandomSeed` in `SimulationOptions` to get fully reproducible results:

```csharp
var options = new SimulationOptions(RandomSeed: 42);
var result1 = simulator.Simulate(fight, options);
var result2 = simulator.Simulate(fight, options);

// Always true — same seed → same fight
Assert.Equal(result1.Winner.Id, result2.Winner.Id);
Assert.Equal(result1.Method, result2.Method);
Assert.Equal(result1.FinishRound, result2.FinishRound);
```

You can also seed via DI at registration time:

```csharp
services.AddSimulationServices(randomSeed: 1234);
```

---

## Running Tests

```bash
dotnet test MmaSimulator.slnx
```

Expected output:
```
Passed!  – Failed:  0, Passed:  9, Total:  9  — MmaSimulator.Core.Tests
Passed!  – Failed:  0, Passed: 21, Total: 21  — MmaSimulator.Simulation.Tests
Passed!  – Failed:  0, Passed: 20, Total: 20  — MmaSimulator.Integration.Tests
```

**Test categories:**

| Project                         | What it tests                                               |
|---------------------------------|-------------------------------------------------------------|
| `MmaSimulator.Core.Tests`       | Model equality, derived properties on `FighterState`        |
| `MmaSimulator.Simulation.Tests` | Stamina drain rates, seeded randomness, fight simulator invariants |
| `MmaSimulator.Integration.Tests`| Full fights with real fighters: completion, reproducibility, 100-seed stability, all weight classes |

---

## Extending the Simulator

### Adding Fighters

Edit `src/MmaSimulator.Simulation/Data/FighterData.cs`. Each fighter is a `Fighter` object
with an immutable set of stats. Assign a unique `Guid`, set the correct `WeightClass`,
and choose the most accurate `FightingStyle` for the simulation to use the right probability tables.

### Adding a New Engine Behaviour

1. Define or extend the relevant interface in `MmaSimulator.Core/Interfaces/`.
2. Implement the interface in `MmaSimulator.Simulation/Engines/`.
3. Register the implementation in `SimulationServiceExtensions.cs`.
4. All dependent simulators (`RoundSimulator`, `FightSimulator`) receive the updated
   dependency via constructor injection — no wiring changes required.

### Plugging in a Custom Random Strategy

Implement `IRandomProvider` and register it before calling `AddSimulationServices()`:

```csharp
services.AddSingleton<IRandomProvider, MyCustomRandomProvider>();
services.AddSimulationServices(); // will skip registering a default provider
```

### Generating a Fight Without the Console UI

```csharp
var sp = new ServiceCollection()
    .AddSimulationServices(randomSeed: 99)
    .BuildServiceProvider();

var repo      = sp.GetRequiredService<IFighterRepository>();
var simulator = sp.GetRequiredService<IFightSimulator>();

var fighters = repo.GetByWeightClass(WeightClass.LightHeavyweight);
var fight = new Fight
{
    Id             = Guid.NewGuid(),
    FighterA       = fighters[0],  // Pereira
    FighterB       = fighters[1],  // Prochazka
    NumberOfRounds = 5,
    IsTitleFight   = true,
    WeightClass    = WeightClass.LightHeavyweight
};

var result = simulator.Simulate(fight, new SimulationOptions(RandomSeed: 99));

Console.WriteLine($"{result.Winner.FullName} wins by {result.Method} in R{result.FinishRound}");
Console.WriteLine($"Sig strikes: {result.StatsSummary.SigStrikesA}–{result.StatsSummary.SigStrikesB}");
```
