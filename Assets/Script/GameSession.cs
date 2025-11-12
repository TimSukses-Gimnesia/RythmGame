public static class GameSession
{
    public static string SelectedOsuFile;
    public static string SelectedBeatmapPath;
    public static string SelectedBeatmapName;

    public static void Clear()
    {
        SelectedOsuFile = null;
        SelectedBeatmapPath = null;
        SelectedBeatmapName = null;
    }
}