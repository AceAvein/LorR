using System;
using System.Collections.Generic;

/// <summary>
/// Doc-aligned pass checks (Comprehensive bible primary).
/// 4 Simulation days → average / requirements → unlock.
/// Phase III–IV also require 2 consecutive qualifying days.
/// </summary>
public static class ProgressionPassRules
{
    public static void FillDayMetrics(DaySessionSave day, float primaryScore, IDictionary<string, object> metrics)
    {
        if (day == null)
            return;

        day.primaryScore = primaryScore;
        day.accuracy = GetFloat(metrics, "accuracy", primaryScore);
        day.completionTimeSec = GetFloat(metrics, "completionTime", GetFloat(metrics, "timeUsed", 0f));
        day.averageReactionMs = GetFloat(metrics, "averageReactionMs", GetFloat(metrics, "averageReaction", 0f) * 1000f);
        day.avgDeviationMm = GetFloat(metrics, "avgDeviationMm", 0f);
        day.maxDeviationMm = GetFloat(metrics, "maxDeviationMm", 0f);
        day.interceptionRate = GetFloat(metrics, "interceptionRate", primaryScore);
        day.correct = (int)GetFloat(metrics, "correct", 0f);
        day.incorrect = (int)GetFloat(metrics, "incorrect", 0f);
        day.missed = (int)GetFloat(metrics, "missed", 0f);
        day.success = (int)GetFloat(metrics, "success", 0f);
        day.fail = (int)GetFloat(metrics, "fail", 0f);
        day.patternsCompleted = (int)GetFloat(metrics, "patternsCompleted", 0f);
        day.patternTotal = (int)GetFloat(metrics, "patternTotal", 0f);
    }

    public static bool DayQualifies(string moduleId, int phase, DaySessionSave day)
    {
        if (day == null || !ProgressionConfig.IsUnguided(day.mode))
            return false;

        phase = ProgressionConfig.ClampPhase(phase);

        if (string.Equals(moduleId, ProgressionConfig.ObjectPlacement, StringComparison.Ordinal))
            return ObjectPlacementDay(phase, day);

        if (string.Equals(moduleId, ProgressionConfig.PenTrace, StringComparison.Ordinal))
            return PenTraceDay(phase, day);

        if (string.Equals(moduleId, ProgressionConfig.BallDrop, StringComparison.Ordinal))
            return BallDropDay(phase, day);

        if (string.Equals(moduleId, ProgressionConfig.ReactionLight, StringComparison.Ordinal))
            return ReactionLightDay(phase, day);

        return day.primaryScore >= ProgressionConfig.PassAverageForPhase(phase);
    }

    public static bool EvaluateWeek(string moduleId, PhaseWeekProgress week, out string detail)
    {
        detail = null;
        if (week == null)
            return false;

        week.weekEvaluated = true;

        var unguided = new List<DaySessionSave>();
        if (week.days != null)
        {
            for (int i = 0; i < week.days.Count; i++)
            {
                DaySessionSave day = week.days[i];
                if (day == null || !ProgressionConfig.IsUnguided(day.mode))
                    continue;
                unguided.Add(day);
            }
        }

        // Sort by playedOn for consecutive-day checks.
        unguided.Sort((a, b) => string.CompareOrdinal(a.playedOn, b.playedOn));

        float sum = 0f;
        int qualifyCount = 0;
        for (int i = 0; i < unguided.Count; i++)
        {
            sum += unguided[i].primaryScore;
            unguided[i].passed = DayQualifies(moduleId, week.phase, unguided[i]);
            if (unguided[i].passed)
                qualifyCount++;
        }

        week.averageUnguidedScore = unguided.Count > 0 ? sum / unguided.Count : 0f;

        bool fullWeek =
            week.playDates != null &&
            week.playDates.Count >= ProgressionConfig.MaxPlayDaysPerWeek &&
            unguided.Count >= ProgressionConfig.MaxPlayDaysPerWeek;

        if (!fullWeek)
        {
            week.weekPassed = false;
            detail = "Need " + ProgressionConfig.MaxPlayDaysPerWeek + " Simulation days.";
            return false;
        }

        int phase = ProgressionConfig.ClampPhase(week.phase);
        bool averageOk = WeekAverageMeets(moduleId, phase, week, unguided);
        bool consecutiveOk = phase < 3 || HasTwoConsecutiveQualifying(moduleId, phase, unguided);
        bool consistencyOk = !(string.Equals(moduleId, ProgressionConfig.PenTrace, StringComparison.Ordinal) && phase == 4)
                             || qualifyCount >= 3;

        week.weekPassed = averageOk && consecutiveOk && consistencyOk;

        if (week.weekPassed)
        {
            detail = "Pass — avg " + week.averageUnguidedScore.ToString("0.0") +
                     (phase >= 3 ? " + 2 consecutive qualifying days" : string.Empty);
        }
        else
        {
            detail = "Fail — avg " + week.averageUnguidedScore.ToString("0.0") +
                     "; qualifyDays=" + qualifyCount +
                     "; consecutive=" + consecutiveOk +
                     "; " + DescribeNeed(moduleId, phase);
        }

        return week.weekPassed;
    }

