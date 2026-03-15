# MMA Simulator

A console-based MMA fight simulator built with C# and .NET 10.

The project focuses on physically coherent matchmaking, style-aware decision making, detailed striking and grappling actions, and round-by-round narration in the terminal.

## Highlights

- Expanded men's UFC roster with the current top 15 for each weight class as of March 2026
- Cross-division fights supported without disabling the feature
- Heavy physical calibration for weight, height, reach, strength, leverage, and size mismatch
- Composite fighter styles with multiple sub-styles and proficiencies
- Matchup-aware fight IQ so fighters can exploit major opponent weaknesses
- Extended action set for striking, ground-and-pound, takedowns, guard passing, and named submissions
- Persistent body-part damage tracking for head, body, and legs across rounds
- Console UI with searchable fighter selection, menu back navigation, fight replay, title-fight presentation, and live health HUD

## Tech Stack

- .NET 10
- C#
- Spectre.Console
- xUnit

## Project Structure

```text
src/
  MmaSimulator.Core/        Core models, enums, value objects, and interfaces
  MmaSimulator.Simulation/  Fight engines, physics, roster data, styles, narration, and simulators
  MmaSimulator.Console/     Console flows and Spectre.Console UI
tests/
  MmaSimulator.Core.Tests/
  MmaSimulator.Simulation.Tests/
  MmaSimulator.Integration.Tests/
```

## Core Simulation Concepts

### Physical Calibration

The simulator no longer treats cross-division fights as simple damage modifiers.

It now models:

- weight difference
- height difference
- reach difference
- strength difference
- leverage and base in grappling exchanges

These factors influence:

- initiative and action share
- strike accuracy and damage
- knockdown threat
- damage resistance
- clinch entries
- takedown success
- top control
- escapes and get-ups

Primary implementation:

- [PhysicalAdvantageModel.cs](src/MmaSimulator.Simulation/Physics/PhysicalAdvantageModel.cs)

### Composite Styles and Specialties

Each fighter can carry multiple style profiles and proficiencies instead of a single flat archetype.

Examples:

- Jon Jones: kickboxing, Muay Thai, and wrestling
- Alex Pereira: elite kickboxing specialist
- Charles Oliveira: jiu-jitsu and Muay Thai
- Khamzat Chimaev: wrestling pressure with submission threat
- Islam Makhachev: wrestling-heavy grappling with front-headlock and control specialties

Primary implementation:

- [Fighter.cs](src/MmaSimulator.Core/Models/Fighter.cs)
- [StyleProfile.cs](src/MmaSimulator.Core/Models/StyleProfile.cs)
- [StyleSpecialty.cs](src/MmaSimulator.Core/Enums/StyleSpecialty.cs)
- [StyleProfileExtensions.cs](src/MmaSimulator.Simulation/Styles/StyleProfileExtensions.cs)

### Matchup-Aware Game Planning

Fighters do not only act from static style percentages anymore.

High-IQ fighters can shift their tactics when they hold a major edge in one phase:

- elite wrestlers can force grappling against opponents with poor defensive grappling
- elite strikers can stay upright when grappling is not a meaningful edge
- small technical differences do not automatically warp the game plan

Primary implementation:

- [RoundSimulator.cs](src/MmaSimulator.Simulation/Simulators/RoundSimulator.cs)

### Expanded Action Set

The simulation distinguishes a much wider set of offensive actions.

Striking includes:

- jab, cross, hook, uppercut, overhand
- body shot
- horizontal, upward, and spinning elbows
- knees to body and head
- teep and front kick
- body kick, low kick, calf kick, oblique kick
- roundhouse, head kick, spinning back kick, stomp
- ground punch, ground elbow, hammerfist

Grappling includes:

- single leg, double leg, body lock, and clinch trips
- guard pass, knee cut pass, stack pass, back take
- rear-naked choke, guillotine, D'Arce, anaconda
- arm triangle, triangle, armbar, kimura, heel hook

