using UnityEngine;

namespace TsilaRun
{
    public sealed class RunnerCharacterRig : MonoBehaviour
    {
        public Transform root;
        public Vector3 anchorOffset = Vector3.zero;
        public Vector3 defaultScale = Vector3.one;
        public float idleBob = 0.04f;
        public float runBob = 0.09f;
        public float menuTilt = 24f;

        float bobPhase;

        void Update()
        {
            if (root == null) return;
            float time = Time.unscaledTime;
            float pace = Mathf.Sin(time * 2.5f + bobPhase);
            Vector3 local = anchorOffset;
            local.y += idleBob * pace;
            root.localPosition = Vector3.Lerp(root.localPosition, local, 0.14f);
            root.localScale = Vector3.Lerp(root.localScale, defaultScale, 0.2f);
        }

        public void ApplyRuntimePose(float speed, bool menuMode, bool sliding, bool grounded)
        {
            if (root == null) return;
            float bob = menuMode ? 0.02f : grounded ? Mathf.Clamp01(speed / RunnerRules.MaxSpeed) * runBob : 0.05f;
            bobPhase = sliding ? 0.7f : 0f;
            float lift = sliding ? -0.18f : grounded ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * (menuMode ? 2f : 8f + speed * 0.2f))) * bob : 0.1f;
            Vector3 pose = anchorOffset;
            pose.y += lift;
            root.localPosition = pose;
            root.localRotation = Quaternion.Euler(menuMode ? 0f : 0f, menuMode ? 180f + menuTilt : 0f, sliding ? -8f : 0f);
            root.localScale = defaultScale * (menuMode ? 1.04f : 1f);
        }
    }
}
