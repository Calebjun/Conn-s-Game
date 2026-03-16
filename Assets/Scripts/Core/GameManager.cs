using UnityEngine;
using System.Collections;
using UInput = UnityEngine.Input;

namespace RhythmGame.Core
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public enum GameState { WaitingForInput, Playing, Paused, GameOver }
        public GameState State { get; private set; } = GameState.WaitingForInput;

        [Header("References")]
        public ScoreManager                  scoreManager;
        public Music.MusicLoopController     musicController;
        public NoteSpawner                   noteSpawner;
        public HitDetector                   hitDetector;
        public HitFeedbackManager            feedbackManager;
        public Fishing.FishingManager        fishingManager;

        [Header("UI")]
        public UI.PressAnyKeyDisplay         pressAnyKeyDisplay;
        public GameObject                    gameOverPanel;

        private bool fishingActivated = false;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            State = GameState.WaitingForInput;
            pressAnyKeyDisplay?.Show();
            if (scoreManager != null)
                scoreManager.OnBarFillChanged += OnBarFillChanged;
        }

        void OnDestroy()
        {
            if (scoreManager != null)
                scoreManager.OnBarFillChanged -= OnBarFillChanged;
        }

        void Update()
        {
            if (State == GameState.WaitingForInput)
            {
                if (UInput.anyKeyDown) StartGame();
            }
            else if (State == GameState.Playing || State == GameState.Paused)
            {
                if (UInput.GetKeyDown(KeyCode.Escape))
                {
                    if (State == GameState.Playing) PauseGame();
                    else ResumeGame();
                }
            }
        }

        void StartGame()
        {
            State = GameState.Playing;
            fishingActivated = false;
            pressAnyKeyDisplay?.Hide();
            scoreManager?.Reset();
            noteSpawner?.StartSong();
            musicController?.StartMusic();
        }

        void OnBarFillChanged(float fill)
        {
            if (fishingActivated || State != GameState.Playing || fishingManager == null) return;
            if (fill >= ScoreManager.PHASE2_THRESHOLD)
            {
                fishingActivated = true;
                fishingManager.gameObject.SetActive(true);
                fishingManager.Activate();
            }
        }

        public void PauseGame()
        {
            if (State != GameState.Playing) return;
            State = GameState.Paused;
            Time.timeScale = 0f;
            musicController?.GetComponent<AudioSource>()?.Pause();
        }

        public void ResumeGame()
        {
            if (State != GameState.Paused) return;
            State = GameState.Playing;
            Time.timeScale = 1f;
            musicController?.GetComponent<AudioSource>()?.UnPause();
        }

        public void EndGame()
        {
            if (State == GameState.GameOver) return;
            State = GameState.GameOver;
            fishingManager?.Deactivate();
            noteSpawner?.StopSong();
            musicController?.StopMusic();
            StartCoroutine(ShowGameOverAndReset());
        }

        IEnumerator ShowGameOverAndReset()
        {
            if (gameOverPanel != null) gameOverPanel.SetActive(true);
            yield return new WaitForSeconds(3f);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);

            scoreManager?.Reset();
            fishingActivated = false;
            State = GameState.WaitingForInput;
            pressAnyKeyDisplay?.Show();
        }
    }
}