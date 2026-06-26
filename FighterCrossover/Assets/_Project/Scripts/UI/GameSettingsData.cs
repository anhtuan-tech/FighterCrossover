using System;
using UnityEngine;

namespace AnimeFighter.UI
{
    [Serializable]
    public class KeybindingsData
    {
        public KeyCode moveLeft;
        public KeyCode moveRight;
        public KeyCode defense;
        public KeyCode attack;
        public KeyCode jump;
        public KeyCode dodge;
        public KeyCode rangedAttack;
        public KeyCode specialMove;
        public KeyCode support;
    }

    [Serializable]
    public class GameSettingsData
    {
        // Audio
        public float masterVolume = 1.0f;
        public bool isMuted = false;

        // Gameplay
        public int botDifficulty = 1; // 0 = Easy, 1 = Medium, 2 = Hell
        public int matchTime = 90;    // 60, 90, 999 (Inf)

        // Keybindings
        public KeybindingsData player1Keys = new KeybindingsData();
        public KeybindingsData player2Keys = new KeybindingsData();

        public GameSettingsData()
        {
            SetDefaultValues();
        }

        public void SetDefaultValues()
        {
            masterVolume = 1.0f;
            isMuted = false;
            botDifficulty = 1; // Medium
            matchTime = 90;    // 90s

            // Player 1 Defaults: WASD + JKLUIO
            player1Keys.moveLeft = KeyCode.A;
            player1Keys.moveRight = KeyCode.D;
            player1Keys.defense = KeyCode.S;
            player1Keys.attack = KeyCode.J;
            player1Keys.jump = KeyCode.K;
            player1Keys.dodge = KeyCode.L;
            player1Keys.rangedAttack = KeyCode.U;
            player1Keys.specialMove = KeyCode.I;
            player1Keys.support = KeyCode.O;

            // Player 2 Defaults: Arrow keys + Keypad keys (1 to 6)
            player2Keys.moveLeft = KeyCode.LeftArrow;
            player2Keys.moveRight = KeyCode.RightArrow;
            player2Keys.defense = KeyCode.DownArrow;
            player2Keys.attack = KeyCode.Keypad1;
            player2Keys.jump = KeyCode.Keypad2;
            player2Keys.dodge = KeyCode.Keypad3;
            player2Keys.rangedAttack = KeyCode.Keypad4;
            player2Keys.specialMove = KeyCode.Keypad5;
            player2Keys.support = KeyCode.Keypad6;
        }
    }
}
