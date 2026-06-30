using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class CharacterSelectionManager : MonoBehaviour
{
    [System.Serializable]
    public class CharacterInfoData
    {
        public string characterName;
        public Sprite avatarSprite;
        public GameObject characterPrefab;
    }

    [Header("--- DATA NHÂN VẬT ---")]
    public List<CharacterInfoData> allCharacters = new List<CharacterInfoData>();

    [Header("--- GIAO DIỆN UI ---")]
    public Text timerText;
    public Image p1Preview;
    public Image p2Preview;
    public Transform gridContainer;
    public GameObject charSlotPrefab;

    [Header("--- CON TRỎ (MŨI TÊN/KHUNG) ---")]
    public RectTransform p1Cursor; // Kéo thả UI Image con trỏ của P1 vào đây
    public RectTransform p2Cursor; // Kéo thả UI Image con trỏ của P2 vào đây

    private List<Image> spawnedSlots = new List<Image>();
    private float timeRemaining = 30f;

    private int p1Index = 0;
    private int p2Index = 0;

    private bool p1Locked = false;
    private bool p2Locked = false;
    private bool isCounting = true;

    void Start()
    {
        if (allCharacters.Count > 0)
        {
            p2Index = allCharacters.Count - 1;
        }

        foreach (Transform child in gridContainer) { Destroy(child.gameObject); }
        spawnedSlots.Clear();

        if (allCharacters.Count == 0) return;

        for (int i = 0; i < allCharacters.Count; i++)
        {
            GameObject newSlot = Instantiate(charSlotPrefab, gridContainer);
            newSlot.name = $"Slot_{i}_{allCharacters[i].characterName}";

            Image slotImage = newSlot.GetComponent<Image>();

            if (slotImage != null && allCharacters[i].avatarSprite != null)
            {
                slotImage.sprite = allCharacters[i].avatarSprite;
            }
            spawnedSlots.Add(slotImage);

            int index = i;
            Button btn = newSlot.GetComponent<Button>();
            if (btn != null)
            {
                btn.onClick.AddListener(() => {
                    if (!p1Locked) { p1Index = index; UpdateVisuals(); }
                });
            }
        }

        // Đảm bảo con trỏ hiển thị đè lên trên các slot
        if (p1Cursor != null) p1Cursor.SetAsLastSibling();
        if (p2Cursor != null) p2Cursor.SetAsLastSibling();

        UpdateVisuals();
    }

    void Update()
    {
        if (isCounting)
        {
            timeRemaining -= Time.deltaTime;
            if (timerText != null) timerText.text = Mathf.CeilToInt(timeRemaining).ToString();
            if (timeRemaining <= 0) { LockAndProceed(); }
        }

        HandleNewInputSystem();
    }

    void HandleNewInputSystem()
    {
        int columns = 4;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // --- PLAYER 1 ---
        if (!p1Locked)
        {
            if (keyboard.aKey.wasPressedThisFrame) p1Index = (p1Index % columns == 0) ? p1Index + (columns - 1) : p1Index - 1;
            if (keyboard.dKey.wasPressedThisFrame) p1Index = (p1Index % columns == columns - 1) ? p1Index - (columns - 1) : p1Index + 1;
            if (keyboard.wKey.wasPressedThisFrame && p1Index - columns >= 0) p1Index -= columns;
            if (keyboard.sKey.wasPressedThisFrame && p1Index + columns < allCharacters.Count) p1Index += columns;

            if (keyboard.jKey.wasPressedThisFrame) { p1Locked = true; }
        }
        else if (keyboard.escapeKey.wasPressedThisFrame)
        {
            p1Locked = false;
        }

        // --- PLAYER 2 ---
        if (!p2Locked)
        {
            if (keyboard.leftArrowKey.wasPressedThisFrame) p2Index = (p2Index % columns == 0) ? p2Index + (columns - 1) : p2Index - 1;
            if (keyboard.rightArrowKey.wasPressedThisFrame) p2Index = (p2Index % columns == columns - 1) ? p2Index - (columns - 1) : p2Index + 1;
            if (keyboard.upArrowKey.wasPressedThisFrame && p2Index - columns >= 0) p2Index -= columns;
            if (keyboard.downArrowKey.wasPressedThisFrame && p2Index + columns < allCharacters.Count) p2Index += columns;

            if (keyboard.enterKey.wasPressedThisFrame) { p2Locked = true; }
        }
        else if (keyboard.backspaceKey.wasPressedThisFrame)
        {
            p2Locked = false;
        }

        UpdateVisuals();

        if (p1Locked && p2Locked) { LockAndProceed(); }
    }

    void UpdateVisuals()
    {
        if (allCharacters.Count == 0) return;

        // Cập nhật Preview Image
        if (p1Preview != null && allCharacters[p1Index].avatarSprite != null)
            p1Preview.sprite = allCharacters[p1Index].avatarSprite;

        if (p2Preview != null && allCharacters[p2Index].avatarSprite != null)
            p2Preview.sprite = allCharacters[p2Index].avatarSprite;

        // Reset màu tất cả các slot về trắng (không còn tô xanh đỏ nữa)
        for (int i = 0; i < spawnedSlots.Count; i++)
        {
            if (spawnedSlots[i] == null) continue;
            spawnedSlots[i].color = Color.white;
        }

        // DI CHUYỂN CON TRỎ P1 & P2 ĐẾN VỊ TRÍ ĐANG CHỌN
        if (p1Cursor != null && spawnedSlots.Count > p1Index)
        {
            p1Cursor.position = spawnedSlots[p1Index].rectTransform.position;
        }

        if (p2Cursor != null && spawnedSlots.Count > p2Index)
        {
            p2Cursor.position = spawnedSlots[p2Index].rectTransform.position;
        }
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
            if (dotIndex != -1)
            {
                cutPath = cutPath.Substring(0, dotIndex);
            }
            return cutPath;
        }
#endif
        return obj.name;
    }

    void LockAndProceed()
    {
        isCounting = false;

        SelectionData.characterImageUrl1 = GetResourcesPath(allCharacters[p1Index].avatarSprite);
        SelectionData.characterPrefabUrl1 = GetResourcesPath(allCharacters[p1Index].characterPrefab);

        SelectionData.characterImageUrl2 = GetResourcesPath(allCharacters[p2Index].avatarSprite);
        SelectionData.characterPrefabUrl2 = GetResourcesPath(allCharacters[p2Index].characterPrefab);

        Debug.Log($"[Đã Đồng Bộ Đường Dẫn] P1 Prefab: {SelectionData.characterPrefabUrl1} | P2 Prefab: {SelectionData.characterPrefabUrl2}");

        SceneManager.LoadScene("MainMenu_Scene");
    }
}