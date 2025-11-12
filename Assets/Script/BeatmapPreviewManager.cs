using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.IO;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class BeatmapPreviewManager : MonoBehaviour
{
    // =============================
// 🧩 Metadata Reader
// =============================

    class OsuMetadata
    {
        public string title;
        public string artist;
        public string mapper;
        public string audioFile;
        public float previewTime;
    }

    OsuMetadata ReadMetadataFromOsu(string folder)
    {
        string[] osuFiles = Directory.GetFiles(folder, "*.osu");
        if (osuFiles.Length == 0)
        {
            Debug.LogWarning("No .osu file found in " + folder);
            return null;
        }

        string osuPath = osuFiles[0];
        OsuMetadata data = new OsuMetadata();

        foreach (string line in File.ReadLines(osuPath))
        {
            if (line.StartsWith("TitleUnicode:"))
                data.title = line.Substring("TitleUnicode:".Length).Trim();
            else if (line.StartsWith("Title:") && string.IsNullOrEmpty(data.title))
                data.title = line.Substring("Title:".Length).Trim();
            else if (line.StartsWith("ArtistUnicode:"))
                data.artist = line.Substring("ArtistUnicode:".Length).Trim();
            else if (line.StartsWith("Artist:") && string.IsNullOrEmpty(data.artist))
                data.artist = line.Substring("Artist:".Length).Trim();
            else if (line.StartsWith("Creator:"))
                data.mapper = line.Substring("Creator:".Length).Trim();
            else if (line.StartsWith("AudioFilename:"))
                data.audioFile = line.Substring("AudioFilename:".Length).Trim();
            else if (line.StartsWith("PreviewTime:"))
            {
                if (float.TryParse(line.Substring("PreviewTime:".Length).Trim(), out float t))
                    data.previewTime = t / 1000f; // convert ms -> seconds
            }
        }

        return data;
    }

    [Header("UI References")]
    public Image backgroundImage;
    public Image coverImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI artistText;
    public TextMeshProUGUI mapperText;
    public TextMeshProUGUI lengthText;

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
    }

    /// <summary>
    /// Tampilkan preview beatmap (gambar + info lagu)
    /// </summary>
    public void ShowPreview(string beatmapFolder)
    {
        string coverPath = FindCoverImage(beatmapFolder);
        if (!string.IsNullOrEmpty(coverPath))
        {
            StartCoroutine(LoadAndShowCover(coverPath));
        }
    
        // 🧠 Coba baca metadata dari file .osu
        OsuMetadata meta = ReadMetadataFromOsu(beatmapFolder);
    
        if (meta != null)
        {
            titleText.text = string.IsNullOrEmpty(meta.title) ? Path.GetFileName(beatmapFolder) : meta.title;
            artistText.text = "Artist: " + (string.IsNullOrEmpty(meta.artist) ? "Unknown" : meta.artist);
            mapperText.text = "Mapper: " + (string.IsNullOrEmpty(meta.mapper) ? "Unknown" : meta.mapper);
            lengthText.text = "Audio: " + (string.IsNullOrEmpty(meta.audioFile) ? "?" : meta.audioFile);
        }
        else
        {
            titleText.text = Path.GetFileName(beatmapFolder);
            artistText.text = "Artist: Unknown";
            mapperText.text = "Mapper: Unknown";
            lengthText.text = "Length: ?";
        }
    }


    /// <summary>
    /// Mainkan preview lagu dari folder
    /// </summary>
    public void PlayPreview(string beatmapFolder)
    {
        string audioPath = FindAudioFile(beatmapFolder);
        if (string.IsNullOrEmpty(audioPath))
        {
            Debug.LogWarning("Audio preview not found in " + beatmapFolder);
            return;
        }

        StartCoroutine(LoadAndPlayAudio(audioPath));
    }

    // =============================
    // 🧩 File Finders
    // =============================

    string FindCoverImage(string folder)
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

    // =============================
    // 🎵 Loaders
    // =============================

    IEnumerator LoadAndPlayAudio(string path)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///" + path, AudioType.UNKNOWN))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Audio load failed: " + www.error);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeInAudio(clip));
        }
    }

    IEnumerator LoadAndShowCover(string path)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture("file:///" + path))
        {
            yield return www.SendWebRequest();

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogWarning("Cover image load failed: " + www.error);
                yield break;
            }

            Texture2D tex = DownloadHandlerTexture.GetContent(www);
            Sprite coverSprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.one * 0.5f);

            coverImage.sprite = coverSprite;
            backgroundImage.sprite = coverSprite;

            StartCoroutine(FadeImage(coverImage, 0f, 1f, 0.3f));
            StartCoroutine(FadeImage(backgroundImage, 0f, 0.6f, 0.5f));
        }
    }

    // =============================
    // ✨ Fade Logic
    // =============================

    IEnumerator FadeInAudio(AudioClip clip)
    {
        // fade out old audio
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

        // fade in new audio
        for (float v = 0f; v < 1f; v += Time.deltaTime * 2f)
        {
            audioSource.volume = v;
            yield return null;
        }
    }

    IEnumerator FadeImage(Image img, float from, float to, float duration)
    {
        Color c = img.color;
        for (float t = 0; t < duration; t += Time.deltaTime)
        {
            c.a = Mathf.Lerp(from, to, t / duration);
            img.color = c;
            yield return null;
        }
        c.a = to;
        img.color = c;
    }
}

