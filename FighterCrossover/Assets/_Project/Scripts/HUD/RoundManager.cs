using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// Quản lý vòng đấu Death Battle (5 trận – mỗi trận 1 round duy nhất).
/// PvP / Training: giữ nguyên best-of-3 bằng round 1-3 sprites.
/// </summary>
public class RoundManager : MonoBehaviour
{
    public static RoundManager Instance { get; private set; }

    // ─── Round Sprite UI (PvP best-of-3) ─────────────────────────────────────
    [Header("--- Round Sprite UI (PvP only) ---")]
    public GameObject round1Object;
    public GameObject round2Object;
    public GameObject round3Object;

    // ─── Result Panel ─────────────────────────────────────────────────────────
    [Header("--- Result / Announce UI ---")]
    public GameObject winPanel;
    [Tooltip("TextMeshPro hiển thị kết quả vòng / toàn trận")]
    public TextMeshProUGUI winText;

    // ─── Scene ────────────────────────────────────────────────────────────────
    [Header("--- Scene ---")]
    public string menuSceneName = "MainMenu_Scene";

    // ─── PvP best-of-3 tracking ───────────────────────────────────────────────
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
        StartNewRound();
    }

    // ─── Public ──────────────────────────────────────────────────────────────

    public void StartNewRound()
    {
        StartCoroutine(StartRoundRoutine());
    }

    /// <summary>Gọi từ MatchManager khi một round kết thúc (HP = 0 hoặc timeout).</summary>
    public void OnRoundEnd(FighterBase p1, FighterBase p2, bool isTimeOut)
    {
        if (SelectionData.CurrentGameMode == GameMode.DeathBattle)
        {
            // Death Battle: 1 round dứt khoát – xác định thắng/thua ngay
            int winner = DetermineWinner(p1, p2, isTimeOut);
            bool playerWon = (winner == 1); // P1 = human player
            StartCoroutine(HandleDeathBattleRoundEnd(playerWon));
        }
        else
        {
            // PvP / Training: best-of-3
            int winner = DetermineWinner(p1, p2, isTimeOut);
            if (winner == 1) P1Wins++;
            else if (winner == 2) P2Wins++;

            bool matchOver = (P1Wins >= 2 || P2Wins >= 2 || CurrentRound >= 3);
            if (matchOver)
            {
                bool p1WonMatch = P1Wins > P2Wins;
                string msg = p1WonMatch ? "PLAYER 1 WINS!" : "PLAYER 2 WINS!";
                ShowResult(msg);
                StartCoroutine(ReturnToMenuAfterDelay(3f));
            }
            else
            {
                string roundMsg = winner == 1 ? "P1 ROUND WIN" : winner == 2 ? "P2 ROUND WIN" : "DRAW";
                ShowResult(roundMsg);
                StartCoroutine(NextPvPRoundRoutine(p1, p2));
            }
        }
    }

    // ─── Death Battle single-round flow ──────────────────────────────────────

    private IEnumerator HandleDeathBattleRoundEnd(bool playerWon)
    {
        var data = SelectionData.DeathBattleData;

        if (!playerWon)
        {
            // Thua → xóa save → về menu
            ShowResult("DEFEATED!\nBetter luck next time...");
            DeathBattleSaveSystem.DeleteProgress();
            yield return new WaitForSeconds(3f);
            HideResult();
            SceneManager.LoadScene(menuSceneName);
            yield break;
        }

        // Thắng trận này
        int nextIndex = data.currentMatchIndex + 1;

        if (nextIndex >= 5)
        {
            // Thắng cả 5 trận → CHAMPION
            data.isComplete = true;
            DeathBattleSaveSystem.SaveProgress(data);
            ShowResult("🏆  CHAMPION!  🏆\nAll 5 challengers defeated!");
            yield return new WaitForSeconds(4f);
            HideResult();
            DeathBattleSaveSystem.DeleteProgress();
            SceneManager.LoadScene(menuSceneName);
        }
        else
        {
            // Còn trận tiếp → lưu → load map kế
            data.currentMatchIndex = nextIndex;
            // Đặt sẵn URL bot tiếp theo vào SelectionData
            SelectionData.characterPrefabUrl2 = data.enemyCharacterUrls[nextIndex];
            if (data.enemyCharacterImageUrls != null && nextIndex < data.enemyCharacterImageUrls.Count)
            {
                SelectionData.characterImageUrl2 = data.enemyCharacterImageUrls[nextIndex];
            }
            DeathBattleSaveSystem.SaveProgress(data);

            string nextMap = data.mapNames[nextIndex];
            int clearedNum = nextIndex; // số trận vừa xong (1-indexed)
            ShowResult($"ROUND {clearedNum} CLEARED!\nPrepare for Round {nextIndex + 1}...");
            yield return new WaitForSeconds(2.5f);
            HideResult();
            SceneManager.LoadScene(nextMap);
        }
    }

    // ─── PvP best-of-3 helpers ────────────────────────────────────────────────

    private IEnumerator NextPvPRoundRoutine(FighterBase oldP1, FighterBase oldP2)
    {
        yield return new WaitForSeconds(2f);
        HideResult();

        if (oldP1 != null) Destroy(oldP1.gameObject);
        if (oldP2 != null) Destroy(oldP2.gameObject);
        yield return null;

        CurrentRound++;

        if (LoadCharacter.Instance != null)
            LoadCharacter.Instance.SpawnStageCharacters();
        else
            Debug.LogError("[RoundManager] LoadCharacter.Instance not found!");

        yield return new WaitForSeconds(0.1f);
        StartNewRound();
    }

    private IEnumerator ReturnToMenuAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        HideResult();
        SceneManager.LoadScene(menuSceneName);
    }

    // ─── Round startup coroutine ─────────────────────────────────────────────

    private IEnumerator StartRoundRoutine()
    {
        DisableAllRoundObjects();

        if (SelectionData.CurrentGameMode == GameMode.DeathBattle)
        {
            // Hiển thị "ROUND X" qua winPanel (không cần round sprite objects riêng)
            int matchNum = SelectionData.DeathBattleData.currentMatchIndex + 1;
            string title = matchNum == 5 ? "⚔  FINAL ROUND  ⚔" : $"ROUND  {matchNum}";
            ShowResult(title);
            yield return new WaitForSeconds(1.8f);
            HideResult();
        }
        else if (SelectionData.CurrentGameMode != GameMode.Training)
        {
            // PvP: dùng round sprite objects
            GameObject ui = GetPvPRoundUI();
            if (ui != null)
            {
                ui.SetActive(true);
                yield return new WaitForSeconds(1.5f);
                ui.SetActive(false);
            }
        }

        if (MatchManager.Instance != null)
            MatchManager.Instance.StartNewRoundMatch();
    }

    private GameObject GetPvPRoundUI() => CurrentRound switch
    {
        1 => round1Object,
        2 => round2Object,
        3 => round3Object,
        _ => null
    };

    // ─── Winner logic ─────────────────────────────────────────────────────────

    private int DetermineWinner(FighterBase p1, FighterBase p2, bool isTimeOut)
    {
        float hp1 = p1 != null ? p1.stats.currentHp : 0f;
        float hp2 = p2 != null ? p2.stats.currentHp : 0f;

        if (isTimeOut)
        {
            if (hp1 > hp2) return 1;
            if (hp2 > hp1) return 2;
            return 0;
        }
        else
        {
            if (hp1 <= 0 && hp2 > 0) return 2;
            if (hp2 <= 0 && hp1 > 0) return 1;
            return 0; // mutual KO (edge case)
        }
    }

    // ─── UI helpers ──────────────────────────────────────────────────────────

    private void ShowResult(string message)
    {
        if (winPanel != null) winPanel.SetActive(true);
        if (winText != null) winText.text = message;
    }

    private void HideResult()
    {
        if (winPanel != null) winPanel.SetActive(false);
    }

    private void DisableAllRoundObjects()
    {
        if (round1Object != null) round1Object.SetActive(false);
        if (round2Object != null) round2Object.SetActive(false);
        if (round3Object != null) round3Object.SetActive(false);
    }

    // ─── Emergency save ───────────────────────────────────────────────────────

    private void OnApplicationQuit()
    {
        if (SelectionData.CurrentGameMode == GameMode.DeathBattle
            && DeathBattleSaveSystem.Current != null)
        {
            DeathBattleSaveSystem.SaveProgress(DeathBattleSaveSystem.Current);
        }
    }
}