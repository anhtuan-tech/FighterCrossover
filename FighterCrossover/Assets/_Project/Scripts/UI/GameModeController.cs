using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace AnimeFighter.UI
{
    public class GameModeController : MonoBehaviour
    {
        [Header("Game Mode")]
        [SerializeField] private Button trainingModeBtn;
        [SerializeField] private Button deathBattleBtn;
        [SerializeField] private Button pvp1v1Btn;
        [SerializeField] private Button closeBtn;

        private const string TrainingSceneName = "Prematch_Scene";
        private const string DeathBattleSceneName = "Prematch_Scene";
        private const string PvpLocalSceneName = "Prematch_Scene";

        private void Start()
        {
            // Gắn listener ngay khi popup được khởi tạo để có thể cắm vào Main Menu cũ.
            if (trainingModeBtn != null)
            {
                trainingModeBtn.onClick.AddListener(OnTrainingModeClicked);
            }

            if (deathBattleBtn != null)
            {
                deathBattleBtn.onClick.AddListener(OnDeathBattleModeClicked);
            }

            if (pvp1v1Btn != null)
            {
                pvp1v1Btn.onClick.AddListener(OnPvp1v1Clicked);
            }

            if (closeBtn != null)
            {
                closeBtn.onClick.AddListener(ClosePopup);
            }
        }

        private void OnDestroy()
        {
            // Hủy listener để tránh popup cũ giữ tham chiếu không cần thiết.
            if (trainingModeBtn != null)
            {
                trainingModeBtn.onClick.RemoveListener(OnTrainingModeClicked);
            }

            if (deathBattleBtn != null)
            {
                deathBattleBtn.onClick.RemoveListener(OnDeathBattleModeClicked);
            }

            if (pvp1v1Btn != null)
            {
                pvp1v1Btn.onClick.RemoveListener(OnPvp1v1Clicked);
            }

            if (closeBtn != null)
            {
                closeBtn.onClick.RemoveListener(ClosePopup);
            }
        }

        // Main Menu cũ có thể gọi hàm này từ nút Start để mở popup chọn mode.
        public void OpenPopup()
        {
            gameObject.SetActive(true);
        }

        // Đóng popup khi người chơi bấm nút X hoặc khi cần hủy lựa chọn.
        public void ClosePopup()
        {
            gameObject.SetActive(false);
        }

        private void OnTrainingModeClicked()
        {
            SelectionData.CurrentGameMode = GameMode.Training; // <--- LƯU LẠI MODE
            LoadGameMode(TrainingSceneName);
        }

        private void OnDeathBattleModeClicked()
        {
            SelectionData.CurrentGameMode = GameMode.DeathBattle; // <--- LƯU LẠI MODE
            LoadGameMode(DeathBattleSceneName);
        }

        private void OnPvp1v1Clicked()
        {
            SelectionData.CurrentGameMode = GameMode.PvP; // <--- LƯU LẠI MODE
            LoadGameMode(PvpLocalSceneName);
        }

        // Bọc toàn bộ logic chuyển scene trong một hàm riêng để dự án có thể thay thế dễ dàng.
        protected virtual void LoadGameMode(string sceneName)
        {
            Debug.Log($"Loading game mode scene: {sceneName}");
            SceneManager.LoadScene(sceneName);
        }
    }
}