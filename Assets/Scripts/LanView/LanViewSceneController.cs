using UnityEngine;

namespace MagesDementiaGame
{
    /// <summary>Baseline static presentation for Act 2 from Lan's doorway viewpoint.</summary>
    public sealed class LanViewSceneController : MonoBehaviour
    {
        [SerializeField] private Texture2D background;

        private void OnGUI()
        {
            if (background == null) return;
            GUI.color = Color.white;
            GUI.DrawTexture(CalculateBackgroundRect(), background, ScaleMode.ScaleToFit, false);
        }

        private Rect CalculateBackgroundRect()
        {
            var sourceAspect = (float)background.width / background.height;
            var screenAspect = (float)Screen.width / Screen.height;
            if (screenAspect > sourceAspect)
            {
                var width = Screen.height * sourceAspect;
                return new Rect((Screen.width - width) * 0.5f, 0f, width, Screen.height);
            }

            var height = Screen.width / sourceAspect;
            return new Rect(0f, (Screen.height - height) * 0.5f, Screen.width, height);
        }
    }
}
