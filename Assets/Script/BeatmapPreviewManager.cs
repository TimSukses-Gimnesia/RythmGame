using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.IO;
using UnityEngine.Networking;

[RequireComponent(typeof(AudioSource))]
public class BeatmapPreviewManager : MonoBehaviour
{
    class OsuMetadata
    {
        public string title;
        public string artist;
        public string audioFile;
        public float previewTime;
    }

    OsuMetadata ReadMetadataFromOsu(string folder)
    {
        string[] osuFiles = Directory.GetFiles(folder, "*.osu");
        if (osuFiles.Length == 0) return null;

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
            else if (line.StartsWith("AudioFilename:"))
                data.audioFile = line.Substring("AudioFilename:".Length).Trim();
            else if (line.StartsWith("PreviewTime:"))
            {
                if (float.TryParse(line.Substring("PreviewTime:".Length).Trim(), out float t))
                    data.previewTime = t / 1000f;
            }
        }

        return data;
    }

    [Header("UI")]
    public Image coverImage;
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI artistText;

    private AudioSource audioSource;
    private Coroutine fadeCoroutine;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.loop = false;
        audioSource.playOnAwake = false;
    }

    public void ShowPreview(string beatmapFolder)
    {
        string coverPath = FindCoverImage(beatmapFolder);
        if (!string.IsNullOrEmpty(coverPath))
            StartCoroutine(LoadCover(coverPath));

        OsuMetadata meta = ReadMetadataFromOsu(beatmapFolder);

        if (meta != null)
        {
            titleText.text = string.IsNullOrEmpty(meta.title)
                             ? Path.GetFileName(beatmapFolder)
                             : meta.title;

            artistText.text = string.IsNullOrEmpty(meta.artist)
                             ? "Unknown"
                             : meta.artist;
        }
        else
        {
            titleText.text = Path.GetFileName(beatmapFolder);
            artistText.text = "Unknown";
        }
    }

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

    IEnumerator LoadCover(string path)
    {
        using (UnityWebRequest www = UnityWebRequestTexture.GetTexture("file:///" + path))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) yield break;

            Texture2D tex = DownloadHandlerTexture.GetContent(www);
            Sprite sp = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            coverImage.sprite = sp;
        }
    }

    IEnumerator LoadAndPlayAudio(string path)
    {
        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file:///" + path, AudioType.UNKNOWN))
        {
            yield return www.SendWebRequest();
            if (www.result != UnityWebRequest.Result.Success) yield break;

            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);

            if (fadeCoroutine != null)
                StopCoroutine(fadeCoroutine);

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
