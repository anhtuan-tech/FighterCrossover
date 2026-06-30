using UnityEngine;

public class LoadCharacter : MonoBehaviour
{
    [Header("--- Spawn Points (P1 / P2 Objects) ---")]
    public GameObject p1;
    public GameObject p2;

    [Header("--- UI Monitors ---")]
    public FighterStatMonitor p1UiMonitor;
    public FighterStatMonitor p2UiMonitor;

    [Header("--- Match Manager ---")]
    public MatchManager matchManager;

    void Start()
    {
        FighterBase p1Fighter = null;
        FighterBase p2Fighter = null;

        // --- BƯỚC 1: SPAWN CÁC NHÂN VẬT RA TRƯỚC ---
        FighterBase spawnedFighter1 = null;
        FighterBase spawnedFighter2 = null;

        // Sinh nhân vật thứ nhất (từ Slot 1 trong SelectionData) tại vị trí p1
        if (p1 != null && !string.IsNullOrEmpty(SelectionData.characterPrefabUrl1))
        {
            spawnedFighter1 = SpawnPlayer(SelectionData.characterPrefabUrl1, p1);
            if (spawnedFighter1 != null)
            {
                spawnedFighter1.InitializePlayer(1);
            }
        }

        // Sinh nhân vật thứ hai (từ Slot 2 trong SelectionData) tại vị trí p2
        if (p2 != null && !string.IsNullOrEmpty(SelectionData.characterPrefabUrl2))
        {
            spawnedFighter2 = SpawnPlayer(SelectionData.characterPrefabUrl2, p2);
            if (spawnedFighter2 != null)
            {
                spawnedFighter2.InitializePlayer(2);
            }
        }

        // --- BƯỚC 2: PHÂN LOẠI THEO PLAYER NUMBER ĐỂ KHỚP UI & MATCH MANAGER ---
        // Kiểm tra nhân vật thứ 1
        ConfigurePlayerByNumber(spawnedFighter1, ref p1Fighter, ref p2Fighter);

        // Kiểm tra nhân vật thứ 2
        ConfigurePlayerByNumber(spawnedFighter2, ref p1Fighter, ref p2Fighter);

        // --- BƯỚC 3: KẾT NỐI SANG MATCH MANAGER ---
        if (matchManager != null)
        {
            matchManager.SetPlayers(p1Fighter, p2Fighter);
        }
    }

    /// <summary>
    /// Hàm kiểm tra Player Number của nhân vật để gán đúng UI Monitor và lưu vào đúng biến p1/p2 Fighter
    /// </summary>
    private void ConfigurePlayerByNumber(FighterBase fighter, ref FighterBase p1Fighter, ref FighterBase p2Fighter)
    {
        if (fighter == null) return;

        // Giả sử biến "Player Number" trong ảnh của bạn tên là 'playerNumber' nằm trong FighterBase
        // Nếu nó nằm ở script khác, hãy thay đổi cách gọi cho đúng (ví dụ: fighter.GetComponent<IchigoSettings>().playerNumber)
        if (fighter.playerNumber == 1)
        {
            p1Fighter = fighter;
            p1Fighter.gameObject.name = "Player1_Character";

            if (p1UiMonitor != null)
            {
                p1UiMonitor.Initialize(p1Fighter);
                Debug.Log("Player 1 (Cấu hình từ Player Number 1) loaded thành công!");
            }
        }
        else if (fighter.playerNumber == 2)
        {
            p2Fighter = fighter;
            p2Fighter.gameObject.name = "Player2_Character";

            if (p2UiMonitor != null)
            {
                p2UiMonitor.Initialize(p2Fighter);
                Debug.Log("Player 2 (Cấu hình từ Player Number 2) loaded thành công!");
            }
        }
        else
        {
            Debug.LogWarning($"Nhân vật {fighter.gameObject.name} có Player Number không hợp lệ: {fighter.playerNumber}");
        }
    }

    /// <summary>
    /// Hàm sinh nhân vật cơ bản từ Resources
    /// </summary>
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
            Debug.LogError($"Không tìm thấy Prefab ở đường dẫn: Resources/{prefabUrl}. Hãy kiểm tra lại cấu trúc thư mục!");
            return null;
        }
    }
}