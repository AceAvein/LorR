# LorR-VR Architecture Alignment Summary

> **Superseded for implementation priority:** use `LorR_VR_Comprehensive_Project_Reference_Summary.md` as the final alignment source of truth. Keep this file for historical/reference comparison only.

Reference summary from `LorR_VR_Developer_Architecture_and_Phase_Guide`.

## Paradigm shift

- **Old:** Easy / Moderate / Hard levels
- **New:** 4-Phase Clinical Progression (Bulletproof Recovery Progressive Fine Motor Protocol, authored by Windsor Ongchan, CAT(C))
- **Goal:** Improve non-dominant hand (NDH) toward dominant hand (DH), measured by **HDI / BSI**

## 4 simulations

| # | Simulation | Layer | Clinical focus |
|---|---|---|---|
| 1 | Reaction Light Burst | Speed | React to lights, hit correct targets, avoid distractors |
| 2 | Ball Drop Reflex | Speed | Catch/intercept falling balls, timing, dual-task later |
| 3 | Pen Control Path Tracing | Precision | Stay on path, small deviation, micro-corrections |
| 4 | Object Placement / Color Sort | Coordination | Sort objects into bins, use both hands under time pressure |

## Phase ladder (all drills)

1. **Phase I – Baseline (Week 1):** easy params, ~4 sessions, log baseline
2. **Phase II – Foundational (Week 2):** harder rules; hit thresholds across 2 consecutive sessions
3. **Phase III – Intermediate (Week 3):** variable difficulty / distractors; tighter thresholds across 2 sessions
4. **Phase IV – Advanced / Mastery (Week 4+):** hardest settings; near-DH performance

Each drill defines:

- Unity parameters (timing, height, tolerance, distractors, set length)
- Unlock thresholds
- Post-session results UI metrics
- Calculation formulas

### Ball Drop phase snapshot

| Phase | Drop setup | Advance when |
|---|---|---|
| I | Fixed 30 cm, 1.0 m/s, basic NDH catch | 4 sessions + baseline logged |
| II | Same height, 1.5 m/s, finger rule | Acc ≥ 75%, rule Acc ≥ 80%, 2 sessions |
| III | Variable 20–50 cm, 1.5–2.5 m/s, catch vs deflect | Acc ≥ 80%, rule Acc ≥ 85%, 2 sessions |
| IV | Fast ≥ 2.5 m/s + audio dual-task | Acc ≥ 85%, bilateral gap ≤ 15% |

Key Ball Drop formulas:

- `Acc_drop = caught / total drops × 100`
- `Asym_drop = Acc_dominant − Acc_nondominant`
- `S_reflex = Acc_drop`

## Composite scores (HDI / BSI)

Normalized sub-scores (0–100):

- `S_speed` from Reaction Light
- `S_reflex` from Ball Drop
- `S_precision` from Pen Trace
- `S_coord` from Object Placement

```text
NDH_Composite = 0.25×S_speed + 0.25×S_reflex + 0.25×S_precision + 0.25×S_coord
BSI% = (NDH_Composite / DH_Baseline_Composite) × 100
```

### BSI bands (Progress Report)

| BSI | Status | System action |
|---|---|---|
| < 60% | Severe NDH deficit | Stay in Phase I |
| 60–74.9% | Moderate | Advance to Phase II |
| 75–84.9% | Functional | Advance to Phase III |
| ≥ 85% | Near-symmetrical mastery | Unlock Phase IV / discharge |

Phase unlocks also happen **per drill** from drill-specific thresholds. BSI/HDI is the overall clinical progress readout across all four simulations.

## Current codebase note

Existing scripts still use **Easy / Moderate / Hard** (Reaction Light, Ball Drop, Color Sort, Pen Trace, music/sky tiers, etc.). Alignment work means replacing that tier model with Phase I–IV configs, results formulas, unlock gates, and HDI/BSI progress UI.

## Alignment checklist (for later)

1. Map each module’s current Easy/Mod/Hard → Phase I–IV params
2. Implement per-session results formulas from the guide
3. Add phase unlock thresholds (including consecutive-session rules)
4. Add DH baseline + HDI/BSI aggregation
5. Wire Progress Report UI to BSI bands
