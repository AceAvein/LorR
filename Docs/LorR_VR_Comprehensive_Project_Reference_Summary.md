# LorR-VR Comprehensive Project Reference Summary

**Source PDF:** `Comprehensive_LorR-VR_General_Project_Reference.pdf`  
**Full title:** LorR-VR: A VR Training System for Non-Dominant Hand Motor Skill Development Utilizing an IMU Algorithm  

**Status:** FINAL alignment source of truth for project implementation.  
When older docs conflict with this guide, **follow this one** unless the team explicitly overrides.

## Related media / side docs

- Dialogue list → `LorR VR Storylines & Dialogue Blueprint`
- Video reference → `Capstone2_PromotionalVideo_2ndSemester20242025_Casco_Tamayo_Tano.mp4`
- Storyboard / layout → `LORR_LAYOUT + STORYBOARD`

## Controller mapping (global)

Training modules must revolve only around these controls:

- **Index** = Trigger
- **Middle** = Grip
- **Thumb** = Joystick

Quest controller can measure: movement, virtual pickup/release, trigger, grip, timing, collisions, pen-path deviation.  
It **cannot** measure: actual grip force, pen pressure, isolated middle-finger movement, muscle activity, physical strength, true finger-joint motion.

## Scene / UX rules

- Short **cutscene** before every simulation (guided or unguided). Creative liberty allowed.
- Player can approach NPC and interact (e.g. trigger pointed at NPC).
- NPC introduces objectives, mechanics, and evaluation metrics at start.
- On unguided training, NPC is mainly an **observer** (taking notes / observing).
- After every simulation: **fade to black**, then show:
  - Metrics / progress UI
  - Exit
  - Repeat (guided, or after ~5 attempts with little/no progress)
  - Continue to unguided/simulation
  - Test Phase II/III/IV button (**Phase I only**)
- Add loading/buffer between applicable module transitions.

## Phase scheduling (unguided)

General rules:

- 4 phases; each planned for ~1 week
- 4 training sessions per phase
- 1 session = 1 training day
- Sessions on separate days; rest days between
- Complete required sessions before next phase
- **Do not auto-advance** just because a week/day passed
- If requirements unmet → repeat the phase for another week
- Option to add more sessions per phase

**Baseline clarification:**  
Phase I baseline values are a performance **reference**, not passing scores / unlock thresholds. Advancement uses phase-specific criteria.

Phase notes:

- **Phase I:** baseline + orientation; introduce basics; document baseline; progress when Phase I requirements met
- **Phase II:** foundational settings; record every session; progress when requirements met
- **Phase III:** requirements must be met in **2 consecutive sessions on different days**
- **Phase IV:** final assessment in **2 consecutive sessions on different days**; else repeat Phase IV week

Progression rules:

- Check requirements after every completed session
- Do not increase difficulty automatically each session
- Stay on same phase until requirements met
- For III/IV, check the 2 most recent consecutive qualifying sessions

## Design split

**Protocol determines:** activity, sets/trials, duration (when specified), difficulty, performance requirements.  
**LorR-VR determines:** VR presentation, interaction, recording, metric display.  
VR should support the exercise, **not change** prescribed training requirements.

---

## Module 1 — Reaction Light Burst (Speed)

**Purpose:** visual-spatial response speed, target acquisition, attention switching.  
**Input model:** controller tip / assigned Trigger vs Grip responses (not literal isolated fingers).

### Phase I — Baseline / Orientation
- Flash: 1 / 3.0s
- Sets: 3 × 30s
- Single target; basic single response; no distractors
- One light active at a time for the full flash interval
- Metrics: RT (ms), tap accuracy, correct / incorrect / missed

### Phase II — Foundational
- Flash: 1 / 1.5s
- Sets: 3 × 45s
- 2 colors:
  - Color A = Index (**Trigger**)
  - Color B = Middle (**Grip**)
- Metrics: avg RT, correct accuracy, incorrect finger-response, missed

### Phase III — Intermediate
- Flash: 1 / 0.8s
- Sets: 3 × 60s
- Multiple finger assignments (same Trigger/Grip model as Phase II)
- Metrics: avg RT, accuracy, incorrect, missed, correct finger-assignment count

