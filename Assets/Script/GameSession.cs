// GameSession.cs
public static class GameSession
{
    public static string SelectedOsuFile;
    public static string SelectedBeatmapPath;    // folder path
    public static string SelectedBeatmapName;    // friendly name/title

    // gameplay settings
    public static BeatmapDifficulty SelectedDifficulty;
    public static float SelectedPhantomChance;

    public static void Clear()
    {
        SelectedOsuFile = null;
        SelectedBeatmapPath = null;
        SelectedBeatmapName = null;
        SelectedPhantomChance = 0f;
    }

    public enum BeatmapDifficulty { Easy, Medium, Hard }
}
