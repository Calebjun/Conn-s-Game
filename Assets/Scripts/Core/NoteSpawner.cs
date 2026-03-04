using UnityEngine;
using System.Collections.Generic;

namespace RhythmGame.Core
{
    public class NoteSpawner : MonoBehaviour
    {
        public static NoteSpawner Instance { get; private set; }

        [Header("Charts (assign manually or via ChartSetup)")]
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

        public float SongTime { get; private set; } = 0f;
        private bool isRunning = false;

        private ChartData activeChart;
        private List<NoteData> activeNoteQueue = new List<NoteData>();
        private int noteQueueIndex = 0;
        private float barStartTime = 0f;
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
            SetActiveChart(phase1Chart);
            lastPhase = Music.MusicLoopController.MusicPhase.Stopped;
        }

        public void StopSong()
        {
            isRunning = false;
            activeNoteQueue.Clear();
        }

        // Called by ChartSetup to assign all charts at runtime
        public void SetPhaseCharts(
            ChartData p1, ChartData t1to2,
            ChartData p2, ChartData t2to3,
            ChartData p3, ChartData t3to2,
            ChartData t2to1)
        {
            phase1Chart        = p1;
            transition1to2Chart = t1to2;
            phase2Chart        = p2;
            transition2to3Chart = t2to3;
            phase3Chart        = p3;
            transition3to2Chart = t3to2;
            transition2to1Chart = t2to1;
        }

        void Update()
        {
            if (!isRunning) return;
            SongTime += Time.deltaTime;

            // Sync chart to current music phase
            SyncChartToPhase();

            ProcessSpawnQueue();
        }

        void SyncChartToPhase()
        {
            if (musicController == null) return;

            var phase = musicController.CurrentPhase;
            if (phase == lastPhase) return;
            lastPhase = phase;

            switch (phase)
            {
                case Music.MusicLoopController.MusicPhase.Phase1:
                    SetActiveChart(phase1Chart); break;
                case Music.MusicLoopController.MusicPhase.Transitioning1to2:
                    SetActiveChart(transition1to2Chart); break;
                case Music.MusicLoopController.MusicPhase.Phase2:
                    SetActiveChart(phase2Chart); break;
                case Music.MusicLoopController.MusicPhase.Transitioning2to3:
                    SetActiveChart(transition2to3Chart); break;
                case Music.MusicLoopController.MusicPhase.Phase3:
                    SetActiveChart(phase3Chart); break;
                case Music.MusicLoopController.MusicPhase.Transitioning3to2:
                    SetActiveChart(transition3to2Chart); break;
                case Music.MusicLoopController.MusicPhase.Transitioning2to1:
                    SetActiveChart(transition2to1Chart); break;
            }
        }

        void SetActiveChart(ChartData chart)
        {
            activeChart = chart;
            activeNoteQueue.Clear();
            noteQueueIndex = 0;
            barStartTime = SongTime;

            if (chart == null) return;

            // Load first bar of the new chart immediately
            LoadBar(0);
        }

        public void OnBarStarted(int barIndex, float barStartSongTime)
        {
            barStartTime = barStartSongTime;
            LoadBar(barIndex);
        }

        void LoadBar(int barIndex)
        {
            activeNoteQueue.Clear();
            noteQueueIndex = 0;

            if (activeChart == null) return;
            if (barIndex < 0 || barIndex >= activeChart.bars.Length) return;

            var bar = activeChart.bars[barIndex];
            if (bar == null || bar.notes == null) return;

            var sorted = new List<NoteData>(bar.notes);
            sorted.Sort((a, b) => a.beatTime.CompareTo(b.beatTime));
            activeNoteQueue = sorted;
        }

        void ProcessSpawnQueue()
        {
            while (noteQueueIndex < activeNoteQueue.Count)
            {
                NoteData note = activeNoteQueue[noteQueueIndex];
                float absoluteHitTime = barStartTime + note.beatTime;
                float spawnTime = absoluteHitTime - NoteObject.LookAheadTime;

                if (SongTime >= spawnTime)
                {
                    SpawnNote(note, absoluteHitTime);
                    noteQueueIndex++;
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
    }
}