using UnityEngine;

public class LoadCharacter : MonoBehaviour
{
    // THÊM: Tạo Instance để RoundManager có thể gọi trực tiếp không sợ bị null
    public static LoadCharacter Instance { get; private set; }

    [Header("--- Spawn Points (P1 / P2 Objects) ---")]
    public GameObject p1;
    public GameObject p2;

    [Header("--- UI Monitors ---")]
    public FighterStatMonitor p1UiMonitor;
    public FighterStatMonitor p2UiMonitor;

    [Header("--- Match Manager ---")]
    public MatchManager matchManager;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // Re-hydrate từ file save nếu SelectionData bị mất khi chuyển scene (DeathBattle mode)
        TryRehydrateFromSave();
    }

    /// <summary>
    /// Khi chuyển map trong Death Battle, static SelectionData có thể bị reset.
    /// Hàm này khôi phục lại từ file JSON nếu cần.
    /// </summary>
    private void TryRehydrateFromSave()
    {
        if (SelectionData.CurrentGameMode != GameMode.DeathBattle) return;
        if (!string.IsNullOrEmpty(SelectionData.characterPrefabUrl1)) return; // đã có dữ liệu

        var saveData = DeathBattleSaveSystem.LoadProgress();
        if (saveData == null)
        {
            Debug.LogWarning("[LoadCharacter] DeathBattle mode nhưng không có file save để khôi phục!");
            return;
        }

        SelectionData.DeathBattleData = saveData;
        SelectionData.characterPrefabUrl1 = saveData.playerCharacterUrl;
        SelectionData.characterImageUrl1 = saveData.playerCharacterImageUrl; // Re-hydrate P1 Avatar

        int idx = saveData.currentMatchIndex;
        if (idx < saveData.enemyCharacterUrls.Count)
        {
            SelectionData.characterPrefabUrl2 = saveData.enemyCharacterUrls[idx];
        }
        if (saveData.enemyCharacterImageUrls != null && idx < saveData.enemyCharacterImageUrls.Count)
        {
            SelectionData.characterImageUrl2 = saveData.enemyCharacterImageUrls[idx]; // Re-hydrate Bot Avatar
        }

        Debug.Log($"[LoadCharacter] Re-hydrated DeathBattle save: Match {idx + 1}/5");
    }

    void Start()
    {
        // Khi game bắt đầu, thực hiện đợt spawn đầu tiên
        SpawnStageCharacters();
    }


    /// <summary>
    /// Hàm Public được gọi khi bắt đầu Game và khi chuyển Round mới để sinh lại nhân vật
    /// </summary>
    public void SpawnStageCharacters()
    {
        FighterBase p1Fighter = null;
        FighterBase p2Fighter = null;

        // --- BƯỚC 1: SPAWN CÁC NHÂN VẬT RA TRƯỚC ---
        FighterBase spawnedFighter1 = null;
        FighterBase spawnedFighter2 = null;

        // Sinh nhân vật thứ nhất tại vị trí p1
        if (p1 != null && !string.IsNullOrEmpty(SelectionData.characterPrefabUrl1))
        {
            spawnedFighter1 = SpawnPlayer(SelectionData.characterPrefabUrl1, p1);
            if (spawnedFighter1 != null)
            {
                spawnedFighter1.InitializePlayer(1);
            }
        }

        // Sinh nhân vật thứ hai tại vị trí p2
        if (p2 != null && !string.IsNullOrEmpty(SelectionData.characterPrefabUrl2))
        {
            spawnedFighter2 = SpawnPlayer(SelectionData.characterPrefabUrl2, p2);
            if (spawnedFighter2 != null)
            {
                spawnedFighter2.InitializePlayer(2);
                if (SelectionData.CurrentGameMode == GameMode.DeathBattle)
                {
                    FighterBotAI botAI = spawnedFighter2.gameObject.AddComponent<FighterBotAI>();
                    botAI.Initialize(spawnedFighter2);
                }
            }
        }

        // --- BƯỚC 2: PHÂN LOẠI THEO PLAYER NUMBER ĐỂ KHỚP UI & MATCH MANAGER ---
        ConfigurePlayerByNumber(spawnedFighter1, ref p1Fighter, ref p2Fighter);
        ConfigurePlayerByNumber(spawnedFighter2, ref p1Fighter, ref p2Fighter);

        // --- BƯỚC 3: KẾT NỐI SANG MATCH MANAGER ---
        if (matchManager != null)
        {
            matchManager.SetPlayers(p1Fighter, p2Fighter);
        }
        else if (MatchManager.Instance != null)
        {
            MatchManager.Instance.SetPlayers(p1Fighter, p2Fighter);
        }
    }

    private void ConfigurePlayerByNumber(FighterBase fighter, ref FighterBase p1Fighter, ref FighterBase p2Fighter)
    {
        if (fighter == null) return;

        if (fighter.playerNumber == 1)
        {
            p1Fighter = fighter;
            p1Fighter.gameObject.name = "Player1_Character";

            if (p1UiMonitor != null)
            {
                p1UiMonitor.Initialize(p1Fighter);
                Debug.Log("Player 1 loaded và gắn vào UI thành công!");
            }
        }
        else if (fighter.playerNumber == 2)
        {
            p2Fighter = fighter;
            p2Fighter.gameObject.name = "Player2_Character";

            if (p2UiMonitor != null)
            {
                p2UiMonitor.Initialize(p2Fighter);
                Debug.Log("Player 2 loaded và gắn vào UI thành công!");
            }
        }
        else
        {
            Debug.LogWarning($"Nhân vật {fighter.gameObject.name} có Player Number không hợp lệ: {fighter.playerNumber}");
        }
    }

    private FighterBase SpawnPlayer(string prefabUrl, GameObject spawnPoint)
    {
        // Thử load theo đường dẫn đầy đủ trước
        GameObject prefab = Resources.Load<GameObject>(prefabUrl);

        // Fallback: nếu path không tìm thấy, tìm bằng tên file (hỗ trợ cả build & editor)
        if (prefab == null)
        {
            string nameOnly = System.IO.Path.GetFileName(prefabUrl);
            GameObject[] allPrefabs = Resources.LoadAll<GameObject>("");
            foreach (var p in allPrefabs)
            {
                if (p.name == nameOnly && p.GetComponent<FighterBase>() != null)
                {
                    prefab = p;
                    Debug.LogWarning($"[LoadCharacter] Dùng fallback tìm prefab '{nameOnly}' (path gốc '{prefabUrl}' thất bại).");
                    break;
                }
            }
        }

        if (prefab != null)
        {
            GameObject newCharacter = Instantiate(prefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
            newCharacter.transform.SetParent(spawnPoint.transform);

            FighterBase fighterScript = newCharacter.GetComponent<FighterBase>();
            if (fighterScript == null)
                Debug.LogError($"Prefab tại '{prefabUrl}' thiếu script FighterBase!");

            return fighterScript;
        }
        else
        {
            Debug.LogError($"[LoadCharacter] Không tìm thấy Prefab nào tại Resources/{prefabUrl} và không có fallback phù hợp.");
            return null;
        }
    }

}