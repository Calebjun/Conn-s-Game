using UnityEngine;
using RhythmGame.Core;

public class ChartSetup : MonoBehaviour
{
    [Header("Audio Clips")]
    public AudioClip phase1Clip;
    public AudioClip transition1to2Clip;
    public AudioClip phase2Clip;
    public AudioClip transition2to3Clip;
    public AudioClip phase3Clip;
    public AudioClip transition3to2Clip;
    public AudioClip transition2to1Clip;

    [Header("BPM (must match your song)")]
    public float bpm = 120f;

    [Header("Sensitivity (0 = more notes, 1 = fewer)")]
    public float sensitivity = 0.5f;

    [Header("References")]
    public NoteSpawner spawner;

    private bool generated = false;

    void Awake()
    {
        GenerateCharts();
    }

    void GenerateCharts()
    {
        if (generated) return;
        generated = true;

        ChartData p1    = ChartBuilder.CreateChartFromAudio(phase1Clip,         bpm, 4, sensitivity);
        ChartData t1to2 = ChartBuilder.CreateChartFromAudio(transition1to2Clip, bpm, 4, sensitivity);
        ChartData p2    = ChartBuilder.CreateChartFromAudio(phase2Clip,         bpm, 4, sensitivity);
        ChartData t2to3 = ChartBuilder.CreateChartFromAudio(transition2to3Clip, bpm, 4, sensitivity);
        ChartData p3    = ChartBuilder.CreateChartFromAudio(phase3Clip,         bpm, 4, sensitivity);
        ChartData t3to2 = ChartBuilder.CreateChartFromAudio(transition3to2Clip, bpm, 4, sensitivity);
        ChartData t2to1 = ChartBuilder.CreateChartFromAudio(transition2to1Clip, bpm, 4, sensitivity);

        spawner.SetPhaseCharts(p1, t1to2, p2, t2to3, p3, t3to2, t2to1);

        Debug.Log("[ChartSetup] All charts generated and assigned.");
    }
}