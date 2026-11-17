# Ball Drop — Overhead Arc Asset

Catch-the-Stick style **overhead arc dropper** (base + pillar + C-arc + 10 spawn points).

Uses your **existing ball templates** — this prefab is only the machine/arc.

## Build (in Unity)

1. Menu: **LorR → Ball Drop → Build Overhead Arc Prefab**
2. Prefab:
   - `Assets/Scenes/Scene Simulation/Ball Drop/Prefabs/BallDropOverheadArc.prefab`
3. Materials:
   - `Assets/Scenes/Scene Simulation/Ball Drop/Materials/`

## Put in scene

1. Open `Ball Drop.unity`
2. Drag `BallDropOverheadArc` into the play/training zone (half court center)
3. Select `BallDropSystem`
4. Set **Transform Above** to the child named `Transform_Above`
5. Keep your current sports-ball templates (Basketball, Baseball, etc.)

`Transform_Above` children are spawn points only (`SpawnPoint_00` … `SpawnPoint_09`).  
`BallDropSystem` already picks a random child position when dropping.

## Alignment notes

- Storyboard: elevated dispenser over training area — this arc is that drop source
- Bible: ball types/actions stay in gameplay logic; this asset does not change Phase rules
- Phase I Ball Drop remains non-required formal training per bible

## Optional cleanup

If an old `Transform_Above` / spawn-point empty already exists in the scene, either:
- repoint `BallDropSystem.transformAbove` to the new prefab’s `Transform_Above`, or
- remove the old empty so only one set of spawn points is used
