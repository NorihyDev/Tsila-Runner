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
        public const float CoinObstacleClearance = 0.35f;
        public const float RowStagger = 3f;
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
        public static float RowSpacing => (MaxSpeed + SpeedBoostBonus) *
            (ReactionSeconds + 2f * LaneSeconds + Mathf.Max(JumpSeconds, SlideSeconds) + 0.1f) + PersonDriftBudget + RowStagger;

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
            return TrySweptOverlap(from, to, itemFrom, itemTo, out _);
        }

        // Each face moves linearly, including genuine bounds-size changes. Taking the
        // largest extent of both endpoints creates contacts outside either silhouette.
        public static bool TrySweptOverlap(Bounds from, Bounds to, Bounds itemFrom, Bounds itemTo, out float contactTime)
        {
            contactTime = 0f;
            Vector3 fromMin = from.min, fromMax = from.max, toMin = to.min, toMax = to.max;
            Vector3 itemFromMin = itemFrom.min, itemFromMax = itemFrom.max;
            Vector3 itemToMin = itemTo.min, itemToMax = itemTo.max;
            float enter = 0f, leave = 1f;
            for (int axis = 0; axis < 3; axis++)
            {
                if (!ClipOverlap(fromMax[axis] - itemFromMin[axis], toMax[axis] - itemToMin[axis], ref enter, ref leave) ||
                    !ClipOverlap(itemFromMax[axis] - fromMin[axis], itemToMax[axis] - toMin[axis], ref enter, ref leave))
                    return false;
            }
            contactTime = enter;
            return true;
        }

        static bool ClipOverlap(float start, float end, ref float enter, ref float leave)
        {
            if (start >= 0f && end >= 0f) return true;
            if (start < 0f && end < 0f) return false;
            float crossing = start / (start - end);
            if (start < 0f) enter = Mathf.Max(enter, crossing);
            else leave = Mathf.Min(leave, crossing);
            return enter <= leave;
        }
    }
}
