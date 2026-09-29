using System;
using UnityEngine;

namespace TsilaRun
{
    public sealed class RunnerGame : MonoBehaviour
    {
        public enum RunState { Ready, Running, Paused, GameOver }
        const string BestKey = "TsilaRun.BestDistance.v1";
        public RunnerPlayer player;
        public RunnerWorld world;
        public RunState State { get; private set; }
        public double Distance { get; private set; }
        public int Score => (int)Math.Min(int.MaxValue, Distance);
        public int Coins { get; private set; }
        public int Best { get; private set; }
        public float Speed { get; private set; }
        public event Action StateChanged;
        bool hasFocus = true, backgrounded;

        void Awake()
        {
            Time.timeScale = 1f;
            Application.targetFrameRate = 60; // A target, not a measured performance claim.
            Screen.orientation = ScreenOrientation.Portrait;
            Best = PlayerPrefs.GetInt(BestKey, 0);
            Speed = RunnerRules.StartSpeed;
            world.ResetWorld(Environment.TickCount);
            player.ResetPlayer();
            State = RunState.Ready;
        }

        public void StartRun()
        {
            Time.timeScale = 1f;
            Distance = 0d;
            Coins = 0;
            Speed = RunnerRules.StartSpeed;
            world.ResetWorld(Environment.TickCount);
            player.ResetPlayer();
            SetState(hasFocus && !backgrounded ? RunState.Running : RunState.Paused);
        }

        void FixedUpdate()
        {
            if (State != RunState.Running) return;
            float dt = Time.fixedDeltaTime;
            Speed = Mathf.Min(RunnerRules.MaxSpeed, Speed + RunnerRules.Acceleration * dt);
            Bounds previous = player.HitBounds;
            player.Simulate(dt);
            float travel = Speed * dt;
            Distance += travel;
            world.Simulate(travel, previous, player.HitBounds);
        }

        public void CollectCoin() { if (State == RunState.Running && Coins < int.MaxValue) Coins++; }

        public void EndRun()
        {
            if (State != RunState.Running) return;
            SaveBest();
            SetState(RunState.GameOver);
        }

        public void Pause()
        {
            if (State != RunState.Running) return;
            SaveBest();
            SetState(RunState.Paused);
        }

        public void Resume()
        {
            if (State == RunState.Paused && hasFocus && !backgrounded) SetState(RunState.Running);
        }

        void SetState(RunState state)
        {
            State = state;
            Time.timeScale = state == RunState.Paused ? 0f : 1f;
            StateChanged?.Invoke();
        }

        void SaveBest()
        {
            if (Score <= Best) return;
            Best = Score;
            PlayerPrefs.SetInt(BestKey, Best);
            PlayerPrefs.Save();
        }

        void OnApplicationFocus(bool focused) { hasFocus = focused; if (!focused) Pause(); }
        void OnApplicationPause(bool paused) { backgrounded = paused; if (paused) Pause(); }
        void OnApplicationQuit() { SaveBest(); Time.timeScale = 1f; }
        void OnDestroy() { Time.timeScale = 1f; }
    }
}
