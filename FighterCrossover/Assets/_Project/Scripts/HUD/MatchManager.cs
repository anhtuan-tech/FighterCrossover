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
        IsMatchStarted = false;
        IsMatchEnded = false;

        if (letterK != null) letterK.SetActive(false);
        if (letterDot != null) letterDot.SetActive(false);
        if (letterO != null) letterO.SetActive(false);

        StartCoroutine(StartMatchRoutine());
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
        // Nếu trận đấu chưa bắt đầu hoặc đã kết thúc rồi thì không check nữa
        if (!IsMatchStarted || IsMatchEnded) return;

        // XỬ LÝ: Nếu player1 hoặc 2 hết máu thì kết thúc trận đấu
        if (player1 != null && player1.stats.currentHp <= 0)
        {
            Debug.Log("[MATCH] Player 1 hết máu -> Kết thúc trận!");
            EndMatch();
        }
        else if (player2 != null && player2.stats.currentHp <= 0)
        {
            Debug.Log("[MATCH] Player 2 hết máu -> Kết thúc trận!");
            EndMatch();
        }

        // Nếu hết giờ
        if (timerScript != null && timerScript.IsEnd())
        {
            EndMatch();
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
        if (IsMatchEnded) return; // Tránh việc gọi trùng lặp nhiều lần
        IsMatchEnded = true;

        StartCoroutine(EndMatchRoutine());
        this.enabled = false; // Tắt Update đi
    }

    IEnumerator EndMatchRoutine()
    {
        yield return new WaitForSeconds(0.1f);
        Time.timeScale = 0.3f;

        if (letterK != null) letterK.SetActive(true); yield return new WaitForSecondsRealtime(0.5f);
        if (letterDot != null) letterDot.SetActive(true); yield return new WaitForSecondsRealtime(0.5f);
        if (letterO != null) letterO.SetActive(true);

        yield return new WaitForSecondsRealtime(2.5f);
        Time.timeScale = 1f;

        letterK.SetActive(false);
        letterDot.SetActive(false);
        letterO.SetActive(false);

        Debug.Log("Kết thúc hiệu ứng K.O. -> Chuyển sang xử lý Winner!");
    }
}