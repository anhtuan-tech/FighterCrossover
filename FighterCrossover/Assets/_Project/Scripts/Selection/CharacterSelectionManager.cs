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

    [Header("--- GIAO DIỆN UI CHÍNH ---")]
    public Image p1Preview;
    public Image p2Preview;
    public Transform gridContainer;
    public GameObject charSlotPrefab;

    [Header("--- UI THỜI GIAN (SPRITE) ---")]
    [Tooltip("Kéo thả 10 ảnh số từ 0 đến 9 vào đây (đúng thứ tự)")]
    public Sprite[] numberSprites;
    [Tooltip("Ô Image hiển thị hàng chục")]
    public Image tensImage;
    [Tooltip("Ô Image hiển thị hàng đơn vị")]
    public Image onesImage;

    [Header("--- CON TRỎ (MŨI TÊN/KHUNG) ---")]
    public RectTransform p1Cursor;
    public RectTransform p2Cursor;
    [Tooltip("Khoảng cách lệch của con trỏ so với Avatar (X, Y, Z)")]
    public Vector3 cursorOffset = new Vector3(0, 70f, 0);

    private List<Image> spawnedSlots = new List<Image>();
    private float timeRemaining = 30f; // Thời gian chọn tướng (30 giây)

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

        if (p1Cursor != null) p1Cursor.SetAsLastSibling();
        if (p2Cursor != null) p2Cursor.SetAsLastSibling();

        UpdateVisuals();
        UpdateTimerUI(timeRemaining); // Cập nhật hình ảnh đồng hồ ngay khi bắt đầu
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

    // HÀM XỬ LÝ HÌNH ẢNH ĐỒNG HỒ
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
        if (allCharacters.Count == 0) return;

        int columns = 4;
        int maxIndex = allCharacters.Count - 1;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // --- PLAYER 1 (WASD + J) ---
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
        else if (keyboard.escapeKey.wasPressedThisFrame) { p1Locked = false; }

        // --- PLAYER 2 (MŨI TÊN + ENTER) ---
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
        else if (keyboard.backspaceKey.wasPressedThisFrame) { p2Locked = false; }

        UpdateVisuals();

        if (p1Locked && p2Locked) { LockAndProceed(); }
    }

    void UpdateVisuals()
    {
        if (allCharacters.Count == 0) return;

        p1Index = Mathf.Clamp(p1Index, 0, allCharacters.Count - 1);
        p2Index = Mathf.Clamp(p2Index, 0, allCharacters.Count - 1);

        if (p1Preview != null && allCharacters[p1Index].avatarSprite != null)
            p1Preview.sprite = allCharacters[p1Index].avatarSprite;

        if (p2Preview != null && allCharacters[p2Index].avatarSprite != null)
            p2Preview.sprite = allCharacters[p2Index].avatarSprite;

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