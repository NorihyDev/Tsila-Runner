using UnityEngine;

namespace TsilaRun
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UiEntrance : MonoBehaviour
    {
        public float delay;
        public float offset = 30f;
        CanvasGroup group;
        RectTransform rect;
        Vector2 rest;
        float elapsed;
        void Awake() { rect = (RectTransform)transform; rest = rect.anchoredPosition; group = GetComponent<CanvasGroup>(); }
        void OnEnable() { elapsed = 0; if (group != null) group.alpha = 0; }
        void Update()
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01((elapsed - delay) / .45f);
            float ease = 1 - Mathf.Pow(1 - t, 3);
            group.alpha = ease;
            rect.anchoredPosition = rest + Vector2.up * offset * (1 - ease);
        }
        void OnDisable() { if (rect != null) rect.anchoredPosition = rest; }
        public void Finish() { if (group == null) group = GetComponent<CanvasGroup>(); group.alpha = 1; }
    }
}
