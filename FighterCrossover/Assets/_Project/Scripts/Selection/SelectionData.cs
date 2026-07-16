public enum GameMode { Training, DeathBattle, PvP }

public static class SelectionData
{
    public static string hudUiPrefabUrl;

    // Lưu GameMode vào PlayerPrefs để không bị mất khi chuyển scene
    private static GameMode _currentGameMode = GameMode.DeathBattle;
    public static GameMode CurrentGameMode
    {
        get
        {
            // Phục hồi từ PlayerPrefs nếu chưa được set trong session này
            if (!_gameModeLoaded)
            {
                _currentGameMode = (GameMode)UnityEngine.PlayerPrefs.GetInt("CurrentGameMode", (int)GameMode.DeathBattle);
                _gameModeLoaded = true;
            }
            return _currentGameMode;
        }
        set
        {
            _currentGameMode = value;
            _gameModeLoaded = true;
            UnityEngine.PlayerPrefs.SetInt("CurrentGameMode", (int)value);
            UnityEngine.PlayerPrefs.Save();
        }
    }
    private static bool _gameModeLoaded = false;

    // Player 1
    public static string characterImageUrl1 { get; set; }
    public static string characterPrefabUrl1 { get; set; }
    public static string supportImageUrl1 { get; set; }
    public static string supportPrefabUrl1 { get; set; }

    // Player 2 (Dùng cho PvP thông thường)
    public static string characterImageUrl2 { get; set; }
    public static string characterPrefabUrl2 { get; set; }
    public static string supportImageUrl2 { get; set; }
    public static string supportPrefabUrl2 { get; set; }

    public static string background { get; set; }
    public static string mapLocation { get; set; }

    // --- DỮ LIỆU RIÊNG CHO DEATH BATTLE ---
    public static DeathBattleSaveData DeathBattleData = new DeathBattleSaveData();
}