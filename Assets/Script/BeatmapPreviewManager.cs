using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.Audio;

[RequireComponent(typeof(AudioSource))]
public class BeatmapPreviewManager : MonoBehaviour
{
    // ... (Class OsuMetadata tetap sama) ...
    class OsuMetadata
    {
        public string title;
        public string artist;
        public string audioFile;
        public float previewTime;
    }

    // ... (Fungsi ReadMetadataFromOsu tetap sama) ...
    OsuMetadata ReadMetadataFromOsu(string folder)
    {
        string[] osuFiles = Directory.GetFiles(folder, "*.osu");
        if (osuFiles.Length == 0) return null;

        string osuPath = osuFiles[0];
        OsuMetadata data = new OsuMetadata();

        foreach (string line in File.ReadLines(osuPath))
        {
            if (line.StartsWith("TitleUnicode:")) data.title = line.Substring("TitleUnicode:".Length).Trim();
            else if (line.StartsWith("Title:") && string.IsNullOrEmpty(data.title)) data.title = line.Substring("Title:".Length).Trim();
            else if (line.StartsWith("ArtistUnicode:")) data.artist = line.Substring("ArtistUnicode:".Length).Trim();
            else if (line.StartsWith("Artist:") && string.IsNullOrEmpty(data.artist)) data.artist = line.Substring("Artist:".Length).Trim();
            else if (line.StartsWith("AudioFilename:")) data.audioFile = line.Substring("AudioFilename:".Length).Trim();
            else if (line.StartsWith("PreviewTime:")) { if (float.TryParse(line.Substring("PreviewTime:".Length).Trim(), out float t)) data.previewTime = t / 1000f; }
        }
        return data;
    }

    [Header("UI")]
    public Image coverImage;
    public Image backgroundImage; // BG
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI artistText;

    [Header("Animation Settings")] // 🔥 BARU: Pengaturan Transisi
    public float fadeDuration = 0.25f; // Kecepatan transisi (detik)

    [Header("Audio Mixer")]
    public AudioMixerGroup musicGroup;

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;
    private Coroutine transitionCoroutine; // Untuk visual

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
        if (musicGroup != null) audioSource.outputAudioMixerGroup = musicGroup;
    }

    // 🔥 FUNGSI UTAMA YANG DIUBAH
    public void ShowPreview(string beatmapFolder)
    {
        // Hentikan transisi lama jika user spam tombol, lalu mulai yang baru
        if (transitionCoroutine != null) StopCoroutine(transitionCoroutine);
        transitionCoroutine = StartCoroutine(TransitionVisuals(beatmapFolder));
    }

    // 🔥 LOGIKA TRANSISI (Fade Out -> Load -> Update -> Fade In)
    IEnumerator TransitionVisuals(string folder)
    {
        // 1. FADE OUT (Menghilang)
        yield return StartCoroutine(FadeUI(0f));

        // 2. LOAD DATA (Saat layar invisible)
        string coverPath = FindCoverImage(folder);
        Sprite newSprite = null;

        // Load Gambar (Texture)
        if (!string.IsNullOrEmpty(coverPath))
        {
            string url = "file:///" + coverPath.Replace("\\", "/");
            using (UnityWebRequest www = UnityWebRequestTexture.GetTexture(url))
            {
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success)
                {
                    Texture2D tex = DownloadHandlerTexture.GetContent(www);
                    newSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                }
            }
        }

        // Load Metadata Teks
        OsuMetadata meta = ReadMetadataFromOsu(folder);
        string newTitle = "Unknown";
        string newArtist = "Unknown";

        if (meta != null)
        {
            newTitle = string.IsNullOrEmpty(meta.title) ? Path.GetFileName(folder) : meta.title;
            newArtist = string.IsNullOrEmpty(meta.artist) ? "Unknown" : meta.artist;
        }
        else
        {
            newTitle = Path.GetFileName(folder);
        }

        // 3. UPDATE UI (Mengganti isi konten)
        if (newSprite != null)
        {
            coverImage.sprite = newSprite;
            if (backgroundImage != null) backgroundImage.sprite = newSprite;
        }
        // Jika tidak ada gambar, bisa set ke null atau default sprite
        // else { coverImage.sprite = defaultSprite; } 

        titleText.text = newTitle;
        artistText.text = newArtist;

        // 4. FADE IN (Muncul Kembali)
        yield return StartCoroutine(FadeUI(1f));
    }

    // Helper untuk mengatur Alpha semua elemen UI
    IEnumerator FadeUI(float targetAlpha)
    {
        float startAlpha = titleText.alpha; // Ambil alpha saat ini (asumsi semua sinkron)
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime / fadeDuration;
            float currentAlpha = Mathf.Lerp(startAlpha, targetAlpha, t);

            // Set Alpha Teks
            titleText.alpha = currentAlpha;
            artistText.alpha = currentAlpha;

        

            // Set Alpha Background (Opsional: biasanya BG tidak perlu sampai 0, atau bisa dibuat lebih redup)
            if (backgroundImage != null)
            {
                Color c = backgroundImage.color;
                // Kita lerp ke targetAlpha, tapi mungkin max alpha BG cuma 0.5 (sesuaikan selera)
                // Di sini saya buat sama (0 ke 1) agar sinkron
                c.a = currentAlpha;
                backgroundImage.color = c;
            }

            yield return null;
        }
    }

    // ... (Fungsi PlayPreview, FindCoverImage, FindAudioFile, dll tetap sama) ...
    public void PlayPreview(string beatmapFolder)
    {
        string audioPath = FindAudioFile(beatmapFolder);
        if (string.IsNullOrEmpty(audioPath)) return;
        StartCoroutine(LoadAndPlayAudio(audioPath));
    }

    public string FindCoverImage(string folder)
    {
        string[] imgs = Directory.GetFiles(folder, "*.jpg");
        if (imgs.Length == 0) imgs = Directory.GetFiles(folder, "*.png");
        return imgs.Length > 0 ? imgs[0] : null;
    }

    string FindAudioFile(string folder)
    {
        string[] audios = Directory.GetFiles(folder, "*.mp3");
        if (audios.Length == 0) audios = Directory.GetFiles(folder, "*.ogg");
        return audios.Length > 0 ? audios[0] : null;
    }

    IEnumerator LoadAndPlayAudio(string path)
    {
        string url = "file:///" + path.Replace("\\", "/");
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.UNKNOWN))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) yield break;
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeInAudio(clip));
        }
    }

    IEnumerator FadeInAudio(AudioClip clip)
    {
        if (audioSource.isPlaying)
        {
            for (float v = 1f; v > 0f; v -= Time.deltaTime * 2f)
            {
                audioSource.volume = v;
                yield return null;
            }
            audioSource.Stop();
        }
        audioSource.clip = clip;
        audioSource.volume = 0f;
        audioSource.Play();
        for (float v = 0f; v < 1f; v += Time.deltaTime * 2f)
        {
            audioSource.volume = v;
            yield return null;
        }
    }
}