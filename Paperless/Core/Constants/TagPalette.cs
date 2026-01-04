namespace Core.Constants;

public static class TagPalette
{
    public static readonly IReadOnlyList<string> AvailableColors =
    [
        "#3b82f6", // Blue
        "#ef4444", // Red
        "#10b981", // Green
        "#f59e0b", // Amber
        "#8b5cf6", // Purple
        "#ec4899", // Pink
        "#6366f1", // Indigo
        "#14b8a6", // Teal
        "#84cc16", // Lime
        "#f97316", // Orange
        "#64748b", // Slate
        "#71717a" // Zinc
    ];

    public static string GetRandomColor()
    {
        return AvailableColors[Random.Shared.Next(AvailableColors.Count)];
    }
}