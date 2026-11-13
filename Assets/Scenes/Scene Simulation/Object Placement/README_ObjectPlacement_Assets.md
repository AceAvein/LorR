# Object Placement — Factory Station Assets

Factory-style sorting station (storyboard + bible aligned).

## Build / rebuild (Unity)

**LorR → Object Placement → Build Station Prefab**

Overwrites:
- `Prefabs/ObjectPlacementStation.prefab`
- `Materials/*`

## What’s included

| Piece | Detail |
|---|---|
| **Conveyor (infeed, right)** | Factory belt with `ExitTunnel` on the **right** — groceries emerge at `GrocerySpawnPoint`. Dedicated `BeltSurface` for later run/scroll code. Flow **+X (right)**. |
| **ConveyorLeft** | Second belt on the left, same style + same **+X** flow, own `ExitTunnel` + `GrocerySpawnPoint` + `BeltSurface`. |
| **Sort bins A/B/C** | Open-top crates `Red Box` / `Blue Box` / `Yellow Box` (interior visible). Under `Boxes` only — **not welded** to conveyors; root BoxCollider for grab later. |
| **Start button** | Near right infeed tunnel — pedestal + green button + `PRESS START!` |
| **Set screen** | Industrial monitor (`SCREEN SET 1`) |

## Scene setup

1. Run **LorR → Object Placement → Build Station Prefab**
2. Open Color Sort / Object Placement scene
3. Remove old `ObjectPlacementStation` instance if present
4. Drag the new prefab in
5. Keep `Red Box` / `Blue Box` / `Yellow Box` names for `ColorSortRound`

## Alignment

- Storyboard: dual conveyors (side infeed + left/outbound), start button, A/B/C stations
- Bible: Red/Blue/Yellow box names unchanged
- Both tunnels ready for item spawn; belt surfaces ready for motion scripts
