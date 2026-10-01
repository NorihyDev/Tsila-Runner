using System;
using UnityEngine;

namespace TsilaRun
{
    public sealed class RunnerGame : MonoBehaviour
    {
        public enum RunState { Ready, Running, Paused, GameOver, Intro, Shop }
        const string BestKey = "TsilaRun.BestDistance.v1";
        public RunnerPlayer player;
        public RunnerWorld world;
        public RunnerChase chase;
        public RunnerVfx vfx;
        public Material[] skinMaterials;
        static readonly Color[] SkinTints =
        {
            Color.white,
            new Color(1f, .55f, .52f),
            new Color(1f, .82f, .35f),
            new Color(.42f, .48f, .75f)
        };
        public RunnerProgress Progress { get; private set; }
        RunState resumeState = RunState.Running;
        RunState returnFromShop = RunState.Ready;
        public RunState State { get; private set; }
        public double Distance { get; private set; }
        public int Score => (int)Math.Min(int.MaxValue, Distance);
        public int Coins { get; private set; }
        public int Best { get; private set; }
        public float Speed { get; private set; }
        public float TravelSpeed => Speed + (BoostRemaining > 0f ? RunnerRules.SpeedBoostBonus : 0f);
        public float MagnetRemaining { get; private set; }
        public float ShieldRemaining { get; private set; }
        public float BoostRemaining { get; private set; }
        public event Action<int> MissionCompleted;
        public event Action StateChanged;
        bool hasFocus = true, backgrounded;

        void Awake()
        {
            Time.timeScale = 1f;
            Application.targetFrameRate = 60; // A target, not a measured performance claim.
            Screen.orientation = ScreenOrientation.Portrait;
            Best = PlayerPrefs.GetInt(BestKey, 0);
            Progress = new RunnerProgress();
            if (vfx == null) vfx = GetComponent<RunnerVfx>() ?? gameObject.AddComponent<RunnerVfx>();
            if (vfx != null) vfx.Bind(this);
            ApplySkin();
            Speed = RunnerRules.StartSpeed;
            world.ResetWorld(Environment.TickCount);
            player.ResetPlayer();
            State = RunState.Ready;
        }

        public void StartRun()
        {
            Progress?.Save();
            Time.timeScale = 1f;
            Distance = 0d;
            Coins = 0;
            Speed = RunnerRules.StartSpeed;
            ResetPowerUps();
            world.ResetWorld(Environment.TickCount);
            player.ResetPlayer();
            if (chase != null) chase.ResetChase();
            resumeState = chase != null ? RunState.Intro : RunState.Running;
            if (vfx != null) vfx.SetRunning(true);
            SetState(hasFocus && !backgrounded ? resumeState : RunState.Paused);
        }

        public void CompleteIntro()
        {
            if (State == RunState.Intro) SetState(RunState.Running);
        }

        void FixedUpdate()
        {
            if (State == RunState.Intro)
            {
                if (chase == null || chase.TickIntro(Time.fixedDeltaTime)) CompleteIntro();
                return;
            }
            if (State != RunState.Running) return;
            float dt = Time.fixedDeltaTime;
            MagnetRemaining = Mathf.Max(0f, MagnetRemaining - dt);
            ShieldRemaining = Mathf.Max(0f, ShieldRemaining - dt);
            BoostRemaining = Mathf.Max(0f, BoostRemaining - dt);
            Speed = Mathf.Min(RunnerRules.MaxSpeed, Speed + RunnerRules.Acceleration * dt);
            Bounds previous = player.HitBounds;
            player.Simulate(dt);
            float travel = TravelSpeed * dt;
            Distance += travel;
            world.Simulate(travel, previous, player.HitBounds);
        }

        public void CollectCoin()
        {
            if (State != RunState.Running || Coins == int.MaxValue) return;
            Coins++;
            if (vfx != null) vfx.SpawnCoinBurst(player != null ? player.transform.position + Vector3.up * 0.8f : transform.position);
            Progress?.EarnCoin();
            AdvanceMission(RunnerMissionKind.CollectCoins);
        }

        public void CollectPowerUp(RunnerItemKind kind)
        {
            if (State != RunState.Running) return;
            float duration = RunnerRules.PowerUpDuration(kind);
            if (duration <= 0f) return;
            switch (kind)
            {
                case RunnerItemKind.CoinMagnet: MagnetRemaining = duration; break;
                case RunnerItemKind.Shield: ShieldRemaining = duration; break;
                case RunnerItemKind.SpeedBoost: BoostRemaining = duration; break;
            }
            if (vfx != null) vfx.SpawnPowerUpBurst(player.transform.position + Vector3.up * 0.9f, kind);
        }

