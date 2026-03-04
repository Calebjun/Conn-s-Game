using UnityEngine;
using System.Collections.Generic;

namespace RhythmGame.Fishing
{
    /// <summary>
    /// Generates the 5 fish movement patterns at runtime.
    /// No ScriptableObject needed — patterns are built from math.
    ///
    /// Each pattern runs for roughly 2-3 seconds of screen time
    /// per the design notes.
    /// </summary>
    public static class FishPatternFactory
    {
        public static FishPatternData CreatePattern(FishPatternType type, float startAngle)
        {
            FishPatternData data = ScriptableObject.CreateInstance<FishPatternData>();
            data.patternType = type;
            data.waypoints   = new List<FishWaypoint>();

            switch (type)
            {
                case FishPatternType.Pattern1_Clockwise:
                    BuildClockwise(data, startAngle);
                    break;
                case FishPatternType.Pattern2_CounterClockwise:
                    BuildCounterClockwise(data, startAngle);
                    break;
                case FishPatternType.Pattern3_ZigZag:
                    BuildZigZag(data, startAngle);
                    break;
                case FishPatternType.Pattern4_Dart:
                    BuildDart(data, startAngle);
                    break;
                case FishPatternType.Pattern5_Spiral:
                    BuildSpiral(data, startAngle);
                    break;
            }

            return data;
        }

        // ── Pattern 1: Smooth clockwise arc, 3 waypoints ─────────────────────
        static void BuildClockwise(FishPatternData data, float start)
        {
            float step = 45f;
            for (int i = 1; i <= 6; i++)
            {
                data.waypoints.Add(new FishWaypoint
                {
                    angle    = start - step * i,
                    radius   = 1f,
                    duration = 0.38f
                });
            }
        }

        // ── Pattern 2: Counter-clockwise, slightly faster ─────────────────────
        static void BuildCounterClockwise(FishPatternData data, float start)
        {
            float step = 50f;
            for (int i = 1; i <= 5; i++)
            {
                data.waypoints.Add(new FishWaypoint
                {
                    angle    = start + step * i,
                    radius   = 1f,
                    duration = 0.32f
                });
            }
        }

        // ── Pattern 3: ZigZag — alternates ahead and behind ──────────────────
        static void BuildZigZag(FishPatternData data, float start)
        {
            float[] offsets = { 30f, -20f, 50f, -10f, 40f, -30f, 60f };
            float current = start;
            foreach (float offset in offsets)
            {
                current += offset;
                data.waypoints.Add(new FishWaypoint
                {
                    angle    = current,
                    radius   = 1f,
                    duration = 0.28f
                });
            }
        }

        // ── Pattern 4: Dart — fast straight shot across then slow return ──────
        static void BuildDart(FishPatternData data, float start)
        {
            // Quick dash across half the circle
            data.waypoints.Add(new FishWaypoint { angle = start + 160f, radius = 1f, duration = 0.5f });
            // Slow drift back
            data.waypoints.Add(new FishWaypoint { angle = start + 200f, radius = 1f, duration = 0.9f });
            data.waypoints.Add(new FishWaypoint { angle = start + 240f, radius = 1f, duration = 0.7f });
        }

        // ── Pattern 5: Spiral — moves in toward center then out ──────────────
        static void BuildSpiral(FishPatternData data, float start)
        {
            // Spiral inward
            float[] radii   = { 0.9f, 0.7f, 0.55f, 0.7f, 0.9f, 1.0f };
            float[] angles  = { 40f,  80f,  130f,  180f, 220f, 260f  };
            for (int i = 0; i < radii.Length; i++)
            {
                data.waypoints.Add(new FishWaypoint
                {
                    angle    = start + angles[i],
                    radius   = radii[i],
                    duration = 0.4f
                });
            }
        }

        public static FishPatternType GetRandomPattern()
        {
            int count = System.Enum.GetValues(typeof(FishPatternType)).Length;
            return (FishPatternType)Random.Range(0, count);
        }
    }
}
