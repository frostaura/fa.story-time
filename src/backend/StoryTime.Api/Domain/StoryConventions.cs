namespace StoryTime.Api.Domain;

public static class StoryModes
{
    public const string Series = "series";
    public const string OneShot = "one-shot";
}

public static class PosterRoles
{
    public const string Background = "BACKGROUND";
    public const string Midground1 = "MIDGROUND_1";
    public const string Midground2 = "MIDGROUND_2";
    public const string Foreground = "FOREGROUND";
    public const string Particles = "PARTICLES";

    public static IReadOnlyList<string> Ordered { get; } =
        [Background, Midground1, Midground2, Foreground, Particles];

    public static IReadOnlyList<string> Required { get; } =
        [Background, Foreground, Particles];
}
