using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public enum SelectionPhase { P1_MainCharacter, P2_MainCharacter, P1_SupportCharacter, P2_SupportCharacter, MapSelection, DeathBattle_BotSelection }

public class CharacterSelectionManager : MonoBehaviour
{
    private SelectionPhase currentPhase = SelectionPhase.P1_MainCharacter;

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
        [HideInInspector] public string mapName;
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
    public Image bigMapPreview;
    public Text mapNameText;
    public Transform gridContainer;
    public GameObject charSlotPrefab;
    public GameObject mapSlotPrefab;

    [Header("--- UI RIÊNG CHO DEATH BATTLE ---")]
    public GameObject deathBattlePanel;
    public Image[] deathBattleBotPreviews;
    private int currentBotSelectionIndex = 0;

    [Header("--- UI THÔNG BÁO PHASE ---")]
    public Image phaseTextImage;
    public Sprite selectFighterSprite;
    public Sprite supportSprite;
    public Sprite mapSprite;

    [Header("--- TÙY CHỈNH KÍCH THƯỚC GRID ---")]
    public Vector2 characterCellSize = new Vector2(110f, 110f);
    public Vector2 characterSpacing = new Vector2(20f, 20f);

    [Space(10)]
    public Vector2 mapCellSize = new Vector2(160f, 90f);
    public Vector2 mapSpacing = new Vector2(15f, 15f);

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
        currentPhase = SelectionPhase.P1_MainCharacter;

        if (SelectionData.CurrentGameMode == GameMode.DeathBattle)
        {
            SelectionData.DeathBattleData = new DeathBattleSaveData();
            currentBotSelectionIndex = 0;
            if (deathBattlePanel != null) deathBattlePanel.SetActive(true);
            if (p2Preview != null) p2Preview.gameObject.SetActive(false);

            foreach (var img in deathBattleBotPreviews)
            {
                img.sprite = null;
                img.color = new Color(1, 1, 1, 0);
            }
        }
        else
        {
            if (deathBattlePanel != null) deathBattlePanel.SetActive(false);
        }