### Phase IV — Advanced
- Flash: ≤ 1 / 0.5s
- Sets: 3 × 90s
- Color + position → assigned input
- Example mapping (implementation example only):
  - Red+Left=Trigger, Red+Right=Grip, Blue+Left=Grip, Blue+Right=Trigger
- Metrics: avg RT, accuracy, incorrect, missed, correct response-mapping count

---

## Module 2 — Ball Drop Reflex (Speed)

**Theme:** Court / “prevent”  
**Environment:** typical half court facing the ring  
**Assets:** different kinds of balls (creator’s decision)

**Story flow:** NPC explains objectives + metrics → enter training court / play zone → perform drops → metrics on monitor/LED → fade + UI

### Critical rule
- Protocol does **not** prescribe Ball Drop for Phase I
- **Do not implement Ball Drop as required Phase I training**
- May show as preview/tutorial only; tutorial results do **not** count as formal training
- Can remain locked until participant reaches Phase II

### Phase II
- Drop height ~30 cm
- Unpredictable drop behavior
- 3 sets × 10 trials (30 total)
- Catch or designated finger interception
- Metrics: success/fail counts, success rate, response time if reliable, correct designated-finger responses if used

### Phase III
- Variable drop height / unpredictable
- 3 sets × 12 trials (36 total)
- Randomized catch or deflect
- Ball-type rules (from project notes section):
  - Ball A (volleyball/basketball) → catch
  - Ball B (baseball) → catch with Trigger (pinch catch)
  - Ball C (football) → deflect with Grip
- Metrics: interception success/fail, correct/incorrect catch-deflect, success rate, RT if reliable

### Phase IV
- 3 sets × 15 trials (45 formal / notes also mention 60 in places — confirm in implementation against set×trial math)
- Concurrent cognitive distractor (exact type not prescribed; confirm clinically)
  - Possible: verbal question / count backwards
  - Alternative distractor idea: press a button that appears quickly
- Do not stack multiple distractors without approval
- Same ball-type rules + variable sources
- Metrics: interception success/fail/rate, secondary-task correct/incorrect, RT if reliable

**Interaction notes:**
- Grip to catch designated balls
- Movement/wave to deflect designated balls
- Virtual collision interception (not real finger-ball contact)

---

## Module 3 — Pen Tracing (Precision)

**Environment:** school / classroom  
**Setup:** training room, desk with virtual tracing tablet/paper, chair, pen, NPC in front  
**No timer** as primary score driver

### Measurement
- Tracked controller = virtual pen
- Compare traced path vs reference path
- Deviation in mm
- `1 Unity unit = 1 meter` → `0.001 Unity units = 1 mm`
- Does **not** measure real pen pressure / isolated finger pressure

### Session structure (implementation recommendation)
- 4 sessions per phase
- 3 sets/session × 3 patterns/set = 9 pattern attempts/session
- One scored attempt per pattern; incomplete ≠ auto-success
- Vary patterns across sessions to reduce memorization
- Note: 3×3 structure is recommendation, not original protocol-prescribed values

### Phase I
- Wide straight + gentle curves; slow; accuracy over speed
- Patterns: horizontal, vertical, gentle curve
- Metrics: avg/max deviation (mm), completion status, time supplementary

### Phase II
- Narrower lines, curves, angles, circles/ovals
- Patterns: narrow angled combo, S-curve, circle/oval
- Same core metrics

### Phase III
- Multidirectional: spirals, irregular polygons, combined turns
- Add directional completion accuracy if implemented

### Phase IV
- Narrow high-fidelity complex paths
- Final tracing deviation requirement: **≤ 1.0 mm**
- Progression: 4 sessions; consistent performance across at least 3 of 4; preserve history

---

## Module 4 — Object Placement / Color Sort (Coordination)

**Purpose:** grasp/release control, visual identification, placement accuracy  
**Environment notes in guide:** office space / facility-escape framing in project notes  
**Assets needed:**
- Colored boxes / paper bags
- Colored items
- Fruits, Vegetables, Beverages, Fragile items, Gadgets

### Core rules
- Item counts are per set
- Wrong placement cannot be corrected
- Once released into wrong container → incorrect; stay/remove automatically; no retry that placement
- Accuracy based on total assigned objects for the set
- Grip for grasp; Trigger only if interaction design requires
- Virtual grip ≠ real grip force
- Do not require injured dominant hand

