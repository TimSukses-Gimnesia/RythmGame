using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

[RequireComponent(typeof(AudioSource))]
public class BeatmapSelectManager : MonoBehaviour
{
    [Header("Difficulty Visuals")]
    public DifficultyButtonVisual easy;
    public DifficultyButtonVisual medium;
    public DifficultyButtonVisual hard;

    private DifficultyButtonVisual selectedVisual;

    [Header("Animator")]
    public CarouselAnimator carousel;
    public CDSpinner cdSpinner;

    [Header("Preview System")]
    public BeatmapPreviewManager previewManager;

    [Header("UI Covers")]
    public Image coverMain;
    public Image prevCover;
    public Image nextCover;

    [Header("Manual Difficulty Buttons")]
    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;

    // 🔥 BARU: UI TEXT UNTUK HIGHSCORE
    [Header("Highscore UI")]
    public TextMeshProUGUI highscoreText; // Hubungkan ini di Inspector

    [Header("SFX")]
    public AudioClip playButtonSound;
    private AudioSource sfxSource;

    private string beatmapFolder;
    private string[] beatmapFolders;
    private int index = 0;

    private bool isAnimating = false;

    // Difficulty for gameplay
    public enum Difficulty { Easy, Medium, Hard }
    public static Difficulty SelectedDifficulty;
    public static float SelectedPhantomChance = 0f;

    void Start()
    {
        sfxSource = GetComponent<AudioSource>();

#if UNITY_EDITOR
        beatmapFolder = Path.Combine(Application.dataPath, "Beatmaps");
#else
        string root = Directory.GetParent(Application.dataPath).FullName;
        beatmapFolder = Path.Combine(root, "Beatmaps");
#endif

        if (!Directory.Exists(beatmapFolder))
        {
            Debug.LogError("[BeatmapSelect] Folder Beatmaps tidak ditemukan.");
            return;
        }

        beatmapFolders = Directory.GetDirectories(beatmapFolder);

        if (beatmapFolders.Length == 0)
        {
            Debug.LogError("[BeatmapSelect] Tidak ada beatmap.");
            return;
        }

        // Bind Difficulty Buttons
        easyButton.onClick.AddListener(() => SetDifficulty(Difficulty.Easy, easyButton));
        mediumButton.onClick.AddListener(() => SetDifficulty(Difficulty.Medium, mediumButton));
        hardButton.onClick.AddListener(() => SetDifficulty(Difficulty.Hard, hardButton));

        // Load first beatmap
        ShowBeatmap(0);
        SelectedDifficulty = Difficulty.Easy;
        SelectedPhantomChance = 0f;
        SetDifficulty(Difficulty.Easy, easyButton);
    }

    void SetDifficultyButtonVisual(Button newButton)
    {
        DifficultyButtonVisual newVisual = GetVisual(newButton);

        if (selectedVisual != null)
        {
            Image oldImg = selectedVisual.button.GetComponent<Image>();
            oldImg.sprite = selectedVisual.defaultSprite;

            var oldColor = selectedVisual.button.colors;
            oldColor.normalColor = Color.white;
            selectedVisual.button.colors = oldColor;
        }

        selectedVisual = newVisual;

        Image img = newVisual.button.GetComponent<Image>();
        SpriteState state = newVisual.button.spriteState;

        if (state.pressedSprite != null)
            img.sprite = state.pressedSprite;

        var colors = newVisual.button.colors;
        colors.normalColor = colors.pressedColor;
        newVisual.button.colors = colors;
    }

    DifficultyButtonVisual GetVisual(Button btn)
    {
        if (btn == easy.button) return easy;
        if (btn == medium.button) return medium;
        if (btn == hard.button) return hard;
        return null;
    }

    public void ShowBeatmap(int i)
    {
        index = (i + beatmapFolders.Length) % beatmapFolders.Length;

        string folder = beatmapFolders[index];
        string beatmapName = Path.GetFileName(folder);

        string osuFile = GetOsuFile(folder);
        if (osuFile == null)
        {
            Debug.LogError("[BeatmapSelect] Beatmap tidak punya file .osu: " + folder);
            return;
        }

        previewManager.ShowPreview(folder);
        previewManager.PlayPreview(folder);

        LoadImageToUI(previewManager.FindCoverImage(folder), coverMain);

        string prevFolder = beatmapFolders[(index - 1 + beatmapFolders.Length) % beatmapFolders.Length];
        LoadImageToUI(previewManager.FindCoverImage(prevFolder), prevCover);

        string nextFolder = beatmapFolders[(index + 1) % beatmapFolders.Length];
        LoadImageToUI(previewManager.FindCoverImage(nextFolder), nextCover);

        // 🔥 BARU: Tampilkan High Score
        DisplayHighscores(beatmapName);
    }

