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
        public const float FastFallSpeed = 18f;
        public const float SlideSeconds = 1f;
        public const float ReactionSeconds = 0.7f;
        public const float StandingHeight = 1.8f;
        public const float SlideHeight = 1.45f;
        public const float OverheadClearance = 1.55f;
        public const float PlayerRadius = 0.32f;
        public const float FirstRow = 55f;
        public const float Horizon = 170f;
        public const float RoadLength = 24f;
        public const int RoadCount = 9;
        public const int PoolPerKind = 16;
        public const int ItemKindCount = 8;
        public const float PersonDriftBudget = 12f;
        public const float MagnetDuration = 8f;
        public const float ShieldDuration = 10f;
        public const float SpeedBoostDuration = 5f;
        public const float SpeedBoostBonus = 6f;
        public const float MagnetRadius = 16f;
        public const float MagnetPullSpeed = 34f;
        public static float JumpSeconds => 2f * JumpVelocity / Gravity;

        public static bool IsObstacle(RunnerItemKind kind)
        {
            return kind == RunnerItemKind.Barrier || kind == RunnerItemKind.Overhead ||
                kind == RunnerItemKind.Tower || kind == RunnerItemKind.RunningPerson;
        }

        public static bool IsPowerUp(RunnerItemKind kind)
        {
            return kind == RunnerItemKind.CoinMagnet || kind == RunnerItemKind.Shield || kind == RunnerItemKind.SpeedBoost;
        }

        public static float PowerUpDuration(RunnerItemKind kind)
        {
            switch (kind)
            {
                case RunnerItemKind.CoinMagnet: return MagnetDuration;
                case RunnerItemKind.Shield: return ShieldDuration;
                case RunnerItemKind.SpeedBoost: return SpeedBoostDuration;
                default: return 0f;
            }
        }

        // Keep a denser challenge curve than the original prototype, while preserving a little recovery room.
        public static float RowSpacing => MaxSpeed *
            (ReactionSeconds + 2f * LaneSeconds + Mathf.Max(JumpSeconds, SlideSeconds) + 0.1f) + PersonDriftBudget;

        public static int NextSafeLane(int previous, System.Random random)
        {
            return Mathf.Clamp(previous + random.Next(-1, 2), 0, 2);
        }

        public static int RecommendedObstacleCount(double distance)
        {
            if (distance >= 540d) return 3;
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
            return kind == RunnerItemKind.Barrier || kind == RunnerItemKind.Overhead;
        }

        public static float ObstacleCoinHeight(RunnerItemKind kind)
        {
            switch (kind)
            {
                case RunnerItemKind.Overhead: return -0.3f;
                case RunnerItemKind.Barrier: return 0.9f;
                default: return 0.8f;
            }
        }

        // Continuous collision in relative space: catches objects crossing the player in one tick.
        // A simple conservative box approximates the player's capsule silhouette.
        public static bool SweptOverlap(Bounds from, Bounds to, Bounds item, float worldTravel)
        {
            Bounds itemEnd = item;
            itemEnd.center -= Vector3.forward * worldTravel;
            return SweptOverlap(from, to, item, itemEnd);
        }

        public static bool SweptOverlap(Bounds from, Bounds to, Bounds itemFrom, Bounds itemTo)
        {
            Vector3 relativeStart = from.center - itemFrom.center;
            Vector3 relativeEnd = to.center - itemTo.center;
            Vector3 extent = Vector3.Max(from.extents, to.extents) + Vector3.Max(itemFrom.extents, itemTo.extents);
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
