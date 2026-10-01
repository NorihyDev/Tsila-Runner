using UnityEngine;

namespace TsilaRun
{
    public sealed class RunnerCamera : MonoBehaviour
    {
        public RunnerPlayer player;
        public RunnerGame game;
        public Camera cameraRef;
        public Vector3 offset = new Vector3(0f, 6.5f, -10f);
        public float baseFov = 62f;
        public float maxFovBoost = 8f;
        float shake;

        void LateUpdate()
        {
            if (game == null) return;
            if (game.State == RunnerGame.RunState.Paused) return;
            if (cameraRef != null)
            {
                float speedRatio = Mathf.InverseLerp(RunnerRules.StartSpeed, RunnerRules.MaxSpeed, game.Speed);
                float targetFov = baseFov + speedRatio * maxFovBoost;
                cameraRef.fieldOfView = Mathf.Lerp(cameraRef.fieldOfView, targetFov, 0.12f);
            }
            if (game.State == RunnerGame.RunState.Ready || game.State == RunnerGame.RunState.Shop)
            {
                Vector3 menuPos = new Vector3(0f, 1.45f, -4.3f);
                transform.position = Vector3.Lerp(transform.position, menuPos, 1f - Mathf.Exp(-6f * Time.deltaTime));
                transform.LookAt(new Vector3(0f, 0.98f, 0f));
                shake = Mathf.Lerp(shake, 0f, 0.12f);
                return;
            }
            float laneSway = player != null ? player.transform.position.x * 0.18f : 0f;
            Vector3 target = offset + Vector3.right * laneSway;
            if (game.State == RunnerGame.RunState.Intro && game.chase != null)
                target = Vector3.Lerp(new Vector3(7f, 5f, -11f), offset, game.chase.IntroProgress);
            shake = Mathf.Lerp(shake, player != null && !player.IsGrounded ? 0.18f : 0.04f, 0.08f);
            Vector3 randomOffset = new Vector3(
                Mathf.PerlinNoise(Time.time * 16f, 0f) - 0.5f,
                Mathf.PerlinNoise(0f, Time.time * 18f) - 0.5f,
                Mathf.PerlinNoise(Time.time * 12f, Time.time * 14f) - 0.5f) * shake * 0.45f;
            transform.position = Vector3.Lerp(transform.position, target + randomOffset, 1f - Mathf.Exp(-7f * Time.deltaTime));
            Vector3 look = new Vector3(transform.position.x * 0.4f, 1f, 16f);
            if (game.State == RunnerGame.RunState.Intro && game.chase != null)
                look = Vector3.Lerp(new Vector3(0f, 1f, -2.5f), look, game.chase.IntroProgress);
            transform.rotation = Quaternion.LookRotation(look - transform.position);
        }
    }
}
