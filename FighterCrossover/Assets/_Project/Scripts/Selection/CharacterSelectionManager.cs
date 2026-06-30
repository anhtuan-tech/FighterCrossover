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
    public RectTransform p1Cursor;
    public RectTransform p2Cursor;

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
        if (allCharacters.Count == 0) return;

        int columns = 4;
        int maxIndex = allCharacters.Count - 1;
        var keyboard = Keyboard.current;
        if (keyboard == null) return;

        // --- PLAYER 1 (WASD + J) ---
        if (!p1Locked)
        {
            // Trái
            if (keyboard.aKey.wasPressedThisFrame)
            {
                if (p1Index % columns == 0) p1Index = Mathf.Min(p1Index + columns - 1, maxIndex);
                else p1Index--;
            }
            // Phải
            if (keyboard.dKey.wasPressedThisFrame)
            {
                if (p1Index % columns == columns - 1 || p1Index == maxIndex) p1Index -= (p1Index % columns);
                else p1Index++;
            }
            // Lên
            if (keyboard.wKey.wasPressedThisFrame && p1Index >= columns) p1Index -= columns;
            // Xuống
            if (keyboard.sKey.wasPressedThisFrame && p1Index + columns <= maxIndex) p1Index += columns;

            // Khóa
            if (keyboard.jKey.wasPressedThisFrame) { p1Locked = true; }
        }
        else if (keyboard.escapeKey.wasPressedThisFrame) { p1Locked = false; }

        // --- PLAYER 2 (MŨI TÊN + ENTER) ---
        if (!p2Locked)
        {
            // Trái
            if (keyboard.leftArrowKey.wasPressedThisFrame)
            {
                if (p2Index % columns == 0) p2Index = Mathf.Min(p2Index + columns - 1, maxIndex);
                else p2Index--;
            }
            // Phải
            if (keyboard.rightArrowKey.wasPressedThisFrame)
            {
                if (p2Index % columns == columns - 1 || p2Index == maxIndex) p2Index -= (p2Index % columns);
                else p2Index++;
            }
            // Lên
            if (keyboard.upArrowKey.wasPressedThisFrame && p2Index >= columns) p2Index -= columns;
            // Xuống
            if (keyboard.downArrowKey.wasPressedThisFrame && p2Index + columns <= maxIndex) p2Index += columns;

            // Khóa
            if (keyboard.enterKey.wasPressedThisFrame) { p2Locked = true; }
        }
        else if (keyboard.backspaceKey.wasPressedThisFrame) { p2Locked = false; }

        UpdateVisuals();

        if (p1Locked && p2Locked) { LockAndProceed(); }
    }

    void UpdateVisuals()
    {
        if (allCharacters.Count == 0) return;

        // Giới hạn index an toàn tuyệt đối
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
            p1Cursor.position = spawnedSlots[p1Index].rectTransform.position;

        if (p2Cursor != null && spawnedSlots.Count > p2Index)
            p2Cursor.position = spawnedSlots[p2Index].rectTransform.position;
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