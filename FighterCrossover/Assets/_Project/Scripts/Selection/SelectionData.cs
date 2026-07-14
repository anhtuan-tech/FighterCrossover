public enum GameMode { Training, DeathBattle, PvP }

public static class SelectionData
{
    public static string hudUiPrefabUrl;
    public static GameMode CurrentGameMode = GameMode.DeathBattle; // Test Mode

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

    // --- THÊM DỮ LIỆU RIÊNG CHO DEATH BATTLE ---
    public static DeathBattleSaveData DeathBattleData = new DeathBattleSaveData();
}