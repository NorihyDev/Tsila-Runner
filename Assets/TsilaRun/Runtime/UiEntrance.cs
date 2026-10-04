using UnityEngine;

namespace TsilaRun
{
    [RequireComponent(typeof(CanvasGroup), typeof(RectTransform))]
    public sealed class UiEntrance : MonoBehaviour
    {
        public float delay;
        public float offset = 30f;
        CanvasGroup group;
        RectTransform rect;
        Vector2 rest;
        float elapsed;
        bool initialized;
        void Awake() { Initialize(); }
        void Initialize()
        {
            if (initialized) return;
            rect = (RectTransform)transform;
            rest = rect.anchoredPosition;
            group = GetComponent<CanvasGroup>();
            initialized = true;
        }
        void OnEnable()
        {
            Initialize();
            elapsed = 0f;
            group.alpha = 0f;
            rect.anchoredPosition = rest + Vector2.up * offset;
        }
        void Update()
        {
            if (elapsed >= Mathf.Max(0f, delay) + .45f) return;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01((elapsed - Mathf.Max(0f, delay)) / .45f);
            float ease = 1 - Mathf.Pow(1 - t, 3);
            group.alpha = ease;
            rect.anchoredPosition = rest + Vector2.up * offset * (1 - ease);
        }
        void OnDisable()
        {
            if (rect != null) rect.anchoredPosition = rest;
            if (group != null) group.alpha = 1f;
        }
        public void Finish()
        {
            Initialize();
            elapsed = Mathf.Max(0f, delay) + .45f;
            group.alpha = 1f;
            rect.anchoredPosition = rest;
        }
    }
}
