using UnityEngine;
using UnityEngine.UI;

namespace RhythmGame.Fishing
{
    /// <summary>
    /// The pink arc bar that tracks the mouse position around the fishing circle.
    ///
    /// Setup:
    ///   - Attach to a UI Image (circle type) or a world-space SpriteRenderer.
    ///   - Set pivot to center (0.5, 0.5).
    ///   - The bar rotates around the circle center to follow the mouse.
    ///
    /// How it works:
    ///   1. Convert mouse position to world space via the game camera.
    ///   2. Calculate angle from circle center to mouse.
    ///   3. Rotate the bar object to that angle, snapped to the circle radius.
    ///
    /// The pink bar is a child object whose pivot is at the circle center.
    /// Its visual sits at the radius offset, so rotating the parent places it
    /// at the correct arc position.
    /// </summary>
    public class PinkBarController : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The camera used to convert mouse position to world space.")]
        public Camera gameCamera;

        [Tooltip("The center of the fishing circle in world space.")]
        public Transform circleCenter;

        [Header("Settings")]
        [Tooltip("Radius of the fishing circle in world units.")]
        public float circleRadius = 3f;

        [Tooltip("Angular width of the pink bar in degrees.")]
        public float barArcDegrees = 40f;

        [Tooltip("Smoothing speed for bar movement. Higher = snappier.")]
        public float followSmoothing = 20f;

        // Current angle of the bar center in degrees (0 = right, 90 = up)
        public float CurrentAngle { get; private set; } = 0f;

        // Min/max angles the bar covers — used for collision checks
        public float BarMinAngle => NormalizeAngle(CurrentAngle - barArcDegrees * 0.5f);
        public float BarMaxAngle => NormalizeAngle(CurrentAngle + barArcDegrees * 0.5f);

        private float targetAngle = 0f;

        void Update()
        {
            if (gameCamera == null || circleCenter == null) return;

            UpdateTargetAngle();
            SmoothToTarget();
            ApplyRotation();
        }

        void UpdateTargetAngle()
        {
            // Convert mouse screen position to world space
            Vector3 mouseScreen = UnityEngine.Input.mousePosition;
            mouseScreen.z = Mathf.Abs(gameCamera.transform.position.z - circleCenter.position.z);
            Vector3 mouseWorld = gameCamera.ScreenToWorldPoint(mouseScreen);

            // Get angle from circle center to mouse
            Vector2 dir = (Vector2)(mouseWorld - circleCenter.position);
            targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        }

        void SmoothToTarget()
        {
            // Smooth rotation across the shortest arc
            float delta = Mathf.DeltaAngle(CurrentAngle, targetAngle);
            CurrentAngle += delta * Mathf.Clamp01(followSmoothing * Time.deltaTime);
        }

        void ApplyRotation()
        {
            // Rotate the bar object — its visual child sits at circleRadius offset
            // so this positions it correctly on the arc
            transform.rotation = Quaternion.Euler(0f, 0f, CurrentAngle);
        }

        /// <summary>
        /// Returns true if the given angle (degrees) falls within the pink bar's arc.
        /// Handles wraparound at 0/360.
        /// </summary>
        public bool ContainsAngle(float angle)
        {
            float half = barArcDegrees * 0.5f;
            float diff = Mathf.Abs(Mathf.DeltaAngle(CurrentAngle, angle));
            return diff <= half;
        }

        /// <summary>
        /// Normalize angle to -180..180 range.
        /// </summary>
        public static float NormalizeAngle(float angle)
        {
            while (angle > 180f)  angle -= 360f;
            while (angle < -180f) angle += 360f;
            return angle;
        }

        void OnDrawGizmosSelected()
        {
            if (circleCenter == null) return;
            Gizmos.color = new Color(1f, 0.3f, 0.6f, 0.6f);
            Gizmos.DrawWireSphere(circleCenter.position, circleRadius);
        }
    }
}
