using UnityEngine;
using TMPro;
using System.Collections;

public class RetroTextEffect : MonoBehaviour
{
    [Header("Components")]
    public TMP_Text textComponent;

    [Header("Settings")]
    [TextArea] public string fullText = "LOADING...";
    public float typingSpeed = 0.15f; 
    public float fadeDuration = 1.0f; 

    void OnEnable()
    {
        if (textComponent == null) textComponent = GetComponent<TMP_Text>();
        StartCoroutine(AnimateTextLoop());
    }

    IEnumerator AnimateTextLoop()
    {
        while (true)
        {
            
            textComponent.text = "";
           
      
            foreach (char letter in fullText.ToCharArray())
            {
                textComponent.text += letter;
                yield return new WaitForSecondsRealtime(typingSpeed);
            }

        
            yield return new WaitForSecondsRealtime(0.5f);

        
            float timer = 0;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
             
                yield return null;
            }

            timer = 0;
            while (timer < fadeDuration)
            {
                timer += Time.unscaledDeltaTime;
              
                yield return null;
            }

            
            yield return new WaitForSecondsRealtime(0.5f);

        
        }
    }
}