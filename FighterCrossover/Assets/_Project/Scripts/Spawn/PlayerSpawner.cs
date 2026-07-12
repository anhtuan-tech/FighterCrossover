using UnityEngine;

public class PlayerSpawner : MonoBehaviour
{
    [Header("--- THIẾT LẬP VỊ TRÍ (SPAWN POINTS) ---")]
    public Transform player1SpawnPoint;
    public Transform player2SpawnPoint;
    public Transform support1SpawnPoint;
    public Transform support2SpawnPoint;

    void Start()
    {
        SpawnPlayers();
    }

    void SpawnPlayers()
    {
        // 1. Load và Spawn Player 1
        if (!string.IsNullOrEmpty(SelectionData.characterPrefabUrl1))
        {
            GameObject p1Prefab = Resources.Load<GameObject>(SelectionData.characterPrefabUrl1);
            if (p1Prefab != null)
            {
                Instantiate(p1Prefab, player1SpawnPoint.position, player1SpawnPoint.rotation);
            }
            else
            {
                Debug.LogError("Không tìm thấy Prefab Player 1 tại: Resources/" + SelectionData.characterPrefabUrl1);
            }
        }

        // 2. Load và Spawn Player 2 (Bỏ qua nếu đang ở chế độ Training và không có Player 2)
        if (!string.IsNullOrEmpty(SelectionData.characterPrefabUrl2))
        {
            GameObject p2Prefab = Resources.Load<GameObject>(SelectionData.characterPrefabUrl2);
            if (p2Prefab != null)
            {
                Instantiate(p2Prefab, player2SpawnPoint.position, player2SpawnPoint.rotation);
            }
            else
            {
                Debug.LogError("Không tìm thấy Prefab Player 2 tại: Resources/" + SelectionData.characterPrefabUrl2);
            }
        }

        // 3. Load và Spawn Support 1
        if (!string.IsNullOrEmpty(SelectionData.supportPrefabUrl1) && support1SpawnPoint != null)
        {
            GameObject s1Prefab = Resources.Load<GameObject>(SelectionData.supportPrefabUrl1);
            if (s1Prefab != null) Instantiate(s1Prefab, support1SpawnPoint.position, support1SpawnPoint.rotation);
        }

        // 4. Load và Spawn Support 2
        if (!string.IsNullOrEmpty(SelectionData.supportPrefabUrl2) && support2SpawnPoint != null)
        {
            GameObject s2Prefab = Resources.Load<GameObject>(SelectionData.supportPrefabUrl2);
            if (s2Prefab != null) Instantiate(s2Prefab, support2SpawnPoint.position, support2SpawnPoint.rotation);
        }
    }
}