using UnityEngine;
using System.Collections;
using TMPro;

namespace RhythmGame.Fishing
{
    /// <summary>
    /// Orchestrates the fishing minigame.
    ///
    /// Flow (per design notes):
    ///   1. ScoreManager.BarFill reaches PHASE2_THRESHOLD (10%).
    ///   2. FishingManager.Activate() is called by GameManager.
    ///   3. Spawn (unhide) fishing UI with spawn-in animation.
    ///   4. Pick 1 of 5 random patterns, spawn fish.
    ///   5. Fish follows waypoints. Pink bar tracks mouse.
    ///   6. Fish score private int increments on pink bar collision.
    ///   7a. Caught (score >= 3): play caught anim, show success,
    ///       move fish to center then up, destroy sprite, reset score to 0,
    ///       allow player to move to Phase 3.
    ///   7b. Missed: play missed anim, show missed message,
    ///       move to UI edge, destroy sprite, reset score to 1.
    ///       Spawn new fish and try again.
    /// </summary>
    public class FishingManager : MonoBehaviour
    {
        public static FishingManager Instance { get; private set; }

        [Header("References")]
        public Core.ScoreManager scoreManager;
        public PinkBarController pinkBar;
        public Transform circleCenter;
        public Transform fishSpawnParent;

        [Header("Prefabs")]
        public GameObject fishPrefab;

        [Header("UI")]
        public GameObject fishingUIRoot;        // Root GameObject to show/hide
        public Animator   fishingUIAnimator;    // Has Spawn/Hide triggers
        public TMP_Text   resultMessageText;    // "CAUGHT!" or "MISSED!"
        public float      resultMessageDuration = 1.2f;

        [Header("Settings")]
        public float spawnAngleVariance = 180f; // Random start angle range

        // Tracks whether the minigame is currently running
        public bool IsActive { get; private set; } = false;

        // Has the player caught a fish yet this phase (unlocks Phase 3)
        public bool FishCaught { get; private set; } = false;

        private FishController currentFish;
        private Coroutine resultMessageCoroutine;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            // Start hidden
            if (fishingUIRoot != null) fishingUIRoot.SetActive(false);
        }

        // ─── Called by GameManager when bar hits 10% ──────────────────────────

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            FishCaught = false;
            StartCoroutine(RunFishingSequence());
        }

        public void Deactivate()
        {
            if (!IsActive) return;
            IsActive = false;

            if (currentFish != null)
            {
                currentFish.ForceStop();
                Destroy(currentFish.gameObject);
                currentFish = null;
            }

            HideFishingUI();
        }

        // ─── Main fishing loop ────────────────────────────────────────────────

        IEnumerator RunFishingSequence()
        {
            // Step 1: Show fishing UI with spawn-in animation
            yield return StartCoroutine(ShowFishingUI());

            // Step 2 onwards: keep spawning fish until one is caught
            while (IsActive && !FishCaught)
            {
                yield return StartCoroutine(SpawnAndRunFish());

                if (!IsActive) yield break;

                // Brief pause between fish attempts
                yield return new WaitForSeconds(0.5f);
            }
        }

        // ─── UI show/hide ─────────────────────────────────────────────────────

        IEnumerator ShowFishingUI()
        {
            if (fishingUIRoot == null) yield break;

            fishingUIRoot.SetActive(true);

            if (fishingUIAnimator != null)
            {
                fishingUIAnimator.SetTrigger("Spawn");
                // Wait for animation (assumes ~0.6s spawn anim)
                yield return new WaitForSeconds(0.6f);
            }
        }

        void HideFishingUI()
        {
            if (fishingUIRoot == null) return;

            if (fishingUIAnimator != null)
                fishingUIAnimator.SetTrigger("Hide");
            else
                fishingUIRoot.SetActive(false);
        }

        // ─── Fish spawn and result handling ───────────────────────────────────

        IEnumerator SpawnAndRunFish()
        {
            if (fishPrefab == null || circleCenter == null) yield break;

            // Pick random pattern and random start angle
            FishPatternType patternType = FishPatternFactory.GetRandomPattern();
            float startAngle = Random.Range(-spawnAngleVariance * 0.5f, spawnAngleVariance * 0.5f);

            // Scale pattern waypoint radii to match the pink bar's circle radius
            FishPatternData pattern = FishPatternFactory.CreatePattern(patternType, startAngle);
            ScalePatternToRadius(pattern, pinkBar != null ? pinkBar.circleRadius : 1f);

            // Instantiate fish
            GameObject fishGO = Instantiate(fishPrefab, circleCenter.position, Quaternion.identity, fishSpawnParent);
            currentFish = fishGO.GetComponent<FishController>();

            if (currentFish == null)
            {
                Debug.LogError("[FishingManager] Fish prefab is missing FishController component.");
                Destroy(fishGO);
                yield break;
            }

            // Subscribe to result
            FishResult result = FishResult.None;
            currentFish.OnFishResult += (r, f) => result = r;

            currentFish.Spawn(pattern, circleCenter, pinkBar, startAngle);

            // Apply pattern-specific color to the fish sprite
            FishColorController colorCtrl = fishGO.GetComponent<FishColorController>();
            colorCtrl?.Apply(patternType);

            // Wait until the fish resolves
            yield return new WaitUntil(() => result != FishResult.None);

            HandleFishResult(result);
            currentFish = null;
        }

        void HandleFishResult(FishResult result)
        {
            if (result == FishResult.Caught)
            {
                FishCaught = true;
                ShowResultMessage("CAUGHT!", Color.cyan);

                // Notify ScoreManager / GameManager that Phase 3 is now unlocked
                Core.ScoreManager.Instance?.OnFishCaught();

                Debug.Log("[Fishing] Fish caught! Phase 3 unlocked.");
            }
            else if (result == FishResult.Missed)
            {
                ShowResultMessage("MISSED!", new Color(1f, 0.4f, 0.4f));
                Debug.Log("[Fishing] Fish missed. Trying again.");
            }
        }

        // ─── Result message display ───────────────────────────────────────────

        void ShowResultMessage(string msg, Color color)
        {
            if (resultMessageText == null) return;

            if (resultMessageCoroutine != null) StopCoroutine(resultMessageCoroutine);
            resultMessageCoroutine = StartCoroutine(DisplayMessage(msg, color));
        }

        IEnumerator DisplayMessage(string msg, Color color)
        {
            resultMessageText.text  = msg;
            resultMessageText.color = color;
            resultMessageText.gameObject.SetActive(true);

            yield return new WaitForSeconds(resultMessageDuration);

            resultMessageText.gameObject.SetActive(false);
        }

        // ─── Helpers ──────────────────────────────────────────────────────────

        /// Scale all waypoint radii so the fish travels on the correct circle.
        void ScalePatternToRadius(FishPatternData pattern, float targetRadius)
        {
            foreach (var wp in pattern.waypoints)
                wp.radius *= targetRadius;
        }
    }
}