        public bool TryAbsorbObstacle()
        {
            if (ShieldRemaining <= 0f) return false;
            ShieldRemaining = 0f;
            if (vfx != null) vfx.SpawnPowerUpBurst(player.transform.position + Vector3.up * 0.9f, RunnerItemKind.Shield);
            return true;
        }

        public void ObstacleCleared()
        {
            if (State == RunState.Running) AdvanceMission(RunnerMissionKind.DodgeObstacles);
        }

        public string PowerUpStatus
        {
            get
            {
                string status = "";
                AppendPowerUpStatus(ref status, "MAGNET", MagnetRemaining);
                AppendPowerUpStatus(ref status, "SHIELD", ShieldRemaining);
                AppendPowerUpStatus(ref status, "BOOST", BoostRemaining);
                return status;
            }
        }

        public void OpenShop()
        {
            if (State != RunState.Ready && State != RunState.GameOver) return;
            returnFromShop = State;
            SetState(RunState.Shop);
        }

        public void CloseShop()
        {
            if (State == RunState.Shop) SetState(returnFromShop);
        }

        public void ApplySkin()
        {
            if (Progress == null || player == null) return;
            var avatar = player.GetComponentInChildren<RunnerAvatar>();
            if (avatar == null) return;
            int selected = Mathf.Clamp(Progress.Selected, 0, SkinTints.Length - 1);
            if (skinMaterials != null && selected < skinMaterials.Length && skinMaterials[selected] != null)
                avatar.SetSuit(skinMaterials[selected]);
            else avatar.SetSuitTint(SkinTints[selected]);
        }

        public void ReturnToMenu()
        {
            SaveBest();
            Time.timeScale = 1f;
            Distance = 0d; Coins = 0; Speed = RunnerRules.StartSpeed;
            ResetPowerUps();
            player.ResetPlayer();
            world.ResetWorld(Environment.TickCount);
            if (chase != null) { chase.ResetChase(); chase.officer.gameObject.SetActive(false); }
            if (vfx != null) vfx.SetRunning(false);
            returnFromShop = RunState.Ready;
            SetState(RunState.Ready);
        }

        public void EndRun()
        {
            if (State != RunState.Running) return;
            SaveBest();
            if (vfx != null) vfx.SpawnCrashBurst(player != null ? player.transform.position : transform.position);
            SetState(RunState.GameOver);
        }

        public void Pause()
        {
            if (State != RunState.Running && State != RunState.Intro) return;
            resumeState = State;
            SaveBest();
            SetState(RunState.Paused);
        }

        public void Resume()
        {
            if (State == RunState.Paused && hasFocus && !backgrounded) SetState(resumeState);
        }

        void SetState(RunState state)
        {
            State = state;
            Time.timeScale = state == RunState.Paused ? 0f : 1f;
            if (vfx != null) vfx.SetRunning(state == RunState.Running || state == RunState.Intro);
            StateChanged?.Invoke();
        }

        void SaveBest()
        {
            Progress?.Save();
            if (Score <= Best) return;
            Best = Score;
            PlayerPrefs.SetInt(BestKey, Best);
            PlayerPrefs.Save();
        }

        void AdvanceMission(RunnerMissionKind kind)
        {
            int reward = Progress != null ? Progress.AdvanceMission(kind) : 0;
            if (reward > 0) MissionCompleted?.Invoke(reward);
        }

        void ResetPowerUps()
        {
            MagnetRemaining = 0f;
            ShieldRemaining = 0f;
            BoostRemaining = 0f;
        }

        static void AppendPowerUpStatus(ref string status, string label, float remaining)
        {
            if (remaining <= 0f) return;
            if (status.Length > 0) status += "   ";
            status += label + " " + Mathf.CeilToInt(remaining) + "s";
        }

        void OnApplicationFocus(bool focused) { hasFocus = focused; if (!focused) Pause(); }
        void OnApplicationPause(bool paused) { backgrounded = paused; if (paused) Pause(); }
        void OnApplicationQuit() { SaveBest(); Time.timeScale = 1f; }
        void OnDestroy() { Time.timeScale = 1f; }
    }
}
