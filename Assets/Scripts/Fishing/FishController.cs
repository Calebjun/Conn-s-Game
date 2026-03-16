using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace RhythmGame.Fishing
{
    /// <summary>
    /// A fish sprite that follows waypoints around the fishing circle.
    ///
    /// Fish score (private int) tracks how many times the pink bar
    /// has hit this fish. At 3 hits it is caught. At 0 before the
    /// waypoint completes, it is missed.
    ///
    /// Per the design notes:
    ///   - Score goes up by 1 when fish sprite collides with pink bar.
    ///   - Fish score >= 3 = caught.
    ///   - Miss resets fish score to 1 (not 0), so the next attempt
    ///     starts at 1 instead of fresh.
    ///   - Caught resets fish score to 0.
    ///   - Only punish LATE, not early. Early bar hits count.
    ///   - Notes destroy on waypoint impact.
    /// </summary>
    public class FishController : MonoBehaviour
    {
        [Header("References")]
        public Transform circleCenter;
        public PinkBarController pinkBar;
        public FishPatternData patternData;

        [Header("Visual")]
        public SpriteRenderer spriteRenderer;
        public Animator animator;

        // Animator parameter names
        private const string ANIM_SWIM    = "Swim";
        private const string ANIM_CAUGHT  = "Caught";
        private const string ANIM_MISSED  = "Missed";
        private const string ANIM_SPAWN   = "Spawn";

        // Per-fish hit counter (private as per design notes)
        private int fishScore = 0;
        private const int CATCH_THRESHOLD = 3;

        // Current angle on the circle in degrees
        private float currentAngle = 0f;
        private float currentRadius = 1f;

        private bool isActive = false;
        private bool resultDetermined = false;
        private float spawnTime = 0f;
        private const float HIT_DELAY = 0.3f; // seconds before collision starts counting

        public event System.Action<FishResult, FishController> OnFishResult;

        // ─── Initialise and start waypoint movement ──────────────────────────

        public void Spawn(FishPatternData pattern, Transform center, PinkBarController bar, float startAngle)
        {
            patternData   = pattern;
            circleCenter  = center;
            pinkBar       = bar;
            currentAngle  = startAngle;
            currentRadius = patternData != null && patternData.waypoints.Count > 0
                ? patternData.waypoints[0].radius : 1f;
            fishScore     = 0;
            isActive      = true;
            resultDetermined = false;
            spawnTime     = Time.time;

            UpdateWorldPosition();

            if (animator != null) animator.SetTrigger(ANIM_SPAWN);

            StartCoroutine(FollowWaypoints());
        }

        // ─── Waypoint traversal ───────────────────────────────────────────────

        IEnumerator FollowWaypoints()
        {
            if (patternData == null || patternData.waypoints.Count == 0) yield break;

            foreach (FishWaypoint wp in patternData.waypoints)
            {
                if (!isActive) yield break;

                float targetAngle  = wp.angle;
                float targetRadius = wp.radius;
                float elapsed      = 0f;
                float startAngle   = currentAngle;
                float startRadius  = currentRadius;

                while (elapsed < wp.duration)
                {
                    if (!isActive) yield break;

                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / wp.duration);

                    currentAngle  = Mathf.LerpAngle(startAngle, targetAngle, t);
                    currentRadius = Mathf.Lerp(startRadius, targetRadius, t);

                    UpdateWorldPosition();
                    CheckPinkBarCollision();

                    yield return null;
                }

                currentAngle  = targetAngle;
                currentRadius = targetRadius;

                // Per design notes: destroy note on waypoint impact
                // (this handles intermediate waypoints that act as "checkpoints")
            }

            // All waypoints finished without being caught
            if (!resultDetermined)
                TriggerMiss();
        }

        // ─── Position update ──────────────────────────────────────────────────

        void UpdateWorldPosition()
        {
            if (circleCenter == null) return;

            float rad = currentAngle * Mathf.Deg2Rad;
            Vector3 offset = new Vector3(
                Mathf.Cos(rad) * currentRadius,
                Mathf.Sin(rad) * currentRadius,
                0f
            );
            transform.position = circleCenter.position + offset;

            // Face the direction of travel (tangent to circle)
            float tangentAngle = currentAngle + 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, tangentAngle);
        }

        // ─── Collision with pink bar ──────────────────────────────────────────

        [Header("Hit Detection")]
        public float cursorHitRadius = 0.3f;

        void CheckPinkBarCollision()
        {
            if (pinkBar == null || resultDetermined) return;
            if (Time.time < spawnTime + HIT_DELAY) return;

            // Check if cursor is close enough to the fish
            if (pinkBar.IsNearPosition(transform.position, cursorHitRadius))
            {
                fishScore++;
                Debug.Log($"[Fish] Hit! fishScore={fishScore}");

                if (fishScore >= CATCH_THRESHOLD)
                    TriggerCatch();
            }
        }

        // ─── Results ──────────────────────────────────────────────────────────

        void TriggerCatch()
        {
            if (resultDetermined) return;
            resultDetermined = true;
            isActive = false;

            if (animator != null) animator.SetTrigger(ANIM_CAUGHT);

            fishScore = 0; // reset per design notes
            StartCoroutine(CatchSequence());
        }

        IEnumerator CatchSequence()
        {
            // Wait for caught animation to play
            yield return new WaitForSeconds(0.5f);

            // Move to center then up (follow waypoint to centre, then up)
            yield return StartCoroutine(MoveToPosition(circleCenter.position, 0.4f));
            yield return StartCoroutine(MoveToPosition(circleCenter.position + Vector3.up * 5f, 0.4f));

            OnFishResult?.Invoke(FishResult.Caught, this);
            Destroy(gameObject);
        }

        void TriggerMiss()
        {
            if (resultDetermined) return;
            resultDetermined = true;
            isActive = false;

            if (animator != null) animator.SetTrigger(ANIM_MISSED);

            fishScore = 1; // reset to 1 per design notes (not 0)
            StartCoroutine(MissSequence());
        }

        IEnumerator MissSequence()
        {
            yield return new WaitForSeconds(0.3f);

            // Follow waypoint to UI edge (move outward)
            Vector3 outDir = (transform.position - circleCenter.position).normalized;
            Vector3 edgePos = transform.position + outDir * 6f;
            yield return StartCoroutine(MoveToPosition(edgePos, 0.5f));

            OnFishResult?.Invoke(FishResult.Missed, this);
            Destroy(gameObject);
        }

        IEnumerator MoveToPosition(Vector3 target, float duration)
        {
            Vector3 start = transform.position;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                transform.position = Vector3.Lerp(start, target, elapsed / duration);
                yield return null;
            }
            transform.position = target;
        }

        // ─── Public interface ─────────────────────────────────────────────────

        public int GetFishScore() => fishScore;

        public void ForceStop()
        {
            isActive = false;
            StopAllCoroutines();
        }
    }
}