    // 🔥 FUNGSI BARU: Hanya Menampilkan SKOR TERTINGGI PERTAMA secara MENDATAR
    private void DisplayHighscores(string beatmapName)
    {
        if (highscoreText == null) return;

        List<HighscoreManager.ScoreEntry> topScores = HighscoreManager.LoadTop3(beatmapName);

        if (topScores == null || topScores.Count == 0)
        {
            // Tampilan jika tidak ada skor
            highscoreText.text = "High Score: ";
        }
        else
        {
            // Ambil entri skor tertinggi (indeks 0)
            HighscoreManager.ScoreEntry topScoreEntry = topScores[0];

            // Format tampilan skor tertinggi secara mendatar:
            // "🏆 High Score: [SCORE] ([DATE])"
            string display = $"High Score: <color=yellow>{topScoreEntry.score:N0}</color> ";

            highscoreText.text = display;
        }
    }


    Sprite LoadSprite(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);
        tex.LoadImage(bytes);

        return Sprite.Create(tex,
            new Rect(0, 0, tex.width, tex.height),
            new Vector2(0.5f, 0.5f));
    }

    public void NextBeatmap()
    {
        if (isAnimating) return;
        cdSpinner.SetDirectionNext();
        StartCoroutine(NextFlow());
    }

    public void PrevBeatmap()
    {
        if (isAnimating) return;
        cdSpinner.SetDirectionPrev();
        StartCoroutine(PrevFlow());
    }

    IEnumerator NextFlow()
    {
        isAnimating = true;

        int len = beatmapFolders.Length;
        if (len == 0)
        {
            isAnimating = false;
            yield break;
        }

        int nextIndex = (index + 1) % len;
        int cloneIndex = (index + 2) % len;

        string cloneFolder = beatmapFolders[cloneIndex];

        Sprite cloneSprite = null;
        string cloneCoverPath = previewManager.FindCoverImage(cloneFolder);
        if (!string.IsNullOrEmpty(cloneCoverPath))
            cloneSprite = LoadSprite(cloneCoverPath);

        yield return StartCoroutine(carousel.AnimateNext(cloneSprite));

        index = nextIndex;
        ShowBeatmap(index);

        isAnimating = false;
    }

    IEnumerator PrevFlow()
    {
        isAnimating = true;

        int len = beatmapFolders.Length;
        if (len == 0)
        {
            isAnimating = false;
            yield break;
        }

        int prevIndex = (index - 1 + len) % len;
        int cloneIndex = (index - 2 + len) % len;

        string cloneFolder = beatmapFolders[cloneIndex];

        Sprite cloneSprite = null;
        string cloneCoverPath = previewManager.FindCoverImage(cloneFolder);
        if (!string.IsNullOrEmpty(cloneCoverPath))
            cloneSprite = LoadSprite(cloneCoverPath);

        yield return StartCoroutine(carousel.AnimatePrev(cloneSprite));

        index = prevIndex;
        ShowBeatmap(index);

        isAnimating = false;
    }

    void SetDifficulty(Difficulty diff, Button btn)
    {
        SelectedDifficulty = diff;

        switch (diff)
        {
            case Difficulty.Easy: SelectedPhantomChance = 0f; break;
            case Difficulty.Medium: SelectedPhantomChance = 0.38f; break;
            case Difficulty.Hard: SelectedPhantomChance = 1f; break;
        }

        Debug.Log($"[Difficulty] {diff} | phantomChance = {SelectedPhantomChance}");
        SetDifficultyButtonVisual(btn);
    }

    private string GetOsuFile(string folder)
    {
        string[] files = Directory.GetFiles(folder, "*.osu");
        if (files.Length == 0) return null;
        return files[0];
    }

    void LoadImageToUI(string path, Image target)
    {
        if (string.IsNullOrEmpty(path)) return;

        byte[] bytes = File.ReadAllBytes(path);
        Texture2D tex = new Texture2D(2, 2);

        if (!tex.LoadImage(bytes))
        {
            Debug.LogError("[BeatmapSelect] Gagal load cover: " + path);
            return;
        }

        Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        target.sprite = sp;
    }

    public void PlayGame()
    {
        StartCoroutine(PlayGameSequence());
    }

    IEnumerator PlayGameSequence()
    {
        if (sfxSource != null && playButtonSound != null)
        {
            sfxSource.PlayOneShot(playButtonSound);
            yield return new WaitForSeconds(0.4f);
        }

        if (beatmapFolders == null || beatmapFolders.Length == 0) yield break;

        string folder = beatmapFolders[index];
        string osuFile = GetOsuFile(folder);
        if (string.IsNullOrEmpty(osuFile)) yield break;

        string beatmapName = Path.GetFileName(folder);

        GameSession.SelectedOsuFile = osuFile;
        GameSession.SelectedBeatmapPath = folder;
        GameSession.SelectedBeatmapName = beatmapName;
        GameSession.SelectedPhantomChance = SelectedPhantomChance;
        GameSession.SelectedDifficulty = (GameSession.BeatmapDifficulty)SelectedDifficulty;

        PlayerPrefs.SetString("SelectedOsuFile", osuFile);
        PlayerPrefs.SetString("SelectedBeatmapPath", folder);
        PlayerPrefs.SetString("SelectedBeatmapName", GameSession.SelectedBeatmapName);
        PlayerPrefs.Save();

        Debug.Log($"[PlayGame] Playing: {osuFile}");

        var loader = FindFirstObjectByType<LoadingManager>();
        if (loader != null)
        {
            loader.LoadLevel("Gameplay");
        }
        else
        {
            SceneManager.LoadScene("Gameplay");
        }
    }
}