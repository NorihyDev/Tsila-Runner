using UnityEngine;

namespace TsilaRun
{
    public sealed class RunnerPlayer : MonoBehaviour
    {
        public Transform visual;
        public CapsuleCollider body;
        public RunnerWorld world;

        public int Lane { get; private set; } = 1;
        public bool IsGrounded => transform.position.y <= 0.0001f;
        public bool IsSliding { get; private set; }
        public float Height => IsSliding ? RunnerRules.SlideHeight : RunnerRules.StandingHeight;
        public Bounds HitBounds => BoundsAtHeight(Height);
        float verticalVelocity, slideRemaining, laneElapsed, laneDuration, laneStart;

        Bounds BoundsAtHeight(float height) => new Bounds(
            transform.position + Vector3.up * (height * 0.5f),
            new Vector3(RunnerRules.PlayerRadius * 2f, height, RunnerRules.PlayerRadius * 2f));

        public void ResetPlayer()
        {
            Lane = 1;
            verticalVelocity = slideRemaining = laneElapsed = laneDuration = laneStart = 0f;
            transform.position = Vector3.zero;
            SetSlide(false);
        }

        public void ChangeLane(int direction)
        {
            int next = Mathf.Clamp(Lane + direction, 0, 2);
            if (next == Lane) return;
            Lane = next;
            laneStart = transform.position.x;
            laneElapsed = 0f;
            laneDuration = Mathf.Max(0.01f, Mathf.Abs((Lane - 1) * RunnerRules.LaneWidth - laneStart)
                / RunnerRules.LaneWidth * RunnerRules.LaneSeconds);
        }

        public void Jump()
        {
            if (!IsGrounded || IsSliding) return;
            verticalVelocity = RunnerRules.JumpVelocity;
        }

        public void Slide()
        {
            if (!IsGrounded || IsSliding || verticalVelocity > 0f) return;
            slideRemaining = RunnerRules.SlideSeconds;
            SetSlide(true);
        }

        public void Simulate(float dt)
        {
            if (IsSliding)
            {
                slideRemaining -= dt;
                // Never expand the collider into an overhead obstacle.
                if (slideRemaining <= 0f && !world.IntersectsObstacle(BoundsAtHeight(RunnerRules.StandingHeight)))
                    SetSlide(false);
            }
            Vector3 position = transform.position;
            laneElapsed += dt;
            float t = laneDuration <= 0f ? 1f : Mathf.Clamp01(laneElapsed / laneDuration);
            position.x = Mathf.Lerp(laneStart, (Lane - 1) * RunnerRules.LaneWidth, t * t * (3f - 2f * t));
            // Exact ballistic integration, with a flat continuous road at y=0 as the ground.
            position.y += verticalVelocity * dt - RunnerRules.Gravity * dt * dt * 0.5f;
            verticalVelocity -= RunnerRules.Gravity * dt;
            if (position.y <= 0f) { position.y = 0f; verticalVelocity = Mathf.Max(0f, verticalVelocity); }
            position.z = 0f;
            transform.position = position;
        }

        void SetSlide(bool slide)
        {
            IsSliding = slide;
            body.height = Height;
            body.center = Vector3.up * (Height * 0.5f);
            visual.localScale = new Vector3(1f, Height / RunnerRules.StandingHeight, 1f);
        }
    }
}
