using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.IO;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.EventSystems;

public class BeatmapSelectManager : MonoBehaviour
{
    [Header("Preview System")]
    public BeatmapPreviewManager previewManager;

    [Header("UI")]
    public Transform listParent;
    public GameObject beatmapSetPrefab;
    public GameObject difficultyButtonPrefab;

    [Header("Visual")]
    public Color normalColor = new Color(0.8f, 0.8f, 0.8f);
    public Color selectedColor = new Color(0.4f, 0.6f, 1f);

    private string beatmapFolder;
    private Button lastSelectedButton = null;
    private GameObject lastOpenedDiffContainer = null;

    void Start()
    {
#if UNITY_EDITOR
        beatmapFolder = Path.Combine(Application.dataPath, "Beatmaps");
#else
        beatmapFolder = Path.Combine(Application.persistentDataPath, "Beatmaps");
#endif

        if (!Directory.Exists(beatmapFolder))
        {
            Debug.LogWarning("Beatmap folder tidak ditemukan: " + beatmapFolder);
            return;
        }

        PopulateBeatmapSets();
    }

    void PopulateBeatmapSets()
    {
        foreach (Transform child in listParent)
            Destroy(child.gameObject);

        var layout = listParent.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            layout = listParent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childAlignment = TextAnchor.UpperCenter;
        }

        var fitter = listParent.GetComponent<ContentSizeFitter>();
        if (fitter == null)
        {
            fitter = listParent.gameObject.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        string[] folders = Directory.GetDirectories(beatmapFolder);

        foreach (string folder in folders)
        {
            string beatmapName = Path.GetFileName(folder);

            GameObject beatmapContainer = new GameObject(beatmapName + "_Container", typeof(RectTransform));
            beatmapContainer.transform.SetParent(listParent, false);

            var containerLayout = beatmapContainer.AddComponent<VerticalLayoutGroup>();
            containerLayout.spacing = 5f;
            containerLayout.childForceExpandHeight = false;
            containerLayout.childForceExpandWidth = true;
            containerLayout.childAlignment = TextAnchor.UpperCenter;

            var containerFitter = beatmapContainer.AddComponent<ContentSizeFitter>();
            containerFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            // --- Beatmap Button ---
            GameObject setBtn = Instantiate(beatmapSetPrefab, beatmapContainer.transform);
            setBtn.GetComponentInChildren<TextMeshProUGUI>().text = beatmapName;

            Image setImg = setBtn.GetComponent<Image>();
            if (setImg == null) setImg = setBtn.AddComponent<Image>();
            setImg.color = normalColor;

            LayoutElement setLayout = setBtn.GetComponent<LayoutElement>();
            if (setLayout == null) setLayout = setBtn.AddComponent<LayoutElement>();
            setLayout.preferredHeight = 100f;

            // Hover effect
            AddHoverAnimation(setBtn);

            // --- Difficulty Container ---
            GameObject diffContainer = new GameObject("DiffContainer", typeof(RectTransform));
            diffContainer.transform.SetParent(beatmapContainer.transform, false);
            diffContainer.SetActive(false);

            var diffLayout = diffContainer.AddComponent<VerticalLayoutGroup>();
            diffLayout.spacing = 6f;
            diffLayout.padding = new RectOffset(40, 0, 5, 5);
            diffLayout.childForceExpandWidth = true;
            diffLayout.childForceExpandHeight = false;

            var diffFitter = diffContainer.AddComponent<ContentSizeFitter>();
            diffFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Button btn = setBtn.GetComponent<Button>();
            btn.onClick.AddListener(() =>
            {
                bool newState = !diffContainer.activeSelf;

                if (lastOpenedDiffContainer != null && lastOpenedDiffContainer != diffContainer)
                {
                    StartCoroutine(FadeOutContainer(lastOpenedDiffContainer));
                    lastOpenedDiffContainer = null;
                }

                if (newState)
                {
                    diffContainer.SetActive(true);
                    StartCoroutine(FadeInContainer(diffContainer));

                    if (diffContainer.transform.childCount == 0)
                        CreateDifficultyButtons(folder, diffContainer.transform);

                    // 🎨 PANGGIL PREVIEW PANEL
                    if (previewManager != null)
                    {
                        previewManager.ShowPreview(folder);
                        previewManager.PlayPreview(folder);
                    }
                        
                }
                else
                {
                    StartCoroutine(FadeOutContainer(diffContainer));
                }

                lastOpenedDiffContainer = newState ? diffContainer : null;
                HighlightButton(btn, newState);
            });
        }
    }

