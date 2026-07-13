using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generic fullscreen ultimate flash effect for any fighter.
/// Spawns a Canvas overlay with a dramatic zoom+flash, waits for the full animation to finish, then calls onComplete.
/// Shared by all fighter controllers (Boa Hancock, Gray, Ichigo, etc.)
/// </summary>
public class UltimateFlashEffect : MonoBehaviour
{
    // --- Static constants for timing ---
    private const float FADE_IN_DURATION = 0.1f;
    private const float HOLD_DURATION = 0.35f;
    private const float FADE_OUT_DURATION = 0.25f;

    private static GameObject activeCanvas;

    /// <summary>
    /// Shows a fullscreen flash of the given sprite, then calls onComplete AFTER the entire flash ends.
    /// </summary>
    /// <param name="runner">The MonoBehaviour that will run the coroutine</param>
    /// <param name="sprite">The portrait image to display</param>
    /// <param name="imageSizeFactor">Size relative to screen height (default 0.6 = 60% screen height)</param>
    /// <param name="imageOffset">Pixel offset from center (e.g. new Vector2(0, 100f) = 100px up)</param>
    /// <param name="onComplete">Called after flash fully ends</param>
    public static void Show(MonoBehaviour runner, Sprite sprite, float imageSizeFactor, Vector2 imageOffset, System.Action onComplete)
    {
        if (activeCanvas != null)
        {
            Object.Destroy(activeCanvas);
        }
        runner.StartCoroutine(RunFlash(sprite, imageSizeFactor, imageOffset, onComplete));
    }

    // Overload with defaults for backward compatibility
    public static void Show(MonoBehaviour runner, Sprite sprite, System.Action onComplete)
    {
        Show(runner, sprite, 0.6f, new Vector2(0f, 100f), onComplete);
    }

    private static IEnumerator RunFlash(Sprite sprite, float sizeFactor, Vector2 offset, System.Action onComplete)
    {
        // === Create Fullscreen Canvas ===
        activeCanvas = new GameObject("UltimateFlashCanvas");
        var canvas = activeCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        activeCanvas.AddComponent<CanvasScaler>();
        activeCanvas.AddComponent<GraphicRaycaster>();

        // === Black background ===
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(activeCanvas.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = Color.clear;

        // === White flash overlay ===
        GameObject flashObj = new GameObject("RadialFlash");
        flashObj.transform.SetParent(activeCanvas.transform, false);
        var flashRect = flashObj.AddComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        var flashImg = flashObj.AddComponent<Image>();
        flashImg.color = Color.clear;

        // === Main portrait image ===
        GameObject imgObj = new GameObject("UltimateImage");
        imgObj.transform.SetParent(activeCanvas.transform, false);
        var imgRect = imgObj.AddComponent<RectTransform>();
        imgRect.anchorMin = new Vector2(0.5f, 0.5f);
        imgRect.anchorMax = new Vector2(0.5f, 0.5f);
        imgRect.pivot = new Vector2(0.5f, 0.5f);
        float screenH = Screen.height;
        imgRect.sizeDelta = new Vector2(screenH * sizeFactor, screenH * sizeFactor);
        imgRect.anchoredPosition = offset;
        var img = imgObj.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.color = Color.clear;

        // === Phase 1: Fast fade in + zoom snap ===
        float elapsed = 0f;
        while (elapsed < FADE_IN_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / FADE_IN_DURATION);
            float eased = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic

            img.color = new Color(1f, 1f, 1f, eased);
            bgImage.color = new Color(0f, 0f, 0f, eased * 0.8f);
            float scale = Mathf.Lerp(1.3f, 1.0f, eased);
            imgRect.localScale = new Vector3(scale, scale, 1f);
            flashImg.color = new Color(1f, 1f, 1f, (1f - t) * 0.7f);
            yield return null;
        }
        img.color = Color.white;
        bgImage.color = new Color(0f, 0f, 0f, 0.8f);
        flashImg.color = Color.clear;
        imgRect.localScale = Vector3.one;

        // === Phase 2: Hold ===
        yield return new WaitForSeconds(HOLD_DURATION);

        // === Phase 3: Fade out ===
        elapsed = 0f;
        Color imgStart = img.color;
        Color bgStart = bgImage.color;
        while (elapsed < FADE_OUT_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / FADE_OUT_DURATION);
            img.color = Color.Lerp(imgStart, Color.clear, t);
            bgImage.color = Color.Lerp(bgStart, Color.clear, t);
            yield return null;
        }

        Object.Destroy(activeCanvas);
        activeCanvas = null;

        // === Trigger animation AFTER flash is fully done ===
        onComplete?.Invoke();
    }
}
