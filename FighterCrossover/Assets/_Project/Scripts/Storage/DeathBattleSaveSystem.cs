using UnityEngine;
using System.Collections.Generic;
using System.IO;

[System.Serializable]
public class DeathBattleSaveData
{
    public string playerCharacterUrl;
    public List<string> enemyCharacterUrls = new List<string>();
    public List<string> mapNames = new List<string>();
    public int currentMatchIndex = 0; // Đang đánh tới trận thứ mấy (0 -> 4)
}

public static class DeathBattleSaveSystem
{
    private static string saveFilePath = Application.persistentDataPath + "/DeathBattleSave.json";

    public static void SaveProgress(DeathBattleSaveData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(saveFilePath, json);
        Debug.Log("Đã lưu tiến trình Death Battle tại: " + saveFilePath);
    }

    public static DeathBattleSaveData LoadProgress()
    {
        if (File.Exists(saveFilePath))
        {
            string json = File.ReadAllText(saveFilePath);
            return JsonUtility.FromJson<DeathBattleSaveData>(json);
        }
        return null; // Trả về null nếu chưa có file save
    }

    public static void DeleteProgress()
    {
        if (File.Exists(saveFilePath))
        {
            File.Delete(saveFilePath);
        }
    }
}