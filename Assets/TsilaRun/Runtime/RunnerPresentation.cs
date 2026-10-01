using UnityEngine;

namespace TsilaRun
{
    // Reuses the player's actual model without another camera or render texture.
    public sealed class RunnerPresentation : MonoBehaviour
    {
        public RunnerGame game;
        public GameObject stage;
        public RectTransform title;
        float menuTime;
        void OnEnable() { game.StateChanged += Refresh; }
        void OnDisable() { game.StateChanged -= Refresh; }
        void Start() { Refresh(); }
        void Refresh()
        {
            ShowMenu(game.State == RunnerGame.RunState.Ready || game.State == RunnerGame.RunState.Shop);
            menuTime = 0f;
        }
        public void ShowMenu(bool menu)
        {
            Color menuTint = menu ? new Color32(11, 18, 30, 255) : new Color32(5, 10, 18, 255);
            if (Camera.main != null) Camera.main.backgroundColor = menu ? menuTint : RenderSettings.fogColor;
            if (stage != null)
            {
                var image = stage.GetComponent<UnityEngine.UI.Image>() ?? stage.AddComponent<UnityEngine.UI.Image>();
                image.color = new Color32(16, 24, 38, 240);
                image.raycastTarget = false;
                if (stage.GetComponent<UnityEngine.UI.Shadow>() == null)
                {
                    var shadow = stage.AddComponent<UnityEngine.UI.Shadow>();
                    shadow.effectColor = new Color32(0, 0, 0, 130);
                    shadow.effectDistance = new Vector2(0f, -8f);
                }
            }
            stage.SetActive(menu);
            game.world.gameObject.SetActive(!menu);
            game.player.visual.localRotation = menu ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            if (title != null)
            {
                title.localScale = Vector3.one;
                title.anchoredPosition = menu ? new Vector2(0f, 15f) : Vector2.zero;
            }
        }
        void Update()
        {
            if (game.State != RunnerGame.RunState.Ready && game.State != RunnerGame.RunState.Shop) return;
            menuTime = Mathf.Repeat(menuTime + Time.deltaTime, Mathf.PI * 200f);
            game.player.visual.localRotation = Quaternion.Euler(0f, 180f + Mathf.Sin(menuTime * 0.45f) * 28f, 0f);
            if (title != null) title.localScale = Vector3.one * (1f + Mathf.Sin(menuTime * 1.8f) * 0.012f);
        }
    }
}
