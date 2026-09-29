using UnityEngine;

namespace TsilaRun
{
    // Shared by movement, generation, and tests. Distances are in Unity metres.
    public static class RunnerRules
    {
        public const float LaneWidth = 2.4f;
        public const float LaneSeconds = 0.24f;
        public const float StartSpeed = 10f;
        public const float MaxSpeed = 22f;
        public const float Acceleration = 0.12f;
        public const float Gravity = 24f;
        public const float JumpVelocity = 9f;
        public const float SlideSeconds = 1.05f;
        public const float ReactionSeconds = 0.8f;
        public const float StandingHeight = 1.8f;
        public const float SlideHeight = 0.7f;
        public const float PlayerRadius = 0.32f;
        public const float FirstRow = 55f;
        public const float Horizon = 170f;
        public const float RoadLength = 24f;
        public const int RoadCount = 9;
        public const int PoolPerKind = 16;
        public static float JumpSeconds => 2f * JumpVelocity / Gravity;

        // Budget even a two-lane recovery plus the longest action, at FUTURE maximum speed.
        // This remains safe when the runner accelerates toward an already-spawned row.
        public static float RowSpacing => MaxSpeed *
            (ReactionSeconds + 2f * LaneSeconds + Mathf.Max(JumpSeconds, SlideSeconds) + 0.15f);

        public static int NextSafeLane(int previous, System.Random random)
        {
            return Mathf.Clamp(previous + random.Next(-1, 2), 0, 2);
        }

        // Continuous collision in relative space: catches objects crossing the player in one tick.
        // A simple conservative box approximates the player's capsule silhouette.
        public static bool SweptOverlap(Bounds from, Bounds to, Bounds item, float worldTravel)
        {
            Vector3 relativeStart = from.center - item.center;
            Vector3 relativeEnd = to.center - (item.center - Vector3.forward * worldTravel);
            Vector3 extent = Vector3.Max(from.extents, to.extents) + item.extents;
            Vector3 delta = relativeEnd - relativeStart;
            float enter = 0f, leave = 1f;
            for (int axis = 0; axis < 3; axis++)
            {
                if (Mathf.Abs(delta[axis]) < 0.00001f)
                {
                    if (Mathf.Abs(relativeStart[axis]) > extent[axis]) return false;
                    continue;
                }
                float a = (-extent[axis] - relativeStart[axis]) / delta[axis];
                float b = (extent[axis] - relativeStart[axis]) / delta[axis];
                enter = Mathf.Max(enter, Mathf.Min(a, b));
                leave = Mathf.Min(leave, Mathf.Max(a, b));
                if (enter > leave) return false;
            }
            return true;
        }
    }
}
