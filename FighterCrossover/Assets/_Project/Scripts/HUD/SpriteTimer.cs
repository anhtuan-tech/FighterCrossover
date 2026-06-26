using System.IO;
using UnityEngine;
using UnityEngine.UI;
using AnimeFighter.UI;

public class SpriteTimer : MonoBehaviour
{
    [Header("--- Công tắc chế độ Vô Hạn ---")]
    [Tooltip("Tích chọn ô này để chơi không giới hạn thời gian (Hiện ký hiệu vô cực)")]
    public bool isInfinite = false;

    [Header("--- Ô chứa ảnh Vô Cực ---")]
    public Image infinityImage;

    [Header("--- Mảng chứa 10 ảnh từ 0 đến 9 ---")]
    public Sprite[] numberSprites;

    [Header("--- Các ô Image hiển thị Giây ---")]
    public Image sec1Image;
    public Image sec2Image;


    [Header("--- Cấu hình thời gian (Nếu không bật Vô Hạn) ---")]
    public float timeRemaining = 99f;
    private bool isTimerRunning = false;
    private bool gameEnd = false;

    void Awake()
    {
        // ==========================================================
        // TỰ ĐỌC FILE JSON VÀ SET UP THỜI GIAN TRẬN ĐẤU TẠI ĐÂY
        // ==========================================================
        string saveFilePath = Path.Combine(Application.persistentDataPath, "settings.json");

        if (File.Exists(saveFilePath))
        {
            try
            {
                string jsonText = File.ReadAllText(saveFilePath);

                // Ép kiểu chuỗi JSON trực tiếp vào Class gốc của bạn
                GameSettingsData data = JsonUtility.FromJson<GameSettingsData>(jsonText);

                if (data != null)
                {
                    if (data.matchTime == 999)
                    {
                        isInfinite = true;
                    }
                    else
                    {
                        isInfinite = false;
                        timeRemaining = data.matchTime; // Gán 60 hoặc 90 giây vào biến chạy của đồng hồ
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SpriteTimer] Lỗi khi đọc dữ liệu cài đặt: {e.Message}");
            }
        }
    }

    void Start()
    {
        if (isInfinite)
        {
            if (infinityImage != null) infinityImage.gameObject.SetActive(true);
            sec1Image.gameObject.SetActive(false);
            sec2Image.gameObject.SetActive(false);
        }
        else
        {
            if (infinityImage != null) infinityImage.gameObject.SetActive(false);

            UpdateSecondsUI(timeRemaining);
        }
    }

    void Update()
    {
        if (isInfinite) return;

        if (isTimerRunning && timeRemaining > 0)
        {
            timeRemaining -= Time.deltaTime;
            UpdateSecondsUI(timeRemaining);
        }
        else if (timeRemaining <= 0)
        {
            timeRemaining = 0;
            isTimerRunning = false;
            gameEnd = true;
            UpdateSecondsUI(0);
        }
    }

    public void UpdateSecondsUI(float timeToDisplay)
    {
        int totalSeconds = Mathf.CeilToInt(timeToDisplay);
        if (totalSeconds > 99) totalSeconds = 99;

        int tens = (totalSeconds / 10) % 10;
        int ones = totalSeconds % 10;

        if (sec1Image != null) sec1Image.sprite = numberSprites[ones];
        if (sec2Image != null) sec2Image.sprite = numberSprites[tens];
    }

    public void RunTimer()
    {
        isTimerRunning = true;
    }

    public bool IsEnd()
    {
        return gameEnd;
    }
}