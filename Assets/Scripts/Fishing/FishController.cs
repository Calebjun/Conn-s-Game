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
 
        [Header("Hit Settings")]
        public float immunityDuration = 1.5f; // seconds of immunity after spawn
        public float hitCooldown = 0.4f;    // seconds between hits
 
        private bool isActive = false;
        private bool resultDetermined = false;
        private float spawnTime = 0f;
        private float lastHitTime = -999f;
 
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
 
            // Loop waypoints forever until caught
            while (isActive && !resultDetermined)
            {
                foreach (FishWaypoint wp in patternData.waypoints)
                {
                    if (!isActive || resultDetermined) yield break;
 
                    float targetAngle  = wp.angle;
                    float targetRadius = wp.radius;
                    float elapsed      = 0f;
                    float startAngle   = currentAngle;
                    float startRadius  = currentRadius;
 
                    while (elapsed < wp.duration)
                    {
                        if (!isActive || resultDetermined) yield break;
 
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
                }
            }
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
            offset.z = 0f;
            transform.position = circleCenter.position + offset;
 
            // Face the direction of travel (tangent to circle)
            float tangentAngle = currentAngle + 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, tangentAngle);
        }
 
        // ─── Collision with pink bar ──────────────────────────────────────────
 
        [Header("Hit Detection")]
        public float cursorHitRadius = 0.5f;
 
        void CheckPinkBarCollision()
        {
            if (pinkBar == null || resultDetermined) return;
 
            // Immunity frames after spawn
            if (Time.time < spawnTime + immunityDuration) return;
 
            // Cooldown between hits so one pass doesnt count multiple times
            if (Time.time < lastHitTime + hitCooldown) return;
 
            if (pinkBar.IsNearPosition(transform.position, cursorHitRadius))
            {
                fishScore++;
                lastHitTime = Time.time;
                Debug.Log($"[Fish] Hit! fishScore={fishScore}/{CATCH_THRESHOLD}");
 
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