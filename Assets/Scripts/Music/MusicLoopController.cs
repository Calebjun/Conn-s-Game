using UnityEngine;
using System.Collections;

namespace RhythmGame.Music
{
    public class MusicLoopController : MonoBehaviour
    {
        public static MusicLoopController Instance { get; private set; }

        [Header("Phase Loops")]
        public AudioClip phase1Clip;
        public AudioClip phase2Clip;
        public AudioClip phase3Clip;

        [Header("Upward Transitions")]
        public AudioClip transition1to2;
        public AudioClip transition2to3;

        [Header("Downward Transitions")]
        public AudioClip transition2to1;
        public AudioClip transition3to2;

        [Header("References")]
        public Core.ScoreManager scoreManager;

        private AudioSource audioSource;

        public enum MusicPhase
        {
            Stopped,
            Phase1,
            Transitioning1to2,
            Phase2,
            Transitioning2to3,
            Phase3,
            Transitioning3to2,
            Transitioning2to1
        }

        public MusicPhase CurrentPhase { get; private set; } = MusicPhase.Stopped;

        private bool transitioning = false;
        public bool IsPlaying { get; private set; } = false;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
                audioSource = gameObject.AddComponent<AudioSource>();

            audioSource.loop = false;
            audioSource.playOnAwake = false;
        }

        void OnEnable()
        {
            if (scoreManager != null)
                scoreManager.OnBarFillChanged += OnBarFillChanged;
        }

        void OnDisable()
        {
            if (scoreManager != null)
                scoreManager.OnBarFillChanged -= OnBarFillChanged;
        }

        public void StartMusic()
        {
            if (IsPlaying) return;
            IsPlaying = true;
            transitioning = false;
            StartPhase(MusicPhase.Phase1, phase1Clip);
        }

        public void StopMusic()
        {
            StopAllCoroutines();
            audioSource.Stop();
            IsPlaying = false;
            transitioning = false;
            CurrentPhase = MusicPhase.Stopped;
        }

        void OnBarFillChanged(float fill)
        {
            if (!IsPlaying || transitioning) return;

            if (CurrentPhase == MusicPhase.Phase1 && fill >= Core.ScoreManager.PHASE2_THRESHOLD)
            {
                StartCoroutine(Transition(MusicPhase.Transitioning1to2, transition1to2, MusicPhase.Phase2, phase2Clip));
                return;
            }

            if (CurrentPhase == MusicPhase.Phase2
                && fill >= Core.ScoreManager.PHASE3_THRESHOLD
                && scoreManager.FishingComplete)
            {
                StartCoroutine(Transition(MusicPhase.Transitioning2to3, transition2to3, MusicPhase.Phase3, phase3Clip));
                return;
            }

            if (CurrentPhase == MusicPhase.Phase3 && fill < Core.ScoreManager.PHASE3_THRESHOLD)
            {
                StartCoroutine(Transition(MusicPhase.Transitioning3to2, transition3to2, MusicPhase.Phase2, phase2Clip));
                return;
            }

            if (CurrentPhase == MusicPhase.Phase2 && fill < Core.ScoreManager.PHASE2_THRESHOLD)
            {
                StartCoroutine(Transition(MusicPhase.Transitioning2to1, transition2to1, MusicPhase.Phase1, phase1Clip));
            }
        }

        IEnumerator Transition(MusicPhase transPhase, AudioClip transClip, MusicPhase nextPhase, AudioClip nextClip)
        {
            transitioning = true;
            CurrentPhase = transPhase;

            if (transClip != null)
            {
                audioSource.loop = false;
                audioSource.clip = transClip;
                audioSource.Play();
                yield return new WaitForSeconds(transClip.length);
            }

            StartPhase(nextPhase, nextClip);
            transitioning = false;
        }

        void StartPhase(MusicPhase phase, AudioClip clip)
        {
            CurrentPhase = phase;
            if (clip == null) return;
            audioSource.loop = true;
            audioSource.clip = clip;
            audioSource.Play();
            // Tell NoteSpawner the phase changed so it resets its clock
            Core.NoteSpawner.Instance?.OnPhaseStarted();
        }

        public MusicPhase GetCurrentPhase() => CurrentPhase;
    }
}