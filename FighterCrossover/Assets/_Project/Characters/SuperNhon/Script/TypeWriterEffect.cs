using System.Collections;
using UnityEngine;
using TMPro;

public class TypewriterEffect : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float timeBetweenChars = 0.05f;

    [Header("UI References")]
    [SerializeField] private TMP_Text textComponent;

    private string fullText;

    private void Start()
    {
        // 1. Check if the reference is assigned
        if (textComponent != null)
        {
            // 2. Grab whatever text you already wrote in the Unity Inspector
            fullText = textComponent.text;

            // 3. Start the typing process
            StartCoroutine(TypeText());
        }
        else
        {
            Debug.LogError("TypewriterEffect: Text Component is missing in the Inspector!");
        }
    }

    private IEnumerator TypeText()
    {
        textComponent.text = ""; // Clear the text so it can start typing from scratch

        foreach (char c in fullText)
        {
            textComponent.text += c;
            yield return new WaitForSeconds(timeBetweenChars);
        }
    }
}