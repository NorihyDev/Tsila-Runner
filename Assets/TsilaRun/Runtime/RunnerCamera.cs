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
            Vector3 target = offset + Vector3.right * (player.transform.position.x * 0.3f);
            transform.position = Vector3.Lerp(transform.position, target, 1f - Mathf.Exp(-7f * Time.deltaTime));
            transform.rotation = Quaternion.LookRotation(new Vector3(transform.position.x * 0.4f, 1f, 16f) - transform.position);
        }
    }
}