Primary implementation:

- [StrikeType.cs](src/MmaSimulator.Core/Enums/StrikeType.cs)
- [GrappleAction.cs](src/MmaSimulator.Core/Enums/GrappleAction.cs)
- [FightPosition.cs](src/MmaSimulator.Core/Enums/FightPosition.cs)
- [StrikingEngine.cs](src/MmaSimulator.Simulation/Engines/StrikingEngine.cs)
- [GrapplingEngine.cs](src/MmaSimulator.Simulation/Engines/GrapplingEngine.cs)
- [RoundSimulator.cs](src/MmaSimulator.Simulation/Simulators/RoundSimulator.cs)

### Damage and Finishes

The engine supports more than standard head-strike knockouts.

Current finish logic includes:

- accumulation-based head KO and knockdown logic
- flash KO potential on very clean, high-damage head strikes
- body-shot knockdown and stun logic
- leg damage accumulation with mobility collapse effects
- stronger top-position ground-and-pound stoppages, especially from elbows
- submission finishes from position-aware grappling chains

Primary implementation:

- [StrikingEngine.cs](src/MmaSimulator.Simulation/Engines/StrikingEngine.cs)
- [FightSimulator.cs](src/MmaSimulator.Simulation/Simulators/FightSimulator.cs)
- [RoundSimulator.cs](src/MmaSimulator.Simulation/Simulators/RoundSimulator.cs)

## Console Experience

The console flow includes:

- searchable fighter selection
- same-division or all-division matchmaking
- back navigation in selection menus
- fighter comparison before the fight
- optional title fights
- live round narration with a 3-second beat between visible actions
- per-action health HUD for both fighters
- pause before official judging
- repeat-the-fight option after the result
- title-fight celebration output

Primary UI files:

- [MainMenuFlow.cs](src/MmaSimulator.Console/Flows/MainMenuFlow.cs)
- [FightFlow.cs](src/MmaSimulator.Console/Flows/FightFlow.cs)
- [FighterSelectionView.cs](src/MmaSimulator.Console/UI/FighterSelectionView.cs)
- [FightSimulationView.cs](src/MmaSimulator.Console/UI/FightSimulationView.cs)
- [FightResultView.cs](src/MmaSimulator.Console/UI/FightResultView.cs)

## Fighter Data

Fighters are currently stored in code, not in JSON or a database.

Data source:

- [FighterData.cs](src/MmaSimulator.Simulation/Data/FighterData.cs)

Each fighter includes:

- physical data
- striking attributes
- grappling attributes
- athletic attributes
- record and nationality
- stance and primary style
- composite `StyleProfiles`
- fight IQ

## Getting Started

Prerequisite:

- .NET 10 SDK

Build:

```bash
dotnet build MmaSimulator.slnx
```

Run:

```bash
dotnet run --project src/MmaSimulator.Console
```

Test:

```bash
dotnet test MmaSimulator.slnx
```

## Testing Notes

The repository includes unit and integration tests for:

- core models and fixtures
- stamina logic
- fight simulation
- roster integrity
- composite style profiles
- extreme physical mismatch calibration

Useful test files:

- [FightSimulatorTests.cs](tests/MmaSimulator.Simulation.Tests/Simulators/FightSimulatorTests.cs)
- [FighterRepositoryIntegrationTests.cs](tests/MmaSimulator.Integration.Tests/FighterRepositoryIntegrationTests.cs)

## Current Limitations

- fighter data is still hardcoded
- attribute calibration can still be refined fighter by fighter
- some advanced positional transitions can be pushed further
- local build and test execution may require NuGet restore access depending on environment restrictions

## License

This project uses a non-commercial license.

- personal use, study, and modification are allowed with attribution
- commercial use, resale, and monetized redistribution are not allowed

See [LICENSE](LICENSE) for the full text.