### Project mechanics notes (category / input progression)
- **Phase I:** Category A Yellow/fruits = Grip only → yellow box; Category B Red/vegetables = Trigger only → red box
- **Phase II:** Category C Blue/beverages = both controls → blue box
- **Phase III:** Category D via joystick / timed grip / grip (random, max ~2s); irregular/fragile need specific angle grip (e.g. from below); green → green box
- **Phase IV:** Category E via timed trigger / trigger (random, max ~2s); after every 5 items, boxes switch places; 5 categories = Fruits, Vegetables, Beverages, Fragile, Gadgets

### Formal phase settings
- **Phase I:** 3 sets × 10 items (30); slow; no time pressure
- **Phase II:** 10 items under 30s; 3 color categories
- **Phase III:** timing under 25s; higher accuracy requirement (example: ≥90% AND under 25s)
- **Phase IV:** under 20s; full category set / moving complexity as configured

Metrics: correct/incorrect placements, placement accuracy, completion time (supplementary / required by phase)

---

## Metrics formulas (selected)

- Placement / response accuracy style: `(correct / total valid attempts) × 100`
- Completion time = finish timestamp − start timestamp
- Object Placement time gates:
  - Phase II &lt; 30s
  - Phase III &lt; 25s
  - Phase IV &lt; 20s
- For accuracy + time phases: **both** must pass

## Protocol gaps (do not invent silently)

1. No Ball Drop in Phase I
2. No exact Pen Tracing line widths from protocol
3. No exact Phase IV Ball Drop distractor type
4. No exact Reaction Light response timeout
5. No exact object names / container designs prescribed

---

## Project revision notes (product requirements)

### Profile / navigation
- Require create/select profile before play
- Greeting when opening profile
- Loading screens on scene transitions as needed
- After guided tutorial: Continue to Simulation | Retry Guided Tutorial
- Add system/controller tutorial module

### Guided tutorial (Ma’am Rox)
- Clearer instructions / indicators / prompts
- Feedback even when correct
- Allow mistakes; do not auto-advance on wrong action
- Corrective feedback on errors (e.g. out of line, wrong hand)
- Separate instructional prompts vs corrective feedback
- Color Sort: block progress on wrong hand
- Tutorial must be fully functional before unguided

### Training structure
- Replace Levels with **Sessions**
- Show phase + session clearly (not arbitrary Easy/Mod/Hard labeling)
- Align 4 sessions/phase + rest days
- Indicate session completed for the day

### Objectives / instructions / mechanics
Separate in every module:
1. Objectives
2. Instructions
3. Mechanics (controls/rules)
Explain evaluation criteria for success/fail.

### Scoring / feedback / stats
- Visible score/timer/progress
- Immediate feedback/affirmation
- Simple post-session stats with necessary metrics only
- Explain scoring basis

## Progress report / dashboard

Overview:
- Current phase, completed/remaining sessions, overall progress, phase completion, averages, advancement eligibility

Per module examples:
- Reaction Light: avg RT, accuracy, correct/incorrect/missed
- Ball Drop: interception rate, success/fail counts, avg RT if recorded
- Pen Tracing: avg/max deviation, path completion, time supplementary
- Object Placement: accuracy, completion time, correct/incorrect counts

Also:
- Performance history charts over time
- Per-phase progress report (averages, session results, module breakdown, unmet requirements, advancement status)

## Settings module

- Audio: master / music / SFX volumes + enable toggles
- Visual: brightness if supported
- Controller: sensitivity + restore defaults if supported
- App: main menu, about/version, exit
- Persist settings across restarts
- Settings must **not** change prescribed training parameters or auto-change difficulty

---

## Alignment checklist vs current codebase

1. Replace Easy/Moderate/Hard presentation with Phase + Session model
2. Implement scheduling / no calendar-only unlocks
3. Ball Drop: remove/skip required Phase I; start formal training at Phase II
4. Map Reaction Light finger rules to Trigger/Grip (not literal fingers)
5. Object Placement: grocery categories + grip/trigger rules per phase
6. Post-session fade + metrics UI buttons per guide
7. Guided tutorials: allow mistakes + corrective feedback
8. Profile gate + progress dashboard + settings persistence
9. Keep storyboard/dialogue docs for presentation; this PDF for training truth
