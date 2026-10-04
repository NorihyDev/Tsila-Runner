using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace TsilaRun
{
    public sealed class RunnerInput : MonoBehaviour
    {
        public RunnerGame game;
        public RunnerPlayer player;
        readonly List<RaycastResult> uiHits = new List<RaycastResult>(16);
        PointerEventData pointer;
        EventSystem pointerEvents;
        RunnerGame subscribedGame;
        int activeFinger = -1;
        Vector2 origin;
        bool consumed;
        Vector2 mouseOrigin;
        bool mouseDragging, mouseConsumed;

        void OnEnable()
        {
            EnhancedTouchSupport.Enable();
            BindGameEvents();
        }

        void OnDisable()
        {
            if (subscribedGame != null) subscribedGame.StateChanged -= ClearGesture;
            subscribedGame = null;
            EnhancedTouchSupport.Disable();
            ClearGesture();
        }

        void BindGameEvents()
        {
            if (subscribedGame == game) return;
            if (subscribedGame != null) subscribedGame.StateChanged -= ClearGesture;
            subscribedGame = game;
            if (subscribedGame != null) subscribedGame.StateChanged += ClearGesture;
            ClearGesture();
        }

        public void ClearGesture()
        {
            activeFinger = -1;
            consumed = mouseDragging = mouseConsumed = false;
        }

        void Update()
        {
            BindGameEvents();
            if (game == null || player == null) { ClearGesture(); return; }
            var keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) game.Pause();
            if (game.State != RunnerGame.RunState.Running) { ClearGesture(); return; }
            var touches = Touch.activeTouches;
            if (activeFinger < 0)
            {
                for (int i = 0; i < touches.Count; i++)
                {
                    var touch = touches[i];
                    if (touch.phase != TouchPhase.Began) continue;
                    activeFinger = touch.finger.index;
                    origin = touch.startScreenPosition;
                    // Raycast at the actual gesture origin instead of relying on last frame's UI state.
                    consumed = BeginsOverButton(origin);
                    break;
                }
            }
            if (activeFinger >= 0)
            {
                bool found = false;
                for (int i = 0; i < touches.Count; i++)
                {
                    var touch = touches[i];
                    if (touch.finger.index != activeFinger) continue;
                    found = true;
                    if (touch.phase == TouchPhase.Canceled) { ClearGesture(); break; }
                    if (!consumed)
                    {
                        Vector2 delta = touch.screenPosition - origin;
                        int direction = ClassifySwipe(delta, Screen.width, Screen.height);
                        if (direction != 0) { consumed = true; ApplySwipe(direction); }
                    }
                    if (touch.phase == TouchPhase.Ended) ClearGesture();
                    break;
                }
                if (!found) ClearGesture();
            }
            // Touchscreens may also emulate a mouse. A touch always owns the gesture
            // while present so the same swipe cannot dispatch twice.
            if (touches.Count == 0) PollMouse();
            else mouseDragging = mouseConsumed = false;
            if (keyboard == null) return;
            if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame || keyboard.qKey.wasPressedThisFrame) player.ChangeLane(-1);
            if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame) player.ChangeLane(1);
            if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame || keyboard.zKey.wasPressedThisFrame) player.Jump();
            if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame) player.Slide();
        }

        void PollMouse()
        {
            var mouse = Mouse.current;
            if (mouse == null) { mouseDragging = false; return; }
            if (mouse.leftButton.wasPressedThisFrame)
            {
                mouseDragging = true;
                mouseOrigin = mouse.position.ReadValue();
                mouseConsumed = BeginsOverButton(mouseOrigin);
            }
            if (!mouseDragging) return;
            if (!mouseConsumed)
            {
                int direction = ClassifySwipe(mouse.position.ReadValue() - mouseOrigin, Screen.width, Screen.height);
                if (direction != 0) { mouseConsumed = true; ApplySwipe(direction); }
            }
            if (mouse.leftButton.wasReleasedThisFrame || !mouse.leftButton.isPressed)
                mouseDragging = mouseConsumed = false;
        }

        // 5% of the short screen edge; responsive across both phone sizes and pixel densities.
        public static int ClassifySwipe(Vector2 delta, int width, int height)
        {
            float threshold = Mathf.Max(1f, Mathf.Min(width, height) * 0.05f);
            if (Mathf.Max(Mathf.Abs(delta.x), Mathf.Abs(delta.y)) < threshold) return 0;
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y)) return delta.x < 0f ? -1 : 1;
            return delta.y > 0f ? 2 : -2;
        }

        void ApplySwipe(int direction)
        {
            if (direction == -1 || direction == 1) player.ChangeLane(direction);
            else if (direction == 2) player.Jump();
            else if (direction == -2) player.Slide();
        }

        bool BeginsOverButton(Vector2 position)
        {
            var events = EventSystem.current;
            if (events == null) return false;
            if (pointer == null || pointerEvents != events)
            {
                pointerEvents = events;
                pointer = new PointerEventData(events);
            }
            pointer.Reset();
            pointer.position = position;
            uiHits.Clear();
            events.RaycastAll(pointer, uiHits);
            for (int i = 0; i < uiHits.Count; i++)
                if (uiHits[i].gameObject.GetComponentInParent<Selectable>() != null) return true;
            return false;
        }

        void OnApplicationFocus(bool focused) { if (!focused) ClearGesture(); }
        void OnApplicationPause(bool paused) { if (paused) ClearGesture(); }
    }
}
