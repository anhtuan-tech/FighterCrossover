using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Generic high-quality fullscreen ultimate flash effect for any fighter.
/// Spawns a Canvas overlay with a cinematic diagonal masked banner, dynamic entry,
/// speed lines, neon aura backlighting, and motion blur.
/// </summary>
public class UltimateFlashEffect : MonoBehaviour
{
    // --- Timing Constants ---
    private const float FADE_IN_DURATION = 0.16f; 
    private const float HOLD_DURATION = 0.45f;    
    private const float FADE_OUT_DURATION = 0.18f;  

    private static GameObject activeCanvas;
    private static Sprite cachedGlowSprite;

    /// <summary>
    /// Shows a fullscreen flash of the given sprite with a custom theme color.
    /// </summary>
    public static void Show(MonoBehaviour runner, Sprite sprite, float imageSizeFactor, Vector2 imageOffset, Color themeColor, System.Action onComplete)
    {
        if (activeCanvas != null)
        {
            Object.Destroy(activeCanvas);
        }
        runner.StartCoroutine(RunFlash(sprite, imageSizeFactor, imageOffset, themeColor, onComplete));
    }

    // Overload for backward compatibility
    public static void Show(MonoBehaviour runner, Sprite sprite, float imageSizeFactor, Vector2 imageOffset, System.Action onComplete)
    {
        Show(runner, sprite, imageSizeFactor, imageOffset, new Color(1f, 0.85f, 0.4f), onComplete);
    }

    // Overload with defaults
    public static void Show(MonoBehaviour runner, Sprite sprite, System.Action onComplete)
    {
        Show(runner, sprite, 0.6f, new Vector2(0f, 100f), new Color(1f, 0.85f, 0.4f), onComplete);
    }

    private static Sprite GetGlowSprite()
    {
        if (cachedGlowSprite != null) return cachedGlowSprite;

        Texture2D tex = new Texture2D(32, 32);
        for (int y = 0; y < 32; y++)
        {
            for (int x = 0; x < 32; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(15.5f, 15.5f)) / 16f;
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = Mathf.Pow(alpha, 2f); // Smooth exponential decay
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.filterMode = FilterMode.Bilinear;
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.Apply();

        cachedGlowSprite = Sprite.Create(tex, new Rect(0, 0, 32, 32), new Vector2(0.5f, 0.5f));
        return cachedGlowSprite;
    }

    private static IEnumerator RunFlash(Sprite sprite, float sizeFactor, Vector2 offset, Color themeColor, System.Action onComplete)
    {
        float screenW = Screen.width;
        float screenH = Screen.height;

        // === Create Fullscreen Canvas ===
        activeCanvas = new GameObject("UltimateFlashCanvas");
        var canvas = activeCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        activeCanvas.AddComponent<CanvasScaler>();
        activeCanvas.AddComponent<GraphicRaycaster>();

        // === Black background dim ===
        GameObject bgObj = new GameObject("BackgroundDim");
        bgObj.transform.SetParent(activeCanvas.transform, false);
        var bgRect = bgObj.AddComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;
        var bgImage = bgObj.AddComponent<Image>();
        bgImage.color = Color.clear;

        // === Cinematic Diagonal Banner Container (rotated -10 degrees) ===
        // Size is wide enough to cover screen edges when rotated
        GameObject bannerObj = new GameObject("BannerContainer");
        bannerObj.transform.SetParent(activeCanvas.transform, false);
        var bannerRect = bannerObj.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 0.5f);
        bannerRect.anchorMax = new Vector2(0.5f, 0.5f);
        bannerRect.pivot = new Vector2(0.5f, 0.5f);
        float bannerHeight = screenH * 0.38f;
        bannerRect.sizeDelta = new Vector2(screenW * 1.5f, bannerHeight); 
        bannerRect.anchoredPosition = new Vector2(0f, offset.y);
        bannerRect.localRotation = Quaternion.Euler(0f, 0f, -10f); // Sleek -10 degree rotation

        // Solid Background (dark charcoal)
        var bannerImg = bannerObj.AddComponent<Image>();
        bannerImg.color = new Color(0.04f, 0.04f, 0.05f, 0f);

        // === Masked Area to clip the portrait and speedlines ===
        GameObject maskObj = new GameObject("MaskedView");
        maskObj.transform.SetParent(bannerObj.transform, false);
        var maskRect = maskObj.AddComponent<RectTransform>();
        maskRect.anchorMin = Vector2.zero;
        maskRect.anchorMax = Vector2.one;
        maskRect.offsetMin = Vector2.zero;
        maskRect.offsetMax = Vector2.zero;
        
        var maskImg = maskObj.AddComponent<Image>();
        maskImg.color = Color.white; // Needed for Mask component
        var mask = maskObj.AddComponent<Mask>();
        mask.showMaskGraphic = false; // Hide the white mask image, only clip children

        // === Neon Backglow Aura (inside mask, behind portrait) ===
        GameObject auraObj = new GameObject("NeonAura");
        auraObj.transform.SetParent(maskObj.transform, false);
        var auraRect = auraObj.AddComponent<RectTransform>();
        auraRect.anchorMin = new Vector2(0.5f, 0.5f);
        auraRect.anchorMax = new Vector2(0.5f, 0.5f);
        auraRect.pivot = new Vector2(0.5f, 0.5f);
        auraRect.sizeDelta = new Vector2(bannerHeight * 2f, bannerHeight * 2f);
        auraRect.anchoredPosition = Vector2.zero;
        var auraImg = auraObj.AddComponent<Image>();
        auraImg.sprite = GetGlowSprite();
        auraImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0f);

