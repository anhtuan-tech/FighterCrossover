using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class CharacterSelectionManager : MonoBehaviour
{
    public enum SelectionPhase { MainCharacter, SupportCharacter, MapSelection }
    private SelectionPhase currentPhase = SelectionPhase.MainCharacter;

    [System.Serializable]
    public class CharacterInfoData
    {
        public string characterName;
        public Sprite avatarSprite;
        public Sprite standeeSprite;
        public GameObject characterPrefab;
    }

    [System.Serializable]
    public class MapInfoData
    {
        [HideInInspector]
        public string mapName;
        public Sprite mapThumbnail;

#if UNITY_EDITOR
        [Tooltip("KÉO THẢ TRỰC TIẾP FILE SCENE VÀO ĐÂY")]
        public UnityEditor.SceneAsset mapSceneFile; 
#endif
    }

    [Header("--- DATA NHÂN VẬT CHÍNH ---")]
    public List<CharacterInfoData> allCharacters = new List<CharacterInfoData>();

    [Header("--- DATA NHÂN VẬT HỖ TRỢ (SUPPORT) ---")]
    public List<CharacterInfoData> allSupports = new List<CharacterInfoData>();

    [Header("--- DATA CHỌN MAP ---")]
    public List<MapInfoData> allMaps = new List<MapInfoData>();

    [Header("--- GIAO DIỆN UI CHÍNH ---")]
    public Image p1Preview;
    public Image p2Preview;
    public Transform gridContainer;
    public GameObject charSlotPrefab;
    public GameObject mapSlotPrefab;

    [Header("--- TÙY CHỈNH KÍCH THƯỚC GRID ---")]
    public Vector2 characterCellSize = new Vector2(110f, 110f);
    public Vector2 characterSpacing = new Vector2(20f, 20f);

    [Space(10)]
    [Tooltip("Kích thước của thẻ Map khi xếp dọc")]
    public Vector2 mapCellSize = new Vector2(550f, 135f);
    [Tooltip("Khoảng cách DỌC giữa các map")]
    public Vector2 mapSpacing = new Vector2(0f, 25f);

    [Header("--- UI THANH TIMER PHÍA TRÊN (30S) ---")]
    public Sprite[] numberSprites;
    public Image tensImage;
    public Image onesImage;

    [Header("--- CON TRỎ DI CHUYỂN ---")]
    public RectTransform p1Cursor;
    public RectTransform p2Cursor;
    public Vector3 cursorOffset = new Vector3(0, 0, 0);

    private List<Image> spawnedSlots = new List<Image>();
    private float timeRemaining = 30f;

    private int p1Index = 0;
    private int p2Index = 0;

    private bool p1Locked = false;
    private bool p2Locked = false;
    private bool isCounting = true;

#if UNITY_EDITOR
    void OnValidate()
    {
        if (allMaps == null) return;
        foreach (var map in allMaps)
        {
            if (map != null && map.mapSceneFile != null)
            {
                map.mapName = map.mapSceneFile.name; 
            }
        }
    }
#endif

    void Start()
    {
        currentPhase = SelectionPhase.MainCharacter;
        SetupSelectionPhase();
    }

    void SetupSelectionPhase()
    {
        foreach (Transform child in gridContainer) { Destroy(child.gameObject); }
        spawnedSlots.Clear();

        int totalItems = GetCurrentItemCount();
        if (totalItems == 0) return;

        p1Index = 0;
        p2Index = totalItems - 1;
        p1Locked = false;
        p2Locked = false;

        if (currentPhase == SelectionPhase.MapSelection)
        {
            if (p1Preview != null) p1Preview.gameObject.SetActive(false);
            if (p2Preview != null) p2Preview.gameObject.SetActive(false);
        }
        else
        {
            if (p1Preview != null) p1Preview.gameObject.SetActive(true);
            if (p2Preview != null) p2Preview.gameObject.SetActive(true);
        }

        GridLayoutGroup gridLayout = gridContainer.GetComponent<GridLayoutGroup>();
        RectTransform gridRect = gridContainer.GetComponent<RectTransform>();

        if (gridLayout != null && gridRect != null)
        {
            // --- KHÓA CỨNG ANCHOR VÀ PIVOT VỀ TRUNG TÂM BẰNG CODE (CHỐNG LỆCH TUYỆT ĐỐI) ---
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);
            gridRect.anchoredPosition = Vector2.zero; // Trả về tâm (0,0) màn hình góc chuẩn

            if (currentPhase == SelectionPhase.MapSelection)
            {
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = 1;
                gridLayout.cellSize = mapCellSize;
                gridLayout.spacing = mapSpacing;
                gridLayout.childAlignment = TextAnchor.MiddleCenter;

                // Tính toán chiều cao
                float requiredHeight = (mapCellSize.y * totalItems) + (mapSpacing.y * (totalItems - 1)) + 40f;
                gridRect.sizeDelta = new Vector2(mapCellSize.x + 40f, requiredHeight);
            }
            else
            {
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = 4;
                gridLayout.cellSize = characterCellSize;
                gridLayout.spacing = characterSpacing;
                gridLayout.childAlignment = TextAnchor.MiddleCenter;

                // Tính toán chiều rộng
                float requiredWidth = (characterCellSize.x * 4) + (characterSpacing.x * 3) + 20f;
                gridRect.sizeDelta = new Vector2(requiredWidth, characterCellSize.y + 20f);
            }

            // Ép thêm một lần nữa sau khi đổi sizeDelta để chắc chắn không bị trôi vị trí
            gridRect.anchoredPosition = Vector2.zero;
        }

        GameObject prefabToUse = (currentPhase == SelectionPhase.MapSelection) ? mapSlotPrefab : charSlotPrefab;

        for (int i = 0; i < totalItems; i++)
        {
            GameObject newSlot = Instantiate(prefabToUse, gridContainer);

            if (currentPhase == SelectionPhase.MapSelection)
            {
                Image slotImage = newSlot.GetComponent<Image>();
                if (slotImage == null) slotImage = newSlot.GetComponentInChildren<Image>();
                if (slotImage != null) slotImage.sprite = allMaps[i].mapThumbnail;

                spawnedSlots.Add(slotImage);

                Text mapText = newSlot.GetComponentInChildren<Text>();
                if (mapText != null)
                {
                    mapText.text = allMaps[i].mapName.ToUpper();
                }
            }
            else
            {
                Image slotImage = newSlot.GetComponent<Image>();
                if (slotImage != null) slotImage.sprite = GetAvatarSpriteAt(i);
                spawnedSlots.Add(slotImage);
            }

            int index = i;
            Button btn = newSlot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => {
                    if (!p1Locked) { p1Index = index; UpdateVisuals(); }
                });
            }
        }

        if (p1Cursor != null) p1Cursor.SetAsLastSibling();
        if (p2Cursor != null) p2Cursor.SetAsLastSibling();

        Canvas.ForceUpdateCanvases();
        if (gridRect != null) LayoutRebuilder.ForceRebuildLayoutImmediate(gridRect);

        UpdateVisuals();
    }

    void Update()
    {
        if (isCounting)
        {
            timeRemaining -= Time.deltaTime;
            UpdateTimerUI(timeRemaining);

            if (timeRemaining <= 0)
            {
                timeRemaining = 0;
                UpdateTimerUI(0);
                LockAndProceed();
            }
        }

        HandleNewInputSystem();
    }

    void UpdateTimerUI(float timeToDisplay)
    {
        if (numberSprites == null || numberSprites.Length < 10) return;

        int totalSeconds = Mathf.CeilToInt(timeToDisplay);
        if (totalSeconds > 99) totalSeconds = 99;
        if (totalSeconds < 0) totalSeconds = 0;

        int tens = (totalSeconds / 10) % 10;
        int ones = totalSeconds % 10;

        if (tensImage != null) tensImage.sprite = numberSprites[tens];
        if (onesImage != null) onesImage.sprite = numberSprites[ones];
    }

    void HandleNewInputSystem()
    {
        int totalItems = GetCurrentItemCount();
        if (totalItems == 0) return;

        int maxIndex = totalItems - 1;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (currentPhase == SelectionPhase.MapSelection)
        {
            if (!p1Locked)
            {
                if (keyboard.aKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
                {
                    p1Index = (p1Index == 0) ? maxIndex : p1Index - 1;
                }
                if (keyboard.dKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
                {
                    p1Index = (p1Index == maxIndex) ? 0 : p1Index + 1;
                }
                if (keyboard.jKey.wasPressedThisFrame) { p1Locked = true; }
            }

            if (!p2Locked)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.upArrowKey.wasPressedThisFrame)
                {
                    p2Index = (p2Index == 0) ? maxIndex : p2Index - 1;
                }
                if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.downArrowKey.wasPressedThisFrame)
                {
                    p2Index = (p2Index == maxIndex) ? 0 : p2Index + 1;
                }
                if (keyboard.enterKey.wasPressedThisFrame) { p2Locked = true; }
            }
        }
        else
        {
            int columns = 4;
            if (!p1Locked)
            {
                if (keyboard.aKey.wasPressedThisFrame)
                {
                    if (p1Index % columns == 0) p1Index = Mathf.Min(p1Index + columns - 1, maxIndex);
                    else p1Index--;
                }
                if (keyboard.dKey.wasPressedThisFrame)
                {
                    if (p1Index % columns == columns - 1 || p1Index == maxIndex) p1Index -= (p1Index % columns);
                    else p1Index++;
                }
                if (keyboard.wKey.wasPressedThisFrame && p1Index >= columns) p1Index -= columns;
                if (keyboard.sKey.wasPressedThisFrame && p1Index + columns <= maxIndex) p1Index += columns;

                if (keyboard.jKey.wasPressedThisFrame) { p1Locked = true; }
            }

            if (!p2Locked)
            {
                if (keyboard.leftArrowKey.wasPressedThisFrame)
                {
                    if (p2Index % columns == 0) p2Index = Mathf.Min(p2Index + columns - 1, maxIndex);
                    else p2Index--;
                }
                if (keyboard.rightArrowKey.wasPressedThisFrame)
                {
                    if (p2Index % columns == columns - 1 || p2Index == maxIndex) p2Index -= (p2Index % columns);
                    else p2Index++;
                }
                if (keyboard.upArrowKey.wasPressedThisFrame && p2Index >= columns) p2Index -= columns;
                if (keyboard.downArrowKey.wasPressedThisFrame && p2Index + columns <= maxIndex) p2Index += columns;

                if (keyboard.enterKey.wasPressedThisFrame) { p2Locked = true; }
            }
        }

        UpdateVisuals();

        if (p1Locked && p2Locked) { LockAndProceed(); }
    }

    void UpdateVisuals()
    {
        int totalItems = GetCurrentItemCount();
        if (totalItems == 0) return;

        p1Index = Mathf.Clamp(p1Index, 0, totalItems - 1);
        p2Index = Mathf.Clamp(p2Index, 0, totalItems - 1);

        if (currentPhase != SelectionPhase.MapSelection)
        {
            if (p1Preview != null) p1Preview.sprite = GetPreviewSpriteAt(p1Index);
            if (p2Preview != null) p2Preview.sprite = GetPreviewSpriteAt(p2Index);
        }

        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] == null) continue;
            spawnedSlots[i].color = Color.white;
        }

        if (p1Cursor != null && spawnedSlots.Count > p1Index)
            p1Cursor.position = spawnedSlots[p1Index].rectTransform.position + cursorOffset;

        if (p2Cursor != null && spawnedSlots.Count > p2Index)
            p2Cursor.position = spawnedSlots[p2Index].rectTransform.position + cursorOffset;
    }

    private int GetCurrentItemCount()
    {
        if (currentPhase == SelectionPhase.MainCharacter) return allCharacters.Count;
        if (currentPhase == SelectionPhase.SupportCharacter) return allSupports.Count;
        return allMaps.Count;
    }

    private Sprite GetAvatarSpriteAt(int index)
    {
        if (currentPhase == SelectionPhase.MainCharacter) return allCharacters[index].avatarSprite;
        return allSupports[index].avatarSprite;
    }

    private Sprite GetPreviewSpriteAt(int index)
    {
        if (currentPhase == SelectionPhase.MainCharacter) return allCharacters[index].standeeSprite;
        if (currentPhase == SelectionPhase.SupportCharacter) return allSupports[index].standeeSprite;
        return null;
    }

    string GetResourcesPath(Object obj)
    {
        if (obj == null) return string.Empty;
#if UNITY_EDITOR
        string fullPath = UnityEditor.AssetDatabase.GetAssetPath(obj);
        if (string.IsNullOrEmpty(fullPath)) return obj.name;

        int resourcesIndex = fullPath.IndexOf("Resources/");
        if (resourcesIndex != -1)
        {
            string cutPath = fullPath.Substring(resourcesIndex + 10);
            int dotIndex = cutPath.LastIndexOf('.');
            if (dotIndex != -1) { cutPath = cutPath.Substring(0, dotIndex); }
            return cutPath;
        }
#endif
        return obj.name;
    }

    void LockAndProceed()
    {
        isCounting = false;

        if (currentPhase == SelectionPhase.MainCharacter)
        {
            SelectionData.characterImageUrl1 = GetResourcesPath(allCharacters[p1Index].avatarSprite);
            SelectionData.characterPrefabUrl1 = GetResourcesPath(allCharacters[p1Index].characterPrefab);
            SelectionData.characterImageUrl2 = GetResourcesPath(allCharacters[p2Index].avatarSprite);
            SelectionData.characterPrefabUrl2 = GetResourcesPath(allCharacters[p2Index].characterPrefab);

            currentPhase = SelectionPhase.SupportCharacter;
            timeRemaining = 30f;
            isCounting = true;
            SetupSelectionPhase();
        }
        else if (currentPhase == SelectionPhase.SupportCharacter)
        {
            SelectionData.supportImageUrl1 = GetResourcesPath(allSupports[p1Index].avatarSprite);
            SelectionData.supportPrefabUrl1 = GetResourcesPath(allSupports[p1Index].characterPrefab);
            SelectionData.supportImageUrl2 = GetResourcesPath(allSupports[p2Index].avatarSprite);
            SelectionData.supportPrefabUrl2 = GetResourcesPath(allSupports[p2Index].characterPrefab);

            currentPhase = SelectionPhase.MapSelection;
            timeRemaining = 30f;
            isCounting = true;
            SetupSelectionPhase();
        }
        else if (currentPhase == SelectionPhase.MapSelection)
        {
            int finalMapIndex = p1Index;

            if (p1Index != p2Index)
            {
                finalMapIndex = (Random.value > 0.5f) ? p1Index : p2Index;
                Debug.Log($"[GACHA MAP] Kết quả: {allMaps[finalMapIndex].mapName}");
            }

            SceneManager.LoadScene(allMaps[finalMapIndex].mapName);
        }
    }
}