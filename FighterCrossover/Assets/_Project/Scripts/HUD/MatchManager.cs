using System.Collections;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
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

    // SỬA LỖI: Hàm Start mặc định của Unity KHÔNG được chứa tham số
    void Start()
    {
        IsMatchStarted = false;
        IsMatchEnded = false;

        if (letterK != null) letterK.SetActive(false);
        if (letterDot != null) letterDot.SetActive(false);
        if (letterO != null) letterO.SetActive(false);

        StartCoroutine(StartMatchRoutine());
    }

    private void Update()
    {
        // Nếu mà player1 hoặc 2 hết máu thì out game.

        // Nếu hết giờ.
        if (timerScript.IsEnd())
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

        // Hiện chữ GO!
        letterGo.SetActive(true);
        IsMatchStarted = true;

        if (timerScript != null) timerScript.enabled = true; // Hoặc timerScript.RunTimer() tùy bạn viết bên SpriteTimer

        // Chờ 1 giây rồi ẩn chữ GO đi
        yield return new WaitForSeconds(1f);
        letterGo.SetActive(false);

        timerScript.RunTimer();
    }

    public void EndMatch()
    {
        StartCoroutine(EndMatchRoutine());
        this.enabled = false;
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