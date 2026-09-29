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
        public Animator animator;
        static readonly int Idle = Animator.StringToHash("Idle"), Run = Animator.StringToHash("Run"),
            Jump = Animator.StringToHash("Jump"), Slide = Animator.StringToHash("Slide");
        int currentAnimation;
        public int CurrentAnimation => currentAnimation;
        float phase;

        void OnEnable() { phase = 0f; currentAnimation = 0; if (animator == null) Pose(0f, 0f); }

        void Update() { Animate(Time.deltaTime); }

        public void Animate(float dt)
        {
            if (Time.timeScale == 0f) return;
            bool running = animate && (alwaysRun || game != null &&
                (game.State == RunnerGame.RunState.Running || game.State == RunnerGame.RunState.Intro));
            if (animator != null)
            {
                int next = !running ? Idle : player != null && player.IsSliding ? Slide :
                    player != null && !player.IsGrounded ? Jump : Run;
                animator.speed = next == Run && game != null ? Mathf.Lerp(0.9f, 1.3f,
                    Mathf.InverseLerp(RunnerRules.StartSpeed, RunnerRules.MaxSpeed, game.Speed)) : 1f;
                if (next != currentAnimation)
                {
                    animator.CrossFadeInFixedTime(next, currentAnimation == 0 ? 0f : 0.08f, 0);
                    currentAnimation = next;
                }
                return;
            }
            if (!running) { Pose(0f, 0f); return; }
            phase = Mathf.Repeat(phase + dt * (game == null ? 10f : 9f + game.Speed * 0.25f), Mathf.PI * 2f);
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
            if (animator != null || material == null || suitRenderers == null) return; // Preserve the supplied atlas and logo slots.
            foreach (var part in suitRenderers) part.sharedMaterial = material;
        }
    }
}
