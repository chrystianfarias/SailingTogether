![SailingTogether](https://raw.githubusercontent.com/chrystianfarias/SailingTogether/main/docs/cover.jpg)

# SailingTogether — cooperative rowing

In vanilla Valheim only the player at the helm can move the ship. With **SailingTogether**, every player **sitting on a ship bench** can row with **W** (forward) and **S** (backward) instead of standing up. **Jump** still stands you up.

Each side of the ship pushes independently, so your crew can go straight, turn or spin the ship in place — together.

## Requirements

- **BepInEx is required** — install [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) (5.4.x) before this mod.
- **Every player on the ship must have the mod installed.**

## How it works

- Rowing works at **speed 1 (oars)** and in reverse. Optionally also while stopped (`RowInStop`), which allows rowing with nobody at the helm.
- Each rower pushes on the side of the ship they are sitting on:

| Left | Right | Result |
|---|---|---|
| W | W | straight, faster |
| W | — | turns right |
| — | W | turns left |
| W | S | spins in place to the right |
| S | S | brakes / goes backward |

- A bench on the ship's centerline only pushes, it does not turn the ship.
- The helmsman keeps full control of speed and rudder; the rowers' force is added on top.

## Ship HUD

A top-down view of the ship in the lower-right corner, built from the game's own icons:

- **Helmsman:** every bench, who is sitting and each rower's direction.
- **Rower:** only your own bench and your rowing direction.
- Animated oars that sway like the vanilla helm oar — short and quick forward, wider and slower backward.
- Green (W) / red (S) arrows outside the hull; everything turns grey when the current speed does not allow rowing.

## Installation

- **Vortex:** install the main file with Vortex (BepInEx must already be installed).
- **Manual:** extract the zip into the Valheim folder. It contains `BepInEx/plugins/SailingTogether/SailingTogether.dll`.
- Start the game — the BepInEx log should show `SailingTogether 1.0.0 loaded.`

## Configuration

`BepInEx/config/farias.SailingTogether.cfg` is created on first run:

| Section | Key | Default | Description |
|---|---|---|---|
| General | `Enabled` | `true` | Enables cooperative rowing |
| General | `ShowHint` | `true` | Shows the controls hint when sitting on a bench |
| Rowing | `PowerPerRower` | `0.35` | Force per rower, as a fraction of the ship's helm-oar force |
| Rowing | `TurnAccelPerRower` | `10` | Turn acceleration (°/s²) per unbalanced rower |
| Rowing | `MaxTurnRate` | `25` | Maximum turn rate (°/s) the rowers can apply |
| Rowing | `CenterDeadZone` | `0.4` | Distance (m) from the centerline treated as "center" |
| Rowing | `RowInStop` | `false` | Allow rowing while the ship is stopped |
| Rowing | `RowInBack` | `true` | Allow rowing while in reverse |
| Hud | `ShowShipHud` | `true` | Shows the top-down ship HUD |
| Hud | `Scale` | `1` | HUD scale |
| Hud | `OffsetX` / `OffsetY` | `30` / `230` | Distance (px at 1080p) from the right / bottom edge |
| Debug | `SimulatorPanel` | `false` | Rower simulator (F8) for testing alone |

## Multiplayer

Ship physics runs on the ship's owner. Each rower's input is synced through their own player data — no extra network traffic. The force/turn values used are the ones from the config of whoever currently owns the ship.

## Compatibility

- Works with any ship whose benches use the game's standard chair component; benches are detected automatically on each ship.
- Does not replace vanilla sailing: the helm, sail and speed levels work as usual.

## Links

- Source code: [github.com/chrystianfarias/SailingTogether](https://github.com/chrystianfarias/SailingTogether)
- Thunderstore / r2modman: search for **SailingTogether**
- Support me: [buymeacoffee.com/chrystianfarias](https://buymeacoffee.com/chrystianfarias)

## License

SailingTogether — Copyright (c) 2025 Chrystian Farias. Licensed under [CC BY-NC 4.0](https://creativecommons.org/licenses/by-nc/4.0/) (non-commercial use only).

BepInEx is licensed under [LGPL-2.1](https://github.com/BepInEx/BepInEx/blob/master/LICENSE) and is not included; it must be installed separately.

Valheim is the property of Iron Gate AB. This mod is not affiliated with or endorsed by Iron Gate or Coffee Stain.
