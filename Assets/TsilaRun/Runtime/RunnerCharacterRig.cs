using UnityEngine;

namespace TsilaRun
{
    public sealed class RunnerCharacterRig : MonoBehaviour
    {
        public Transform root;
        public Vector3 anchorOffset = Vector3.zero;
        public Vector3 defaultScale = Vector3.one;
        public float idleBob;
        public float runBob;
        public float menuTilt = 24f;

        public void ApplyRuntimePose(float speed, bool menuMode, bool sliding, bool grounded)
        {
            if (root == null) return;
            float bob = menuMode ? 0.02f : grounded ? Mathf.Clamp01(speed / RunnerRules.MaxSpeed) * runBob : 0.05f;
            float lift = sliding ? 0f : grounded ? Mathf.Abs(Mathf.Sin(Time.time * (menuMode ? 2f : 8f + speed * 0.2f))) * bob : 0f;
            Vector3 pose = anchorOffset;
            pose.y += lift;
            root.localPosition = pose;
            // RunnerPresentation already turns the visual towards the menu camera.
            root.localRotation = Quaternion.identity;
            root.localScale = defaultScale;
        }
    }
}
