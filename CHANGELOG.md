# Changelog

## 1.0.0

Initial release.

- Cooperative rowing: players seated on ship benches row with **W** / **S** instead of standing up.
- Per-side thrust: each side of the ship pushes independently, so the crew can go straight, turn or
  spin the ship in place.
- Works at speed 1 (oars) and in reverse; optional rowing while stopped (`RowInStop`).
- Multiplayer sync through each player's own ZDO (no extra RPCs). All players need the mod.
- Top-down ship HUD using the game's own sprites: benches, occupancy, animated oars and direction
  arrows (helmsman sees every bench, rowers see only their own).
- Optional debug simulator panel (F8, disabled by default) for testing alone.
