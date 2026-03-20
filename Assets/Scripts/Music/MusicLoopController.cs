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

        private AudioSource sourceA;
        private AudioSource sourceB;
        private AudioSource activeSource;

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
        public AudioSource ActiveSource => activeSource;

        private bool transitioning = false;
        public bool IsPlaying { get; private set; } = false;

        private AudioClip loopingClip;
        private double nextLoopTime;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            sourceA = gameObject.AddComponent<AudioSource>();
            sourceB = gameObject.AddComponent<AudioSource>();
            foreach (var src in new[] { sourceA, sourceB })
            {
                src.loop = false;
                src.playOnAwake = false;
            }
            activeSource = sourceA;
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

        void Update()
        {
            if (!IsPlaying || transitioning || loopingClip == null) return;

            double dspNow = AudioSettings.dspTime;

            if (dspNow >= nextLoopTime - 0.1 && dspNow < nextLoopTime + 0.1)
            {
                if (transitionPending)
                {
                    // Fire the queued transition at the loop boundary
                    transitionPending = false;
                    StartCoroutine(DoTransition(pendingTransPhase, pendingTransClip, pendingNextPhase, pendingNextClip));
                }
                else
                {
                    AudioSource next = GetInactiveSource();
                    next.clip = loopingClip;
                    next.PlayScheduled(nextLoopTime);
                    nextLoopTime += loopingClip.length;
                    activeSource = next;
                }
            }
            else if (dspNow > nextLoopTime + 0.1)
            {
                // Missed the window, recover
                double scheduleAt = dspNow + 0.05;
                AudioSource next = GetInactiveSource();
                next.clip = loopingClip;
                next.PlayScheduled(scheduleAt);
                nextLoopTime = scheduleAt + loopingClip.length;
                activeSource = next;
            }
        }

        public void StartMusic()
        {
            if (IsPlaying) return;
            IsPlaying = true;
            transitioning = false;
            PlayLoop(MusicPhase.Phase1, phase1Clip);
        }

        public void StopMusic()
        {
            StopAllCoroutines();
            sourceA.Stop();
            sourceB.Stop();
            loopingClip = null;
            IsPlaying = false;
            transitioning = false;
            CurrentPhase = MusicPhase.Stopped;
        }

        // Pending transition — queued to fire at the end of the current loop
        private MusicPhase pendingTransPhase = MusicPhase.Stopped;
        private AudioClip  pendingTransClip;
        private MusicPhase pendingNextPhase = MusicPhase.Stopped;
        private AudioClip  pendingNextClip;
        private bool       transitionPending = false;

        void OnBarFillChanged(float fill)
        {
            if (!IsPlaying || transitioning || transitionPending) return;

            if (CurrentPhase == MusicPhase.Phase1 && fill >= Core.ScoreManager.PHASE2_THRESHOLD)
            {
                QueueTransition(MusicPhase.Transitioning1to2, transition1to2, MusicPhase.Phase2, phase2Clip);
                return;
            }

            if (CurrentPhase == MusicPhase.Phase2
                && fill >= Core.ScoreManager.PHASE3_THRESHOLD
                && scoreManager.FishingComplete)
            {
                QueueTransition(MusicPhase.Transitioning2to3, transition2to3, MusicPhase.Phase3, phase3Clip);
                return;
            }

            if (CurrentPhase == MusicPhase.Phase3 && fill < Core.ScoreManager.PHASE3_THRESHOLD)
            {
                QueueTransition(MusicPhase.Transitioning3to2, transition3to2, MusicPhase.Phase2, phase2Clip);
                return;
            }

            if (CurrentPhase == MusicPhase.Phase2 && fill < Core.ScoreManager.PHASE2_THRESHOLD)
            {
                QueueTransition(MusicPhase.Transitioning2to1, transition2to1, MusicPhase.Phase1, phase1Clip);
            }
        }

        void QueueTransition(MusicPhase transPhase, AudioClip transClip, MusicPhase nextPhase, AudioClip nextClip)
        {
            pendingTransPhase = transPhase;
            pendingTransClip  = transClip;
            pendingNextPhase  = nextPhase;
            pendingNextClip   = nextClip;
            transitionPending = true;
            Debug.Log($"[Music] Transition queued: {transPhase} -> {nextPhase}. Will fire at end of current loop.");
        }

        IEnumerator DoTransition(MusicPhase transPhase, AudioClip transClip, MusicPhase nextPhase, AudioClip nextClip)
        {
            transitioning = true;
            loopingClip = null;

            // Stop current loop immediately and start transition
            sourceA.Stop();
            sourceB.Stop();

            CurrentPhase = transPhase;
            Debug.Log($"[Music] DoTransition started. transPhase={transPhase} nextPhase={nextPhase} transClip={transClip?.name}");

            if (transClip != null)
            {
                activeSource = sourceA;
                activeSource.clip = transClip;
                activeSource.loop = false;
                activeSource.Play();
                yield return new WaitForSeconds(transClip.length);
            }

            Debug.Log($"[Music] Transition complete. Starting {nextPhase}");
            PlayLoop(nextPhase, nextClip);
            transitioning = false;
        }

        void PlayLoop(MusicPhase phase, AudioClip clip)
        {
            CurrentPhase = phase;
            loopingClip = clip;

            if (clip == null) return;

            double startTime = AudioSettings.dspTime + 0.02;
            AudioSource next = GetInactiveSource();
            next.clip = clip;
            next.PlayScheduled(startTime);
            nextLoopTime = startTime + clip.length;
            activeSource = next;
        }

        AudioSource GetInactiveSource()
        {
            return activeSource == sourceA ? sourceB : sourceA;
        }

        public MusicPhase GetCurrentPhase() => CurrentPhase;
    }
}