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
            min = Vector2.zero;
            max = Vector2.one;
            if (width <= 0 || height <= 0 || area.width <= 0f || area.height <= 0f) return;
            Vector2 clampedMin = new Vector2(Mathf.Clamp01(area.xMin / width), Mathf.Clamp01(area.yMin / height));
            Vector2 clampedMax = new Vector2(Mathf.Clamp01(area.xMax / width), Mathf.Clamp01(area.yMax / height));
            // Resolution changes can briefly report the previous screen's safe area.
            if (clampedMax.x <= clampedMin.x || clampedMax.y <= clampedMin.y) return;
            min = clampedMin;
            max = clampedMax;
        }
    }
}
