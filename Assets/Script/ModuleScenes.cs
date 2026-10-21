public static class ModuleScenes
{
    public static bool IsPenTrace(string name)
    {
        return name == "PenControlTracing"
            || name == "Pen Control Tracing"
            || name == "Guided Pen Control Tracing";
    }

    public static bool IsBallDrop(string name)
    {
        return name == "Ball Drop"
            || name == "Guided Ball Drop"
            || name == "BallDrop_HalfwayTest";
    }

    public static bool IsColorSort(string name)
    {
        return name == "ColorSort"
            || name == "Color Sort"
            || name == "Guided Color Sort";
    }

    public static bool IsReactionLight(string name)
    {
        return name == "ReactionLight"
            || name == "Reaction Light"
            || name == "Guided Reaction Light"
            || name == "09_ReactionLight";
    }

    public static bool IsPlayModule(string name)
    {
        return IsPenTrace(name) || IsBallDrop(name) || IsColorSort(name) || IsReactionLight(name);
    }

    public static bool IsGuided(string name)
    {
        return !string.IsNullOrEmpty(name) && name.StartsWith("Guided ");
    }
}
