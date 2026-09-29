using UnityEngine;

namespace TsilaRun
{
    // A tiny procedural rig: no animation clips, Animator, or downloaded character assets.
    public sealed class RunnerAvatar : MonoBehaviour
    {
        public Transform leftArm, rightArm, leftLeg, rightLeg, leftKnee, rightKnee;
        public Renderer[] suitRenderers;
        public RunnerGame game;
        public RunnerPlayer player;
        public bool alwaysRun;
        public bool animate = true;
        float phase;

        void OnEnable() { phase = 0f; Pose(0f, 0f); }

        void Update()
        {
            if (Time.timeScale == 0f) return;
            bool running = animate && (alwaysRun || game != null &&
                (game.State == RunnerGame.RunState.Running || game.State == RunnerGame.RunState.Intro));
            if (!running) { Pose(0f, 0f); return; }
            phase = Mathf.Repeat(phase + Time.deltaTime * (game == null ? 10f : 9f + game.Speed * 0.25f), Mathf.PI * 2f);
            if (player != null && player.IsSliding) { Pose(-65f, 65f); return; }
            if (player != null && !player.IsGrounded) { Pose(-65f, -25f); return; }
            float swing = Mathf.Sin(phase) * 42f;
            Pose(swing, -swing);
        }

        void Pose(float left, float right)
        {
            if (leftArm == null) return; // Allows the Editor builder to wire a fresh rig.
            leftArm.localRotation = Quaternion.Euler(left, 0f, -8f);
            rightArm.localRotation = Quaternion.Euler(right, 0f, 8f);
            leftLeg.localRotation = Quaternion.Euler(right, 0f, 0f);
            rightLeg.localRotation = Quaternion.Euler(left, 0f, 0f);
            leftKnee.localRotation = Quaternion.Euler(Mathf.Max(0f, -right) * 1.2f, 0f, 0f);
            rightKnee.localRotation = Quaternion.Euler(Mathf.Max(0f, -left) * 1.2f, 0f, 0f);
        }

        public void SetSuit(Material material)
        {
            if (material == null || suitRenderers == null) return;
            foreach (var part in suitRenderers) part.sharedMaterial = material;
        }
    }
}
