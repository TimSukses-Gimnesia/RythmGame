using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class CarouselAnimator : MonoBehaviour
{
    public RectTransform prevCover;
    public RectTransform mainCover;
    public RectTransform nextCover;

    public float animTime = 0.30f;
    public float offsetY = 450f;

    private Vector2 posPrev;
    private Vector2 posMain;
    private Vector2 posNext;

    void Start()
    {
        posPrev = prevCover.anchoredPosition; // (0, +450)
        posMain = mainCover.anchoredPosition; // (-150, 0)
        posNext = nextCover.anchoredPosition; // (0, -450)
    }

    // ==========================================================
    // NEXT  (scroll downward)
    // ==========================================================
    public IEnumerator AnimateNext(Sprite newSpriteForNext)
    {
        // clone untuk next yang baru
        GameObject clone = Instantiate(prevCover.gameObject, prevCover.parent);
        clone.transform.SetAsLastSibling();

        RectTransform cloneRT = clone.GetComponent<RectTransform>();
        Image cloneIMG = clone.GetComponent<Image>();

        cloneIMG.sprite = newSpriteForNext;

        // CLONE muncul dari bawah
        cloneRT.anchoredPosition = new Vector2(0, posNext.y - offsetY);

        float t = 0;

        Vector2 prevStart = posPrev;
        Vector2 mainStart = posMain;
        Vector2 nextStart = posNext;
        Vector2 cloneStart = cloneRT.anchoredPosition;

        Vector2 prevEnd = new Vector2(0, posPrev.y + offsetY); // keluar atas
        Vector2 mainEnd = posPrev;                             // main → prev
        Vector2 nextEnd = posMain;                             // next → main
        Vector2 cloneEnd = posNext;                            // clone → next

        while (t < animTime)
        {
            t += Time.deltaTime;
            float k = t / animTime;

            prevCover.anchoredPosition = Vector2.Lerp(prevStart, prevEnd, k);
            mainCover.anchoredPosition = Vector2.Lerp(mainStart, mainEnd, k);
            nextCover.anchoredPosition = Vector2.Lerp(nextStart, nextEnd, k);
            cloneRT.anchoredPosition = Vector2.Lerp(cloneStart, cloneEnd, k);

            yield return null;
        }

        // assign sprite baru
        nextCover.GetComponent<Image>().sprite = newSpriteForNext;

        // cleanup
        prevCover.anchoredPosition = posPrev;
        mainCover.anchoredPosition = posMain;
        nextCover.anchoredPosition = posNext;

        Destroy(clone);
    }

    // ==========================================================
    // PREV  (scroll upward)
    // ==========================================================
    public IEnumerator AnimatePrev(Sprite newSpriteForPrev)
    {
        // clone untuk prev yang baru
        GameObject clone = Instantiate(prevCover.gameObject, prevCover.parent);
        clone.transform.SetAsLastSibling();

        RectTransform cloneRT = clone.GetComponent<RectTransform>();
        Image cloneIMG = clone.GetComponent<Image>();

        cloneIMG.sprite = newSpriteForPrev;

        // clone muncul dari atas
        cloneRT.anchoredPosition = new Vector2(0, posPrev.y + offsetY);

        float t = 0;

        Vector2 prevStart = posPrev;
        Vector2 mainStart = posMain;
        Vector2 nextStart = posNext;
        Vector2 cloneStart = cloneRT.anchoredPosition;

        Vector2 prevEnd = posMain;                             // prev → main
        Vector2 mainEnd = posNext;                             // main → next
        Vector2 nextEnd = new Vector2(0, posNext.y - offsetY); // next keluar bawah
        Vector2 cloneEnd = posPrev;                            // clone → prev

        while (t < animTime)
        {
            t += Time.deltaTime;
            float k = t / animTime;

            prevCover.anchoredPosition = Vector2.Lerp(prevStart, prevEnd, k);
            mainCover.anchoredPosition = Vector2.Lerp(mainStart, mainEnd, k);
            nextCover.anchoredPosition = Vector2.Lerp(nextStart, nextEnd, k);
            cloneRT.anchoredPosition = Vector2.Lerp(cloneStart, cloneEnd, k);

            yield return null;
        }

        prevCover.GetComponent<Image>().sprite = newSpriteForPrev;

        prevCover.anchoredPosition = posPrev;
        mainCover.anchoredPosition = posMain;
        nextCover.anchoredPosition = posNext;

        Destroy(clone);
    }
}
