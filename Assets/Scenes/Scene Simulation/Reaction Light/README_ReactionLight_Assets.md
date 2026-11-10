# Reaction Light — Generator Console Assets

Storyboard-aligned **12-slot (4×3)** generator board + status screen.

## Build the prefab (in Unity)

1. Open the project in Unity
2. Menu: **LorR → Reaction Light → Build Generator Console Prefab**
3. Prefab is created at:
   - `Assets/Scenes/Scene Simulation/Reaction Light/Prefabs/ReactionLightGeneratorConsole.prefab`
4. Materials land in:
   - `Assets/Scenes/Scene Simulation/Reaction Light/Materials/`

## Put it in the scene

1. Open `Assets/Scenes/Scene Simulation/Reaction Light.unity`
2. Drag `ReactionLightGeneratorConsole` into the scene (place in the Set/generator room)
3. **Delete the old floating** `Ball`, `Ball (1)` … `Ball (11)` objects so only the console’s 12 lights remain
4. Enter Play Mode — `ReactionLightRound` finds lights named `Ball` / `Ball (n)` and keeps them on the board

## What you get

- Tilted board with **12 circular slots**
- Light pads named for existing gameplay wiring
- Status screen with `TIME:` + `SET COMPLETE! PROCEED` (status hidden until you show it)
- Uniform low-poly materials (shared LorR look)

## Notes

- Matches storyboard panel: board + screen (`TIME` / `SET COMPLETE! PROCEED`)
- Bible controls still apply later (Trigger/Grip mapping by phase); this pass is **assets + layout**
- Duplicate this prefab into Set 1 / Set 2 / Set 3 rooms when building the escape layout
