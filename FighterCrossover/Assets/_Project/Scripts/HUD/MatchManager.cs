using System.Collections;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
    // THÊM: Tạo Instance để các script khác (như UI Monitor) có thể gọi trực tiếp
    public static MatchManager Instance { get; private set; }

    [Header("--- Start HUD ---")]
    public GameObject letter3;
    public GameObject letter2;
    public GameObject letter1;
    public GameObject letterGo;

    [Header("--- End HUD ---")]
    public GameObject letterK;
    public GameObject letterDot;
    public GameObject letterO;

    [Header("--- Timer Script ---")]
    public SpriteTimer timerScript;

    public static bool IsMatchStarted { get; private set; } = false;
    public static bool IsMatchEnded { get; private set; } = false;

    // THÊM: Lưu trữ tham chiếu tới 2 nhân vật để check máu trong Update
    private FighterBase player1;
    private FighterBase player2;

    private void Awake()
    {
        // Khởi tạo Instance
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        DisableAllMatchHUD();
        //IsMatchStarted = false;
        //IsMatchEnded = false;

        //if (letterK != null) letterK.SetActive(false);
        //if (letterDot != null) letterDot.SetActive(false);
        //if (letterO != null) letterO.SetActive(false);

        //StartCoroutine(StartMatchRoutine());
    }

    /// <summary>
    /// Hàm này được RoundManager gọi để khởi động đếm ngược cho một Round mới
    /// </summary>
    public void StartNewRoundMatch()
    {
        this.enabled = true; // Bật lại Update để theo dõi máu
        IsMatchStarted = false;
        IsMatchEnded = false;

        DisableAllMatchHUD();
        StartCoroutine(StartMatchRoutine());
    }

    private void DisableAllMatchHUD()
    {
        if (letter3 != null) letter3.SetActive(false);
        if (letter2 != null) letter2.SetActive(false);
        if (letter1 != null) letter1.SetActive(false);
        if (letterGo != null) letterGo.SetActive(false);
        if (letterK != null) letterK.SetActive(false);
        if (letterDot != null) letterDot.SetActive(false);
        if (letterO != null) letterO.SetActive(false);
    }

    /// <summary>
    /// Hàm này để LoadCharacter truyền tham chiếu nhân vật sang sau khi spawn xong
    /// </summary>
    public void SetPlayers(FighterBase p1, FighterBase p2)
    {
        player1 = p1;
        player2 = p2;
    }

    private void Update()
    {
        if (!IsMatchStarted || IsMatchEnded) return;

        // 1. Kiểm tra hết máu
        if (player1 != null && player1.stats.currentHp <= 0)
        {
            Debug.Log("[MATCH] Player 1 hết máu!");
            EndMatch(false);
        }
        else if (player2 != null && player2.stats.currentHp <= 0)
        {
            Debug.Log("[MATCH] Player 2 hết máu!");
            EndMatch(false);
        }

        // 2. Kiểm tra hết thời gian
        if (timerScript != null && timerScript.IsEnd())
        {
            Debug.Log("[MATCH] Hết giờ!");
            EndMatch(true); // Gửi tham số true báo hiệu hết giờ
        }
    }

    IEnumerator StartMatchRoutine()
    {
        yield return new WaitForSeconds(0.5f);
        letter3.SetActive(true); yield return new WaitForSeconds(1f); letter3.SetActive(false);
        letter2.SetActive(true); yield return new WaitForSeconds(1f); letter2.SetActive(false);
        letter1.SetActive(true); yield return new WaitForSeconds(1f); letter1.SetActive(false);

        letterGo.SetActive(true);
        IsMatchStarted = true;

        if (timerScript != null) timerScript.enabled = true;

        yield return new WaitForSeconds(1f);
        letterGo.SetActive(false);

        if (timerScript != null) timerScript.RunTimer();
    }

    public void EndMatch()
    {
        EndMatch(false);
    }

    public void EndMatch(bool isTimeOut)
    {
        if (IsMatchEnded) return;
        IsMatchEnded = true;

        StartCoroutine(EndMatchRoutine(isTimeOut));
        this.enabled = false; // Tắt Update
    }

    IEnumerator EndMatchRoutine(bool isTimeOut)
    {
        yield return new WaitForSeconds(0.1f);
        Time.timeScale = 0.3f;

        // Hiển thị hiệu ứng K.O.
        if (letterK != null) letterK.SetActive(true); yield return new WaitForSecondsRealtime(0.5f);
        if (letterDot != null) letterDot.SetActive(true); yield return new WaitForSecondsRealtime(0.5f);
        if (letterO != null) letterO.SetActive(true);

        yield return new WaitForSecondsRealtime(2.0f);
        Time.timeScale = 1f;

        letterK.SetActive(false);
        letterDot.SetActive(false);
        letterO.SetActive(false);

        Debug.Log("Kết thúc hiệu ứng K.O. -> Báo cáo kết quả lên RoundManager!");

        // Báo kết quả round này về cho RoundManager xử lý điểm số
        if (RoundManager.Instance != null)
        {
            RoundManager.Instance.OnRoundEnd(player1, player2, isTimeOut);
        }
    }
}