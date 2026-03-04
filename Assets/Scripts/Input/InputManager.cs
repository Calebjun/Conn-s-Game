using UnityEngine;
using System;

namespace RhythmGame.Input
{
    /// <summary>
    /// Maps WASD keys to the four circular lanes.
    /// Lane 0 = Up (W), Lane 1 = Left (A), Lane 2 = Down (S), Lane 3 = Right (D)
    /// Rebindable at runtime via SetBinding().
    /// </summary>
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        [Header("Key Bindings (W=Up, A=Left, S=Down, D=Right)")]
        public KeyCode lane0Key = KeyCode.W;   // Up
        public KeyCode lane1Key = KeyCode.A;   // Left
        public KeyCode lane2Key = KeyCode.S;   // Down
        public KeyCode lane3Key = KeyCode.D;   // Right

        // Fired when a lane key is pressed, passes lane index 0-3
        public event Action<int> OnLanePressed;
        // Fired when a lane key is released (for hold notes)
        public event Action<int> OnLaneReleased;

        private KeyCode[] bindings;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            RefreshBindings();
        }

        void Update()
        {
            for (int i = 0; i < bindings.Length; i++)
            {
                if (UnityEngine.Input.GetKeyDown(bindings[i]))
                    OnLanePressed?.Invoke(i);

                if (UnityEngine.Input.GetKeyUp(bindings[i]))
                    OnLaneReleased?.Invoke(i);
            }
        }

        public void SetBinding(int lane, KeyCode key)
        {
            switch (lane)
            {
                case 0: lane0Key = key; break;
                case 1: lane1Key = key; break;
                case 2: lane2Key = key; break;
                case 3: lane3Key = key; break;
            }
            RefreshBindings();
        }

        void RefreshBindings()
        {
            bindings = new KeyCode[] { lane0Key, lane1Key, lane2Key, lane3Key };
        }

        public KeyCode GetBinding(int lane) => bindings[lane];
    }
}
