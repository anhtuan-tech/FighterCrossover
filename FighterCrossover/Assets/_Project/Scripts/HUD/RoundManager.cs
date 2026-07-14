using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement; // THÊM: Thư viện quản lý chuyển Scene

public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    [Header("--- Round TMPro UI GameObjects ---")]
    [Tooltip("Kéo Object chứa chữ ROUND 1 (TextMeshPro) vào đây")]
    public GameObject round1Object;
    [Tooltip("Kéo Object chứa chữ ROUND 2 (TextMeshPro) vào đây")]
    public GameObject round2Object;
    [Tooltip("Kéo Object chứa chữ ROUND 3 / FINAL ROUND (TextMeshPro) vào đây")]
    public GameObject round3Object;

    [Header("--- End Game UI ---")]
    public GameObject winPanel;
    [Tooltip("Kéo Component TextMeshPro hiển thị thông báo Winner vào đây")]
    public TextMeshProUGUI winText;

    [Header("--- Scene Configuration ---")]
    [Tooltip("Điền chính xác tên của Scene Menu để quay về")]
    public string menuSceneName = "MainMenu_Scene";

    // Lưu trữ điểm số thắng của mỗi Player (thắng 2 round là win)
    public int P1Wins { get; private set; } = 0;
    public int P2Wins { get; private set; } = 0;
    public int CurrentRound { get; private set; } = 1;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        DisableAllRoundObjects();

        // Khởi động Round 1 ngay khi vào Game
        StartNewRound();
    }

    public void StartNewRound()
    {
        StartCoroutine(StartRoundRoutine());
    }

    private IEnumerator StartRoundRoutine()
    {
        DisableAllRoundObjects();

        // 1. Tìm Object tương ứng với Round hiện tại để bật lên
        GameObject activeRoundUI = null;
        if (CurrentRound == 1) activeRoundUI = round1Object;
        else if (CurrentRound == 2) activeRoundUI = round2Object;
        else if (CurrentRound == 3) activeRoundUI = round3Object;

        if (activeRoundUI != null)
        {
            activeRoundUI.SetActive(true);
            yield return new WaitForSeconds(1.5f); // Hiển thị chữ Round trong 1.5 giây
            activeRoundUI.SetActive(false);
        }

        // 2. Gọi MatchManager bắt đầu đếm 3, 2, 1, GO và bắt đầu tính thời gian
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.StartNewRoundMatch();
        }
    }

    /// <summary>
    /// Được gọi từ MatchManager khi một Round kết thúc
    /// </summary>
    public void OnRoundEnd(FighterBase p1, FighterBase p2, bool isTimeOut)
    {
        int roundWinner = 0; // 0: Hòa, 1: Player 1, 2: Player 2

        if (isTimeOut)
        {
            float p1Hp = p1 != null ? p1.stats.currentHp : 0;
            float p2Hp = p2 != null ? p2.stats.currentHp : 0;

            Debug.Log($"[ROUND END] Hết giờ! HP P1: {p1Hp} | HP P2: {p2Hp}");

            if (p1Hp > p2Hp) roundWinner = 1;
            else if (p2Hp > p1Hp) roundWinner = 2;
            else roundWinner = 0;
        }
        else
        {
            float p1Hp = p1 != null ? p1.stats.currentHp : 0;
            float p2Hp = p2 != null ? p2.stats.currentHp : 0;

            if (p1Hp <= 0 && p2Hp > 0) roundWinner = 2;
            else if (p2Hp <= 0 && p1Hp > 0) roundWinner = 1;
        }

        // Cập nhật điểm số & Thiết lập nội dung thông báo Round
        string roundResultMessage = "ROUND TIE!";
        if (roundWinner == 1)
        {
            P1Wins++;
            roundResultMessage = "PLAYER 1 WINS ROUND!";
            Debug.Log($"[ROUND END] Player 1 thắng Round {CurrentRound}!");
        }
        else if (roundWinner == 2)
        {
            P2Wins++;
            roundResultMessage = "PLAYER 2 WINS ROUND!";
            Debug.Log($"[ROUND END] Player 2 thắng Round {CurrentRound}!");
        }

        // Kích hoạt Coroutine điều hướng kết quả
        StartCoroutine(HandleRoundEndRoutine(roundResultMessage, p1, p2));
    }

    /// <summary>
    /// Coroutine hiển thị chữ thông báo người thắng và chờ 3 giây trước khi xử lý tiếp hoặc về Menu
    /// </summary>
    private IEnumerator HandleRoundEndRoutine(string resultMessage, FighterBase p1, FighterBase p2)
    {
        // THẦY ĐỔI LOGIC: Kiểm tra kết thúc toàn trận (Thắng trước 2 trận HOẶC đã đấu xong trận thứ 3)
        if (P1Wins >= 2 || P2Wins >= 2 || CurrentRound >= 3)
        {
            string finalWinnerMessage = "MATCH TIE!"; // Mặc định nếu đấu xong 3 trận mà hòa

            if (P1Wins > P2Wins) finalWinnerMessage = "PLAYER 1 WINS THE MATCH!";
            else if (P2Wins > P1Wins) finalWinnerMessage = "PLAYER 2 WINS THE MATCH!";

            // 1. Hiện bảng kết quả Game Over chung cuộc
            if (winPanel != null) winPanel.SetActive(true);
            if (winText != null) winText.text = finalWinnerMessage;

            Debug.Log($"[GAME OVER] {finalWinnerMessage} -> Chuẩn bị về Menu.");

            // 2. Chờ 3 giây để người chơi nhìn rõ thông báo Win toàn trận
            yield return new WaitForSeconds(3f);

            // 3. Tiến hành load lại Menu Scene
            SceneManager.LoadScene(menuSceneName);
            yield break;
        }

        // --- NẾU CHƯA ĐỦ ĐIỀU KIỆN KẾT THÚC GAME TRÊN (Ví dụ: Tỉ số đang là 1-0 hoặc 1-1 ở Round 1, 2) ---
        if (winPanel != null) winPanel.SetActive(true);
        if (winText != null) winText.text = resultMessage;

        // Chờ 3 giây hiển thị thông báo thắng Round như cũ
        yield return new WaitForSeconds(3f);

        if (winPanel != null) winPanel.SetActive(false);

        // Tiến hành tăng số Round và Spawn lại nhân vật mới
        CurrentRound++;
        StartCoroutine(CleanAndSpawnRoutine(p1, p2));
    }

    /// <summary>
    /// Tiến hành xóa nhân vật cũ, gọi spawn nhân vật mới rồi mới chạy Round tiếp theo
    /// </summary>
    private IEnumerator CleanAndSpawnRoutine(FighterBase oldP1, FighterBase oldP2)
    {
        Debug.Log("[ROUND SYSTEM] Đang dọn dẹp nhân vật cũ...");

        if (oldP1 != null) Destroy(oldP1.gameObject);
        if (oldP2 != null) Destroy(oldP2.gameObject);

        yield return null;

        Debug.Log("[ROUND SYSTEM] Gọi LoadCharacter thực hiện spawn nhân vật mới...");

        if (LoadCharacter.Instance != null)
        {
            LoadCharacter.Instance.SpawnStageCharacters();
        }
        else
        {
            Debug.LogError("Không tìm thấy LoadCharacter Instance trong Scene!");
        }

        yield return new WaitForSeconds(0.1f);

        StartNewRound();
    }

    private void DisableAllRoundObjects()
    {
        if (round1Object != null) round1Object.SetActive(false);
        if (round2Object != null) round2Object.SetActive(false);
        if (round3Object != null) round3Object.SetActive(false);
    }
}