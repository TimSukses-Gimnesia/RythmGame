using UnityEngine;
using TMPro;
using System.Collections; 

public class ScoreDisplay : MonoBehaviour
{
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI comboText;

    [Header("Combo Pop")]
    public float popScale = 1.5f;     
    public float popDuration = 0.1f;  

    private int lastCombo = 0;
    private Vector3 originalComboScale;
    private Coroutine popCoroutine;

    void Start()
    {
     
        if (comboText != null)
        {
            originalComboScale = comboText.transform.localScale;
            comboText.enabled = false; 
        }
        lastCombo = 0;
    }

    void Update()
    {
        if (scoreText != null)
        {
            scoreText.text = HitJudgement.score.ToString("D0");
        }

        if (comboText != null)
        {
   
            if (HitJudgement.combo != lastCombo)
            {
                if (HitJudgement.combo > lastCombo && HitJudgement.combo > 1)
                {
                    comboText.text = "COMBO\n" + HitJudgement.combo.ToString();
                    comboText.enabled = true;


                    if (popCoroutine != null)
                    {
                        StopCoroutine(popCoroutine);
                    }
                    popCoroutine = StartCoroutine(PopComboText());
                }

                else if (HitJudgement.combo == 0)
                {
                    comboText.enabled = false;
                }

            
                lastCombo = HitJudgement.combo;
            }
        }
    }

    
    private IEnumerator PopComboText()
    {
     
        comboText.transform.localScale = originalComboScale * popScale;

        yield return new WaitForSeconds(popDuration);

        comboText.transform.localScale = originalComboScale;
        popCoroutine = null;
    }
}