    static bool WeekAverageMeets(string moduleId, int phase, PhaseWeekProgress week, List<DaySessionSave> days)
    {
        if (string.Equals(moduleId, ProgressionConfig.ObjectPlacement, StringComparison.Ordinal))
        {
            // Phase I: baseline week complete (4 days) — no strict unlock % in Comprehensive.
            if (phase == 1)
                return true;

            float avgAcc = Average(days, d => d.accuracy > 0f ? d.accuracy : d.primaryScore);
            float avgTime = Average(days, d => d.completionTimeSec);

            if (phase == 2)
                return avgAcc >= 70f && avgTime < 30f;
            if (phase == 3)
                return avgAcc >= 90f && avgTime < 25f;
            return avgAcc >= 85f && avgTime < 20f;
        }

        if (string.Equals(moduleId, ProgressionConfig.PenTrace, StringComparison.Ordinal))
        {
            if (phase == 4)
                return Average(days, d => d.avgDeviationMm) <= 1.0f;
            if (phase == 1)
                return true; // baseline week
            return week.averageUnguidedScore >= ProgressionConfig.PassAverageForPhase(phase);
        }

        if (string.Equals(moduleId, ProgressionConfig.BallDrop, StringComparison.Ordinal))
        {
            // Architecture secondary numbers where Comprehensive has gaps; Phase I not required training.
            float avg = Average(days, d => d.interceptionRate > 0f ? d.interceptionRate : d.primaryScore);
            if (phase == 1) return true;
            if (phase == 2) return avg >= 75f;
            if (phase == 3) return avg >= 80f;
            return avg >= 85f;
        }

        // Reaction Light — Comprehensive lists metrics but no exact %; Phase I = baseline complete.
        if (phase == 1)
            return true;
        return week.averageUnguidedScore >= ProgressionConfig.PassAverageForPhase(phase);
    }

    static bool HasTwoConsecutiveQualifying(string moduleId, int phase, List<DaySessionSave> sortedDays)
    {
        for (int i = 0; i < sortedDays.Count - 1; i++)
        {
            DaySessionSave a = sortedDays[i];
            DaySessionSave b = sortedDays[i + 1];
            if (a == null || b == null)
                continue;
            if (!DayQualifies(moduleId, phase, a) || !DayQualifies(moduleId, phase, b))
                continue;
            if (!ProgressionConfig.TryParseDay(a.playedOn, out DateTime da) ||
                !ProgressionConfig.TryParseDay(b.playedOn, out DateTime db))
                continue;

            // Different calendar days and consecutive in the play sequence (next play day after prior).
            if (db.Date > da.Date)
                return true;
        }

        return false;
    }

    static bool ObjectPlacementDay(int phase, DaySessionSave day)
    {
        float acc = day.accuracy > 0f ? day.accuracy : day.primaryScore;
        float time = day.completionTimeSec;
        if (phase == 1) return true; // baseline / no time pressure
        if (phase == 2) return acc >= 70f && (time <= 0f || time < 30f);
        if (phase == 3) return acc >= 90f && time > 0f && time < 25f;
        return acc >= 85f && time > 0f && time < 20f;
    }

    static bool PenTraceDay(int phase, DaySessionSave day)
    {
        if (phase == 4)
            return day.avgDeviationMm > 0f && day.avgDeviationMm <= 1.0f;
        if (phase == 1)
            return true;
        return day.primaryScore >= ProgressionConfig.PassAverageForPhase(phase);
    }

    static bool BallDropDay(int phase, DaySessionSave day)
    {
        float rate = day.interceptionRate > 0f ? day.interceptionRate : day.primaryScore;
        if (phase == 1) return true;
        if (phase == 2) return rate >= 75f;
        if (phase == 3) return rate >= 80f;
        return rate >= 85f;
    }

    static bool ReactionLightDay(int phase, DaySessionSave day)
    {
        if (phase == 1) return true;
        return day.primaryScore >= ProgressionConfig.PassAverageForPhase(phase);
    }

    static string DescribeNeed(string moduleId, int phase)
    {
        if (string.Equals(moduleId, ProgressionConfig.ObjectPlacement, StringComparison.Ordinal))
        {
            if (phase == 2) return "need avg accuracy≥70% and time<30s";
            if (phase == 3) return "need avg accuracy≥90% and time<25s + 2 consecutive pass days";
            if (phase == 4) return "need avg accuracy≥85% and time<20s + 2 consecutive pass days";
            return "need 4 baseline Simulation days";
        }

        if (string.Equals(moduleId, ProgressionConfig.PenTrace, StringComparison.Ordinal) && phase == 4)
            return "need avg deviation≤1.0mm and ≥3 of 4 qualifying days";

        if (phase >= 3)
            return "need week average + 2 consecutive qualifying days";
        if (phase == 1)
            return "need 4 baseline Simulation days";
        return "need week average ≥ " + ProgressionConfig.PassAverageForPhase(phase);
    }

    static float Average(List<DaySessionSave> days, Func<DaySessionSave, float> selector)
    {
        if (days == null || days.Count == 0)
            return 0f;
        float sum = 0f;
        for (int i = 0; i < days.Count; i++)
            sum += selector(days[i]);
        return sum / days.Count;
    }

    static float GetFloat(IDictionary<string, object> metrics, string key, float fallback)
    {
        if (metrics == null || string.IsNullOrEmpty(key) || !metrics.TryGetValue(key, out object raw) || raw == null)
            return fallback;
        try { return Convert.ToSingle(raw); }
        catch { return fallback; }
    }
}
