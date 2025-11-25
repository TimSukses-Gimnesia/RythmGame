using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Collections;
using UnityEngine.SceneManagement;
using TMPro;

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

    private string beatmapFolder;
    private string[] beatmapFolders;
    private int index = 0;

    private bool isAnimating = false;
    private Button selectedDifficultyButton;


    // Difficulty for gameplay
    public enum Difficulty { Easy, Medium, Hard }
    public static Difficulty SelectedDifficulty;
    public static float SelectedPhantomChance = 0f;

    void Start()
    {
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

        // Kembalikan button sebelumnya
        if (selectedVisual != null)
        {
            Image oldImg = selectedVisual.button.GetComponent<Image>();
            oldImg.sprite = selectedVisual.defaultSprite;

            // warna kembali normal
            var oldColor = selectedVisual.button.colors;
            oldColor.normalColor = Color.white;
            selectedVisual.button.colors = oldColor;
        }

        // Set button baru
        selectedVisual = newVisual;

        Image img = newVisual.button.GetComponent<Image>();
        SpriteState state = newVisual.button.spriteState;

        // gunakan pressedSprite sebagai current sprite
        if (state.pressedSprite != null)
            img.sprite = state.pressedSprite;

        // warna normal mengikuti pressedColor
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
    // ==================================
    // === MENAMPILKAN BEATMAP BARU  ===
    // ==================================
    public void ShowBeatmap(int i)
    {
        index = (i + beatmapFolders.Length) % beatmapFolders.Length;

        string folder = beatmapFolders[index];

        // 1 OSU FILE PER BEATMAP (sesuai requirement baru)
        string osuFile = GetOsuFile(folder);
        if (osuFile == null)
        {
            Debug.LogError("[BeatmapSelect] Beatmap tidak punya file .osu: " + folder);
            return;
        }

        // Preview
        previewManager.ShowPreview(folder);
        previewManager.PlayPreview(folder);

        // Cover
        LoadImageToUI(previewManager.FindCoverImage(folder), coverMain);

        // Prev cover
        string prevFolder = beatmapFolders[(index - 1 + beatmapFolders.Length) % beatmapFolders.Length];
        LoadImageToUI(previewManager.FindCoverImage(prevFolder), prevCover);

        // Next cover
        string nextFolder = beatmapFolders[(index + 1) % beatmapFolders.Length];
        LoadImageToUI(previewManager.FindCoverImage(nextFolder), nextCover);
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


    // ==================================
    // === NEXT / PREV BUTTON LOGIC   ===
    // ==================================
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

        // current index = index
        // nextIndex = index + 1 (yang nanti menjadi main setelah animasi)
        int nextIndex = (index + 1) % len;

        // cloneIndex harus next dari next -> index + 2
        int cloneIndex = (index + 2) % len;

        // Jika hanya ada 1 atau 2 beatmap, fallback supaya tidak crash
        // - jika len == 1 : cloneIndex == index
        // - jika len == 2 : cloneIndex == (index + 0) or (index + 1) depending wrap,
        //   tapi hasil masih masuk akal (akan menampilkan yang tersedia)
        string nextFolder = beatmapFolders[nextIndex];
        string cloneFolder = beatmapFolders[cloneIndex];

        // Ambil sprite untuk animasi: sprite yang akan muncul sebagai clone (next-of-next)
        Sprite cloneSprite = null;
        string cloneCoverPath = previewManager.FindCoverImage(cloneFolder);
        if (!string.IsNullOrEmpty(cloneCoverPath))
            cloneSprite = LoadSprite(cloneCoverPath);

        // Untuk safety juga kita dapat menyiapkan sprite untuk next (yang nanti jadi main)
        Sprite nextSprite = null;
        string nextCoverPath = previewManager.FindCoverImage(nextFolder);
        if (!string.IsNullOrEmpty(nextCoverPath))
            nextSprite = LoadSprite(nextCoverPath);

        // Panggil animasi: berikan cloneSprite (yang merupakan next-of-next)
        // Pastikan AnimateNext di CarouselAnimator memakai parameter ini sebagai sprite clone yang muncul dari atas/bawah.
        yield return StartCoroutine(carousel.AnimateNext(cloneSprite));

        // Update index menjadi nextIndex (setelah animasi selesai)
        index = nextIndex;

        // Tampilkan info beatmap baru (termasuk men-set coverMain, prevCover, nextCover)
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

        // prevIndex = index - 1 (yang nanti jadi main)
        int prevIndex = (index - 1 + len) % len;

        // cloneIndex = prev - 1 (index - 2)
        int cloneIndex = (index - 2 + len) % len;

        string prevFolder = beatmapFolders[prevIndex];
        string cloneFolder = beatmapFolders[cloneIndex];

        // Ambil sprite clone (prev-of-prev)
        Sprite cloneSprite = null;
        string cloneCoverPath = previewManager.FindCoverImage(cloneFolder);
        if (!string.IsNullOrEmpty(cloneCoverPath))
            cloneSprite = LoadSprite(cloneCoverPath);

        // Safety: sprite for prev (yang nanti jadi main)
        Sprite prevSprite = null;
        string prevCoverPath = previewManager.FindCoverImage(prevFolder);
        if (!string.IsNullOrEmpty(prevCoverPath))
            prevSprite = LoadSprite(prevCoverPath);

        // Panggil animasi PREV: berikan cloneSprite (prev-of-prev)
        yield return StartCoroutine(carousel.AnimatePrev(cloneSprite));

        // Update index
        index = prevIndex;

        // Update UI
        ShowBeatmap(index);

        isAnimating = false;
    }


    // ==================================
    // === MANUAL DIFFICULTY SYSTEM   ===
    // ==================================
    void SetDifficulty(Difficulty diff, Button btn)
    {
        SelectedDifficulty = diff;

        switch (diff)
        {
            case Difficulty.Easy:
                SelectedPhantomChance = 0f;
                break;
            case Difficulty.Medium:
                SelectedPhantomChance = 0.38f;
                break;
            case Difficulty.Hard:
                SelectedPhantomChance = 1f;
                break;
        }

        Debug.Log($"[Difficulty] {diff} | phantomChance = {SelectedPhantomChance}");

        // Ubah visual
        SetDifficultyButtonVisual(btn);
    }


    // ==================================
    // === HELPER: GET SINGLE OSU FILE ===
    // ==================================
    private string GetOsuFile(string folder)
    {
        string[] files = Directory.GetFiles(folder, "*.osu");
        if (files.Length == 0) return null;
        return files[0];  // hanya ambil satu
    }

    // ==================================
    // === HELPER: LOAD COVER IMAGE   ===
    // ==================================
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
        if (beatmapFolders == null || beatmapFolders.Length == 0)
        {
            Debug.LogError("[PlayGame] No beatmaps available.");
            return;
        }

        // folder beatmap yang sedang dipilih
        string folder = beatmapFolders[index];

        // ambil file .osu (GetOsuFile sudah ada di script)
        string osuFile = GetOsuFile(folder);
        if (string.IsNullOrEmpty(osuFile))
        {
            Debug.LogError("[PlayGame] Tidak ada file .osu untuk beatmap ini!");
            return;
        }

        // set GameSession (sesuai struktur baru)
        GameSession.SelectedOsuFile = osuFile;
        GameSession.SelectedBeatmapPath = folder;
        GameSession.SelectedBeatmapName = Path.GetFileName(folder); // atau previewManager metadata
        GameSession.SelectedPhantomChance = SelectedPhantomChance;
        GameSession.SelectedDifficulty = (GameSession.BeatmapDifficulty) SelectedDifficulty;

        // simpan juga ke PlayerPrefs sebagai fallback (opsional)
        PlayerPrefs.SetString("SelectedOsuFile", osuFile);
        PlayerPrefs.SetString("SelectedBeatmapPath", folder);
        PlayerPrefs.SetString("SelectedBeatmapName", GameSession.SelectedBeatmapName);
        PlayerPrefs.Save();

        Debug.Log($"[PlayGame] Playing: {osuFile} | Difficulty: {SelectedDifficulty} | phantomChance: {SelectedPhantomChance}");

        // Cari LoadingManager di scene — jika ada, gunakan; jika tidak, langsung load scene
        var loader = FindFirstObjectByType<LoadingManager>();
        if (loader != null)
        {
            loader.LoadLevel("Gameplay"); // pastikan nama scene gameplay sesuai (cek build settings)
        }
        else
        {
            // fallback sederhana
            SceneManager.LoadScene("Gameplay");
        }
    }

}
