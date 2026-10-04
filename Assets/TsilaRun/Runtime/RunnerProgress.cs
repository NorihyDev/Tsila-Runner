using System;
using UnityEngine;

namespace TsilaRun
{
    public enum RunnerMissionKind { CollectCoins, DodgeObstacles, TravelMetres, Jump, Roll, CollectPowerUps, MountainMetres, TunnelMetres }

    // One local save stores balance, ownership, and selection together. No real-money purchases.
    public sealed class RunnerProgress
    {
        public static readonly string[] SkinNames = { "Tsila", "Sunset Coral", "Golden Trail", "Midnight", "Lucef", "Mianja", "Punky" };
        public static readonly int[] Prices = { 0, 40, 100, 150, 1000, 5000, 500 };
        static readonly string[] MissionNames = { "COLLECT COINS", "DODGE OBSTACLES", "RUN 500 METRES", "MAKE 12 JUMPS", "DO 8 ROLLS", "COLLECT 3 POWER UPS", "RUN IN THE ROCKY BIOME", "RUN THROUGH THE TUNNEL" };
        static readonly int[] MissionTargets = { 30, 10, 500, 12, 8, 3, 200, 150 };
        public static int SkinModelIndex(int skin) => skin < 4 ? 0 : skin - 3;
        public const int MissionReward = 40;
        [Serializable] sealed class SaveData
        {
            public int coins;
            public int owned = 1;
            public int selected;
            public int missionIndex;
            public int missionProgress;
        }
        readonly string key;
        SaveData data;
        public int Wallet => data.coins;
        public int Selected => data.selected;
        public RunnerMissionKind ActiveMission => (RunnerMissionKind)data.missionIndex;
        public int ActiveMissionProgress => data.missionProgress;
        public int ActiveMissionTarget => MissionTargets[data.missionIndex];
        public string ActiveMissionName => MissionNames[data.missionIndex];

        public RunnerProgress(string saveKey = "TsilaRun.Progress.v1")
        {
            key = saveKey;
            try { data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(key, "")); }
            catch (ArgumentException) { data = null; }
            if (data == null) data = new SaveData();
            data.coins = Mathf.Max(0, data.coins);
            // Keep the original four indices so existing purchases and balances survive.
            data.owned = (data.owned & ((1 << Prices.Length) - 1)) | 1;
            if (!Owns(data.selected)) data.selected = 0;
            data.missionIndex = Mathf.Clamp(data.missionIndex, 0, MissionTargets.Length - 1);
            data.missionProgress = Mathf.Clamp(data.missionProgress, 0, ActiveMissionTarget - 1);
        }

        public bool Owns(int index) => index >= 0 && index < Prices.Length && (data.owned & (1 << index)) != 0;

        public void EarnCoin() { if (data.coins < int.MaxValue) data.coins++; }

        public int AdvanceMission(RunnerMissionKind kind, int amount = 1)
        {
            if (kind != ActiveMission || amount <= 0) return 0;
            data.missionProgress = (int)Math.Min(ActiveMissionTarget, (long)data.missionProgress + amount);
            if (data.missionProgress < ActiveMissionTarget) return 0;

            data.coins = (int)Math.Min(int.MaxValue, (long)data.coins + MissionReward);
            data.missionIndex = (data.missionIndex + 1) % MissionTargets.Length;
            data.missionProgress = 0;
            Save();
            return MissionReward;
        }

        public bool BuyOrEquip(int index)
        {
            if (index < 0 || index >= Prices.Length) return false;
            if (!Owns(index))
            {
                if (data.coins < Prices[index]) return false;
                data.coins -= Prices[index];
                data.owned |= 1 << index;
            }
            data.selected = index;
            Save();
            return true;
        }

        public void Save()
        {
            PlayerPrefs.SetString(key, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }
    }
}
