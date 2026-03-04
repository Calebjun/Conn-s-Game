using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace RhythmGame.UI
{
    /// <summary>
    /// Shown at the end of a run. Displays final stats.
    /// Assign all fields in the Inspector.
    /// </summary>
    public class ResultsScreen : MonoBehaviour
    {
        [Header("Stat Labels")]
        public TMP_Text finalScoreText;
        public TMP_Text perfectCountText;
        public TMP_Text greatCountText;
        public TMP_Text goodCountText;
        public TMP_Text missCountText;
        public TMP_Text gradeText;

        [Header("Buttons")]
        public Button retryButton;
        public Button mainMenuButton;

        [Header("Scene Names")]
        public string gameSceneName  = "GameScene";
        public string menuSceneName  = "MainMenu";

        void Start()
        {
            var sm = Core.ScoreManager.Instance;
            if (sm == null) return;

            if (finalScoreText   != null) finalScoreText.text   = sm.Score.ToString();
            if (perfectCountText != null) perfectCountText.text = sm.PerfectCount.ToString();
            if (greatCountText   != null) greatCountText.text   = sm.GreatCount.ToString();
            if (goodCountText    != null) goodCountText.text    = sm.GoodCount.ToString();
            if (missCountText    != null) missCountText.text    = sm.MissCount.ToString();
            if (gradeText        != null) gradeText.text        = CalculateGrade(sm);

            if (retryButton    != null) retryButton.onClick.AddListener(Retry);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(GoToMenu);
        }

        string CalculateGrade(Core.ScoreManager sm)
        {
            if (sm.MissCount == 0 && sm.BarFill >= Core.ScoreManager.MAX_FILL) return "S";
            if (sm.BarFill >= Core.ScoreManager.PHASE4_THRESHOLD) return "A";
            if (sm.BarFill >= Core.ScoreManager.PHASE3_THRESHOLD) return "B";
            if (sm.BarFill >= Core.ScoreManager.PHASE2_THRESHOLD) return "C";
            return "D";
        }

        void Retry()    => SceneManager.LoadScene(gameSceneName);
        void GoToMenu() => SceneManager.LoadScene(menuSceneName);
    }
}
