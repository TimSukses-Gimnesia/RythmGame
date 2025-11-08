using UnityEngine;
using TMPro;
using System.Collections;

public class HitPopup : MonoBehaviour
{
    public TextMeshProUGUI textMesh;
    public float moveSpeed = 50f;     
    public float fadeOutTime = 0.5f;  

    private Color originalColor;
    private Vector3 originalPosition; 

    void Awake()
    {
        if (textMesh == null)
        {
            textMesh = GetComponent<TextMeshProUGUI>();
        }

        originalPosition = transform.localPosition; 
        originalColor = textMesh.color;
    }

 
    public void Setup(string text, Color color)
    {
        textMesh.text = text;
        textMesh.color = color;
    }

    void OnEnable()
    {
  
        transform.localPosition = originalPosition;
        textMesh.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f); 

        StartCoroutine(AnimatePopup());
    }

    private IEnumerator AnimatePopup()
    {
        float timer = 0f;
        Color startColor = textMesh.color; 

        while (timer < fadeOutTime)
        {
    
            transform.Translate(Vector3.up * moveSpeed * Time.deltaTime);

          
            float alpha = 1.0f - (timer / fadeOutTime);
            textMesh.color = new Color(startColor.r, startColor.g, startColor.b, alpha);

            timer += Time.deltaTime;
            yield return null;
        }

       
        gameObject.SetActive(false);
    }
}

