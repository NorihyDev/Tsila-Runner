using UnityEngine;

namespace TsilaRun
{
    public sealed class RunnerCamera : MonoBehaviour
    {
        public RunnerPlayer player;
        public RunnerGame game;
        public Vector3 offset = new Vector3(0f, 6.5f, -10f);

        void LateUpdate()
        {
            if (game.State == RunnerGame.RunState.Paused) return;
            if (game.State == RunnerGame.RunState.Ready || game.State == RunnerGame.RunState.Shop)
            {
                transform.position = new Vector3(0f, 1.45f, -4.3f);
                transform.LookAt(new Vector3(0f, 0.98f, 0f));
                return;
            }
            Vector3 target = offset + Vector3.right * (player.transform.position.x * 0.3f);
            if (game.State == RunnerGame.RunState.Intro && game.chase != null)
                target = Vector3.Lerp(new Vector3(7f, 5f, -11f), offset, game.chase.IntroProgress);
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-7f * Time.deltaTime));
            Vector3 look = new Vector3(transform.position.x * 0.4f, 1f, 16f);
            if (game.State == RunnerGame.RunState.Intro && game.chase != null)
                look = Vector3.Lerp(new Vector3(0f, 1f, -2.5f), look, game.chase.IntroProgress);
            transform.rotation = Quaternion.LookRotation(look - transform.position);
        }
    }
}
