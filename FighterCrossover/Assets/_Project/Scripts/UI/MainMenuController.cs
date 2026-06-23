using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.InputSystem;

namespace AnimeFighter.UI
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Main Menu Panels")]
        [SerializeField] private GameObject settingsPopup;

        [Header("Two-Stage Sub-Panels")]
        [SerializeField] private GameObject generalPanel;
        [SerializeField] private GameObject keyboardPanel;

        [Header("Audio Components")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Image muteToggleImage;
        [SerializeField] private Sprite unmuteSprite; // on_volume
        [SerializeField] private Sprite muteSprite;   // mute_volume

        [Header("Gameplay Controls")]
        [SerializeField] private TextMeshProUGUI difficultyText;
        [SerializeField] private TextMeshProUGUI matchTimeText;

        [Header("Keybind Buttons P1")]
        [SerializeField] private Button p1MoveLeftBtn;
        [SerializeField] private Button p1MoveRightBtn;
        [SerializeField] private Button p1DefenseBtn;
        [SerializeField] private Button p1AttackBtn;
        [SerializeField] private Button p1JumpBtn;
        [SerializeField] private Button p1DodgeBtn;
        [SerializeField] private Button p1RangedBtn;
        [SerializeField] private Button p1SpecialBtn;
        [SerializeField] private Button p1SupportBtn;

        [Header("Keybind Buttons P2")]
        [SerializeField] private Button p2MoveLeftBtn;
        [SerializeField] private Button p2MoveRightBtn;
        [SerializeField] private Button p2DefenseBtn;
        [SerializeField] private Button p2AttackBtn;
        [SerializeField] private Button p2JumpBtn;
        [SerializeField] private Button p2DodgeBtn;
        [SerializeField] private Button p2RangedBtn;
        [SerializeField] private Button p2SpecialBtn;
        [SerializeField] private Button p2SupportBtn;

        private GameSettingsData settingsData;
        private string saveFilePath;

        // Rebinding State
        private bool isListening = false;
        private int rebindPlayerNum = 0; // 1 or 2
        private string rebindActionName = ""; // "moveLeft", etc.
        private TextMeshProUGUI activeRebindText = null;

        private readonly string[] difficulties = { "Easy", "Medium", "Hard" };
        private readonly int[] matchTimes = { 60, 90, 999 };

        private void Awake()
        {
            saveFilePath = Path.Combine(Application.persistentDataPath, "settings.json");
            LoadSettings();
        }

        private void Start()
        {
            // Register button click listeners automatically to avoid inspector binding errors
            AddRebindListener(p1MoveLeftBtn, 1, "moveLeft");
            AddRebindListener(p1MoveRightBtn, 1, "moveRight");
            AddRebindListener(p1DefenseBtn, 1, "defense");
            AddRebindListener(p1AttackBtn, 1, "attack");
            AddRebindListener(p1JumpBtn, 1, "jump");
            AddRebindListener(p1DodgeBtn, 1, "dodge");
            AddRebindListener(p1RangedBtn, 1, "rangedAttack");
            AddRebindListener(p1SpecialBtn, 1, "specialMove");
            AddRebindListener(p1SupportBtn, 1, "support");

            AddRebindListener(p2MoveLeftBtn, 2, "moveLeft");
            AddRebindListener(p2MoveRightBtn, 2, "moveRight");
            AddRebindListener(p2DefenseBtn, 2, "defense");
            AddRebindListener(p2AttackBtn, 2, "attack");
            AddRebindListener(p2JumpBtn, 2, "jump");
            AddRebindListener(p2DodgeBtn, 2, "dodge");
            AddRebindListener(p2RangedBtn, 2, "rangedAttack");
            AddRebindListener(p2SpecialBtn, 2, "specialMove");
            AddRebindListener(p2SupportBtn, 2, "support");

            // Apply loaded settings to UI components
            ApplySettingsToUI();
            
            // Start playing background music if it exists
            if (bgmSource != null && !bgmSource.isPlaying)
            {
                bgmSource.Play();
            }

            // Register volume slider listener
            if (volumeSlider != null)
            {
                volumeSlider.onValueChanged.AddListener(OnVolumeSliderChanged);
            }
        }

        private void Update()
        {
            if (!isListening) return;

            // Capture next keyboard input key down using New Input System
            if (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
            {
                foreach (var keyControl in Keyboard.current.allKeys)
                {
                    if (keyControl.wasPressedThisFrame)
                    {
                        Key inputKey = keyControl.keyCode;

                        // Escape key cancels rebinding
                        if (inputKey == Key.Escape)
                        {
                            isListening = false;
                            ApplySettingsToUI();
                            return;
                        }

                        KeyCode kcode = ConvertInputSystemKeyToKeyCode(inputKey);
                        if (kcode != KeyCode.None)
                        {
                            SetKeybind(rebindPlayerNum, rebindActionName, kcode);
                            isListening = false;
                            ApplySettingsToUI();
                        }
                        break;
                    }
                }
            }
        }

        // --- Main Menu Navigation ---
        public void StartGame()
        {
            Debug.Log("Loading Pre_Match_Scene...");
            SceneManager.LoadScene("Pre_Match_Scene");
        }

        public void OpenSettings()
        {
            if (settingsPopup != null)
            {
                settingsPopup.SetActive(true);
                ShowGeneralSetup();
                settingsPopup.transform.localScale = Vector3.zero;
                StartCoroutine(ScalePanel(settingsPopup, Vector3.one, 0.2f));
            }
        }

        public void ShowGeneralSetup()
        {
            if (generalPanel != null) generalPanel.SetActive(true);
            if (keyboardPanel != null) keyboardPanel.SetActive(false);
        }

        public void ShowKeyboardSetup()
        {
            if (generalPanel != null) generalPanel.SetActive(false);
            if (keyboardPanel != null) keyboardPanel.SetActive(true);
        }

        public void SaveAndCloseSettings()
        {
            SaveSettings();
            if (settingsPopup != null)
            {
                StartCoroutine(ScalePanel(settingsPopup, Vector3.zero, 0.2f, () => {
                    settingsPopup.SetActive(false);
                }));
            }
        }

        public void ExitGame()
        {
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #else
            Application.Quit();
            #endif
        }

        private IEnumerator ScalePanel(GameObject panel, Vector3 targetScale, float duration, System.Action onComplete = null)
        {
            Vector3 startScale = panel.transform.localScale;
            float time = 0;
            while (time < duration)
            {
                panel.transform.localScale = Vector3.Lerp(startScale, targetScale, time / duration);
                time += Time.unscaledDeltaTime;
                yield return null;
            }
            panel.transform.localScale = targetScale;
            onComplete?.Invoke();
        }

        // --- Audio Logic ---
        public void ToggleMute()
        {
            if (settingsData != null)
            {
                settingsData.isMuted = !settingsData.isMuted;
                UpdateMuteUI();
                ApplyAudioVolume();
            }
        }

        private void OnVolumeSliderChanged(float val)
        {
            if (settingsData != null)
            {
                settingsData.masterVolume = val;
                ApplyAudioVolume();
            }

            // Rotate slider handle (shuriken) based on the slider value
            if (volumeSlider != null && volumeSlider.handleRect != null)
            {
                float rotationAngle = val * -720f;
                volumeSlider.handleRect.localRotation = Quaternion.Euler(0f, 0f, rotationAngle);
            }
        }

        private void ApplyAudioVolume()
        {
            if (settingsData == null) return;
            float targetVolume = settingsData.isMuted ? 0f : settingsData.masterVolume;
            AudioListener.volume = targetVolume;
            if (bgmSource != null)
            {
                bgmSource.volume = targetVolume;
            }
        }

        private void UpdateMuteUI()
        {
            if (muteToggleImage != null && settingsData != null)
            {
                muteToggleImage.sprite = settingsData.isMuted ? muteSprite : unmuteSprite;
                // Tint to fit theme: unmuted is orange, muted is semi-transparent dark grey/purple
                muteToggleImage.color = settingsData.isMuted ? new Color(0.5f, 0.5f, 0.6f, 0.6f) : new Color(1f, 0.5f, 0f, 1f);
            }
        }

        // --- Gameplay Selection Cycles ---
        public void CycleDifficulty(int direction)
        {
            if (settingsData == null) return;
            settingsData.botDifficulty += direction;
            if (settingsData.botDifficulty < 0) settingsData.botDifficulty = difficulties.Length - 1;
            else if (settingsData.botDifficulty >= difficulties.Length) settingsData.botDifficulty = 0;

            UpdateDifficultyText();
        }

        private void UpdateDifficultyText()
        {
            if (difficultyText != null && settingsData != null)
            {
                difficultyText.text = difficulties[settingsData.botDifficulty];
            }
        }

        public void CycleMatchTime(int direction)
        {
            if (settingsData == null) return;
            int currentIdx = 1; // default to 90
            for (int i = 0; i < matchTimes.Length; i++)
            {
                if (matchTimes[i] == settingsData.matchTime)
                {
                    currentIdx = i;
                    break;
                }
            }

            currentIdx += direction;
            if (currentIdx < 0) currentIdx = matchTimes.Length - 1;
            else if (currentIdx >= matchTimes.Length) currentIdx = 0;

            settingsData.matchTime = matchTimes[currentIdx];
            UpdateMatchTimeText();
        }

        private void UpdateMatchTimeText()
        {
            if (matchTimeText != null && settingsData != null)
            {
                if (settingsData.matchTime == 999)
                    matchTimeText.text = "∞";
                else
                    matchTimeText.text = settingsData.matchTime.ToString();
            }
        }

        // --- Rebinding Actions ---
        private void AddRebindListener(Button btn, int playerNum, string actionName)
        {
            if (btn != null)
            {
                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    btn.onClick.RemoveAllListeners();
                    btn.onClick.AddListener(() => StartRebind(playerNum, actionName, tmp));
                }
            }
        }

        public void StartRebind(int playerNum, string actionName, TextMeshProUGUI buttonText)
        {
            if (isListening) return; // Prevent multiple active rebinding listeners

            isListening = true;
            rebindPlayerNum = playerNum;
            rebindActionName = actionName;
            activeRebindText = buttonText;

            buttonText.text = "Listening...";
        }

        private void SetKeybind(int playerNum, string actionName, KeyCode key)
        {
            if (settingsData == null) return;
            KeybindingsData keys = (playerNum == 1) ? settingsData.player1Keys : settingsData.player2Keys;

            switch (actionName)
            {
                case "moveLeft": keys.moveLeft = key; break;
                case "moveRight": keys.moveRight = key; break;
                case "defense": keys.defense = key; break;
                case "attack": keys.attack = key; break;
                case "jump": keys.jump = key; break;
                case "dodge": keys.dodge = key; break;
                case "rangedAttack": keys.rangedAttack = key; break;
                case "specialMove": keys.specialMove = key; break;
                case "support": keys.support = key; break;
            }
        }

        // --- Data Persistence ---
        private void LoadSettings()
        {
            if (File.Exists(saveFilePath))
            {
                try
                {
                    string json = File.ReadAllText(saveFilePath);
                    settingsData = JsonUtility.FromJson<GameSettingsData>(json);
                }
                catch
                {
                    settingsData = new GameSettingsData();
                }
            }
            else
            {
                settingsData = new GameSettingsData();
            }

            // Sync from PlayerPrefs if available (per requirement: map audio to PlayerPrefs)
            if (PlayerPrefs.HasKey("MasterVolume"))
            {
                settingsData.masterVolume = PlayerPrefs.GetFloat("MasterVolume");
            }
            if (PlayerPrefs.HasKey("IsMuted"))
            {
                settingsData.isMuted = PlayerPrefs.GetInt("IsMuted") == 1;
            }

            // Version migration check to reset keys if the old config format was present
            if (PlayerPrefs.GetInt("KeybindingsVersion", 0) < 3)
            {
                settingsData.SetDefaultValues();
                SaveSettings();
                PlayerPrefs.SetInt("KeybindingsVersion", 3);
                PlayerPrefs.Save();
            }

            ApplyAudioVolume();
        }

        private void SaveSettings()
        {
            if (settingsData == null) settingsData = new GameSettingsData();

            try
            {
                string json = JsonUtility.ToJson(settingsData, true);
                File.WriteAllText(saveFilePath, json);
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error saving settings: " + e.Message);
            }

            // Map audio to PlayerPrefs
            PlayerPrefs.SetFloat("MasterVolume", settingsData.masterVolume);
            PlayerPrefs.SetInt("IsMuted", settingsData.isMuted ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void ApplySettingsToUI()
        {
            if (settingsData == null) return;

            if (volumeSlider != null)
            {
                volumeSlider.value = settingsData.masterVolume;
                
                // Color slider handle to orange, fill to purple
                if (volumeSlider.handleRect != null)
                {
                    var handleImage = volumeSlider.handleRect.GetComponent<Image>();
                    if (handleImage != null)
                    {
                        handleImage.color = new Color(1f, 0.5f, 0f, 1f); // Vibrant orange
                    }
                    
                    // Rotate based on initial volume
                    float rotationAngle = settingsData.masterVolume * -720f;
                    volumeSlider.handleRect.localRotation = Quaternion.Euler(0f, 0f, rotationAngle);
                }
                if (volumeSlider.fillRect != null)
                {
                    var fillImage = volumeSlider.fillRect.GetComponent<Image>();
                    if (fillImage != null)
                    {
                        fillImage.color = new Color(1f, 0.5f, 0f, 1f); // Vibrant orange
                    }
                }
            }
            UpdateMuteUI();
            UpdateDifficultyText();
            UpdateMatchTimeText();

            // P1 Keybind Buttons text update
            UpdateKeybindButtonText(p1MoveLeftBtn, settingsData.player1Keys.moveLeft);
            UpdateKeybindButtonText(p1MoveRightBtn, settingsData.player1Keys.moveRight);
            UpdateKeybindButtonText(p1DefenseBtn, settingsData.player1Keys.defense);
            UpdateKeybindButtonText(p1AttackBtn, settingsData.player1Keys.attack);
            UpdateKeybindButtonText(p1JumpBtn, settingsData.player1Keys.jump);
            UpdateKeybindButtonText(p1DodgeBtn, settingsData.player1Keys.dodge);
            UpdateKeybindButtonText(p1RangedBtn, settingsData.player1Keys.rangedAttack);
            UpdateKeybindButtonText(p1SpecialBtn, settingsData.player1Keys.specialMove);
            UpdateKeybindButtonText(p1SupportBtn, settingsData.player1Keys.support);

            // P2 Keybind Buttons text update
            UpdateKeybindButtonText(p2MoveLeftBtn, settingsData.player2Keys.moveLeft);
            UpdateKeybindButtonText(p2MoveRightBtn, settingsData.player2Keys.moveRight);
            UpdateKeybindButtonText(p2DefenseBtn, settingsData.player2Keys.defense);
            UpdateKeybindButtonText(p2AttackBtn, settingsData.player2Keys.attack);
            UpdateKeybindButtonText(p2JumpBtn, settingsData.player2Keys.jump);
            UpdateKeybindButtonText(p2DodgeBtn, settingsData.player2Keys.dodge);
            UpdateKeybindButtonText(p2RangedBtn, settingsData.player2Keys.rangedAttack);
            UpdateKeybindButtonText(p2SpecialBtn, settingsData.player2Keys.specialMove);
            UpdateKeybindButtonText(p2SupportBtn, settingsData.player2Keys.support);
        }

        private string GetReadableKeyName(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.LeftArrow: return "Left";
                case KeyCode.RightArrow: return "Right";
                case KeyCode.DownArrow: return "Down";
                case KeyCode.UpArrow: return "Up";
                case KeyCode.Keypad1: return "1";
                case KeyCode.Keypad2: return "2";
                case KeyCode.Keypad3: return "3";
                case KeyCode.Keypad4: return "4";
                case KeyCode.Keypad5: return "5";
                case KeyCode.Keypad6: return "6";
                case KeyCode.Keypad7: return "7";
                case KeyCode.Keypad8: return "8";
                case KeyCode.Keypad9: return "9";
                case KeyCode.Keypad0: return "0";
                case KeyCode.Alpha1: return "1";
                case KeyCode.Alpha2: return "2";
                case KeyCode.Alpha3: return "3";
                case KeyCode.Alpha4: return "4";
                case KeyCode.Alpha5: return "5";
                case KeyCode.Alpha6: return "6";
                case KeyCode.Alpha7: return "7";
                case KeyCode.Alpha8: return "8";
                case KeyCode.Alpha9: return "9";
                case KeyCode.Alpha0: return "0";
                default: return key.ToString();
            }
        }

        private void UpdateKeybindButtonText(Button btn, KeyCode key)
        {
            if (btn != null)
            {
                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.text = GetReadableKeyName(key);
                }
            }
        }

        private KeyCode ConvertInputSystemKeyToKeyCode(Key key)
        {
            switch (key)
            {
                // Letters
                case Key.A: return KeyCode.A;
                case Key.B: return KeyCode.B;
                case Key.C: return KeyCode.C;
                case Key.D: return KeyCode.D;
                case Key.E: return KeyCode.E;
                case Key.F: return KeyCode.F;
                case Key.G: return KeyCode.G;
                case Key.H: return KeyCode.H;
                case Key.I: return KeyCode.I;
                case Key.J: return KeyCode.J;
                case Key.K: return KeyCode.K;
                case Key.L: return KeyCode.L;
                case Key.M: return KeyCode.M;
                case Key.N: return KeyCode.N;
                case Key.O: return KeyCode.O;
                case Key.P: return KeyCode.P;
                case Key.Q: return KeyCode.Q;
                case Key.R: return KeyCode.R;
                case Key.S: return KeyCode.S;
                case Key.T: return KeyCode.T;
                case Key.U: return KeyCode.U;
                case Key.V: return KeyCode.V;
                case Key.W: return KeyCode.W;
                case Key.X: return KeyCode.X;
                case Key.Y: return KeyCode.Y;
                case Key.Z: return KeyCode.Z;

                // Digits
                case Key.Digit0: return KeyCode.Alpha0;
                case Key.Digit1: return KeyCode.Alpha1;
                case Key.Digit2: return KeyCode.Alpha2;
                case Key.Digit3: return KeyCode.Alpha3;
                case Key.Digit4: return KeyCode.Alpha4;
                case Key.Digit5: return KeyCode.Alpha5;
                case Key.Digit6: return KeyCode.Alpha6;
                case Key.Digit7: return KeyCode.Alpha7;
                case Key.Digit8: return KeyCode.Alpha8;
                case Key.Digit9: return KeyCode.Alpha9;

                // Arrows
                case Key.LeftArrow: return KeyCode.LeftArrow;
                case Key.RightArrow: return KeyCode.RightArrow;
                case Key.UpArrow: return KeyCode.UpArrow;
                case Key.DownArrow: return KeyCode.DownArrow;

                // Keypad
                case Key.Numpad0: return KeyCode.Keypad0;
                case Key.Numpad1: return KeyCode.Keypad1;
                case Key.Numpad2: return KeyCode.Keypad2;
                case Key.Numpad3: return KeyCode.Keypad3;
                case Key.Numpad4: return KeyCode.Keypad4;
                case Key.Numpad5: return KeyCode.Keypad5;
                case Key.Numpad6: return KeyCode.Keypad6;
                case Key.Numpad7: return KeyCode.Keypad7;
                case Key.Numpad8: return KeyCode.Keypad8;
                case Key.Numpad9: return KeyCode.Keypad9;
                case Key.NumpadEnter: return KeyCode.KeypadEnter;

                // Modifier & Control keys
                case Key.Space: return KeyCode.Space;
                case Key.Enter: return KeyCode.Return;
                case Key.Escape: return KeyCode.Escape;
                case Key.Tab: return KeyCode.Tab;
                case Key.LeftShift: return KeyCode.LeftShift;
                case Key.RightShift: return KeyCode.RightShift;
                case Key.LeftCtrl: return KeyCode.LeftControl;
                case Key.RightCtrl: return KeyCode.RightControl;
                case Key.LeftAlt: return KeyCode.LeftAlt;
                case Key.RightAlt: return KeyCode.RightAlt;

                // Special / Symbol keys
                case Key.Semicolon: return KeyCode.Semicolon;
                case Key.Comma: return KeyCode.Comma;
                case Key.Period: return KeyCode.Period;
                case Key.Slash: return KeyCode.Slash;
                case Key.Backslash: return KeyCode.Backslash;
                case Key.Quote: return KeyCode.Quote;
                case Key.Backquote: return KeyCode.BackQuote;
                case Key.Minus: return KeyCode.Minus;
                case Key.Equals: return KeyCode.Equals;
                case Key.LeftBracket: return KeyCode.LeftBracket;
                case Key.RightBracket: return KeyCode.RightBracket;

                default: return KeyCode.None;
            }
        }
    }
}
