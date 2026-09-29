using System;
using UnityEngine;

namespace TsilaRun
{
    // One local save stores balance, ownership, and selection together. No real-money purchases.
    public sealed class RunnerProgress
    {
        public static readonly string[] SkinNames = { "Island Teal", "Sunset Coral", "Golden Trail", "Midnight" };
        public static readonly int[] Prices = { 0, 40, 100, 150 };
        [Serializable] sealed class SaveData { public int coins; public int owned = 1; public int selected; }
        readonly string key;
        SaveData data;
        public int Wallet => data.coins;
        public int Selected => data.selected;

        public RunnerProgress(string saveKey = "TsilaRun.Progress.v1")
        {
            key = saveKey;
            try { data = JsonUtility.FromJson<SaveData>(PlayerPrefs.GetString(key, "")); }
            catch (ArgumentException) { data = null; }
            if (data == null) data = new SaveData();
            data.coins = Mathf.Max(0, data.coins);
            data.owned = (data.owned & 15) | 1;
            if (!Owns(data.selected)) data.selected = 0;
        }

        public bool Owns(int index) => index >= 0 && index < Prices.Length && (data.owned & (1 << index)) != 0;

        public void EarnCoin() { if (data.coins < int.MaxValue) data.coins++; }

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
