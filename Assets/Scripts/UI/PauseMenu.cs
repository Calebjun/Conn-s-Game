using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace RhythmGame.UI
{
    public class PauseMenu : MonoBehaviour
    {
        public GameObject pausePanel;
        public Button resumeButton;
        public Button quitButton;
        public string mainMenuScene = "MainMenu";

        void Start()
        {
            pausePanel?.SetActive(false);
            resumeButton?.onClick.AddListener(Resume);
            quitButton?.onClick.AddListener(Quit);

            Core.GameManager.Instance?.GetComponent<Core.GameManager>();
        }

        public void Show()  => pausePanel?.SetActive(true);
        public void Hide()  => pausePanel?.SetActive(false);

        void Resume()
        {
            Hide();
            Core.GameManager.Instance?.ResumeGame();
        }

        void Quit() => SceneManager.LoadScene(mainMenuScene);
    }
}
