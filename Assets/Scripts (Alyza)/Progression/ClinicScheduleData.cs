using System;
using System.Collections.Generic;

public enum ClinicDayPlanType
{
    Session = 0,
    Rest = 1
}

public enum ClinicDayMark
{
    None = 0,
    PlannedSession = 1,
    PlannedRest = 2,
    CompletedOnPlan = 3,
    CompletedEarly = 4,
    Missed = 5,
    Today = 6
}

[Serializable]
public class ClinicDayPlan
{
    public string date;
    public int type; // ClinicDayPlanType
    public int slot; // 1..4 when session
    public int phase; // which phase week this belongs to
}

[Serializable]
public class ClinicScheduleState
{
    public string profileId;
    public string trainingStartDate;
    public List<ClinicDayPlan> days = new List<ClinicDayPlan>();
}

public static class ClinicScheduleUtil
{
    /// <summary>P-R-P-R-P-R-P from start (4 sessions, 3 rests).</summary>
    public static List<ClinicDayPlan> BuildPhaseWeek(string startYyyyMmDd, int phase)
    {
        var list = new List<ClinicDayPlan>();
        if (!ProgressionConfig.TryParseDay(startYyyyMmDd, out DateTime start))
            return list;

        int sessionSlot = 0;
        for (int i = 0; i < ProgressionConfig.WeekLengthDays; i++)
        {
            bool isSession = (i % 2) == 0; // 0,2,4,6 session; 1,3,5 rest
            var plan = new ClinicDayPlan
            {
                date = start.AddDays(i).ToString("yyyy-MM-dd"),
                phase = phase,
                type = isSession ? (int)ClinicDayPlanType.Session : (int)ClinicDayPlanType.Rest,
                slot = 0
            };
            if (isSession)
            {
                sessionSlot++;
                plan.slot = sessionSlot;
            }

            list.Add(plan);
        }

        return list;
    }

    public static ClinicDayPlan FindDay(ClinicScheduleState state, string date)
    {
        if (state == null || state.days == null || string.IsNullOrEmpty(date))
            return null;

        for (int i = 0; i < state.days.Count; i++)
        {
            if (state.days[i] != null &&
                string.Equals(state.days[i].date, date, StringComparison.Ordinal))
                return state.days[i];
        }

        return null;
    }
}
