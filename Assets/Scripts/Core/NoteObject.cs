using UnityEngine;

namespace RhythmGame.Core
{
    /// <summary>
    /// Attached to each note GameObject.
    /// Spawns at the outer edge of its lane and travels inward toward the
    /// center hit zone, arriving exactly at BeatTime.
    ///
    /// Lane layout (circular):
    ///   Lane 0 = Top    (W) — travels downward  (Vector3.down)
    ///   Lane 1 = Left   (A) — travels rightward (Vector3.right)
    ///   Lane 2 = Bottom (S) — travels upward    (Vector3.up)
    ///   Lane 3 = Right  (D) — travels leftward  (Vector3.left)
    /// </summary>
    public class NoteObject : MonoBehaviour
    {
        public int Lane       { get; private set; }
        public float BeatTime { get; private set; }
        public NoteType Type  { get; private set; }
        public bool WasHit    { get; private set; } = false;
        public bool WasMissed { get; private set; } = false;

        // Seconds before hit time that the note appears at the spawn radius
        public static float LookAheadTime = 1.0f;

        // Distance from center to spawn point (world units)
        public static float SpawnRadius   = 2.5f;

        // Distance from center to the hit zone ring
        public static float HitZoneRadius = 0.9f;

        // Inward travel directions for each lane
        private static readonly Vector3[] LaneDirections = new Vector3[]
        {
            Vector3.down,   // Lane 0: W = Top (Blue)
            Vector3.up,     // Lane 1: A = Left (Green)
            Vector3.left,   // Lane 2: S = Bottom (Orange)
            Vector3.right,  // Lane 3: D = Right (Red)
        };

        private Vector3 travelDirection;
        private float moveSpeed;
        private NotePoolManager pool;

        public void Initialize(int lane, float beatTime, NoteType type, NotePoolManager poolRef)
        {
            Lane      = lane;
            BeatTime  = beatTime;
            Type      = type;
            pool      = poolRef;
            WasHit    = false;
            WasMissed = false;

            travelDirection = LaneDirections[Mathf.Clamp(lane, 0, LaneDirections.Length - 1)];

            // Speed so it crosses SpawnRadius -> HitZoneRadius in LookAheadTime seconds
            moveSpeed = (SpawnRadius - HitZoneRadius) / LookAheadTime;

            // Register with HitDetector so key presses can find this note
            HitDetector.Instance?.RegisterNote(this);
        }

        void Update()
        {
            transform.Translate(travelDirection * moveSpeed * Time.deltaTime, Space.World);

            if (!WasHit && !WasMissed)
            {
                float songTime = NoteSpawner.Instance != null ? NoteSpawner.Instance.SongTime : 0f;
                if (songTime > BeatTime + 0.6f)
                    Miss();
            }
        }

        public void Hit()
        {
            WasHit = true;
            ReturnToPool();
        }

        public void Miss()
        {
            WasMissed = true;
            ScoreManager.Instance?.RegisterHit(HitRating.Miss);
            ReturnToPool();
        }

        void ReturnToPool()
        {
            HitDetector.Instance?.UnregisterNote(this);
            pool?.ReturnNote(this, Lane);
        }

        // Returns the world spawn position for a given lane, centered on a given origin
        public static Vector3 GetSpawnPosition(int lane, Vector3 center)
        {
            // Spawn in the opposite direction of travel, at SpawnRadius distance
            Vector3 outward = -LaneDirections[Mathf.Clamp(lane, 0, LaneDirections.Length - 1)];
            return center + outward * SpawnRadius;
        }

        // Returns the world hit zone position for a given lane, centered on a given origin
        public static Vector3 GetHitZonePosition(int lane, Vector3 center)
        {
            Vector3 outward = -LaneDirections[Mathf.Clamp(lane, 0, LaneDirections.Length - 1)];
            return center + outward * HitZoneRadius;
        }
    }
}