    void HighlightButton(Button btn, bool isActive)
    {
        if (lastSelectedButton != null && lastSelectedButton != btn)
        {
            Image prevImg = lastSelectedButton.GetComponent<Image>();
            if (prevImg != null)
                prevImg.color = normalColor;
        }

        Image img = btn.GetComponent<Image>();
        if (img != null)
            img.color = isActive ? selectedColor : normalColor;

        lastSelectedButton = isActive ? btn : null;
    }

    // --- Fade Animations for expand/collapse ---
    IEnumerator FadeInContainer(GameObject container)
    {
        VerticalLayoutGroup layout = container.GetComponentInParent<VerticalLayoutGroup>();
        if (layout) layout.enabled = false; // ⛔ hentikan reflow sementara

        CanvasGroup cg = container.GetComponent<CanvasGroup>();
        if (cg == null) cg = container.AddComponent<CanvasGroup>();
        cg.alpha = 0f;
        container.transform.localScale = Vector3.one * 0.95f;

        float t = 0f;
        while (t < 0.25f)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(0, 1, t / 0.25f);
            container.transform.localScale = Vector3.Lerp(Vector3.one * 0.95f, Vector3.one, t / 0.25f);
            yield return null;
        }

        cg.alpha = 1f;
        container.transform.localScale = Vector3.one;

        // ✅ Reaktifkan layout setelah animasi selesai
        if (layout)
        {
            layout.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(layout.GetComponent<RectTransform>());
        }
    }


    IEnumerator FadeOutContainer(GameObject container)
    {
        VerticalLayoutGroup layout = container.GetComponentInParent<VerticalLayoutGroup>();
        if (layout) layout.enabled = false; // ⛔ hentikan layout sementara
    
        CanvasGroup cg = container.GetComponent<CanvasGroup>();
        if (cg == null) cg = container.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        container.transform.localScale = Vector3.one;
    
        float t = 0f;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(1, 0, t / 0.2f);
            container.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.95f, t / 0.2f);
            yield return null;
        }
    
        cg.alpha = 0f;
        container.SetActive(false);
    
        // ✅ Reaktifkan layout sesudah fade out selesai
        if (layout)
        {
            layout.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate(layout.GetComponent<RectTransform>());
        }
    }


    // --- Difficulty Buttons ---
    void CreateDifficultyButtons(string folderPath, Transform parent)
    {
        string[] osuFiles = Directory.GetFiles(folderPath, "*.osu");

        foreach (string osuPath in osuFiles)
        {
            string diffName = Path.GetFileNameWithoutExtension(osuPath);
            GameObject diffBtn = Instantiate(difficultyButtonPrefab, parent);
            diffBtn.GetComponentInChildren<TextMeshProUGUI>().text = diffName;

            LayoutElement layoutElement = diffBtn.GetComponent<LayoutElement>();
            if (layoutElement == null) layoutElement = diffBtn.AddComponent<LayoutElement>();
            layoutElement.preferredHeight = 90f;
            layoutElement.preferredWidth = 700f;

            AddHoverAnimation(diffBtn);

            diffBtn.GetComponent<Button>().onClick.AddListener(() =>
            {
                Debug.Log("Selected: " + diffName);
                PlayerPrefs.SetString("SelectedOsuFile", osuPath);
                PlayerPrefs.SetString("SelectedBeatmapPath", folderPath);

                GameSession.SelectedOsuFile = osuPath;
                GameSession.SelectedBeatmapPath = folderPath;
                GameSession.SelectedBeatmapName = diffName;

                SceneManager.LoadScene("Gameplay");
            });
        }
    }

    // --- Hover Animation ---
    void AddHoverAnimation(GameObject buttonObj)
    {
        EventTrigger trigger = buttonObj.AddComponent<EventTrigger>();

        var enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener((_) => StartCoroutine(HoverScale(buttonObj.transform, 1.05f)));
        trigger.triggers.Add(enter);

        var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener((_) => StartCoroutine(HoverScale(buttonObj.transform, 1.0f)));
        trigger.triggers.Add(exit);
    }

    IEnumerator HoverScale(Transform t, float target)
    {
        Vector3 start = t.localScale;
        Vector3 end = Vector3.one * target;
        float elapsed = 0f;

        while (elapsed < 0.15f)
        {
            elapsed += Time.deltaTime;
            t.localScale = Vector3.Lerp(start, end, elapsed / 0.15f);
            yield return null;
        }
        t.localScale = end;
    }
}
