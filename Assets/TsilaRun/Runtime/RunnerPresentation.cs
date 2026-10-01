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
                stage.SetActive(menu);
                stage.transform.localScale = menu ? Vector3.one : new Vector3(0.96f, 0.96f, 1f);
            }
            if (game != null && game.world != null) game.world.gameObject.SetActive(!menu);
            if (game != null && game.player != null && game.player.visual != null)
                game.player.visual.localRotation = menu ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
            if (title != null)
            {
                title.localScale = menu ? Vector3.one : new Vector3(0.93f, 0.93f, 1f);
                title.anchoredPosition = menu ? new Vector2(0f, 15f) : Vector2.zero;
            }
        }
        void Update()
        {
            if (game == null || (game.State != RunnerGame.RunState.Ready && game.State != RunnerGame.RunState.Shop)) return;
            menuTime = Mathf.Repeat(menuTime + Time.deltaTime, Mathf.PI * 200f);
            if (game.player != null && game.player.visual != null)
                game.player.visual.localRotation = Quaternion.Euler(0f, 180f + Mathf.Sin(menuTime * 0.45f) * 28f, 0f);
            if (game.player != null && game.player.rig != null)
                game.player.rig.ApplyRuntimePose(RunnerRules.StartSpeed, true, false, true);
            if (title != null) title.localScale = Vector3.one * (1f + Mathf.Sin(menuTime * 1.8f) * 0.012f);
        }
    }
}
