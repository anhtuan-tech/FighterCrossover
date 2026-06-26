using UnityEngine;

public class LoadCharacter : MonoBehaviour
{
    [Header("--- Spawn Points ---")]
    public GameObject p1;
    public GameObject p2;

    [Header("--- UI Monitors ---")]
    public FighterStatMonitor p1UiMonitor;
    public FighterStatMonitor p2UiMonitor;

    [Header("--- Match Manager ---")]
    public MatchManager matchManager; // Kéo thả MatchManager vào đây

    void Start()
    {
        FighterBase p1Fighter = null;
        FighterBase p2Fighter = null;

        // 1. Tải và thiết lập cho Player 1
        if (p1 != null && !string.IsNullOrEmpty(SelectionData.characterPrefabUrl1))
        {
            p1Fighter = SpawnPlayer(SelectionData.characterPrefabUrl1, p1, "Player1");
            if (p1Fighter != null && p1UiMonitor != null) p1UiMonitor.Initialize(p1Fighter);
        }

        // 2. Tải và thiết lập cho Player 2
        if (p2 != null && !string.IsNullOrEmpty(SelectionData.characterPrefabUrl2))
        {
            p2Fighter = SpawnPlayer(SelectionData.characterPrefabUrl2, p2, "Player2");
            if (p2Fighter != null && p2UiMonitor != null) p2UiMonitor.Initialize(p2Fighter);
        }

        // KẾT NỐI SANG MATCH MANAGER
        if (matchManager != null)
        {
            matchManager.SetPlayers(p1Fighter, p2Fighter);
        }
    }

    private FighterBase SpawnPlayer(string prefabUrl, GameObject spawnPoint, string playerName)
    {
        GameObject prefab = Resources.Load<GameObject>(prefabUrl);
        if (prefab != null)
        {
            GameObject newPlayer = Instantiate(prefab, spawnPoint.transform.position, spawnPoint.transform.rotation);
            if (spawnPoint.transform.parent != null) newPlayer.transform.SetParent(spawnPoint.transform.parent);
            newPlayer.name = playerName;

            FighterBase fighterScript = newPlayer.GetComponent<FighterBase>();
            Destroy(spawnPoint);
            return fighterScript;
        }
        return null;
    }
}