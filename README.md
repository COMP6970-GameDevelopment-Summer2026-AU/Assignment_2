# PuKu's Adventure 🗺️

> **Assignment 2** — 2D Platformer Development
> **Course:** COMP 6970 Game Development | Summer 2026
> **Developer:** Jahidul Arafat — PhD Student, CSSE, Auburn University
> **Fellowship:** Presidential & Woltosz Graduate Research Fellow
> **Industry:** Former L3 Senior Solution Architect (MLOps), Oracle (Singapore)

---

## Overview

**PuKu's Adventure** is a 2D platformer built on the Unity Platformer2D skeleton, extended with a complete cartography-themed world using the Kenney Cartography Pack. The player journeys across 7 distinct zones — from a harbor village to a legendary castle — navigating bridges, hazards, enemies, and elevation changes to reach the summit flag.

The project demonstrates a **modular event-driven architecture** where every system communicates through a central static event bus (`GameEvents.cs`), with zero direct script-to-script references.

---

## Assignment Requirements

| Criterion | Requirement | Implementation | Status | Points |
|-----------|-------------|----------------|--------|--------|
| Player Movement | Move, jump, interact | `PlayerMovement.cs` — WASD, jump, double jump, coyote time, jump buffering | ✅ | 10 |
| Level Design | Complete playable level | 7-zone cartography world, 14 platforms, multi-directional vertical exploration | ✅ | 20 |
| Enemy System | Enemy causes player to lose | `EnemyPatrol.cs` — patrol, flip, stomp kill | ✅ | 20 |
| Visual Customization | Different from class example | Kenney Cartography Pack — parchment background, map-themed world | ✅ | 20 |
| Collectible System | Collect all before goal | `GoalFlag.cs` + `CollectibleManager.cs` | ✅ | 20 |
| Gameplay Polish | Complete, functional | Start screen, HUD, win/lose overlays, all via OnGUI | ✅ | 10 |
| **Total** | | | | **100** |

---

## Enhancements (Beyond Requirements)

| Feature | Implementation |
|---------|---------------|
| Double jump | `maxJumps = 2` in `PlayerMovement` |
| Coyote time | 0.12s grace window after leaving platform |
| Jump buffering | 0.15s buffer before landing |
| Variable jump height | Release SPACE early = shorter jump |
| Better fall gravity | Extra gravity multiplier for snappy feel |
| Enemy stomp kill | Jump on enemy top = kill + bounce reward |
| Score system | Bridge +10, hazard -10, bump -1 |
| 3 checkpoints | Save progress, respawn at latest checkpoint |
| Cliff fall detection | Player y < -8 → respawn at checkpoint |
| Hazard objects | Cactus/bush/campfire/lake → -10pts, -½ life, 3s stun |
| Bridge bonus | Every 3 bridges → +1 bonus life |
| Smooth camera | SmoothDamp with configurable bounds and look-ahead |
| Animated start screen | Panel slides down from top, dark green overlay |
| Auto Jump mode | TAB key — AI demo that auto-jumps obstacles |
| OnGUI tooltips | Every object shows score/penalty above it at all times |
| Debug console | 17+ log events for full gameplay traceability |
| Parchment background | `parchmentAncient.png` loaded from Resources at runtime |
| Sound system | 6 clip slots — walk, jump, water, enemy, flag, gem |

---

## World Design — "The Last Cartographer"

A lone explorer journeys across an ancient hand-drawn map to reach the Legendary Castle and restore the lost map of civilisation. Each zone sits at a different elevation, forcing the player to climb, descend, and zigzag through stepping-stone platforms.

### Elevation Map

```
y=24         ★ GOAL FLAG (castle summit)
y=16 [══ MOUNTAIN ══]      stepping stones up ↑↑
y=13          [step][step][step]
y=12 [══ RUINS ══]         zigzag up ↑↑
y= 9    [step]
y= 8 [══ SETTLEMENT ══]    climb up ↑
y= 5               [══ DESERT ══]    step down ↓
y= 4    [step][step]
y= 1 [══ HARBOR ══]   [══ FOREST ══]
```

### Zone Guide

| Zone | x range | Ground y | Key Tiles | Challenge |
|------|---------|---------|-----------|-----------|
| ⚓ Harbor | -28 to -21 | 1 | compass, lighthouse, dock | Flat — tutorial area |
| 🏘 Settlement | -16 to -9 | 8 | church, mill, houseChimney | First climb +7 |
| 🌲 Forest | -4 to 3 | 1 | treePineTall ×2 | TALL trees — jump over! |
| ⛩ Ruins | 8 to 15 | 12 | gate, pyramid, campfire | Big zigzag +11, fire hazard |
| 🌵 Desert | 20 to 27 | 5 | cactusLarge, palmLarge | Jump over cacti and palms |
| ⛰ Mountain | 32 to 39 | 16 | rocksMountain, watchtower | Highest ground +11 |
| 🏰 Castle | 44 to 52 | 8 | gate, castle, flag | Internal climb to summit y=24 |

---

## Game Rules

### Lives
| Event | Effect |
|-------|--------|
| Start | 3 lives |
| Enemy hit | -1 life |
| Water / Hazard trigger | -½ life |
| Hazard object (cactus etc.) | -½ life + -10 score + 3s stun |
| Cliff fall (y < -8) | Respawn at checkpoint, no life loss |
| Every 3 bridges crossed | +1 bonus life (if below max 3) |

