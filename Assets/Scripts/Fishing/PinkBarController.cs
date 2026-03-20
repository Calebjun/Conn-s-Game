using UnityEngine;
using UInput = UnityEngine.Input;

namespace RhythmGame.Fishing
{
    /// <summary>
    /// Tracks the mouse cursor position in world space.
    /// FishController checks distance from cursor to fish to detect hits.
    /// </summary>
    public class PinkBarController : MonoBehaviour
    {
        [Header("References")]
        public Camera gameCamera;
        public Transform circleCenter;

        [Header("Settings")]
        public float circleRadius = 1.582f;

        // Current mouse position in world space
        public Vector3 CursorWorldPosition { get; private set; }

        void Update()
        {
            if (gameCamera == null) return;

            // Use the circle center Z to ensure cursor is on the same plane as the fish
            float targetZ = circleCenter != null ? circleCenter.position.z : 0f;
            float distToPlane = Mathf.Abs(gameCamera.transform.position.z - targetZ);

            Vector3 mouseScreen = UInput.mousePosition;
            mouseScreen.z = distToPlane;
            CursorWorldPosition = gameCamera.ScreenToWorldPoint(mouseScreen);
        }

        // Keep this so existing code that calls ContainsAngle still compiles
        // but it now checks cursor proximity to a world position instead
        public bool IsNearPosition(Vector3 worldPos, float hitRadius = 0.5f)
        {
            return Vector3.Distance(CursorWorldPosition, worldPos) <= hitRadius;
        }

        // Legacy method kept for compatibility
        public bool ContainsAngle(float angle) => false;

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0.6f, 0.6f);
            Gizmos.DrawWireSphere(CursorWorldPosition, 0.3f);
        }
    }
}
