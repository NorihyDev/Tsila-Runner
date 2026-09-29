using UnityEngine;

namespace TsilaRun
{
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeAreaPanel : MonoBehaviour
    {
        Rect lastArea;
        Vector2Int lastSize;
        RectTransform panel;

        void OnEnable() { panel = (RectTransform)transform; Apply(); }
        void Update()
        {
            if (lastArea != Screen.safeArea || lastSize.x != Screen.width || lastSize.y != Screen.height) Apply();
        }

        void Apply()
        {
            lastArea = Screen.safeArea;
            lastSize = new Vector2Int(Screen.width, Screen.height);
            if (lastSize.x <= 0 || lastSize.y <= 0) return;
            NormalizedAnchors(lastArea, lastSize.x, lastSize.y, out Vector2 min, out Vector2 max);
            panel.anchorMin = min;
            panel.anchorMax = max;
            panel.offsetMin = panel.offsetMax = Vector2.zero;
        }

        public static void NormalizedAnchors(Rect area, int width, int height, out Vector2 min, out Vector2 max)
        {
            min = new Vector2(area.xMin / width, area.yMin / height);
            max = new Vector2(area.xMax / width, area.yMax / height);
        }
    }
}
