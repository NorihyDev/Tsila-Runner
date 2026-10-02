using UnityEngine;

namespace TsilaRun
{
    public sealed class RunnerVfx : MonoBehaviour
    {
        public RunnerGame game;
        public ParticleSystem speedTrail;
        public ParticleSystem coinBurst;
        public ParticleSystem crashBurst;
        public ParticleSystem powerUpBurst;
        public ParticleSystem magnetAura;
        public ParticleSystem shieldAura;
        Material particleMaterial;

        void Awake()
        {
            Bind(GetComponent<RunnerGame>() ?? GetComponentInParent<RunnerGame>());
        }

        public void Bind(RunnerGame owner)
        {
            game = owner;
            EnsureSystems();
        }

        public void SetRunning(bool running)
        {
            if (speedTrail == null) return;
            var emission = speedTrail.emission;
            emission.enabled = running;
            if (!running) speedTrail.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            else if (!speedTrail.isPlaying) speedTrail.Play();
        }

        public void SpawnCoinBurst(Vector3 position)
        {
            if (coinBurst == null) return;
            coinBurst.transform.position = position;
            coinBurst.Emit(10);
        }

        public void SpawnCrashBurst(Vector3 position)
        {
            if (crashBurst == null) return;
            crashBurst.transform.position = position + Vector3.up * 0.4f;
            crashBurst.Emit(16);
        }

        public void SpawnPowerUpBurst(Vector3 position, RunnerItemKind kind)
        {
            if (powerUpBurst == null) return;
            powerUpBurst.transform.position = position;
            var main = powerUpBurst.main;
            main.startColor = PowerUpColor(kind);
            powerUpBurst.Emit(12);
        }

        void Update()
        {
            if (game == null) return;
            bool running = game.State == RunnerGame.RunState.Running || game.State == RunnerGame.RunState.Intro;
            if (speedTrail != null)
            {
                var main = speedTrail.main;
                main.startColor = !running ? new Color(0f, 0f, 0f, 0f) :
                    game.BoostRemaining > 0f ? new Color(1f, 0.48f, 0.15f, 0.9f) : new Color(0.65f, 0.95f, 1f, 0.75f);
                var emission = speedTrail.emission;
                emission.rateOverTime = running ? Mathf.Lerp(4f, 12f, Mathf.InverseLerp(RunnerRules.StartSpeed, RunnerRules.MaxSpeed + RunnerRules.SpeedBoostBonus, game.TravelSpeed)) : 0f;
            }
            if (speedTrail != null) SetRunning(running);
            Vector3 auraPosition = game.player != null ? game.player.transform.position + Vector3.up * 0.9f : transform.position;
            SetAura(magnetAura, running && game.MagnetRemaining > 0f, auraPosition);
            SetAura(shieldAura, running && game.ShieldRemaining > 0f, auraPosition);
        }

        void EnsureSystems()
        {
            if (game == null) return;
            if (particleMaterial == null) particleMaterial = Resources.Load<Material>("TsilaRunParticles");
            if (speedTrail == null)
            {
                var trail = new GameObject("Speed FX");
                trail.transform.SetParent(game.player != null ? game.player.transform : transform, false);
                trail.transform.localPosition = new Vector3(0f, 0.25f, -0.25f);
                speedTrail = trail.AddComponent<ParticleSystem>();
                Configure(speedTrail, 8f, 0.22f, 0.08f, new Color(0.65f, 0.95f, 1f, 0.65f));
                var main = speedTrail.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
            if (coinBurst == null)
            {
                var coin = new GameObject("Coin Burst");
                coin.transform.SetParent(transform, false);
                coinBurst = coin.AddComponent<ParticleSystem>();
                Configure(coinBurst, 0f, 0.3f, 0.1f, new Color(1f, 0.8f, 0.2f, 1f));
            }
            if (crashBurst == null)
            {
                var crash = new GameObject("Crash Burst");
                crash.transform.SetParent(transform, false);
                crashBurst = crash.AddComponent<ParticleSystem>();
                Configure(crashBurst, 0f, 0.45f, 0.14f, new Color(1f, 0.35f, 0.2f, 1f));
            }
            if (powerUpBurst == null)
            {
                var burst = new GameObject("Power Up Burst");
                burst.transform.SetParent(transform, false);
                powerUpBurst = burst.AddComponent<ParticleSystem>();
                Configure(powerUpBurst, 0f, 0.4f, 0.12f, Color.white);
            }
            if (magnetAura == null) magnetAura = CreateAura("Magnet Aura", new Color(1f, 0.72f, 0.12f, 0.85f));
            if (shieldAura == null) shieldAura = CreateAura("Shield Aura", new Color(0.2f, 0.78f, 1f, 0.85f));
        }

        ParticleSystem CreateAura(string name, Color color)
        {
            var aura = new GameObject(name);
            aura.transform.SetParent(transform, false);
            var system = aura.AddComponent<ParticleSystem>();
            Configure(system, 8f, 0.45f, 0.07f, color);
            var main = system.main;
            main.loop = true;
            var emission = system.emission;
            emission.rateOverTime = 8f;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.48f;
            return system;
        }

        static void SetAura(ParticleSystem system, bool active, Vector3 position)
        {
            if (system == null) return;
            system.transform.position = position;
            var emission = system.emission;
            emission.enabled = active;
            if (active && !system.isPlaying) system.Play();
            else if (!active && system.isPlaying) system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }

        static Color PowerUpColor(RunnerItemKind kind)
        {
            switch (kind)
            {
                case RunnerItemKind.CoinMagnet: return new Color(1f, 0.72f, 0.12f, 1f);
                case RunnerItemKind.Shield: return new Color(0.2f, 0.78f, 1f, 1f);
                default: return new Color(1f, 0.42f, 0.12f, 1f);
            }
        }

        void Configure(ParticleSystem system, float rate, float lifetime, float size, Color color)
        {
            var main = system.main;
            main.startLifetime = lifetime;
            main.startSpeed = 0.55f;
            main.startSize = size;
            main.startColor = color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.loop = false;
            main.maxParticles = 48;
            var emission = system.emission;
            emission.enabled = true;
            emission.rateOverTime = rate;
            var shape = system.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;
            var trail = system.trails;
            trail.enabled = false;
            var renderer = system.GetComponent<ParticleSystemRenderer>();
            if (particleMaterial != null) renderer.sharedMaterial = particleMaterial;
            else renderer.enabled = false;
            system.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        }
    }
}
