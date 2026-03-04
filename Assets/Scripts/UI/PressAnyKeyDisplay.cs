using UnityEngine;
using TMPro;

namespace RhythmGame.UI
{
    /// <summary>
    /// Simple "Press Any Key" prompt shown before the game starts.
    /// Assign a Text or Image in the Inspector.
    /// </summary>
    public class PressAnyKeyDisplay : MonoBehaviour
    {
        public TMP_Text promptText;
        public string message = "PRESS ANY KEY TO START";

        void Start()
        {
            if (promptText != null)
                promptText.text = message;
        }

        public void Show() => gameObject.SetActive(true);
        public void Hide() => gameObject.SetActive(false);
    }
}
