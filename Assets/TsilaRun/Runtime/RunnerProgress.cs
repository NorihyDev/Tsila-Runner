using System;
using UnityEngine;

namespace TsilaRun
{
    public enum RunnerMissionKind { CollectCoins, DodgeObstacles }

    // One local save stores balance, ownership, and selection together. No real-money purchases.
    public sealed class RunnerProgress
    {
        public static readonly string[] SkinNames = { "Island Teal", "Sunset Coral", "Golden Trail", "Midnight" };
        public static readonly int[] Prices = { 0, 40, 100, 150 };
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
        public int ActiveMissionTarget => ActiveMission == RunnerMissionKind.CollectCoins ? 30 : 10;
        public string ActiveMissionName => ActiveMission == RunnerMissionKind.CollectCoins ? "COLLECT COINS" : "DODGE OBSTACLES";

        public RunnerProgress(string saveKey = "TsilaRun.Progress.v1")
        {
            key = saveKey;
            try { data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(key, "")); }
            catch (ArgumentException) { data = null; }
            if (data == null) data = new SaveData();
            data.coins = Mathf.Max(0, data.coins);
            data.owned = (data.owned & 15) | 1;
            if (!Owns(data.selected)) data.selected = 0;
            data.missionIndex = Mathf.Clamp(data.missionIndex, 0, 1);
            data.missionProgress = Mathf.Clamp(data.missionProgress, 0, ActiveMissionTarget - 1);
        }

        public bool Owns(int index) => index >= 0 && index < Prices.Length && (data.owned & (1 << index)) != 0;

        public void EarnCoin() { if (data.coins < int.MaxValue) data.coins++; }

        public int AdvanceMission(RunnerMissionKind kind)
        {
            if (kind != ActiveMission) return 0;
            data.missionProgress++;
            if (data.missionProgress < ActiveMissionTarget) return 0;

            data.coins = (int)Math.Min(int.MaxValue, (long)data.coins + MissionReward);
            data.missionIndex = 1 - data.missionIndex;
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
