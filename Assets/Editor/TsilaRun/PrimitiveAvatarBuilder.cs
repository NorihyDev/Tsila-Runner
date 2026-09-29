using System.Collections.Generic;
using UnityEngine;

namespace TsilaRun.Editor
{
    public static class PrimitiveAvatarBuilder
    {
        public static RunnerAvatar Create(Transform parent, Material suit, Material skin, Material dark, Material accent, bool police = false)
        {
            var root = new GameObject(police ? "Officer Rig" : "Runner Rig");
            root.transform.SetParent(parent, false);
            var avatar = root.AddComponent<RunnerAvatar>();
            var parts = new List<Renderer>();
            parts.Add(Shape(root.transform, "Torso", PrimitiveType.Capsule, new Vector3(0f, 1.12f, 0f), new Vector3(0.58f, 0.35f, 0.38f), suit));
            Shape(root.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.59f, 0f), Vector3.one * 0.46f, skin);
            Shape(root.transform, "Hair or Cap", PrimitiveType.Sphere, new Vector3(0f, 1.72f, -0.02f), new Vector3(0.48f, 0.2f, 0.46f), dark);
            for (int sign = -1; sign <= 1; sign += 2)
                Shape(root.transform, "Eye", PrimitiveType.Sphere, new Vector3(sign * 0.09f, 1.62f, 0.215f), Vector3.one * 0.05f, dark);
            Shape(root.transform, police ? "Badge" : "Chest Stripe", PrimitiveType.Cube,
                new Vector3(police ? -0.12f : 0f, 1.27f, 0.192f), new Vector3(police ? 0.1f : 0.4f, 0.13f, 0.035f), accent);
            Shape(root.transform, "Backpack", PrimitiveType.Cube, new Vector3(0f, 1.17f, -0.26f), new Vector3(0.35f, 0.4f, 0.2f), accent);
            if (police) Shape(root.transform, "Cap Brim", PrimitiveType.Cube, new Vector3(0f, 1.73f, 0.2f), new Vector3(0.52f, 0.045f, 0.3f), dark);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                Transform arm = Pivot(root.transform, sign < 0 ? "Left Shoulder" : "Right Shoulder", new Vector3(sign * 0.35f, 1.35f, 0f));
                parts.Add(Shape(arm, "Sleeve", PrimitiveType.Capsule, new Vector3(0f, -0.16f, 0f), new Vector3(0.18f, 0.18f, 0.18f), suit));
                Shape(arm, "Forearm", PrimitiveType.Capsule, new Vector3(0f, -0.38f, 0.015f), new Vector3(0.14f, 0.15f, 0.14f), skin);
                Shape(arm, "Hand", PrimitiveType.Sphere, new Vector3(0f, -0.52f, 0.02f), Vector3.one * 0.17f, skin);
                Transform leg = Pivot(root.transform, sign < 0 ? "Left Hip" : "Right Hip", new Vector3(sign * 0.17f, 0.8f, 0f));
                parts.Add(Shape(leg, "Thigh", PrimitiveType.Capsule, new Vector3(0f, -0.17f, 0f), new Vector3(0.23f, 0.21f, 0.23f), suit));
                Transform knee = Pivot(leg, "Knee", new Vector3(0f, -0.35f, 0f));
                parts.Add(Shape(knee, "Shin", PrimitiveType.Capsule, new Vector3(0f, -0.15f, 0f), new Vector3(0.19f, 0.18f, 0.19f), suit));
                Shape(knee, "Shoe", PrimitiveType.Cube, new Vector3(0f, -0.35f, 0.065f), new Vector3(0.24f, 0.16f, 0.38f), dark);
                if (sign < 0) { avatar.leftArm = arm; avatar.leftLeg = leg; avatar.leftKnee = knee; }
                else { avatar.rightArm = arm; avatar.rightLeg = leg; avatar.rightKnee = knee; }
            }
            avatar.suitRenderers = parts.ToArray();
            return avatar;
        }

        static Transform Pivot(Transform parent, string name, Vector3 position)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(parent, false); pivot.localPosition = position;
            return pivot;
        }

        static Renderer Shape(Transform parent, string name, PrimitiveType type, Vector3 position, Vector3 scale, Material material)
            => MobilePrototypeBuilder.Shape(parent, name, type, position, scale, material).GetComponent<Renderer>();
    }
}
