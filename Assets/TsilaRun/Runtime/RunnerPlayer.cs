using UnityEngine;

namespace TsilaRun
{
    public sealed class RunnerPlayer : MonoBehaviour
    {
        public Transform visual;
        public CapsuleCollider body;
        public RunnerWorld world;
        public RunnerCharacterRig rig;
        public bool animatedSlide;

        public int Lane { get; private set; } = 1;
        public bool IsGrounded => transform.position.y <= 0.0001f;
        public bool IsSliding { get; private set; }
        public float RollProgress => IsSliding ? Mathf.Clamp01(1f - slideRemaining / RunnerRules.SlideSeconds) : 0f;
        public float Height => IsSliding ? RunnerRules.SlideHeight : RunnerRules.StandingHeight;
        public Bounds HitBounds => BoundsAtHeight(Height);
        // Instant posture changes precede this tick's movement, so collision sweeps
        // must start with the new silhouette rather than stretching the old one.
        public Bounds MovementStartBounds { get; private set; }
        float verticalVelocity, slideRemaining, laneElapsed, laneDuration, laneStart;
        bool jumpWhenClear;
        Transform scaledVisual;
        Vector3 visualScale = Vector3.one;

        Bounds BoundsAtHeight(float height) => new Bounds(
            transform.position + Vector3.up * (height * 0.5f),
            new Vector3(RunnerRules.PlayerRadius * 2f, height, RunnerRules.PlayerRadius * 2f));

        public void ResetPlayer()
        {
            Lane = 1;
            jumpWhenClear = false;
            verticalVelocity = slideRemaining = laneElapsed = laneDuration = laneStart = 0f;
            transform.position = Vector3.zero;
            SetSlide(false);
            MovementStartBounds = HitBounds;
            var avatar = GetComponentInChildren<RunnerAvatar>();
            if (avatar != null) avatar.ResetPose();
            if (rig != null) rig.ApplyRuntimePose(RunnerRules.StartSpeed, false, IsSliding, IsGrounded);
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
            if (IsSliding)
            {
                if (world != null && world.IntersectsObstacle(BoundsAtHeight(RunnerRules.StandingHeight)))
                {
                    jumpWhenClear = true;
                    return;
                }
                SetSlide(false);
            }
            jumpWhenClear = false;
            if (!IsGrounded || verticalVelocity > 0f) return;
            verticalVelocity = RunnerRules.JumpVelocity;
            if (world != null && world.game != null) world.game.RegisterPlayerAction(RunnerMissionKind.Jump);
        }

        public void Slide()
        {
            if (IsSliding) return;
            jumpWhenClear = false;
            slideRemaining = RunnerRules.SlideSeconds;
            SetSlide(true);
            if (world != null && world.game != null) world.game.RegisterPlayerAction(RunnerMissionKind.Roll);
            if (!IsGrounded) verticalVelocity = Mathf.Min(verticalVelocity, -RunnerRules.FastFallSpeed);
        }

        public void Simulate(float dt)
        {
            if (dt < 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            if (IsSliding)
            {
                if (IsGrounded) slideRemaining -= dt;
                if (jumpWhenClear && (world == null || !world.IntersectsObstacle(BoundsAtHeight(RunnerRules.StandingHeight))))
                    Jump();
                // Never expand the collider into an overhead obstacle.
                if (IsSliding && slideRemaining <= 0f &&
                    (world == null || !world.IntersectsObstacle(BoundsAtHeight(RunnerRules.StandingHeight))))
                    SetSlide(false);
            }
            MovementStartBounds = HitBounds;
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
            if (rig != null) rig.ApplyRuntimePose(world != null && world.game != null ? world.game.TravelSpeed : RunnerRules.StartSpeed, false, IsSliding, IsGrounded);
        }

        void SetSlide(bool slide)
        {
            IsSliding = slide;
            if (body == null) body = GetComponent<CapsuleCollider>();
            if (body != null)
            {
                body.height = Height;
                body.center = Vector3.up * (Height * 0.5f);
            }
            if (visual != null)
            {
                if (scaledVisual != visual) { scaledVisual = visual; visualScale = visual.localScale; }
                visual.localScale = animatedSlide ? visualScale :
                    Vector3.Scale(visualScale, new Vector3(1f, Height / RunnerRules.StandingHeight, 1f));
            }
            if (rig != null) rig.ApplyRuntimePose(RunnerRules.StartSpeed, false, IsSliding, IsGrounded);
        }
    }
}
