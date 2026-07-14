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
        GameObject prefab = Resources.Load<GameObject>(prefabUrl);

        if (prefab != null)
        {
            GameObject newCharacter = Instantiate(prefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
            newCharacter.transform.SetParent(spawnPoint.transform);

            FighterBase fighterScript = newCharacter.GetComponent<FighterBase>();

            if (fighterScript == null)
            {
                Debug.LogError($"Prefab tại '{prefabUrl}' thiếu script FighterBase!");
            }

            return fighterScript;
        }
        else
        {
            Debug.LogError($"Không tìm thấy Prefab ở đường dẫn: Resources/{prefabUrl}");
            return null;
        }
    }
}