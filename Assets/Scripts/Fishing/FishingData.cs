using UnityEngine;
using System;
using System.Collections.Generic;

namespace RhythmGame.Fishing
{
    public enum FishResult { None, Caught, Missed }

    public enum FishPatternType
    {
        Pattern1,
        Pattern2,
        Pattern3,
        Pattern4,
        Pattern5
    }

    [Serializable]
    public class FishWaypoint
    {
        // Position on the circle expressed as an angle in degrees (0 = right, 90 = up)
        public float angle;
        // Radius from center (allows spiral/dart patterns to move in/out)
        public float radius = 1f;
        // How long to spend moving to this waypoint
        public float duration = 0.4f;
    }

    [CreateAssetMenu(fileName = "FishPattern", menuName = "RhythmGame/Fish Pattern")]
    public class FishPatternData : ScriptableObject
    {
        public FishPatternType patternType;
        public List<FishWaypoint> waypoints = new List<FishWaypoint>();
        public float moveSpeed = 200f; // degrees per second along arc
    }
}
