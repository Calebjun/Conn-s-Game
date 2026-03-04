using UnityEngine;
using System;

namespace RhythmGame.Core
{
    public enum HitRating
    {
        Miss,
        Good,
        Great,
        Perfect
    }

    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        // Phase thresholds — these are bar fill values, not raw points.
        // The bar goes from 0.0 to 1.0. Thresholds are normalized positions.
        public const float PHASE2_THRESHOLD = 20f  / 200f;   // 0.10
        public const float PHASE3_THRESHOLD = 100f / 200f;   // 0.50
        public const float PHASE4_THRESHOLD = 180f / 200f;   // 0.90
        public const float MAX_FILL         = 1.0f;

        // Hit windows in seconds
        public const float PERFECT_WINDOW = 0.05f;
        public const float GREAT_WINDOW   = 0.10f;
        public const float GOOD_WINDOW    = 0.15f;

        [Header("Bar Fill Amounts (0.0 - 1.0)")]
        public float perfectFill = 0.025f;   // +5 out of 200
        public float greatFill   = 0.015f;   // +3 out of 200
        public float goodFill    = 0.005f;   // +1 out of 200
        public float missDrain   = 0.010f;   // -2 out of 200

        // BarFill is the authoritative score value (0.0 to 1.0)
        public float BarFill     { get; private set; } = 0f;

        // Convenience integer version matching your diagram's 0-200 scale
        public int Score => Mathf.RoundToInt(BarFill * 200f);

        public int PerfectCount { get; private set; } = 0;
        public int GreatCount   { get; private set; } = 0;
        public int GoodCount    { get; private set; } = 0;
        public int MissCount    { get; private set; } = 0;

        public event Action<float, HitRating> OnNoteHit;    // passes BarFill, rating
        public event Action<float> OnBarFillChanged;         // passes new BarFill
        public event Action OnFishCaughtEvent;               // fired when fishing minigame is won

        // True once the fishing minigame has been won, gates Phase 3 music transition
        public bool FishingComplete { get; private set; } = false;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void RegisterHit(HitRating rating)
        {
            switch (rating)
            {
                case HitRating.Perfect:
                    BarFill += perfectFill;
                    PerfectCount++;
                    break;
                case HitRating.Great:
                    BarFill += greatFill;
                    GreatCount++;
                    break;
                case HitRating.Good:
                    BarFill += goodFill;
                    GoodCount++;
                    break;
                case HitRating.Miss:
                    BarFill -= missDrain;
                    MissCount++;
                    break;
            }

            BarFill = Mathf.Clamp(BarFill, 0f, MAX_FILL);

            OnNoteHit?.Invoke(BarFill, rating);
            OnBarFillChanged?.Invoke(BarFill);
        }

        public HitRating EvaluateTiming(float timingError)
        {
            float abs = Mathf.Abs(timingError);
            if (abs <= PERFECT_WINDOW) return HitRating.Perfect;
            if (abs <= GREAT_WINDOW)   return HitRating.Great;
            if (abs <= GOOD_WINDOW)    return HitRating.Good;
            return HitRating.Miss;
        }

        public void Reset()
        {
            BarFill      = 0f;
            PerfectCount = 0;
            GreatCount   = 0;
            GoodCount    = 0;
            MissCount    = 0;
            FishingComplete = false;
        }
        /// <summary>
        /// Called by FishingManager when a fish is successfully caught.
        /// </summary>
        public void OnFishCaught()
        {
            FishingComplete = true;
            OnFishCaughtEvent?.Invoke();
        }
    }
}
