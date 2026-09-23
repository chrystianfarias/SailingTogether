# SailingTogether

A [BepInEx](https://github.com/BepInEx/BepInEx) mod for **Valheim** that adds **cooperative rowing**.
In vanilla, only the player at the helm can move the ship. With SailingTogether, every player
**sitting on a ship bench** can row with **W** (forward) and **S** (backward) instead of standing up.
**Jump** still stands you up.

## Requirements

> **BepInEx is required.** SailingTogether is a BepInEx plugin and does nothing without it.

- Valheim (Steam)
- [BepInExPack Valheim](https://thunderstore.io/c/valheim/p/denikson/BepInExPack_Valheim/) (BepInEx 5.4.x) installed in the game folder
- **Every player on the ship must have the mod installed** (see [Multiplayer](#multiplayer))

## Installation

1. Install **BepInExPack Valheim** and run the game once, so the `BepInEx` folder is created.
2. Copy `SailingTogether.dll` to `<Valheim>\BepInEx\plugins\SailingTogether\`.
3. Start the game. The log (`BepInEx\LogOutput.log`) should show `SailingTogether ... carregado.`

## How it works

- Rowing works when the ship is at **speed 1 (oars)**. It also works in reverse (`RowInBack`) and,
  optionally, while stopped (`RowInStop`, which also allows rowing with nobody at the helm).
- Each rower pushes on the side of the ship they are sitting on:

  | Port (left) | Starboard (right) | Result |
  |---|---|---|
  | W | W | goes straight, faster |
  | W | — | turns right |
  | — | W | turns left |
  | W | S | spins in place to the right |
  | S | S | brakes / goes backward |

- A bench on the ship's centerline (`CenterDeadZone`) only pushes, it does not turn the ship.
- The helmsman keeps full control of speed and rudder; the rowers' force is added on top.

## Ship HUD

A top-down view of the ship (bow up) in the lower-right corner, built from the game's own sprites:
`ship_top` (the orange boat from the wind indicator), `winddirection` (the wind arrow) and
`ship_rudder_icon` (the oar).

- **Helmsman:** every bench — grey ring = free, blue = player, purple = simulated (debug panel).
- **Rower:** the ship and **only your own bench** (gold).
- Each occupied bench shows an **upright oar** that sways like the vanilla helm oar: short and
  quick when rowing forward (W), wider and slower when rowing backward (S), still when idle.
- A green (W) or red (S) **arrow outside the hull**, next to each rowing bench.
- The helm is shown in yellow (filled when someone is steering). Oars and arrows turn grey when the
  current speed does not allow rowing.

## Debug simulator (optional)

For testing alone. **Disabled by default** — enable it with `[Debug] SimulatorPanel = true`.

Press **F8** on board a ship: the panel lists the ship's real benches (bow to stern, with the
detected side), each with a **-1 (S) / 0 / 1 (W)** slider, plus shortcuts (all W/S, left W,
right W, spin, reset).

1. Take the helm and set speed 1.
2. Press F8, set the benches, press F8 again to close. The simulated rowers keep rowing while you steer.
3. While the simulation is active, a status line shows rowers, thrust, turn, speed, yaw rate and
   whether force is being applied.

While the panel is open the cursor is free and character input is blocked.

## Multiplayer

Ship physics only runs on the ship's **owner** (the client simulating it). Each rower writes their
input into their own Player ZDO (synced automatically by the game) and the owner reads and applies
it — no extra RPCs. **All players need the mod.** The force/turn values used are the ones from the
config of whoever currently owns the ship.

## Configuration

`BepInEx\config\farias.SailingTogether.cfg` (created on first run):

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
| Hud | `ShipSprite` | `ship_top` | Game sprite used for the ship |
| Hud | `ArrowSprite` | `winddirection` | Game sprite used for the arrows |
| Hud | `OarSprite` | `ship_rudder_icon` | Game sprite used for the oars (empty = no oar) |
| Debug | `SimulatorPanel` | `false` | Enables the rower simulator panel |
| Debug | `SimulatorHotkey` | `F8` | Key that opens/closes the simulator panel |

## Building from source

Requires the .NET SDK. BepInEx must be installed in the game folder, because the project references
`BepInEx.dll` and `0Harmony.dll` from `<Valheim>\BepInEx\core` and the game assemblies from
`<Valheim>\valheim_Data\Managed`.

```
dotnet build -c Release -p:GamePath="C:\Path\To\Valheim"
```

The DLL is copied to `<Valheim>\BepInEx\plugins\SailingTogether\` after the build.
Use `-p:DeployToGame=false` to skip copying.

## Support

If you enjoy this mod, consider buying me a coffee:

[![Buy Me a Coffee](https://img.shields.io/badge/Buy%20me%20a%20coffee-chrystianfarias-FFDD00?logo=buymeacoffee&logoColor=black)](https://buymeacoffee.com/chrystianfarias)

https://buymeacoffee.com/chrystianfarias

## License

**SailingTogether** — Copyright (c) 2025 Chrystian Farias.

Licensed under [Creative Commons Attribution-NonCommercial 4.0 International (CC BY-NC 4.0)](https://creativecommons.org/licenses/by-nc/4.0/).
You may share and adapt this work with attribution, **for non-commercial purposes only**.
See [LICENSE](LICENSE) for the full text.

### Third-party

- **BepInEx** — [github.com/BepInEx/BepInEx](https://github.com/BepInEx/BepInEx), licensed under the
  [GNU Lesser General Public License v2.1 (LGPL-2.1)](https://github.com/BepInEx/BepInEx/blob/master/LICENSE).
  BepInEx is **not** included in this repository or in the mod release; it must be installed separately.
- **HarmonyX** (shipped with BepInEx) — MIT License.
- **Valheim** and its assets are the property of Iron Gate AB. This project is not affiliated with or
  endorsed by Iron Gate or Coffee Stain. No game assets are included: the HUD sprites are loaded
  from the installed game at runtime.
