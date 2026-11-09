# LorR-VR Storyboard & Layout Instructions

**Source:** hand-drawn storyboard / layout sheets  
- `Storyboard_FINAL`
- `BALL DROP LAYOUT - HALF COURT`
- `OBJECT PLACEMENT — LAYOUT`
- `REACTION LIGHT` layout

**Role in docs:**
- **Comprehensive Project Reference** = training bible (phases, metrics, controls)
- **This file** = scene layout, cutscene flow, set progression, environment instructions
- Dialogue Blueprint = full spoken lines (these storyboards have only short prompts)

---

## Shared pattern (all modules)

1. NPC introduces the player / objectives
2. User is prompted to walk in to start
3. User completes sets/tasks in the themed space
4. NPC commendation
5. **Fade to black + results UI**

Exact NPC speech is mostly not written here (bubbles / short UI text only).

---

## 1) Reaction Light

### Theme
Escape / facility rooms (“escape”).

### Storyline (storyboard)
- NPC introduces the player
- User enters a room and performs reaction-light sets to turn on a generator and unlock the next room
- Same mechanics for following sets
- Last room features a dummy that needs to be saved
- After sets, user exits with the dummy through the 2nd door and finishes the session
- Fade to black + UI

### Layout (top-down)
Clockwise room loop:

1. **SPAWN** — looks like Clinic / Training Lobby → door/hallway to Set 1
2. **SET 1** — wall panel with light grid → door to Set 2
3. **SET 2** — wall panel with light grid → door to Set 3
4. **SET 3** — reaction panel (+ large circular wall element) → **FINISH** door back toward Spawn

### Gameplay instruction on sheet
- Correctly press the appearing lights to enter the next rooms
- Do it under the timer

### Panel flow
1. NPC close-up / speech → user prompted to walk in to start
2. Inside generator room → finish set (`SET COMPLETE! TIME:`)
3. Doorway → prompted to next room for set 2; repeat
4. User exits → NPC commend → fade to black

---

## 2) Ball Drop

### Theme
Court (“prevent” / present training court).

### Storyline (storyboard)
- NPC explains objectives and metrics, then prompts user to enter the training court
- Once player enters the play zone, perform ball-drop mechanics
- Timer/metrics shown on monitor or LED wall beside ring
- Session end → fade to black + UI

### Layout — Half Court
Two connected spaces:

- **Spawn room** (smaller, with player start markers)
- **Half court** training room
  - Player near entrance into court
  - **NPC** on the side of the court
  - Center circle = **Area of Training**
  - Hoop / key at far end

NPC objective note on sheet:
> Catch and intercept the correct balls, with the specified non-dominant hand.

### Cutscene idea (written on layout)
1. Fade in — **3rd person view**
2. Show NPC explaining objectives, asks user to enter
3. Camera pan to user entering the half court
4. Switch to **1st person** (fade in)

### Panel flow
1. NPC at door → user introduced, prompted to walk in
2. Court / hoop view with timer (`1:00`) → enter designated zone
3. Intro panel with **TRIGGER** / **GRIP** → press start if ready
4. Ceiling dispenser dropping balls → repeat with different balls
5. `YOU DID IT!` → commend player → fade to black + UI

---

## 3) Object Placement

### Theme
Office / training room (“post”); donation sorting story.

### Storyline (layout sheet)
- Items need to be sorted for donation
- Two conveyor belts:
  - Side belt = unsorted item box delivery
  - Front belt = sorted boxes transport / delivery
- NPC introduces the task
- After first set is sorted, side conveyor delivers another box for next set, and so on
- Once a box’s objects are sorted, they are transported to the front conveyor

### Layout
Room split by **glass barrier / window + door**:

**User side**
- User in center
- Side conveyor with incoming box
- Front conveyor for sorted boxes
- Sorting stations / boxes (A, B, C shown in storyboard)
- **Start Conveyor** button near side belt
- Door through glass barrier

**NPC side (behind glass)**
- NPC observing / introducing from other side

### Mechanic note on sheet
- Physical button starts the conveyor
- Optional timer may start when pressed
- Item box is delivered to the side → user begins sorting

### Panel flow
1. NPC near doorway → introduced, walk in to start
2. Counter with boxes A/B/C + button + `SCREEN SET 1` → enter zone, press btn to start
3. `SCREEN SET 2` → new box for next set; press btn for next set
4. `FINISHED! PUT BOXES IN CONVEYOR BELT` → put boxes on conveyor; press button beside conveyor after
5. NPC `GOOD JOB!` → fade to black + UI

---

## 4) Pen Tracing

### Theme
Office / classroom (“free”).

### Storyline (storyboard)
- NPC introduces objectives
- Pen selector workstation (different pens on desk)
- User picks a pen, goes to first desk, traces
- After finishing: get paper, press bell, move to next desk
- Repeat until 3rd desk
- NPC collects papers, commends player
- Fade to black + UI

### Panel flow
0. Introduction to workstation (pens laid out)
1. Selecting pen
2. Desk with paper + bell + metrics monitor  
   - Note: if cloud, NPC can guide user layout (visuals in-progress)  
   - User must get paper after tracing
3. Trace path on paper → after pressing bell, move to next desk (repeat)
4. Evaluation / time-up style UI → fade to black, only UI shown

---

## Implementation checklist from storyboards

- [ ] Build module environments to match layouts (rooms, barriers, court, conveyors, desks)
- [ ] Add pre-sim cutscene / NPC intro + walk-in start prompt
- [ ] Gate set progression (doors / next-set buttons / next desk)
- [ ] Show in-scene status UI (`SET COMPLETE`, timers, Trigger/Grip panel, screens)
- [ ] End every module with NPC commend + fade to black + results UI
- [ ] Ball Drop: spawn room + half court + 3rd→1st person cutscene
- [ ] Object Placement: glass-separated NPC, dual conveyors, start button, A/B/C stations
- [ ] Reaction Light: lobby spawn + 3 set rooms unlocked by correct timed presses
- [ ] Pen Tracing: pen select → 3 desks with bell transition → paper collection

## Notes / gaps

- Full dialogue lines are **not** in these sheets — pull from Dialogue Blueprint or write to match prompts
- Some wording differs slightly across sheets (e.g. Ball Drop theme “prevent” vs “present”); follow Comprehensive Reference for training rules, these sheets for space/flow
- Reaction Light storyboard uses room-to-room escape fantasy; Comprehensive Reference still owns formal phase metrics/controls
