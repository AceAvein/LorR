# Pen Tracing — Classroom Assets

Storyboard classroom props for Pen Control Tracing.

## Build (Unity)

**LorR → Pen Tracing → Build Classroom Prefab**

Creates:
- `Assets/Scenes/Scene Simulation/Pen Tracing/Prefabs/PenTracingClassroom.prefab`
- Materials under `.../Pen Tracing/Materials/`

## Contents

| Piece | Detail |
|---|---|
| **PenSelectStation** | Table + Pencil / Ballpen / Highlighter stands + labels |
| **TraceDesk_01..03** | Desk, chair, `TracingBoard` paper pad, paper tray, **Bell**, metrics monitor |
| **NpcStandMarker** | Floor marker at front of room |

## Scene setup

1. Run the menu item
2. Open `Pen Control Tracing.unity`
3. Drag `PenTracingClassroom` into the room
4. Keep existing `PathManager` / `Patterns` / real grab pens working — this prefab is **environment/storyboard layout**
5. Later you can wire bells / desk progression and optionally swap the active tracing surface to each desk’s `TracingBoard`

## Alignment

- Storyboard: select pen → desk 1–3 → paper + bell → monitor → next desk
- Bible: classroom desk + tablet/paper + pen; no timer as primary score
- Pen names match `PenGrabChoice` kinds (visual props; add XR grab later if needed)
