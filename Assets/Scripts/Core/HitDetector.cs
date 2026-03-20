using UnityEngine;
using System.Collections.Generic;
using RhythmGame.Input;

namespace RhythmGame.Core
{
    /// <summary>
    /// Listens for lane key presses and checks if any active note in that
    /// lane is within the hit window. Passes the result to ScoreManager.
    /// </summary>
    public class HitDetector : MonoBehaviour
    {
        public static HitDetector Instance { get; private set; }

        [Header("References")]
        public ScoreManager scoreManager;
        public NoteSpawner noteSpawner;

        // Active notes tracked per lane
        private Dictionary<int, List<NoteObject>> laneNotes = new Dictionary<int, List<NoteObject>>();

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            for (int i = 0; i < 4; i++)
                laneNotes[i] = new List<NoteObject>();
        }

        void Start()
        {
            if (InputManager.Instance != null)
                InputManager.Instance.OnLanePressed += OnLanePressed;
            else
                Debug.LogError("[HitDetector] InputManager.Instance is null. Make sure InputManager is in the scene.");
        }

        void OnDisable()
        {
            if (InputManager.Instance != null)
                InputManager.Instance.OnLanePressed -= OnLanePressed;
        }

        // NoteObjects register themselves when they become active
        public void RegisterNote(NoteObject note)
        {
            if (laneNotes.ContainsKey(note.Lane))
                laneNotes[note.Lane].Add(note);
        }

        public void UnregisterNote(NoteObject note)
        {
            if (laneNotes.ContainsKey(note.Lane))
                laneNotes[note.Lane].Remove(note);

            Debug.Log($"[HitDetector] Unregistering lane {note.Lane} note.");
            if (laneNotes.ContainsKey(note.Lane))
                laneNotes[note.Lane].Remove(note);
        }

        void OnLanePressed(int lane)
        {
            if (!laneNotes.ContainsKey(lane)) return;

            float songTime = noteSpawner != null ? noteSpawner.SongTime : 0f;
            Debug.Log($"[HitDetector] Lane {lane} pressed. Notes in lane: {laneNotes[lane].Count}. SongTime: {songTime}");
            NoteObject closest = FindClosestNote(lane, songTime);

            if (closest == null) { Debug.Log($"[HitDetector] No note found in lane {lane}."); return; }

            float timingError2 = songTime - closest.BeatTime;
            Debug.Log($"[HitDetector] Closest note BeatTime: {closest.BeatTime}, timingError: {timingError2}");

            float timingError = songTime - closest.BeatTime;
            HitRating rating = scoreManager.EvaluateTiming(timingError);

            if (rating != HitRating.Miss)
            {
                closest.Hit();
                laneNotes[lane].Remove(closest);
                scoreManager.RegisterHit(rating);

                // Visual feedback
                HitFeedbackManager.Instance?.ShowFeedback(lane, rating);
            }
            // If miss window: do nothing here, NoteObject auto-misses when it falls past
        }

        NoteObject FindClosestNote(int lane, float songTime)
        {
            NoteObject closest = null;
            float closestDelta = float.MaxValue;

            foreach (var note in laneNotes[lane])
            {
                if (note.WasHit || note.WasMissed) continue;

                float delta = Mathf.Abs(songTime - note.BeatTime);
                if (delta < closestDelta)
                {
                    closestDelta = delta;
                    closest = note;
                }
            }

            return closest;
        }
    }
}
