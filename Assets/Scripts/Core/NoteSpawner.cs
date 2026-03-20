using UnityEngine;
using System.Collections.Generic;

namespace RhythmGame.Core
{
    public class NoteSpawner : MonoBehaviour
    {
        public static NoteSpawner Instance { get; private set; }

        [Header("Charts")]
        public ChartData phase1Chart;
        public ChartData transition1to2Chart;
        public ChartData phase2Chart;
        public ChartData transition2to3Chart;
        public ChartData phase3Chart;
        public ChartData transition3to2Chart;
        public ChartData transition2to1Chart;

        [Header("Circular Layout")]
        public Transform centerPoint;

        [Header("References")]
        public NotePoolManager pool;
        public Music.MusicLoopController musicController;


        [Header("BPM (must match ChartSetup)")]
        public float bpm = 116f;

        [Header("Clip lengths in seconds (set these to match your actual audio clips)")]
        public float phase1ClipLength = 14f;
        public float transition1to2ClipLength = 4f;
        public float phase2ClipLength = 14f;
        public float transition2to3ClipLength = 4f;
        public float phase3ClipLength = 14f;
        public float transition3to2ClipLength = 4f;
        public float transition2to1ClipLength = 4f;

        public float SongTime { get; private set; } = 0f;
        private bool isRunning = false;

        private ChartData activeChart;
        private float secondsPerBar;
        private float chartStartTime;
        private int lastSpawnedNoteIndex;
        private List<(float hitTime, NoteData data)> flatNoteList = new List<(float, NoteData)>();

        private Music.MusicLoopController.MusicPhase lastPhase;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void StartSong()
        {
            SongTime = 0f;
            isRunning = true;
            secondsPerBar = GetSecondsPerBar(Music.MusicLoopController.MusicPhase.Phase1);
            lastPhase = Music.MusicLoopController.MusicPhase.Stopped;
            SetActiveChart(phase1Chart, Music.MusicLoopController.MusicPhase.Phase1);
        }

        public void StopSong()
        {
            isRunning = false;
            flatNoteList.Clear();
        }

        public void SetPhaseCharts(
            ChartData p1, ChartData t1to2,
            ChartData p2, ChartData t2to3,
            ChartData p3, ChartData t3to2,
            ChartData t2to1)
        {
            phase1Chart         = p1;
            transition1to2Chart = t1to2;
            phase2Chart         = p2;
            transition2to3Chart = t2to3;
            phase3Chart         = p3;
            transition3to2Chart = t3to2;
            transition2to1Chart = t2to1;
        }

        private float lastSongTime = 0f;

        void Update()
        {
            if (!isRunning) return;

            // Sync to the currently active AudioSource in MusicLoopController
            var src = musicController?.ActiveSource;
            float newTime = (src != null && src.isPlaying) ? src.time : SongTime + Time.deltaTime;

            // If time went backward the clip looped — reset note index only
            if (newTime < lastSongTime - 0.2f)
            {
                lastSpawnedNoteIndex = 0;
                // Recalculate hit times relative to new loop start
                if (activeChart != null)
                {
                    flatNoteList.Clear();
                    for (int b = 0; b < activeChart.bars.Length; b++)
                    {
                        var bar = activeChart.bars[b];
                        if (bar == null || bar.notes == null) continue;
                        foreach (var note in bar.notes)
                        {
                            float hitTime = (b * secondsPerBar) + note.beatTime;
                            flatNoteList.Add((hitTime, note));
                        }
                    }
                    flatNoteList.Sort((a, b2) => a.hitTime.CompareTo(b2.hitTime));
                }
            }

            lastSongTime = newTime;
            SongTime = newTime;

            SyncChartToPhase();
            ProcessSpawnQueue();
        }

        void SyncChartToPhase()
        {
            if (musicController == null) return;
            var phase = musicController.CurrentPhase;
            if (phase == lastPhase) return;
            Debug.Log($"[NoteSpawner] Phase changed from {lastPhase} to {phase}");
            lastPhase = phase;

            switch (phase)
            {
                case Music.MusicLoopController.MusicPhase.Phase1:
                    SetActiveChart(phase1Chart, Music.MusicLoopController.MusicPhase.Phase1); break;
                case Music.MusicLoopController.MusicPhase.Phase2:
                    SetActiveChart(phase2Chart, Music.MusicLoopController.MusicPhase.Phase2); break;
                case Music.MusicLoopController.MusicPhase.Phase3:
                    SetActiveChart(phase3Chart, Music.MusicLoopController.MusicPhase.Phase3); break;
                // No notes during transitions
                case Music.MusicLoopController.MusicPhase.Transitioning1to2:
                case Music.MusicLoopController.MusicPhase.Transitioning2to3:
                case Music.MusicLoopController.MusicPhase.Transitioning3to2:
                case Music.MusicLoopController.MusicPhase.Transitioning2to1:
                    ClearNotes(); break;
            }
        }

