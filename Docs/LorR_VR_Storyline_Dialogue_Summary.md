# LorR-VR Storyline & Dialogue Blueprint Summary

> **Note:** For training rules, phases, metrics, and unlocks, follow `LorR_VR_Comprehensive_Project_Reference_Summary.md`. Use this file mainly for dialogue / presentation reference (also cited by the comprehensive guide).

Reference summary from `LorR_VR_Storylines___Dialogue_Blueprint`.

- Architecture doc = rules, params, formulas
- Storyline doc = what the player hears, sees, and chooses at each gate

## Companion

**Dr. Alex Rivera** — Hand Rehabilitation Specialist / Motor Performance Coach NPC.

Same character, different persona per drill:

| Drill | Rivera persona | World progression |
|---|---|---|
| Pen Trace | Hand Rehab Specialist | Classroom → Chalkboard → Blueprint studio → Cleanroom circuits |
| Object Placement | Market / Logistics coach | Corner store → Express checkout → Warehouse → Robotic fulfillment |
| Reaction Light | Esports / Reflex coach | Neon arcade → Gym FitLight → Stadium → 360 hologram dome |
| Ball Drop | Neuro-motor specialist | Therapy room → Biomechanics lab → Velocity field → Dual-task chamber |

## Clinical session structure & lock logic

```text
Week 1 Session
  → Dominant-hand baseline (where applicable)
  → Guided tutorial (with live error corrections)
  → Phase I unguided play
  → Telemetry checkpoint
       PASS → schedule next appt in 7 days + lock next phase until then
       FAIL → tip + Retry now / Main menu

Week 2 / 3 / 4
  → Unlock next phase environment
  → Play → Checkpoint → Pass (lock + 7-day appt) or Fail (retry)
```

System rules:

1. First appointment = baseline + **Phase I only** across drills
2. Guided mode: wrong action → Rivera interrupts with correction
3. Pass → “come back in 7 days” + phase lock until appointment date
4. Fail → encouraging tip + immediate retry or menu

## Shared scene structure per drill

### Part A — Dominant baseline (Pen Trace + Reaction Light)

Record DH gold standard first, then clear Phase I.

### Part B — Guided tutorial (4 steps + retry checkpoint)

Typical pattern:

1. Correct hand / ready pose
2. Correct control (trigger / grip / tap)
3. Core rule + scoring metric explained
4. “Ready for Phase I?” → **Proceed** | **Try tutorial again**

Every step has: success line, error trigger, error dialogue, UI action/button.

### Part C — Unguided Phase I–IV

Each phase has:

- Themed environment + Rivera briefing (params match architecture guide)
- Clinical transition checkpoint at Consultation Desk / Telemetry Hologram
- **Success branch:** PASS metrics + “return in 7 days” + lock next phase
- **Remedial branch:** UNMET metrics + tip + Retry / Main menu

## Ball Drop storyline snapshot

**Goal:** predictive timing, interception, dual-task.

**Guided steps:**

1. NDH in glowing blue target box
2. **Green = CATCH** (grip), **Orange = DEFLECT** (palm slap)
3. Pre-shape fingers from overhead prompt (e.g. Pinch Catch) before drop
4. Ready for Phase I gym?

**Phases (story):**

- **I** — Fixed 30 cm, 1.0 m/s, basic catch
- **II** — 1.5 m/s + finger pose prompts
- **III** — Variable 20–50 cm height, catch vs deflect colors
- **IV** — ≥2.5 m/s + count backward by 3s while intercepting

## Ending: Combined HDI / BSI consultation

After all four drills, user enters **Rivera’s Consultation Office**. Hologram shows:

- DH baseline composite vs NDH final composite
- BSI %
- Same formulas as architecture guide

Final NPC lines by band:

| BSI | Classification | Tone |
|---|---|---|
| < 60% | Severe NDH deficits | Stay foundational; accuracy over speed |
| 60–74.9% | Moderate function | Phase II foundation; keep pushing |
| 75–84.9% | Functional dexterity | Phase III; RT within ~25% of DH |
| ≥ 85% | Near-symmetrical mastery | Phase IV victory; “functionally ambidextrous” |

UI: **Return to Main Menu** | **Export Clinical Progress Report**

## Current codebase note

Existing guided modules (`GuidedBallDropLesson`, `GuidedReactionLightLesson`, etc.) and Easy/Mod/Hard rounds need upgrading into:

- Named Rivera dialogue + error interrupts
- Phase I–IV environments + appointment locks
- Pass/fail clinical transition scenes
- Final BSI consultation screen
