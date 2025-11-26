using UnityEngine;
using System.Collections;

public class NoteHitEffect : MonoBehaviour
{
    [Header("Settings")]
    public float animDuration = 0.2f;

    [Header("Visual Differences")]
    public Color perfectColor = new Color(0f, 1f, 1f); // Cyan/Emas
    public Color goodColor = new Color(0.6f, 1f, 0.6f); // Hijau Pucat

    [Header("Scale Settings")]
    public float perfectEndScale = 1.6f; // Membesar (Explosive)
    public float goodEndScale = 0.5f;    // Mengecil (Implosive/Kempis)

    [Header("VFX References")]
    [Tooltip("Prefab Particle System untuk ledakan (Opsional)")]
    public GameObject hitParticlePrefab;

    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    // Fungsi ini dipanggil oleh HitJudgement
    public void PlayEffectAndDestroy(string judgement)
    {
        Color targetColor;
        float targetScaleMult;

        // 1. Tentukan Gaya Animasi berdasarkan Judgement
        if (judgement == "Perfect")
        {
            targetColor = perfectColor;
            targetScaleMult = perfectEndScale; // Membesar
        }
        else // "Good"
        {
            targetColor = goodColor;
            targetScaleMult = goodEndScale;   // Mengecil
        }

        // 2. Spawn Particle (Hanya jika ada prefab)
        // Tips: Anda bisa membedakan partikel juga jika mau, tapi satu partikel beda warna sudah cukup oke
        if (hitParticlePrefab != null)
        {
            GameObject vfx = Instantiate(hitParticlePrefab, transform.position, Quaternion.identity);

            // Ubah warna partikel
            var main = vfx.GetComponent<ParticleSystem>().main;
            main.startColor = targetColor;

            // Hancurkan partikel setelah selesai
            Destroy(vfx, 1f);
        }

        // 3. Matikan komponen Note agar diam
        if (GetComponent<Note>()) GetComponent<Note>().enabled = false;
        if (GetComponent<Collider2D>()) GetComponent<Collider2D>().enabled = false;

        // 4. Mulai Animasi pada Sprite Note
        StartCoroutine(HitAnimationRoutine(targetColor, targetScaleMult));
    }

    IEnumerator HitAnimationRoutine(Color flashColor, float endScaleMult)
    {
        Vector3 startScale = transform.localScale;
        Vector3 endScale = startScale * endScaleMult;

        // Flash putih sedikit di awal (Impact Frame)
        if (sr != null) sr.color = Color.white;
        yield return new WaitForSeconds(0.05f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / animDuration;

            // Animasi Scale (Membesar untuk Perfect, Mengecil untuk Good)
            // Menggunakan SmoothStep agar gerakan lebih luwes
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            transform.localScale = Vector3.Lerp(startScale, endScale, smoothT);

            // Animasi Warna & Alpha (Fade Out ke Warna Judgement)
            if (sr != null)
            {
                // Fade dari Putih -> Warna Target -> Transparan
                Color c = Color.Lerp(flashColor, new Color(flashColor.r, flashColor.g, flashColor.b, 0f), t);
                sr.color = c;
            }

            yield return null;
        }

        Destroy(gameObject);
    }
}