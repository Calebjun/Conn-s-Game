using UnityEngine;
using System.Collections;
using TMPro;

namespace RhythmGame.Fishing
{
    public class FishingManager : MonoBehaviour
    {
        public static FishingManager Instance { get; private set; }

        [Header("References")]
        public Core.ScoreManager scoreManager;
        public PinkBarController pinkBar;
        public Transform circleCenter;
        public Transform fishSpawnParent;

        [Header("Fish Prefabs")]
        public GameObject fish1Prefab;
        public GameObject fish2Prefab;
        public GameObject fish3Prefab;
        public GameObject fish4Prefab;
        public GameObject fish5Prefab;

        [Header("UI")]
        public GameObject fishingUIRoot;
        public Animator fishingUIAnimator;
        public TMP_Text resultMessageText;
        public float resultMessageDuration = 1.2f;

        [Header("Settings")]
        public float spawnAngleVariance = 180f;

        public bool IsActive { get; private set; } = false;
        public bool FishCaught { get; private set; } = false;

        private FishController currentFish;
        private Coroutine resultMessageCoroutine;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            FishCaught = false;
            gameObject.SetActive(true);
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

            if (fishingUIRoot != null) fishingUIRoot.SetActive(false);
        }

        [Header("Catch Goal")]
        public int fishNeededToCatch = 1;
        private int fishCaughtCount = 0;

        IEnumerator RunFishingSequence()
        {
            yield return StartCoroutine(ShowFishingUI());

            fishCaughtCount = 0;
            while (IsActive && fishCaughtCount < fishNeededToCatch)
            {
                yield return StartCoroutine(SpawnAndRunFish());
            }
        }

        IEnumerator ShowFishingUI()
        {
            if (fishingUIRoot == null) yield break;
            fishingUIRoot.SetActive(true);
            if (fishingUIAnimator != null)
            {
                fishingUIAnimator.SetTrigger("Spawn");
                yield return new WaitForSeconds(0.6f);
            }
        }

        IEnumerator SpawnAndRunFish()
        {
            if (circleCenter == null) yield break;

            FishPatternType patternType = FishPatternFactory.GetRandomPattern();
            float startAngle = Random.Range(-spawnAngleVariance * 0.5f, spawnAngleVariance * 0.5f);

            FishPatternData pattern = FishPatternFactory.CreatePattern(patternType, startAngle);
            ScalePatternToRadius(pattern, pinkBar != null ? pinkBar.circleRadius : 1f);

            GameObject selectedPrefab = patternType switch
            {
                FishPatternType.Pattern1 => fish1Prefab,
                FishPatternType.Pattern2 => fish2Prefab,
                FishPatternType.Pattern3 => fish3Prefab,
                FishPatternType.Pattern4 => fish4Prefab,
                FishPatternType.Pattern5 => fish5Prefab,
                _ => fish1Prefab
            };

            if (selectedPrefab == null)
            {
                Debug.LogError($"[FishingManager] No prefab assigned for pattern {patternType}");
                yield break;
            }

            GameObject fishGO = Instantiate(selectedPrefab, circleCenter.position, Quaternion.identity, fishSpawnParent);
            currentFish = fishGO.GetComponent<FishController>();

            if (currentFish == null)
            {
                Debug.LogError("[FishingManager] Fish prefab is missing FishController component.");
                Destroy(fishGO);
                yield break;
            }

            FishResult result = FishResult.None;
            currentFish.OnFishResult += (r, f) => result = r;
            currentFish.Spawn(pattern, circleCenter, pinkBar, startAngle);

            yield return new WaitUntil(() => result != FishResult.None);

            HandleFishResult(result);
            currentFish = null;
        }

        void HandleFishResult(FishResult result)
        {
            if (result == FishResult.Caught)
            {
                fishCaughtCount++;
                ShowResultMessage("CAUGHT!", Color.cyan);
                if (fishCaughtCount >= fishNeededToCatch)
                {
                    FishCaught = true;
                    Core.ScoreManager.Instance?.OnFishCaught();
                    Debug.Log("[Fishing] All fish caught! Phase 3 unlocked.");
                }
            }
        }

        void ShowResultMessage(string msg, Color color)
        {
            if (resultMessageText == null) return;
            if (resultMessageCoroutine != null) StopCoroutine(resultMessageCoroutine);
            resultMessageCoroutine = StartCoroutine(DisplayMessage(msg, color));
        }

        IEnumerator DisplayMessage(string msg, Color color)
        {
            resultMessageText.text = msg;
            resultMessageText.color = color;
            resultMessageText.gameObject.SetActive(true);
            yield return new WaitForSeconds(resultMessageDuration);
            resultMessageText.gameObject.SetActive(false);
        }

        void ScalePatternToRadius(FishPatternData pattern, float targetRadius)
        {
            foreach (var wp in pattern.waypoints)
                wp.radius *= targetRadius;
        }
    }
}
