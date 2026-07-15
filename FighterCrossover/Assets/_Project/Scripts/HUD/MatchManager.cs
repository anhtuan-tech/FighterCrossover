using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

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
    private GameObject escPopupInstance;

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
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ToggleEscPopup();
        }

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

        if (timerScript != null) timerScript.StartTimerWithTime();
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
        if (isTimeOut)
        {
            yield return new WaitForSeconds(0.1f);
        }
        else
        {
            // Chờ 1.5 giây ở tốc độ bình thường để nhân vật kịp chạy hoàn chỉnh hoạt ảnh ngã xuống (Die)
            yield return new WaitForSeconds(1.5f);
        }

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

    private void ToggleEscPopup()
    {
        if (escPopupInstance == null)
        {
            CreateEscPopup();
        }
        else
        {
            bool nextActive = !escPopupInstance.activeSelf;
            escPopupInstance.SetActive(nextActive);
            Time.timeScale = nextActive ? 0f : 1f;
        }
    }

    private void CreateEscPopup()
    {
        // 0. Check for EventSystem, create one if it is missing
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
            Debug.Log("[ESC POPUP] Created dynamic EventSystem and InputSystemUIInputModule.");
        }

        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = FindFirstObjectByType<Canvas>();
        }
        if (canvas == null)
        {
            Debug.LogError("[ESC POPUP] Không tìm thấy Canvas nào để hiển thị popup!");
            return;
        }

        // 1. Panel Container (Background overlay)
        escPopupInstance = new GameObject("EscPausePopup");
        escPopupInstance.transform.SetParent(canvas.transform, false);

        RectTransform popRect = escPopupInstance.AddComponent<RectTransform>();
        popRect.anchorMin = Vector2.zero;
        popRect.anchorMax = Vector2.one;
        popRect.pivot = new Vector2(0.5f, 0.5f);
        popRect.anchoredPosition = Vector2.zero;
        popRect.sizeDelta = Vector2.zero;

        Image bgImage = escPopupInstance.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.7f); // Semi-transparent black background

        // 2. Dialog Panel Box
        GameObject dialogBox = new GameObject("DialogBox");
        dialogBox.transform.SetParent(escPopupInstance.transform, false);

        RectTransform dialogRect = dialogBox.AddComponent<RectTransform>();
        dialogRect.anchorMin = new Vector2(0.5f, 0.5f);
        dialogRect.anchorMax = new Vector2(0.5f, 0.5f);
        dialogRect.pivot = new Vector2(0.5f, 0.5f);
        dialogRect.anchoredPosition = Vector2.zero;
        dialogRect.sizeDelta = new Vector2(400f, 200f);

        Image dialogBg = dialogBox.AddComponent<Image>();
        dialogBg.color = new Color(0.12f, 0.12f, 0.12f, 0.95f); // Dark grey

        // 3. Question Text
        GameObject textObj = new GameObject("PopupText");
        textObj.transform.SetParent(dialogBox.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.05f, 0.5f);
        textRect.anchorMax = new Vector2(0.95f, 0.9f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = new Vector2(0f, 15f);
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = "Do you want to go back to the Menu?";
        tmpText.fontSize = 20;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.Center;
        tmpText.raycastTarget = false; // Prevent blocking click raycasts

        // 4. YES Button
        GameObject yesBtnObj = new GameObject("YesButton");
        yesBtnObj.transform.SetParent(dialogBox.transform, false);

        RectTransform yesRect = yesBtnObj.AddComponent<RectTransform>();
        yesRect.anchorMin = new Vector2(0.15f, 0.15f);
        yesRect.anchorMax = new Vector2(0.45f, 0.4f);
        yesRect.pivot = new Vector2(0.5f, 0.5f);
        yesRect.anchoredPosition = Vector2.zero;
        yesRect.sizeDelta = Vector2.zero;

        Image yesImg = yesBtnObj.AddComponent<Image>();
        yesImg.color = new Color(0.15f, 0.6f, 0.15f, 1f); // Green button for Yes/Exit

        Button yesBtn = yesBtnObj.AddComponent<Button>();
        yesBtn.onClick.AddListener(OnYesClicked);

        // YES Button Text
        GameObject yesTextObj = new GameObject("YesText");
        yesTextObj.transform.SetParent(yesBtnObj.transform, false);

        RectTransform yesTextRect = yesTextObj.AddComponent<RectTransform>();
        yesTextRect.anchorMin = Vector2.zero;
        yesTextRect.anchorMax = Vector2.one;
        yesTextRect.pivot = new Vector2(0.5f, 0.5f);
        yesTextRect.anchoredPosition = Vector2.zero;
        yesTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI yesTmp = yesTextObj.AddComponent<TextMeshProUGUI>();
        yesTmp.text = "YES";
        yesTmp.fontSize = 16;
        yesTmp.color = Color.white;
        yesTmp.alignment = TextAlignmentOptions.Center;
        yesTmp.raycastTarget = false; // Prevent blocking click raycasts

        // 5. NO Button
        GameObject noBtnObj = new GameObject("NoButton");
        noBtnObj.transform.SetParent(dialogBox.transform, false);

        RectTransform noRect = noBtnObj.AddComponent<RectTransform>();
        noRect.anchorMin = new Vector2(0.55f, 0.15f);
        noRect.anchorMax = new Vector2(0.85f, 0.4f);
        noRect.pivot = new Vector2(0.5f, 0.5f);
        noRect.anchoredPosition = Vector2.zero;
        noRect.sizeDelta = Vector2.zero;

        Image noImg = noBtnObj.AddComponent<Image>();
        noImg.color = new Color(0.75f, 0.15f, 0.15f, 1f); // Red button for No/Continue

        Button noBtn = noBtnObj.AddComponent<Button>();
        noBtn.onClick.AddListener(OnNoClicked);

        // NO Button Text
        GameObject noTextObj = new GameObject("NoText");
        noTextObj.transform.SetParent(noBtnObj.transform, false);

        RectTransform noTextRect = noTextObj.AddComponent<RectTransform>();
        noTextRect.anchorMin = Vector2.zero;
        noTextRect.anchorMax = Vector2.one;
        noTextRect.pivot = new Vector2(0.5f, 0.5f);
        noTextRect.anchoredPosition = Vector2.zero;
        noTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI noTmp = noTextObj.AddComponent<TextMeshProUGUI>();
        noTmp.text = "NO";
        noTmp.fontSize = 16;
        noTmp.color = Color.white;
        noTmp.alignment = TextAlignmentOptions.Center;
        noTmp.raycastTarget = false; // Prevent blocking click raycasts

        // Pause time
        Time.timeScale = 0f;
    }

    private void OnYesClicked()
    {
        Time.timeScale = 1f; // Restore timescale
        string menuName = "MainMenu_Scene";
        if (RoundManager.Instance != null && !string.IsNullOrEmpty(RoundManager.Instance.menuSceneName))
        {
            menuName = RoundManager.Instance.menuSceneName;
        }
        UnityEngine.SceneManagement.SceneManager.LoadScene(menuName);
    }

    private void OnNoClicked()
    {
        if (escPopupInstance != null)
        {
            escPopupInstance.SetActive(false);
        }
        Time.timeScale = 1f; // Resume timescale
    }
}