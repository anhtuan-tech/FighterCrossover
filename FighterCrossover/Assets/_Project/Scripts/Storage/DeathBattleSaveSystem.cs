using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class DeathBattleSaveData
{
    public string playerCharacterUrl;
    public string playerCharacterImageUrl; // Thêm
    public List<string> enemyCharacterUrls = new List<string>();
    public List<string> enemyCharacterImageUrls = new List<string>(); // Thêm
    public List<string> mapNames = new List<string>();
    public int currentMatchIndex = 0; // Trận hiện tại (0-4 tương ứng vòng 1-5)
    public bool isComplete = false;   // Đã hoàn thành cả 5 vòng chưa
}

public static class DeathBattleSaveSystem
{
    private static string SaveFilePath => Path.Combine(Application.persistentDataPath, "DeathBattleSave.json");

    // Lưu reference hiện tại để emergency-save khi quit
    public static DeathBattleSaveData Current { get; set; }

    /// <summary>Ghi dữ liệu ra file JSON (an toàn với try/catch)</summary>
    public static void SaveProgress(DeathBattleSaveData data)
    {
        if (data == null) return;
        Current = data;
        try
        {
            string json = JsonUtility.ToJson(data, true);
            File.WriteAllText(SaveFilePath, json);
            Debug.Log("[DeathBattle] Đã lưu tiến trình: " + SaveFilePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DeathBattle] Lỗi khi lưu: " + e.Message);
        }
    }

    /// <summary>Đọc file save. Trả về null nếu chưa có hoặc file bị lỗi.</summary>
    public static DeathBattleSaveData LoadProgress()
    {
        try
        {
            if (File.Exists(SaveFilePath))
            {
                string json = File.ReadAllText(SaveFilePath);
                var data = JsonUtility.FromJson<DeathBattleSaveData>(json);
                Current = data;
                return data;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DeathBattle] Lỗi khi đọc save: " + e.Message);
        }
        return null;
    }

    /// <summary>Xoá file save (dùng sau khi hoàn thành hoặc thua)</summary>
    public static void DeleteProgress()
    {
        try
        {
            if (File.Exists(SaveFilePath))
                File.Delete(SaveFilePath);
        }
        catch (System.Exception e)
        {
            Debug.LogError("[DeathBattle] Lỗi khi xoá save: " + e.Message);
        }
        Current = null;
    }
}