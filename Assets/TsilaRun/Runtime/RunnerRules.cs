using UnityEngine;

namespace TsilaRun
{
    // Shared by movement, generation, and tests. Distances are in Unity metres.
    public static class RunnerRules
    {
        public const float LaneWidth = 2.4f;
        public const float LaneSeconds = 0.24f;
        public const float StartSpeed = 12f;
        public const float MaxSpeed = 26f;
        public const float Acceleration = 0.18f;
        public const float Gravity = 24f;
        public const float JumpVelocity = 9.2f;
        public const float SlideSeconds = 1f;
        public const float ReactionSeconds = 0.7f;
        public const float StandingHeight = 1.8f;
        public const float SlideHeight = 0.7f;
        public const float PlayerRadius = 0.32f;
        public const float FirstRow = 55f;
        public const float Horizon = 170f;
        public const float RoadLength = 24f;
        public const int RoadCount = 9;
        public const int PoolPerKind = 16;
        public const int ItemKindCount = 5;
        public const float PersonDriftBudget = 12f;
        public static float JumpSeconds => 2f * JumpVelocity / Gravity;

        // Keep a denser challenge curve than the original prototype, while preserving a little recovery room.
        public static float RowSpacing => MaxSpeed *
            (ReactionSeconds + 2f * LaneSeconds + Mathf.Max(JumpSeconds, SlideSeconds) + 0.1f) + PersonDriftBudget;

        public static int NextSafeLane(int previous, System.Random random)
        {
            return Mathf.Clamp(previous + random.Next(-1, 2), 0, 2);
        }

        public static int RecommendedObstacleCount(double distance)
        {
            if (distance >= 540d) return 4;
            if (distance >= 240d) return 3;
            return 2;
        }

        public static int RecommendedCoinTrailLength(double distance)
        {
            if (distance >= 540d) return 9;
            if (distance >= 240d) return 7;
            return 5;
        }

        public static bool CanCoinRideObstacle(RunnerItemKind kind)
        {
            return kind == RunnerItemKind.Barrier || kind == RunnerItemKind.Overhead || kind == RunnerItemKind.Tower;
        }

        public static float ObstacleCoinHeight(RunnerItemKind kind)
        {
            switch (kind)
            {
                case RunnerItemKind.Overhead: return 1.5f;
                case RunnerItemKind.Tower: return 1.2f;
                case RunnerItemKind.Barrier: return 0.9f;
                default: return 0.8f;
            }
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
