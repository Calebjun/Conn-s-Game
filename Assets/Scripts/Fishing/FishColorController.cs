using UnityEngine;

namespace RhythmGame.Fishing
{
    /// <summary>
    /// Applies a unique color to a fish based on its pattern type.
    /// Per design notes: "diff colors for diff fish would be so sick."
    ///
    /// Attach to the same GameObject as FishController.
    /// Call Apply() after Spawn() to set the color.
    /// </summary>
    public class FishColorController : MonoBehaviour
    {
        [Header("Colors per pattern type")]
        public Color pattern1Color = new Color(1.00f, 0.40f, 0.40f); // red
        public Color pattern2Color = new Color(0.40f, 0.80f, 1.00f); // sky blue
        public Color pattern3Color = new Color(0.60f, 1.00f, 0.50f); // lime green
        public Color pattern4Color = new Color(1.00f, 0.85f, 0.20f); // gold
        public Color pattern5Color = new Color(0.80f, 0.50f, 1.00f); // purple

        private SpriteRenderer spriteRenderer;

        void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
        }

        public void Apply(FishPatternType patternType)
        {
            if (spriteRenderer == null) return;

            spriteRenderer.color = patternType switch
            {
                FishPatternType.Pattern1_Clockwise        => pattern1Color,
                FishPatternType.Pattern2_CounterClockwise => pattern2Color,
                FishPatternType.Pattern3_ZigZag           => pattern3Color,
                FishPatternType.Pattern4_Dart             => pattern4Color,
                FishPatternType.Pattern5_Spiral           => pattern5Color,
                _                                         => Color.white
            };
        }
    }
}
