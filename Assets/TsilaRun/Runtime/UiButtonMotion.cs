using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace TsilaRun
{
    public sealed class UiButtonMotion : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        Button button;
        bool hovered, pressed;
        float scale = 1f;
        void Awake() { button = GetComponent<Button>(); }
        void OnDisable() { hovered = pressed = false; scale = 1f; transform.localScale = Vector3.one; }
        void Update()
        {
            float target = button != null && button.IsInteractable() ? pressed ? .97f : hovered ? 1.012f : 1f : 1f;
            if (Mathf.Abs(scale - target) < .0001f) return;
            scale = Mathf.Lerp(scale, target, 1f - Mathf.Exp(-22f * Time.unscaledDeltaTime));
            transform.localScale = Vector3.one * scale;
        }
        public void OnPointerEnter(PointerEventData e) { hovered = true; }
        public void OnPointerExit(PointerEventData e) { hovered = pressed = false; }
        public void OnPointerDown(PointerEventData e) { pressed = true; }
        public void OnPointerUp(PointerEventData e) { pressed = false; }
    }
}
