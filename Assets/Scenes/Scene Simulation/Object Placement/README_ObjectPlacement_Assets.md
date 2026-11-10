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
| **SupplyCrate (left)** | Cardboard crate on pallet + metal exit chute — groceries come out here (`GrocerySpawnPoint`) |
| **Factory conveyor** | Steel chassis, rubber belt, side rails, warning stripes, rollers, motor, **4 legs with foot plates + bolts**, cross braces |
| **Sort bins A/B/C** | `Red Box` / `Blue Box` / `Yellow Box` with rim + **bin feet**, letter labels |
| **Start button** | Heavy pedestal with foot plate + green button + `PRESS START!` |
| **Set screen** | Industrial monitor on post (`SCREEN SET 1`) |

## Scene setup

1. Run the menu item above (rebuild if you already built the old simple version)
2. Open `Color Sort.unity`
3. Remove old `ObjectPlacementStation` instance if present
4. Drag the new prefab in
5. Keep only one `Boxes` hierarchy with Red/Blue/Yellow names

## Alignment

- Storyboard: side supply box → sort into A/B/C → start button near side
- Bible: Red/Blue/Yellow box names unchanged for `ColorSortRound`
- `GrocerySpawnPoint` ready for later grocery spawn logic
