using UnityEngine;
using UnityEngine.UI; // Cần thêm thư viện này để làm việc với Slider

public class LoadCharacter : MonoBehaviour
{
    [Header("--- Spawn Points ---")]
    public GameObject p1;
    public GameObject p2;

    [Header("--- UI Elements Player 1 ---")]
    public Slider p1HealthSlider;
    public Slider p1ManaSlider;
    public Slider p1StaminaSlider;

    [Header("--- UI Elements Player 2 ---")]
    public Slider p2HealthSlider;
    public Slider p2ManaSlider;
    public Slider p2StaminaSlider;

    void Start()
    {
        // 1. Tải và thiết lập cho Player 1
        if (p1 != null && !string.IsNullOrEmpty(SelectionData.characterPrefabUrl1))
        {
            p1 = SpawnAndSetupPlayer(SelectionData.characterPrefabUrl1, p1, "Player1", p1HealthSlider, p1ManaSlider, p1StaminaSlider);
        }

        // 2. Tải và thiết lập cho Player 2
        if (p2 != null && !string.IsNullOrEmpty(SelectionData.characterPrefabUrl2))
        {
            p2 = SpawnAndSetupPlayer(SelectionData.characterPrefabUrl2, p2, "Player2", p2HealthSlider, p2ManaSlider, p2StaminaSlider);
        }
    }

    /// <summary>
    /// Hàm phụ giúp tối ưu code: Khởi tạo nhân vật và liên kết các thanh UI
    /// </summary>
    private GameObject SpawnAndSetupPlayer(string prefabUrl, GameObject spawnPoint, string playerName, Slider hp, Slider mp, Slider stamina)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabUrl);

        if (prefab != null)
        {
            // Khởi tạo nhân vật mới tại vị trí spawn point
            GameObject newPlayer = Instantiate(prefab, spawnPoint.transform.position, spawnPoint.transform.rotation);

            // Giữ lại cấu trúc cha-con nếu có
            if (spawnPoint.transform.parent != null)
                newPlayer.transform.SetParent(spawnPoint.transform.parent);

            newPlayer.name = playerName;

            // --- ĐÂY LÀ PHẦN KẾT NỐI UI VÀO CƠ CHẾ CỦA NHÂN VẬT ---
            // Giả sử trên Prefab của bạn có một Script tên là "PlayerStats" đảm nhận việc quản lý chỉ số.
            // Bạn hãy thay thế "PlayerStats" bằng tên script thực tế của bạn (ví dụ: CharacterController, PlayerHealth,...)
            PlayerStats stats = newPlayer.GetComponent<PlayerStats>();
            if (stats != null)
            {
                // Gọi hàm khởi tạo UI trên script của nhân vật
                stats.SetupUI(hp, mp, stamina);
            }
            else
            {
                Debug.LogWarning($"Prefab {playerName} không chứa script PlayerStats để nhận dữ liệu Slider!");
            }
            // ---------------------------------------------------

            // Xóa Object định vị cũ
            Destroy(spawnPoint);
            return newPlayer;
        }
        else
        {
            Debug.LogError($"Không tìm thấy Prefab tại địa chỉ: {prefabUrl}");
            return spawnPoint; // Trả về lại cái cũ nếu lỗi
        }
    }
}