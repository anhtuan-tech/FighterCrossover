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

        // 1. Tải và thiết lập cho Player 1
        if (p1 != null && !string.IsNullOrEmpty(SelectionData.characterPrefabUrl1))
        {
            p1Fighter = SpawnPlayer(SelectionData.characterPrefabUrl1, p1, "Player1_Character");
            if (p1Fighter != null && p1UiMonitor != null)
            {
                p1UiMonitor.Initialize(p1Fighter);
                Debug.Log("Player 1 loaded thành công!");
            }
        }

        // 2. Tải và thiết lập cho Player 2
        if (p2 != null && !string.IsNullOrEmpty(SelectionData.characterPrefabUrl2))
        {
            p2Fighter = SpawnPlayer(SelectionData.characterPrefabUrl2, p2, "Player2_Character");
            if (p2Fighter != null && p2UiMonitor != null)
            {
                p2UiMonitor.Initialize(p2Fighter);
                Debug.Log("Player 2 loaded thành công!");
            }
        }

        // KẾT NỐI SANG MATCH MANAGER
        if (matchManager != null)
        {
            matchManager.SetPlayers(p1Fighter, p2Fighter);
        }
    }

    private FighterBase SpawnPlayer(string prefabUrl, GameObject spawnPoint, string characterName)
    {
        // QUAN TRỌNG: Hãy chắc chắn prefabUrl đã tuân theo quy tắc của thư mục Resources
        GameObject prefab = Resources.Load<GameObject>(prefabUrl);

        if (prefab != null)
        {
            // Sinh nhân vật tại vị trí và góc quay của spawnPoint (P1 hoặc P2)
            GameObject newCharacter = Instantiate(prefab, spawnPoint.transform.position, spawnPoint.transform.rotation);

            // Đặt nhân vật làm con của P1/P2 để quản lý gọn gàng (Không làm ảnh hưởng đến Monitor cũ)
            newCharacter.transform.SetParent(spawnPoint.transform);
            newCharacter.name = characterName;

            // Lấy script tính năng của nhân vật
            FighterBase fighterScript = newCharacter.GetComponent<FighterBase>();

            if (fighterScript == null)
            {
                Debug.LogError($"Prefab tại '{prefabUrl}' thiếu script FighterBase!");
            }

            // KHÔNG Destroy(spawnPoint) nữa để giữ lại cấu trúc và UI Monitor bên trong nó
            return fighterScript;
        }
        else
        {
            Debug.LogError($"Không tìm thấy Prefab ở đường dẫn: Resources/{prefabUrl}. Hãy kiểm tra lại cấu trúc thư mục!");
            return null;
        }
    }
}