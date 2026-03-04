using UnityEngine;

namespace RhythmGame.Core
{
    /// <summary>
    /// Procedurally builds the visual ring and lane indicator sprites
    /// for the circular layout at scene start.
    ///
    /// Attach to an empty GameObject at the center of your scene.
    /// Assign the sprite fields in the Inspector.
    ///
    /// What this creates at runtime:
    ///   - A thin circle sprite at HitZoneRadius (the ring players aim for)
    ///   - 4 lane arrow/indicator sprites at the hit zone positions
    ///   - 4 lane "track" lines from the hit zone outward to SpawnRadius
    /// </summary>
    public class CircularLaneVisuals : MonoBehaviour
    {
        [Header("Sprites")]
        [Tooltip("A circular ring sprite, or leave null to skip the ring.")]
        public Sprite hitRingSprite;

        [Tooltip("Arrow or chevron sprite pointing inward, used for each lane indicator.")]
        public Sprite laneIndicatorSprite;

        [Tooltip("Thin rectangle sprite used to draw the approach track for each lane.")]
        public Sprite laneTrackSprite;

        [Header("Colors")]
        public Color hitRingColor      = new Color(1f, 1f, 1f, 0.4f);
        public Color laneTrackColor    = new Color(1f, 1f, 1f, 0.15f);
        public Color[] laneColors = new Color[]
        {
            new Color(0.4f, 0.8f, 1f),   // Lane 0: Top    — blue
            new Color(1f,   0.6f, 0.2f), // Lane 1: Left   — orange
            new Color(0.4f, 1f,   0.4f), // Lane 2: Bottom — green
            new Color(1f,   0.4f, 0.6f), // Lane 3: Right  — pink
        };

        [Header("Sizes")]
        public float hitRingScale      = 0.1f;   // Scale of the ring sprite
        public float indicatorScale    = 0.6f;
        public float trackWidth        = 0.25f;

        // Rotation offsets so the arrow sprite points inward for each lane
        // Lane 0 = Top (arrow points down = 180), Lane 1 = Left (arrow points right = 90)
        // Lane 2 = Bottom (arrow points up = 0),  Lane 3 = Right (arrow points left = 270)
        private static readonly float[] LaneRotations = { 180f, 90f, 0f, 270f };

        void Start()
        {
            BuildHitRing();
            BuildLaneVisuals();
        }

        void BuildHitRing()
        {
            if (hitRingSprite == null) return;

            GameObject ring = new GameObject("HitRing");
            ring.transform.SetParent(transform, false);

            var sr = ring.AddComponent<SpriteRenderer>();
            sr.sprite     = hitRingSprite;
            sr.color      = hitRingColor;
            sr.sortingOrder = -1;

            // Scale the ring to match HitZoneRadius
            float diameter = NoteObject.HitZoneRadius * 2f;
            ring.transform.localScale = new Vector3(diameter, diameter, 1f);
        }

        void BuildLaneVisuals()
        {
            for (int lane = 0; lane < 4; lane++)
            {
                Vector3 hitPos  = NoteObject.GetHitZonePosition(lane, Vector3.zero);
                Vector3 spawnPos = NoteObject.GetSpawnPosition(lane, Vector3.zero);

                BuildTrack(lane, hitPos, spawnPos);
                BuildIndicator(lane, hitPos);
            }
        }

        void BuildTrack(int lane, Vector3 hitPos, Vector3 spawnPos)
        {
            if (laneTrackSprite == null) return;

            GameObject track = new GameObject($"LaneTrack_{lane}");
            track.transform.SetParent(transform, false);

            var sr = track.AddComponent<SpriteRenderer>();
            sr.sprite       = laneTrackSprite;
            sr.color        = laneTrackColor;
            sr.sortingOrder = -2;

            // Center between hit zone and spawn, stretched to fill the gap
            track.transform.position = (hitPos + spawnPos) * 0.5f;

            float length = Vector3.Distance(hitPos, spawnPos);
            bool vertical = lane == 0 || lane == 2;
            track.transform.localScale = vertical
                ? new Vector3(trackWidth, length, 1f)
                : new Vector3(length, trackWidth, 1f);
        }

        void BuildIndicator(int lane, Vector3 hitPos)
        {
            if (laneIndicatorSprite == null) return;

            GameObject indicator = new GameObject($"LaneIndicator_{lane}");
            indicator.transform.SetParent(transform, false);
            indicator.transform.position = hitPos;
            indicator.transform.rotation = Quaternion.Euler(0f, 0f, LaneRotations[lane]);

            var sr = indicator.AddComponent<SpriteRenderer>();
            sr.sprite       = laneIndicatorSprite;
            sr.color        = laneColors[lane];
            sr.sortingOrder = 1;

            indicator.transform.localScale = Vector3.one * indicatorScale;
        }
    }
}