### Score
| Event | Points |
|-------|--------|
| Bridge crossed | +10 |
| Hazard object hit | -10 |
| Platform side bump | -1 |
| Score minimum | 0 (never negative) |

### Checkpoints
3 checkpoints placed at zone transitions. Touching one saves your position (turns green). All respawns use the latest checkpoint.

---

## Controls

| Key | Action |
|-----|--------|
| A / ← | Move left |
| D / → | Move right |
| SPACE | Jump / Double jump |
| SPACE (on enemy) | Stomp kill |
| SPACE (win/lose) | Restart |
| TAB | Toggle Auto Jump demo |

---

## Architecture

Event-driven modular design — zero direct script-to-script references.

```
GameEvents.cs  (central static event bus)
      │
      ├── PlayerMovement    fires: LivesChanged, PlayerDied, PlayerWon
      ├── EnemyPatrol       fires: (stomped via PlayerMovement)
      ├── BridgeObject      fires: BridgeTouched
      ├── HazardObject      fires: HazardObjectHit
      ├── Checkpoint        fires: CheckpointReached
      ├── CheckpointSystem  fires: CliffFall
      │
      ├── GameManager       listens: PlayerDied, PlayerWon, LivesChanged...
      ├── ScoreSystem       listens: BridgeTouched, HazardObjectHit, PlatformBump
      └── SoundManager      listens: PlayerSounds events
```

---

## File Structure

```
Assets/
├── Editor/
│   ├── ProjectSetup.cs            creates tags/layers, fixes sprite imports
│   └── CartographySceneBuilder.cs builds entire scene from cartography tiles
│
├── Scripts/
│   ├── GameEvents.cs              central static event bus
│   ├── GameManager.cs             state machine + full OnGUI UI
│   ├── PlayerMovement.cs          movement, jump, lives, collision
│   ├── EnemyPatrol.cs             patrol, flip, stomp death, tooltip
│   ├── CollectibleManager.cs      gem tracking (testing mode = disabled)
│   ├── Collectible.cs             individual gem behaviour
│   ├── GoalFlag.cs                win trigger, locked until gems
│   ├── CameraFollow.cs            smooth camera, auto-bounds from tilemap
│   ├── BackgroundLoader.cs        parchment background from Resources
│   ├── CheckpointSystem.cs        cliff detection, spawn point manager
│   ├── Checkpoint.cs              checkpoint trigger + tooltip
│   ├── ScoreSystem.cs             score rules + bridge life bonus
│   ├── BridgeObject.cs            solid+trigger colliders, +10 score
│   ├── HazardObject.cs            -10 score, -½ life, 3s stun, tooltip
│   ├── AutoJump.cs                AI demo mode toggled with TAB
│   ├── PlayerSounds.cs            static sound event bus
│   └── SoundManager.cs            6 AudioClip slots
│
├── Cartography/
│   ├── Default/                   83 tiles at 64×64px (PPU=64)
│   └── Retina/                    83 tiles at 128×128px (PPU=128)
│
├── Resources/
│   └── parchmentAncient.png       background texture
│
└── Tiles/
    └── Cartography/               auto-generated .asset files
```

---

## Setup (One-Time)

```
1. Tools → Platformer2D A2 → Create Tags and Layers
2. Tools → Platformer2D A2 → Fix Background Tile Imports
3. Tools → Cartography → Build Platform Level (Auto-detect)
4. Attach scripts to GameObjects (see table below)
5. Press Play ▶
```

### Script Attachment

| GameObject | Scripts to Attach |
|-----------|------------------|
| `_GameManager` | `GameManager`, `CollectibleManager`, `CheckpointSystem`, `ScoreSystem`, `SoundManager`, `BackgroundLoader` |
| `Player` | `PlayerMovement`, `AutoJump` |
| Each Enemy | `EnemyPatrol` |
| `GoalFlag` | `GoalFlag` |
| `Main Camera` | `CameraFollow` |

---

## Debug Console Reference

```
[Player]           ══ DEBUG MODE ══ Started at (-28, 3)
[CheckpointSystem] ══ Initialized | defaultSpawn=(-28, 3)
[ScoreSystem]      ══ Initialized | Rules: Bridge=+10, Hazard=-10, Bump=-1
[Enemy]            'Enemy_1' patrol started at (-5, 2)
[Player]           LANDED at (-25, 1.5) | jumpsReset=2
[Player]           Steps=5 | score=0 | lives=3.0
[Score]            ✦ BRIDGE #1 → +10pts | totalScore=10
[Checkpoint]       ✦ SAVED at (-12, 9)
[Player]           ⚠ HAZARD OBJECT 'HazObj_cactus' → -10pts, -0.5 life, 3s
[Player]           ✦ RESPAWN → checkpoint=(-12, 9) | lives=2.5
[Score]            ★ BONUS LIFE! 3 bridges | score=30
[Enemy]            'Enemy_1' STOMPED and died
[Player]           ★ WIN! 42 jumps, 180 steps, 87.4 units
```

---

## Credits

| Asset | Source | License |
|-------|--------|---------|
| Cartography tiles | [Kenney Cartography Pack](https://kenney.nl/assets/cartography-pack) | CC0 Public Domain |
| Pixel platformer sprites | [Kenney Pixel Platformer](https://kenney.nl/assets/pixel-platformer) | CC0 Public Domain |
| Project skeleton | [Platformer2D-Skeleton](https://github.com/ajariwala1/Platformer2D-Skeleton) | MIT |

---

*PuKu's Adventure — COMP 6910 Game Development, Auburn University, Summer 2026*