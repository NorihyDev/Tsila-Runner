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
        float velocity;
        AudioSource sound;
        static AudioClip click;
        void Awake() { button = GetComponent<Button>(); }
        void OnDisable() { hovered = pressed = false; scale = 1f; velocity = 0f; transform.localScale = Vector3.one; }
        void Update()
        {
            float target = button != null && button.IsInteractable() ? pressed ? .97f : hovered ? 1.012f : 1f : 1f;
            if (Mathf.Abs(scale - target) < .0001f) return;
            // Damped spring gives a small rebound without another tween dependency.
            float dt = Mathf.Min(Time.unscaledDeltaTime, .033f);
            velocity += (target - scale) * 520f * dt;
            velocity *= Mathf.Exp(-25f * dt);
            scale += velocity * dt;
            transform.localScale = Vector3.one * scale;
        }
        public void OnPointerEnter(PointerEventData e) { hovered = true; }
        public void OnPointerExit(PointerEventData e) { hovered = pressed = false; }
        public void OnPointerDown(PointerEventData e)
        {
            if (button == null || !button.IsInteractable()) return;
            pressed = true;
            if (click == null)
            {
                const int rate = 22050; var samples = new float[1323];
                for (int i = 0; i < samples.Length; i++)
                    samples[i] = Mathf.Sin(i * 2f * Mathf.PI * 740f / rate) * Mathf.Exp(-i / 230f) * .08f;
                click = AudioClip.Create("UI tap", samples.Length, 1, rate, false); click.SetData(samples, 0);
            }
            if (sound == null) { sound = gameObject.AddComponent<AudioSource>(); sound.playOnAwake = false; sound.ignoreListenerPause = true; }
            sound.PlayOneShot(click);
        }
        public void OnPointerUp(PointerEventData e) { pressed = false; }
    }
}
