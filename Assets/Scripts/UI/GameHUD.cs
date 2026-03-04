using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace RhythmGame.UI
{
    /// <summary>
    /// Drives the in-game HUD.
    /// The main visual is a progress bar that fills/drains based on BarFill (0-1).
    /// Phase markers sit at 0.10, 0.50, and 0.90 along the bar.
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("Score Bar")]
        [Tooltip("Filled Image. fillAmount is driven by BarFill (0-1).")]
        public Image scoreBarFill;

        [Tooltip("Optional gradient. Assign a Gradient and the bar color shifts as it fills.")]
        public Gradient barGradient;
        public bool useGradient = true;

        [Header("Phase Label")]
        public TMP_Text phaseLabel;

        [Header("Phase Marker Lines (optional)")]
        [Tooltip("Place these UI Images at x positions matching the thresholds on the bar.")]
        public RectTransform phase2Marker;   // sits at 10% along bar
        public RectTransform phase3Marker;   // sits at 50%
        public RectTransform phase4Marker;   // sits at 90%

        [Header("Key Binding Labels (optional, one per lane: Up/Left/Down/Right)")]
        public TMP_Text[] laneKeyLabels;         // 4 labels showing W / A / S / D

        private Core.ScoreManager scoreManager;
        private Input.InputManager inputManager;

        void Start()
        {
            scoreManager = Core.ScoreManager.Instance;
            inputManager = Input.InputManager.Instance;

            if (scoreManager != null)
                scoreManager.OnBarFillChanged += OnBarFillChanged;

            UpdateKeyLabels();
            Refresh(0f);
        }

        void OnDestroy()
        {
            if (scoreManager != null)
                scoreManager.OnBarFillChanged -= OnBarFillChanged;
        }

        void OnBarFillChanged(float fill)
        {
            Refresh(fill);
        }

        void Refresh(float fill)
        {
            if (scoreBarFill != null)
            {
                scoreBarFill.fillAmount = fill;

                if (useGradient && barGradient != null)
                    scoreBarFill.color = barGradient.Evaluate(fill);
            }

            UpdatePhaseLabel(fill);
        }

        void UpdatePhaseLabel(float fill)
        {
            if (phaseLabel == null) return;

            if      (fill < Core.ScoreManager.PHASE2_THRESHOLD) phaseLabel.text = "PHASE 1";
            else if (fill < Core.ScoreManager.PHASE3_THRESHOLD) phaseLabel.text = "PHASE 2";
            else if (fill < Core.ScoreManager.PHASE4_THRESHOLD) phaseLabel.text = "PHASE 3";
            else if (fill < Core.ScoreManager.MAX_FILL)         phaseLabel.text = "PHASE 4";
            else                                                 phaseLabel.text = "PERFECT!";
        }

        void UpdateKeyLabels()
        {
            if (inputManager == null || laneKeyLabels == null) return;

            for (int i = 0; i < laneKeyLabels.Length && i < 4; i++)
            {
                if (laneKeyLabels[i] != null)
                    laneKeyLabels[i].text = inputManager.GetBinding(i).ToString();
            }
        }

        /// <summary>
        /// Call this from the Editor or a setup script to position phase markers
        /// along the bar rect at the correct normalized positions.
        /// </summary>
        public void PositionPhaseMarkers(RectTransform barRect)
        {
            if (barRect == null) return;
            float w = barRect.rect.width;

            SetMarkerX(phase2Marker, w * Core.ScoreManager.PHASE2_THRESHOLD);
            SetMarkerX(phase3Marker, w * Core.ScoreManager.PHASE3_THRESHOLD);
            SetMarkerX(phase4Marker, w * Core.ScoreManager.PHASE4_THRESHOLD);
        }

        void SetMarkerX(RectTransform marker, float x)
        {
            if (marker == null) return;
            Vector2 pos = marker.anchoredPosition;
            pos.x = x;
            marker.anchoredPosition = pos;
        }
    }
}
