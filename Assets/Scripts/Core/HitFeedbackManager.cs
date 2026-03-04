using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

namespace RhythmGame.Core
{
    /// <summary>
    /// Shows PERFECT / GREAT / GOOD / MISS text and lane flash effects.
    /// Wire up the UI Text elements and lane flash images in the Inspector.
    /// </summary>
    public class HitFeedbackManager : MonoBehaviour
    {
        public static HitFeedbackManager Instance { get; private set; }

        [Header("Rating Text")]
        public TMP_Text ratingText;
        public float ratingDisplayTime = 0.6f;

        [Header("Lane Flash Images (one per lane)")]
        public Image[] laneFlashImages;
        public float flashDuration = 0.1f;

        [Header("Colors")]
        public Color perfectColor = new Color(1f, 0.9f, 0f);   // Gold
        public Color greatColor   = new Color(0f, 0.8f, 1f);   // Cyan
        public Color goodColor    = new Color(0f, 1f, 0.4f);   // Green
        public Color missColor    = new Color(1f, 0.2f, 0.2f); // Red

        private Coroutine ratingCoroutine;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void ShowFeedback(int lane, HitRating rating)
        {
            ShowRatingText(rating);
            if (lane >= 0 && lane < laneFlashImages.Length)
                StartCoroutine(FlashLane(lane, rating));
        }

        void ShowRatingText(HitRating rating)
        {
            if (ratingText == null) return;

            if (ratingCoroutine != null) StopCoroutine(ratingCoroutine);
            ratingCoroutine = StartCoroutine(DisplayRating(rating));
        }

        IEnumerator DisplayRating(HitRating rating)
        {
            ratingText.gameObject.SetActive(true);

            switch (rating)
            {
                case HitRating.Perfect:
                    ratingText.text  = "PERFECT";
                    ratingText.color = perfectColor;
                    break;
                case HitRating.Great:
                    ratingText.text  = "GREAT";
                    ratingText.color = greatColor;
                    break;
                case HitRating.Good:
                    ratingText.text  = "GOOD";
                    ratingText.color = goodColor;
                    break;
                case HitRating.Miss:
                    ratingText.text  = "MISS";
                    ratingText.color = missColor;
                    break;
            }

            yield return new WaitForSeconds(ratingDisplayTime);
            ratingText.gameObject.SetActive(false);
        }

        IEnumerator FlashLane(int lane, HitRating rating)
        {
            Image img = laneFlashImages[lane];
            Color col = rating switch
            {
                HitRating.Perfect => perfectColor,
                HitRating.Great   => greatColor,
                HitRating.Good    => goodColor,
                _                 => missColor
            };
            col.a = 0.5f;
            img.color = col;
            img.gameObject.SetActive(true);

            yield return new WaitForSeconds(flashDuration);

            img.gameObject.SetActive(false);
        }
    }
}