        void SetActiveChart(ChartData chart, Music.MusicLoopController.MusicPhase phase)
        {
            activeChart = chart;
            // Calculate secondsPerBar from actual clip length / bar count
            float clipLen = GetClipLengthForPhase(phase);
            int barCount = (chart != null && chart.bars != null && chart.bars.Length > 0) ? chart.bars.Length : 1;
            secondsPerBar = clipLen / barCount;
            Debug.Log($"[NoteSpawner] Phase {phase} — clipLen={clipLen} bars={barCount} secondsPerBar={secondsPerBar:F4}");
            SongTime = 0f;
            chartStartTime = 0f;
            lastSpawnedNoteIndex = 0;
            flatNoteList.Clear();

            if (chart == null) return;

            // Flatten all bars into a single time-sorted list
            for (int b = 0; b < chart.bars.Length; b++)
            {
                var bar = chart.bars[b];
                if (bar == null || bar.notes == null) continue;
                foreach (var note in bar.notes)
                {
                    float hitTime = chartStartTime + (b * secondsPerBar) + note.beatTime;
                    flatNoteList.Add((hitTime, note));
                }
            }

            flatNoteList.Sort((a, b2) => a.hitTime.CompareTo(b2.hitTime));
            Debug.Log($"[NoteSpawner] Chart set. {flatNoteList.Count} notes loaded.");
        }

        void ProcessSpawnQueue()
        {
            while (lastSpawnedNoteIndex < flatNoteList.Count)
            {
                var entry = flatNoteList[lastSpawnedNoteIndex];
                float spawnTime = entry.hitTime - NoteObject.LookAheadTime;

                if (SongTime >= spawnTime)
                {
                    SpawnNote(entry.data, entry.hitTime);
                    lastSpawnedNoteIndex++;
                }
                else break;
            }
        }

        void SpawnNote(NoteData data, float absoluteHitTime)
        {
            if (pool == null || data.lane < 0 || data.lane >= 4) return;

            NoteObject note = pool.GetNote(data.lane);
            if (note == null) return;

            Vector3 center = centerPoint != null ? centerPoint.position : Vector3.zero;
            Vector3 pos = NoteObject.GetSpawnPosition(data.lane, center);

            note.transform.position = pos;
            note.Initialize(data.lane, absoluteHitTime, data.noteType, pool);
        }
        float GetSecondsPerBar(Music.MusicLoopController.MusicPhase phase)
        {
            // Use actual clip length divided by bar count for accurate timing
            ChartData chart = GetChartForPhase(phase);
            float clipLength = GetClipLengthForPhase(phase);
            if (chart == null || chart.bars.Length == 0) return (60f / bpm) * 4f;
            return clipLength / chart.bars.Length;
        }

        float GetClipLengthForPhase(Music.MusicLoopController.MusicPhase phase)
        {
            switch (phase)
            {
                case Music.MusicLoopController.MusicPhase.Phase1:           return phase1ClipLength;
                case Music.MusicLoopController.MusicPhase.Transitioning1to2: return transition1to2ClipLength;
                case Music.MusicLoopController.MusicPhase.Phase2:           return phase2ClipLength;
                case Music.MusicLoopController.MusicPhase.Transitioning2to3: return transition2to3ClipLength;
                case Music.MusicLoopController.MusicPhase.Phase3:           return phase3ClipLength;
                case Music.MusicLoopController.MusicPhase.Transitioning3to2: return transition3to2ClipLength;
                case Music.MusicLoopController.MusicPhase.Transitioning2to1: return transition2to1ClipLength;
                default: return (60f / bpm) * 4f;
            }
        }

        ChartData GetChartForPhase(Music.MusicLoopController.MusicPhase phase)
        {
            switch (phase)
            {
                case Music.MusicLoopController.MusicPhase.Phase1:           return phase1Chart;
                case Music.MusicLoopController.MusicPhase.Transitioning1to2: return transition1to2Chart;
                case Music.MusicLoopController.MusicPhase.Phase2:           return phase2Chart;
                case Music.MusicLoopController.MusicPhase.Transitioning2to3: return transition2to3Chart;
                case Music.MusicLoopController.MusicPhase.Phase3:           return phase3Chart;
                case Music.MusicLoopController.MusicPhase.Transitioning3to2: return transition3to2Chart;
                case Music.MusicLoopController.MusicPhase.Transitioning2to1: return transition2to1Chart;
                default: return null;
            }
        }

        // Clears all pending notes immediately (called on phase transition)
        public void ClearNotes()
        {
            flatNoteList.Clear();
            lastSpawnedNoteIndex = 0;
        }
    }
}