        SetupSelectionPhase();
    }

    void SetupSelectionPhase()
    {
        UpdatePhaseTextVisual();

        foreach (Transform child in gridContainer) { Destroy(child.gameObject); }
        spawnedSlots.Clear();

        int totalItems = GetCurrentItemCount();
        if (totalItems == 0) return;

        p1Locked = false;
        p2Locked = false;

        // Bỏ Training ra khỏi block này để nó dùng chung P1 & P2 giống PvP
        if (SelectionData.CurrentGameMode == GameMode.DeathBattle)
        {
            p1Index = 0;
            if (p2Cursor != null) p2Cursor.gameObject.SetActive(false);
        }
        else
        {
            p1Index = 0;
            p2Index = totalItems - 1;
            if (p2Cursor != null) p2Cursor.gameObject.SetActive(true);
        }

        GridLayoutGroup gridLayout = gridContainer.GetComponent<GridLayoutGroup>();
        RectTransform gridRect = gridContainer.GetComponent<RectTransform>();

        if (gridLayout != null && gridRect != null)
        {
            gridRect.anchorMin = new Vector2(0.5f, 0.5f);
            gridRect.anchorMax = new Vector2(0.5f, 0.5f);
            gridRect.pivot = new Vector2(0.5f, 0.5f);

            if (currentPhase == SelectionPhase.MapSelection)
            {
                if (p1Preview != null) p1Preview.gameObject.SetActive(false);
                if (p2Preview != null && SelectionData.CurrentGameMode != GameMode.DeathBattle) p2Preview.gameObject.SetActive(false);
                if (bigMapPreview != null) bigMapPreview.gameObject.SetActive(true);
                if (mapNameText != null) mapNameText.gameObject.SetActive(true);

                int mapColumns = 6;
                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = mapColumns;
                gridLayout.cellSize = mapCellSize;
                gridLayout.spacing = mapSpacing;
                gridLayout.childAlignment = TextAnchor.MiddleCenter;

                int totalRows = Mathf.CeilToInt((float)totalItems / mapColumns);
                float requiredWidth = (mapCellSize.x * mapColumns) + (mapSpacing.x * (mapColumns - 1)) + 20f;
                float requiredHeight = (mapCellSize.y * totalRows) + (mapSpacing.y * (totalRows - 1)) + 20f;
                gridRect.sizeDelta = new Vector2(requiredWidth, requiredHeight);
                gridRect.anchoredPosition = new Vector2(0f, -150f);
            }
            else
            {
                if (p1Preview != null) p1Preview.gameObject.SetActive(true);
                if (p2Preview != null && SelectionData.CurrentGameMode != GameMode.DeathBattle) p2Preview.gameObject.SetActive(true);
                if (bigMapPreview != null) bigMapPreview.gameObject.SetActive(false);
                if (mapNameText != null) mapNameText.gameObject.SetActive(false);

                gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
                gridLayout.constraintCount = 4;
                gridLayout.cellSize = characterCellSize;
                gridLayout.spacing = characterSpacing;
                gridLayout.childAlignment = TextAnchor.MiddleCenter;

                float requiredWidth = (characterCellSize.x * 4) + (characterSpacing.x * 3) + 20f;
                gridRect.sizeDelta = new Vector2(requiredWidth, characterCellSize.y + 20f);
                gridRect.anchoredPosition = Vector2.zero;
            }
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

    void UpdatePhaseTextVisual()
    {
        if (phaseTextImage == null) return;

        if (currentPhase == SelectionPhase.P1_MainCharacter || currentPhase == SelectionPhase.P2_MainCharacter || currentPhase == SelectionPhase.DeathBattle_BotSelection)
        {
            if (selectFighterSprite != null) phaseTextImage.sprite = selectFighterSprite;
        }
        else if (currentPhase == SelectionPhase.P1_SupportCharacter || currentPhase == SelectionPhase.P2_SupportCharacter)
        {
            if (supportSprite != null) phaseTextImage.sprite = supportSprite;
        }
        else if (currentPhase == SelectionPhase.MapSelection)
        {
            if (mapSprite != null) phaseTextImage.sprite = mapSprite;
        }
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

        int columns = (currentPhase == SelectionPhase.MapSelection) ? 6 : 4;

        if (SelectionData.CurrentGameMode == GameMode.DeathBattle)
        {
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

                if (keyboard.jKey.wasPressedThisFrame)
                {
                    // Tránh chọn trùng nhân vật trong Bot Selection (Death Battle)
                    if (currentPhase == SelectionPhase.DeathBattle_BotSelection)
                    {
                        string botPrefabPath = GetResourcesPath(allCharacters[p1Index].characterPrefab);
                        if (SelectionData.DeathBattleData.enemyCharacterUrls.Contains(botPrefabPath))
                        {
                            return; // Bỏ qua nếu nhân vật đã được chọn trước đó
                        }
                    }

                    p1Locked = true;
                    LockAndProceed();
                }
            }
        }
        else // Áp dụng cho cả PvP và Training
        {
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

                if (keyboard.numpad1Key.wasPressedThisFrame) { p2Locked = true; }
            }

            if (p1Locked && p2Locked) { LockAndProceed(); }
        }

        UpdateVisuals();
    }

    void UpdateVisuals()
    {
        int totalItems = GetCurrentItemCount();
        if (totalItems == 0) return;

        p1Index = Mathf.Clamp(p1Index, 0, totalItems - 1);
        p2Index = Mathf.Clamp(p2Index, 0, totalItems - 1);

        if (currentPhase != SelectionPhase.MapSelection)
        {
            if (SelectionData.CurrentGameMode == GameMode.DeathBattle)
            {
                // CHỈ CẬP NHẬT ẢNH TO KHI ĐANG CHỌN NHÂN VẬT CHÍNH
                // Sau khi đã sang phase chọn Bot, ảnh to của P1 sẽ đứng im ở nhân vật đã lock
                if (currentPhase == SelectionPhase.P1_MainCharacter)
                {
                    if (p1Preview != null) p1Preview.sprite = GetPreviewSpriteAt(p1Index);
                }
            }
            else // Chung cho cả Training & PvP
            {
                if (p1Preview != null) p1Preview.sprite = GetPreviewSpriteAt(p1Index);
                if (p2Preview != null) p2Preview.sprite = GetPreviewSpriteAt(p2Index);
            }
        }
        else
        {
            if (allMaps.Count > p1Index && allMaps[p1Index] != null)
            {
                if (bigMapPreview != null) bigMapPreview.sprite = allMaps[p1Index].mapThumbnail;
                if (mapNameText != null) mapNameText.text = allMaps[p1Index].mapName.ToUpper();
            }
        }

        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] == null) continue;
            spawnedSlots[i].color = Color.white;

            // Làm tối màu (dim) những bot đã được chọn trong DeathBattle
            if (SelectionData.CurrentGameMode == GameMode.DeathBattle && currentPhase == SelectionPhase.DeathBattle_BotSelection)
            {
                if (i < allCharacters.Count)
                {
                    string path = GetResourcesPath(allCharacters[i].characterPrefab);
                    if (SelectionData.DeathBattleData.enemyCharacterUrls.Contains(path))
                    {
                        spawnedSlots[i].color = new Color(0.3f, 0.3f, 0.3f, 1f);
                    }
                }
            }
        }

        if (p1Cursor != null && spawnedSlots.Count > p1Index)
            p1Cursor.position = spawnedSlots[p1Index].rectTransform.position + cursorOffset;

        if (SelectionData.CurrentGameMode != GameMode.DeathBattle) // Training & PvP đều dùng chung P2 Cursor
        {
            if (p2Cursor != null && spawnedSlots.Count > p2Index)
                p2Cursor.position = spawnedSlots[p2Index].rectTransform.position + cursorOffset;
        }
    }

    private int GetCurrentItemCount()
    {
        if (currentPhase == SelectionPhase.P1_MainCharacter || currentPhase == SelectionPhase.P2_MainCharacter || currentPhase == SelectionPhase.DeathBattle_BotSelection) return allCharacters.Count;
        if (currentPhase == SelectionPhase.P1_SupportCharacter || currentPhase == SelectionPhase.P2_SupportCharacter) return allSupports.Count;
        return allMaps.Count;
    }

    private Sprite GetAvatarSpriteAt(int index)
    {
        if (currentPhase == SelectionPhase.P1_MainCharacter || currentPhase == SelectionPhase.P2_MainCharacter || currentPhase == SelectionPhase.DeathBattle_BotSelection) return allCharacters[index].avatarSprite;
        return allSupports[index].avatarSprite;
    }

    private Sprite GetPreviewSpriteAt(int index)
    {
        if (currentPhase == SelectionPhase.P1_MainCharacter || currentPhase == SelectionPhase.P2_MainCharacter || currentPhase == SelectionPhase.DeathBattle_BotSelection) return allCharacters[index].standeeSprite;
        if (currentPhase == SelectionPhase.P1_SupportCharacter || currentPhase == SelectionPhase.P2_SupportCharacter) return allSupports[index].standeeSprite;
        return null;
    }

    string GetResourcesPath(Object obj)
    {
        if (obj == null) return string.Empty;

#if UNITY_EDITOR
        // In Editor: derive exact path from AssetDatabase
        string fullPath = UnityEditor.AssetDatabase.GetAssetPath(obj);
        if (!string.IsNullOrEmpty(fullPath))
        {
            int resourcesIndex = fullPath.IndexOf("Resources/");
            if (resourcesIndex != -1)
            {
                string cutPath = fullPath.Substring(resourcesIndex + 10); // skip "Resources/"
                int dotIndex = cutPath.LastIndexOf('.');
                if (dotIndex != -1) cutPath = cutPath.Substring(0, dotIndex);
                return cutPath;
            }
        }
#endif
        // Runtime fallback: Unity Resources.Load requires the path relative to Resources/.
        // Since all character prefabs live at  Resources/<CharacterFolder>/<PrefabName>
        // and all character sprites at          Resources/<CharacterFolder>/<SpriteName>
        // we cannot infer the folder at runtime without extra data.
        // SOLUTION: use obj.name only — callers (LoadCharacter) must use
        //   Resources.Load<T>(path) where path may be just the filename
        //   IF all prefabs are directly under Resources/ (flat).
        // For nested paths the Editor path is authoritative and saved to JSON on first run.
        return obj.name;
    }


    void LockAndProceed()
    {
        isCounting = false;

        if (SelectionData.CurrentGameMode == GameMode.DeathBattle)
        {
            if (currentPhase == SelectionPhase.P1_MainCharacter)
            {
                SelectionData.characterImageUrl1 = GetResourcesPath(allCharacters[p1Index].avatarSprite);
                SelectionData.characterPrefabUrl1 = GetResourcesPath(allCharacters[p1Index].characterPrefab);
                SelectionData.DeathBattleData.playerCharacterUrl = SelectionData.characterPrefabUrl1;
                SelectionData.DeathBattleData.playerCharacterImageUrl = SelectionData.characterImageUrl1; // Lưu avatar P1

                currentPhase = SelectionPhase.DeathBattle_BotSelection;
            }
            else if (currentPhase == SelectionPhase.DeathBattle_BotSelection)
            {
                string botPrefabPath = GetResourcesPath(allCharacters[p1Index].characterPrefab);
                string botImageUrl = GetResourcesPath(allCharacters[p1Index].avatarSprite); // Lấy avatar bot

                // Fallback chống lỗi: Lỡ timer = 0 ép lock ngay ô trùng lặp, game tự dò 1 ô chưa chọn
                if (SelectionData.DeathBattleData.enemyCharacterUrls.Contains(botPrefabPath))
                {
                    for (int i = 0; i < allCharacters.Count; i++)
                    {
                        string fallbackPath = GetResourcesPath(allCharacters[i].characterPrefab);
                        if (!SelectionData.DeathBattleData.enemyCharacterUrls.Contains(fallbackPath))
                        {
                            p1Index = i;
                            botPrefabPath = fallbackPath;
                            botImageUrl = GetResourcesPath(allCharacters[i].avatarSprite);
                            break;
                        }
                    }
                }

                SelectionData.DeathBattleData.enemyCharacterUrls.Add(botPrefabPath);
                SelectionData.DeathBattleData.enemyCharacterImageUrls.Add(botImageUrl); // Lưu avatar bot

                if (currentBotSelectionIndex < deathBattleBotPreviews.Length)
                {
                    deathBattleBotPreviews[currentBotSelectionIndex].sprite = allCharacters[p1Index].avatarSprite;
                    deathBattleBotPreviews[currentBotSelectionIndex].color = Color.white;
                }

                currentBotSelectionIndex++;

                if (currentBotSelectionIndex >= 5)
                {
                    GenerateRandomMapsAndStartDeathBattle();
                    return;
                }
            }
        }
        else // Chạy logic chung cho PvP và Training
        {
            if (currentPhase == SelectionPhase.P1_MainCharacter)
            {
                SelectionData.characterImageUrl1 = GetResourcesPath(allCharacters[p1Index].avatarSprite);
                SelectionData.characterPrefabUrl1 = GetResourcesPath(allCharacters[p1Index].characterPrefab);
                SelectionData.characterImageUrl2 = GetResourcesPath(allCharacters[p2Index].avatarSprite);
                SelectionData.characterPrefabUrl2 = GetResourcesPath(allCharacters[p2Index].characterPrefab);

                currentPhase = SelectionPhase.P1_SupportCharacter;
            }
            else if (currentPhase == SelectionPhase.P1_SupportCharacter)
            {
                SelectionData.supportImageUrl1 = GetResourcesPath(allSupports[p1Index].avatarSprite);
                SelectionData.supportPrefabUrl1 = GetResourcesPath(allSupports[p1Index].characterPrefab);
                SelectionData.supportImageUrl2 = GetResourcesPath(allSupports[p2Index].avatarSprite);
                SelectionData.supportPrefabUrl2 = GetResourcesPath(allSupports[p2Index].characterPrefab);

                currentPhase = SelectionPhase.MapSelection;
            }
            else if (currentPhase == SelectionPhase.MapSelection)
            {
                int finalMapIndex = p1Index;
                if (p1Index != p2Index)
                {
                    finalMapIndex = (Random.value > 0.5f) ? p1Index : p2Index;
                }

                SceneManager.LoadScene(allMaps[finalMapIndex].mapName);
                return;
            }
        }

        timeRemaining = 30f;
        isCounting = true;
        SetupSelectionPhase();
    }

    void GenerateRandomMapsAndStartDeathBattle()
    {
        List<MapInfoData> tempMaps = new List<MapInfoData>(allMaps);
        for (int i = 0; i < 5; i++)
        {
            if (tempMaps.Count == 0) break;
            int ranIndex = Random.Range(0, tempMaps.Count);
            SelectionData.DeathBattleData.mapNames.Add(tempMaps[ranIndex].mapName);
            tempMaps.RemoveAt(ranIndex);
        }

        SelectionData.DeathBattleData.currentMatchIndex = 0;
        DeathBattleSaveSystem.SaveProgress(SelectionData.DeathBattleData);
        SelectionData.characterPrefabUrl2 = SelectionData.DeathBattleData.enemyCharacterUrls[0];
        SelectionData.characterImageUrl2 = SelectionData.DeathBattleData.enemyCharacterImageUrls[0];
 
        SceneManager.LoadScene(SelectionData.DeathBattleData.mapNames[0]);
    }
}