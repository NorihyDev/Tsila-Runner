using UnityEngine;

namespace TsilaRun
{
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class UiPanelMotion : MonoBehaviour
    {
        CanvasGroup group;
        float elapsed;
        void OnEnable()
        {
            group = GetComponent<CanvasGroup>();
            elapsed = 0f; group.alpha = 0f;
        }
        void Update()
        {
            if (elapsed >= .2f) return;
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / .2f);
            group.alpha = 1f - Mathf.Pow(1f - t, 3f);
        }
        void OnDisable() { if (group != null) group.alpha = 1f; }
    }
}
