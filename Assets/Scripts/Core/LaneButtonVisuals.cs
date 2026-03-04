using UnityEngine;
using UInput = UnityEngine.Input;

namespace RhythmGame.Core
{
    /// <summary>
    /// Each lane has its own default sprite and hit sprite.
    /// Swaps to the hit sprite while the key is held, back on release.
    /// </summary>
    public class LaneButtonVisuals : MonoBehaviour
    {
        [System.Serializable]
        public class LaneVisual
        {
            public SpriteRenderer renderer;
            public Sprite defaultSprite;
            public Sprite hitSprite;
        }

        [Header("One entry per lane: 0=Up(W), 1=Left(A), 2=Down(S), 3=Right(D)")]
        public LaneVisual[] lanes = new LaneVisual[4];

        [Header("Keys")]
        public KeyCode[] laneKeys = new KeyCode[]
        {
            KeyCode.W,
            KeyCode.A,
            KeyCode.S,
            KeyCode.D
        };

        void Start()
        {
            for (int i = 0; i < lanes.Length; i++)
            {
                if (lanes[i]?.renderer != null)
                    lanes[i].renderer.sprite = lanes[i].defaultSprite;
            }
        }

        void Update()
        {
            for (int i = 0; i < laneKeys.Length && i < lanes.Length; i++)
            {
                if (lanes[i]?.renderer == null) continue;

                if (UInput.GetKeyDown(laneKeys[i]))
                    lanes[i].renderer.sprite = lanes[i].hitSprite;

                if (UInput.GetKeyUp(laneKeys[i]))
                    lanes[i].renderer.sprite = lanes[i].defaultSprite;
            }
        }
    }
}