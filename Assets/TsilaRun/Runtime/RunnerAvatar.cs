using UnityEngine;

namespace TsilaRun
{
    // Drives the supplied in-place animation clips; keeps a fallback for primitive rigs.
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

        void OnEnable() { ResetPose(); }

        public void ResetPose()
        {
            phase = 0f;
            currentAnimation = 0;
            if (animator == null) { Pose(0f, 0f); return; }
            animator.speed = 1f;
            if (animator.runtimeAnimatorController == null || !animator.isActiveAndEnabled) return;
            animator.Play(Idle, 0, 0f);
            animator.Update(0f);
        }

        void Update() { Animate(Time.deltaTime); }

        public void Animate(float dt)
        {
            if (Time.timeScale == 0f) return;
            // Freeze impact/world poses; the chaser still runs to the player before
            // stopping. ResetPlayer restores Idle for the next run or the menu.
            if (game != null && game.State == RunnerGame.RunState.GameOver &&
                (game.chase == null || game.chase.officer != this))
            {
                if (animator != null) animator.speed = 0f;
                return;
            }
            bool running = animate && (alwaysRun || game != null &&
                (game.State == RunnerGame.RunState.Running || game.State == RunnerGame.RunState.Intro));
            if (animator != null)
            {
                int next = !running ? Idle : player != null && player.IsSliding ? Slide :
                    player != null && !player.IsGrounded ? Jump : Run;
                animator.speed = next == Slide ? 0f : next == Run && game != null ? Mathf.Lerp(0.9f, 1.3f,
                    Mathf.InverseLerp(RunnerRules.StartSpeed, RunnerRules.MaxSpeed + RunnerRules.SpeedBoostBonus, game.TravelSpeed)) : 1f;
                if (next != currentAnimation)
                {
                    animator.CrossFadeInFixedTime(next, currentAnimation == 0 || next == Slide ? 0f : 0.08f, 0);
                    currentAnimation = next;
                }
                if (next == Slide && player != null) animator.Play(Slide, 0, player.RollProgress * .98f);
                return;
            }
            if (!running) { Pose(0f, 0f); return; }
            phase = Mathf.Repeat(phase + dt * (game == null ? 10f : 9f + game.TravelSpeed * 0.25f), Mathf.PI * 2f);
            if (player != null && player.IsSliding) { Pose(-65f, 65f); return; }
            if (player != null && !player.IsGrounded) { Pose(-65f, -25f); return; }
            float swing = Mathf.Sin(phase) * 42f;
            Pose(swing, -swing);
        }

        void Pose(float left, float right)
        {
            if (leftArm != null) leftArm.localRotation = Quaternion.Euler(left, 0f, -8f);
            if (rightArm != null) rightArm.localRotation = Quaternion.Euler(right, 0f, 8f);
            if (leftLeg != null) leftLeg.localRotation = Quaternion.Euler(right, 0f, 0f);
            if (rightLeg != null) rightLeg.localRotation = Quaternion.Euler(left, 0f, 0f);
            if (leftKnee != null) leftKnee.localRotation = Quaternion.Euler(Mathf.Max(0f, -right) * 1.2f, 0f, 0f);
            if (rightKnee != null) rightKnee.localRotation = Quaternion.Euler(Mathf.Max(0f, -left) * 1.2f, 0f, 0f);
        }

        public void SetSuit(Material material)
        {
            if (material == null || suitRenderers == null) return;
            if (animator != null) { SetSuitTint(material.color); return; }
            foreach (var part in suitRenderers) if (part != null) part.sharedMaterial = material;
        }

        public void SetSuitTint(Color tint)
        {
            if (suitRenderers == null) return;
            var block = new MaterialPropertyBlock();
            foreach (var part in suitRenderers)
            {
                if (part == null) continue;
                var materials = part.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] == null || !materials[i].name.Contains("Body")) continue;
                    part.GetPropertyBlock(block, i);
                    block.SetColor("_BaseColor", tint);
                    part.SetPropertyBlock(block, i);
                    block.Clear();
                }
            }
        }
    }
}
