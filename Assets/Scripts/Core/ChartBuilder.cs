using UnityEngine;
using System.Collections.Generic;
using RhythmGame.Core;

namespace RhythmGame.Core
{
    /// <summary>
    /// Builds ChartData from an AudioClip by detecting energy spikes in the audio.
    ///
    /// Usage:
    ///   var chart = ChartBuilder.CreateChartFromAudio(myClip, bpm: 116f);
    ///
    /// You can also still build charts manually with AddNote().
    /// </summary>
    public static class ChartBuilder
    {
        // ─── Audio-based chart generation ─────────────────────────────────────────

        /// <summary>
        /// Analyzes an AudioClip and generates a ChartData with notes placed
        /// at detected energy peaks, snapped to the nearest beat.
        /// </summary>
        /// <param name="clip">The song to analyze.</param>
        /// <param name="bpm">Tempo of the song in beats per minute.</param>
        /// <param name="beatsPerBar">Time signature numerator (default 4).</param>
        /// <param name="sensitivity">0-1, lower = more notes, higher = fewer notes.</param>
        public static ChartData CreateChartFromAudio(AudioClip clip, float bpm, int beatsPerBar = 4, float sensitivity = 0.5f)
        {
            ChartData chart = ScriptableObject.CreateInstance<ChartData>();
            chart.bpm = bpm;

            float secondsPerBeat = 60f / bpm;
            float barDuration    = secondsPerBeat * beatsPerBar;
            float songLength     = clip.length;
            int barCount         = Mathf.Min(Mathf.FloorToInt(songLength / barDuration), 18);

           chart.bars = new BarData[barCount];
for (int b = 0; b < barCount; b++)
    chart.bars[b] = new BarData { barIndex = b };

            // Read raw audio samples (mono mixdown)
            float[] samples = GetMonoSamples(clip);
            int sampleRate  = clip.frequency;

            // Detect beat times using energy analysis
            List<float> beatTimes = DetectBeats(samples, sampleRate, bpm, sensitivity);

            // Snap each detected beat to the nearest beat grid position
            // and assign it to the correct bar + lane
            int laneCounter = 0;
            foreach (float time in beatTimes)
            {
                float snapped = SnapToBeat(time, secondsPerBeat);
                int barIndex  = Mathf.FloorToInt(snapped / barDuration);

                if (barIndex < 0 || barIndex >= barCount) continue;

                float beatTimeInBar = snapped - (barIndex * barDuration);
                int lane = Random.Range(0, 4);

                chart.bars[barIndex].notes.Add(new NoteData
                {
                    lane     = lane,
                    beatTime = beatTimeInBar,
                    noteType = NoteType.Normal
                });

                laneCounter++;
            }

            // Remove duplicate notes (same bar, same lane, same beat time)
            for (int b = 0; b < barCount; b++)
                RemoveDuplicates(chart.bars[b]);

            Debug.Log($"[ChartBuilder] Generated chart from \"{clip.name}\": {bpm} BPM, {barCount} bars, {CountNotes(chart)} notes");
            return chart;
        }

        // ─── Audio analysis helpers ───────────────────────────────────────────────

        static float[] GetMonoSamples(AudioClip clip)
        {
            float[] data = new float[clip.samples * clip.channels];
            clip.GetData(data, 0);

            if (clip.channels == 1) return data;

            // Mix down to mono
            float[] mono = new float[clip.samples];
            for (int i = 0; i < clip.samples; i++)
            {
                float sum = 0f;
                for (int ch = 0; ch < clip.channels; ch++)
                    sum += data[i * clip.channels + ch];
                mono[i] = sum / clip.channels;
            }
            return mono;
        }

        static List<float> DetectBeats(float[] samples, int sampleRate, float bpm, float sensitivity)
        {
            List<float> beats = new List<float>();

            // Window size: analyze in chunks roughly 1/8 of a beat
            float secondsPerBeat = 60f / bpm;
            int windowSize = Mathf.Max(1, (int)(sampleRate * secondsPerBeat / 8f));
            int windowCount = samples.Length / windowSize;

            // Calculate energy for each window
            float[] energy = new float[windowCount];
            for (int w = 0; w < windowCount; w++)
            {
                float sum = 0f;
                int start = w * windowSize;
                for (int s = 0; s < windowSize && (start + s) < samples.Length; s++)
                {
                    float sample = samples[start + s];
                    sum += sample * sample;
                }
                energy[w] = sum / windowSize;
            }

            // Compare each window to a local average to find peaks
            int avgRadius = Mathf.Max(1, (int)(sampleRate / (float)windowSize * 0.5f));
            float threshold = Mathf.Lerp(1.2f, 2.5f, sensitivity);
            float minGap = secondsPerBeat * 0.4f;
            float lastBeatTime = -1f;

            for (int w = avgRadius; w < windowCount - avgRadius; w++)
            {
                // Local average energy
                float avg = 0f;
                for (int j = w - avgRadius; j <= w + avgRadius; j++)
                    avg += energy[j];
                avg /= (avgRadius * 2 + 1);

                // Peak detection: energy must exceed threshold * local average
                if (energy[w] > avg * threshold && energy[w] > energy[w - 1] && energy[w] >= energy[w + 1])
                {
                    float time = (float)w * windowSize / sampleRate;

                    // Enforce minimum gap between detections
                    if (time - lastBeatTime >= minGap)
                    {
                        beats.Add(time);
                        lastBeatTime = time;
                    }
                }
            }

            return beats;
        }

        static float SnapToBeat(float time, float secondsPerBeat)
        {
            return Mathf.Round(time / secondsPerBeat) * secondsPerBeat;
        }

        // ─── Cleanup helpers ──────────────────────────────────────────────────────

        static void RemoveDuplicates(BarData bar)
        {
            if (bar == null || bar.notes.Count < 2) return;

            var unique = new List<NoteData>();
            var seen   = new HashSet<string>();

            foreach (var note in bar.notes)
            {
                string key = $"{note.lane}_{note.beatTime:F4}";
                if (seen.Add(key))
                    unique.Add(note);
            }

            bar.notes = unique;
        }

        static int CountNotes(ChartData chart)
        {
            int total = 0;
            foreach (var bar in chart.bars)
                if (bar != null) total += bar.notes.Count;
            return total;
        }

        // ─── Manual chart building ────────────────────────────────────────────────

        /// <summary>
        /// Adds a note to a specific bar in a chart.
        /// beatTime is in seconds from the start of that bar.
        /// </summary>
        public static void AddNote(ChartData chart, int barIndex, int lane, float beatTime, NoteType type = NoteType.Normal)
        {
            if (chart == null || barIndex < 0 || barIndex >= chart.bars.Length) return;
            if (chart.bars[barIndex] == null) chart.bars[barIndex] = new BarData { barIndex = barIndex };

            chart.bars[barIndex].notes.Add(new NoteData
            {
                lane     = lane,
                beatTime = beatTime,
                noteType = type
            });
        }
    }
}
