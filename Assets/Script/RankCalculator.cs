public static class RankCalculator
{
    public static string GetRank(float accuracy)
    {
        if (accuracy >= 90) return "A";
        if (accuracy >= 80) return "B+";
        if (accuracy >= 70) return "B";
        if (accuracy >= 60) return "C+";
        if (accuracy >= 50) return "C";
        return "D";
    }
}