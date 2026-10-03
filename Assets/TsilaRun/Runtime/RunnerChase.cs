using UnityEngine;

namespace TsilaRun
{
    public sealed class RunnerChase : MonoBehaviour
    {
        public const float IntroSeconds = 2.8f;
        public RunnerGame game;
        public RunnerAvatar officer;
        public float IntroProgress { get; private set; }

        public void ResetChase()
        {
            IntroProgress = 0f;
            officer.transform.position = new Vector3(-1.2f, 0f, -4.8f);
            officer.transform.rotation = Quaternion.identity;
            officer.animate = true;
            if (officer.animator != null) { officer.animator.Rebind(); if (officer.gameObject.activeInHierarchy) officer.animator.Update(0f); }
        }

        public bool TickIntro(float dt)
        {
            IntroProgress = Mathf.Min(1f, IntroProgress + dt / IntroSeconds);
            officer.transform.position = Vector3.Lerp(new Vector3(-1.2f, 0f, -4.8f), new Vector3(0f, 0f, -3.2f), IntroProgress);
            return IntroProgress >= 1f;
        }

        void Update()
        {
            if (game.State == RunnerGame.RunState.Paused) return;
            bool visible = game.State == RunnerGame.RunState.Intro || game.State == RunnerGame.RunState.GameOver ||
                game.State == RunnerGame.RunState.Running && game.Distance < 60d;
            officer.gameObject.SetActive(visible);
            if (!visible) return;
            if (game.State == RunnerGame.RunState.Running)
                officer.transform.position = new Vector3(game.player.transform.position.x * 0.6f, 0f,
                    Mathf.Lerp(-3.2f, -5.2f, Mathf.Clamp01((float)game.Distance / 60f)));
            if (game.State == RunnerGame.RunState.GameOver)
            {
                Vector3 target = new Vector3(game.player.transform.position.x + 0.8f, 0f, -0.8f);
                officer.transform.position = Vector3.MoveTowards(officer.transform.position, target, 5f * Time.deltaTime);
                officer.animate = Vector3.SqrMagnitude(officer.transform.position - target) > 0.01f;
            }
        }
    }
}