        // === Dynamic Speed Lines (inside mask, behind portrait) ===
        int numLines = 6;
        RectTransform[] speedLines = new RectTransform[numLines];
        float[] lineSpeeds = new float[numLines];
        for (int i = 0; i < numLines; i++)
        {
            GameObject lineObj = new GameObject($"SpeedLine_{i}");
            lineObj.transform.SetParent(maskObj.transform, false);
            var lineRect = lineObj.AddComponent<RectTransform>();
            lineRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineRect.pivot = new Vector2(0.5f, 0.5f);
            lineRect.sizeDelta = new Vector2(Random.Range(8f, 32f), bannerHeight * 1.5f);
            lineRect.anchoredPosition = new Vector2(Random.Range(-screenW * 0.7f, screenW * 0.7f), 0f);
            
            var lineImg = lineObj.AddComponent<Image>();
            lineImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0f); // Controlled by fade in
            
            speedLines[i] = lineRect;
            lineSpeeds[i] = Random.Range(700f, 1500f);
        }

        // === Character Portrait Setup ===
        // We preserve the exact aspect ratio of the character's card sprite
        float aspect = (sprite.rect.width / sprite.rect.height);
        float portraitWidth = bannerHeight * aspect * (sizeFactor / 0.6f);
        float portraitHeight = bannerHeight * (sizeFactor / 0.6f);

        GameObject portParent = new GameObject("PortraitParent");
        portParent.transform.SetParent(maskObj.transform, false);
        var portParentRect = portParent.AddComponent<RectTransform>();
        portParentRect.anchorMin = new Vector2(0.5f, 0.5f);
        portParentRect.anchorMax = new Vector2(0.5f, 0.5f);
        portParentRect.pivot = new Vector2(0.5f, 0.5f);
        portParentRect.sizeDelta = new Vector2(portraitWidth, portraitHeight);
        portParentRect.anchoredPosition = Vector2.zero; // Perfect center of the masked banner

        // Ghost Speed Blur Portrait
        GameObject ghostObj = new GameObject("GhostPortrait");
        ghostObj.transform.SetParent(portParent.transform, false);
        var ghostRect = ghostObj.AddComponent<RectTransform>();
        ghostRect.anchorMin = Vector2.zero;
        ghostRect.anchorMax = Vector2.one;
        ghostRect.offsetMin = Vector2.zero;
        ghostRect.offsetMax = Vector2.zero;
        var ghostImg = ghostObj.AddComponent<Image>();
        ghostImg.sprite = sprite;
        ghostImg.preserveAspect = true;
        ghostImg.color = Color.clear;

        // Main Portrait Image
        GameObject imgObj = new GameObject("PortraitImage");
        imgObj.transform.SetParent(portParent.transform, false);
        var imgRect = imgObj.AddComponent<RectTransform>();
        imgRect.anchorMin = Vector2.zero;
        imgRect.anchorMax = Vector2.one;
        imgRect.offsetMin = Vector2.zero;
        imgRect.offsetMax = Vector2.zero;
        var img = imgObj.AddComponent<Image>();
        img.sprite = sprite;
        img.preserveAspect = true;
        img.color = Color.clear;

        // === Borders (outside mask to prevent clipping) ===
        // Top Border Line
        GameObject topBorder = new GameObject("TopBorder");
        topBorder.transform.SetParent(bannerObj.transform, false);
        var topRect = topBorder.AddComponent<RectTransform>();
        topRect.anchorMin = new Vector2(0f, 1f);
        topRect.anchorMax = new Vector2(1f, 1f);
        topRect.pivot = new Vector2(0.5f, 1f);
        topRect.sizeDelta = new Vector2(0f, 8f);
        topRect.anchoredPosition = Vector2.zero;
        var topImg = topBorder.AddComponent<Image>();
        topImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0f);

        // Bottom Border Line
        GameObject bottomBorder = new GameObject("BottomBorder");
        bottomBorder.transform.SetParent(bannerObj.transform, false);
        var bottomRect = bottomBorder.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0f, 0f);
        bottomRect.anchorMax = new Vector2(1f, 0f);
        bottomRect.pivot = new Vector2(0.5f, 0f);
        bottomRect.sizeDelta = new Vector2(0f, 8f);
        bottomRect.anchoredPosition = Vector2.zero;
        var bottomImg = bottomBorder.AddComponent<Image>();
        bottomImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0f);

        // === Fullscreen Flash Overlay ===
        GameObject flashObj = new GameObject("RadialFlash");
        flashObj.transform.SetParent(activeCanvas.transform, false);
        var flashRect = flashObj.AddComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        var flashImg = flashObj.AddComponent<Image>();
        flashImg.color = Color.clear;

        // === PHASE 1: Dynamic Entry (Opposing slide-in + zoom snap) ===
        float elapsed = 0f;
        Vector2 bannerStartPos = new Vector2(-screenW * 1.0f, offset.y);
        Vector2 bannerEndPos = new Vector2(0f, offset.y);

        Vector2 portStartPos = new Vector2(screenW * 0.8f, 0f); // Slide in from right inside the banner
        Vector2 portEndPos = Vector2.zero;

        while (elapsed < FADE_IN_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / FADE_IN_DURATION);
            
            float easeOutBack = 1f - Mathf.Pow(1f - t, 4f); 
            float easeExpo = 1f - Mathf.Pow(2f, -10f * t);

            // Opacity animations
            bgImage.color = new Color(0f, 0f, 0f, t * 0.80f);
            bannerImg.color = new Color(0.04f, 0.04f, 0.05f, t * 0.95f);
            topImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, t * 0.95f);
            bottomImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, t * 0.95f);
            auraImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, t * 0.65f);

            // Fade in speed lines
            for (int i = 0; i < numLines; i++)
            {
                var lineImg = speedLines[i].GetComponent<Image>();
                lineImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, t * Random.Range(0.2f, 0.5f));
            }

            // Banner slides in
            bannerRect.anchoredPosition = Vector2.Lerp(bannerStartPos, bannerEndPos, easeOutBack);

            // Portrait slides in from the right and snaps down in scale
            portParentRect.anchoredPosition = Vector2.Lerp(portStartPos, portEndPos, easeOutBack);
            float scale = Mathf.Lerp(1.7f, 1.0f, easeExpo);
            portParentRect.localScale = new Vector3(scale, scale, 1f);
            img.color = new Color(1f, 1f, 1f, t);

            // Ghost Speed Blur Portrait
            float ghostScale = Mathf.Lerp(1.0f, 2.0f, t);
            ghostRect.localScale = new Vector3(ghostScale, ghostScale, 1f);
            ghostImg.color = new Color(1f, 1f, 1f, (1f - t) * 0.7f);

            // Fullscreen dynamic flash peak on impact
            flashImg.color = new Color(1f, 1f, 1f, Mathf.Sin(t * Mathf.PI) * 0.8f);

            // Move speed lines
            for (int i = 0; i < numLines; i++)
            {
                var lp = speedLines[i].anchoredPosition;
                lp.x += lineSpeeds[i] * Time.deltaTime;
                if (lp.x > screenW * 0.7f) lp.x = -screenW * 0.7f;
                speedLines[i].anchoredPosition = lp;
            }

            yield return null;
        }

        // Lock exactly
        bgImage.color = new Color(0f, 0f, 0f, 0.80f);
        bannerImg.color = new Color(0.04f, 0.04f, 0.05f, 0.95f);
        topImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.95f);
        bottomImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.95f);
        auraImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.65f);
        bannerRect.anchoredPosition = bannerEndPos;
        portParentRect.anchoredPosition = portEndPos;
        portParentRect.localScale = Vector3.one;
        img.color = Color.white;
        ghostImg.color = Color.clear;
        flashImg.color = Color.clear;

        // === PHASE 2: Symmetrical Hold & Slow Ken Burns Drift ===
        elapsed = 0f;
        Vector2 driftStartOffset = portEndPos;
        Vector2 driftEndOffset = driftStartOffset + new Vector2(-screenW * 0.02f, 0f); // Smooth drift to the left
        
        while (elapsed < HOLD_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / HOLD_DURATION;

            // Slow drift on portrait
            portParentRect.anchoredPosition = Vector2.Lerp(driftStartOffset, driftEndOffset, t);

            // Slow zoom on portrait (1.0 to 1.07)
            float slowZoom = Mathf.Lerp(1.0f, 1.07f, t);
            portParentRect.localScale = new Vector3(slowZoom, slowZoom, 1f);

            // Pulsing glow effect on neon borders and backdrop
            float glowPulse = 0.75f + Mathf.Sin(t * Mathf.PI * 2f) * 0.2f;
            topImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, glowPulse * 0.95f);
            bottomImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, glowPulse * 0.95f);
            auraImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, glowPulse * 0.65f);

            // Update speed lines movement
            for (int i = 0; i < numLines; i++)
            {
                var lp = speedLines[i].anchoredPosition;
                lp.x += lineSpeeds[i] * Time.deltaTime;
                if (lp.x > screenW * 0.7f) lp.x = -screenW * 0.7f;
                speedLines[i].anchoredPosition = lp;
            }

            yield return null;
        }

        // === PHASE 3: Fast Exit ===
        elapsed = 0f;
        Vector2 bannerExitPos = new Vector2(screenW * 1.0f, offset.y);
        Vector2 portExitPos = portParentRect.anchoredPosition - new Vector2(screenW * 0.35f, 0f); // Slide out left
        
        Vector2 bannerStartDrift = bannerRect.anchoredPosition;
        Vector2 portStartDrift = portParentRect.anchoredPosition;
        Vector3 portraitStartScale = portParentRect.localScale;

        while (elapsed < FADE_OUT_DURATION)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / FADE_OUT_DURATION);
            float easeInBack = t * t * t; 

            bgImage.color = new Color(0f, 0f, 0f, (1f - t) * 0.80f);
            bannerImg.color = new Color(0.04f, 0.04f, 0.05f, (1f - t) * 0.95f);
            topImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, (1f - t) * 0.95f);
            bottomImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, (1f - t) * 0.95f);
            auraImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, (1f - t) * 0.65f);

            // Fade out speed lines
            for (int i = 0; i < numLines; i++)
            {
                var lineImg = speedLines[i].GetComponent<Image>();
                lineImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, (1f - t) * 0.3f);
            }

            // Banner slides out right
            bannerRect.anchoredPosition = Vector2.Lerp(bannerStartDrift, bannerExitPos, easeInBack);

            // Portrait slides out left and scales down
            portParentRect.anchoredPosition = Vector2.Lerp(portStartDrift, portExitPos, easeInBack);
            img.color = new Color(1f, 1f, 1f, 1f - t);
            float scale = Mathf.Lerp(portraitStartScale.x, 0.7f, easeInBack);
            portParentRect.localScale = new Vector3(scale, scale, 1f);

            yield return null;
        }

        Object.Destroy(activeCanvas);
        activeCanvas = null;

        // === Trigger actual Ultimate sequence ===
        onComplete?.Invoke();
    }
}
