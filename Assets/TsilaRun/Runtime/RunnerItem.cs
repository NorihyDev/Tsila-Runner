using UnityEngine;

namespace TsilaRun
{
    public enum RunnerItemKind
    {
        Barrier, Overhead, Tower, Coin, RunningPerson,
        CoinMagnet, Shield, SpeedBoost
    }

    public sealed class RunnerItem : MonoBehaviour
    {
        public RunnerItemKind kind;
        public BoxCollider hitbox;
        public RunnerGame game;
        public bool InUse { get; private set; }
        float forwardDrift;
        Transform animatedVisual;
        Vector3 visualRestPosition;
        Quaternion visualRestRotation;
        float animationPhase;
        // Derive bounds directly from the collider transform without waiting for a
        // physics sync after a pooled item moves (also supports scaled prefab roots).
        public Bounds HitBounds
        {
            get
            {
                if (hitbox == null) hitbox = GetComponent<BoxCollider>();
                if (hitbox == null) return new Bounds(transform.position, Vector3.zero);
                Transform boxTransform = hitbox.transform;
                Vector3 x = boxTransform.TransformVector(Vector3.right * hitbox.size.x);
                Vector3 y = boxTransform.TransformVector(Vector3.up * hitbox.size.y);
                Vector3 z = boxTransform.TransformVector(Vector3.forward * hitbox.size.z);
                Vector3 size = new Vector3(Mathf.Abs(x.x) + Mathf.Abs(y.x) + Mathf.Abs(z.x),
                    Mathf.Abs(x.y) + Mathf.Abs(y.y) + Mathf.Abs(z.y), Mathf.Abs(x.z) + Mathf.Abs(y.z) + Mathf.Abs(z.z));
                return new Bounds(boxTransform.TransformPoint(hitbox.center), size);
            }
        }

        void Awake()
        {
            CacheCoinVisual();
        }

        void CacheCoinVisual()
        {
            if (animatedVisual != null || kind != RunnerItemKind.Coin || transform.childCount == 0) return;
            animatedVisual = transform.GetChild(0);
            visualRestPosition = animatedVisual.localPosition;
            visualRestRotation = animatedVisual.localRotation;
            animationPhase = Random.value * Mathf.PI * 2f;
        }

        void Update()
        {
            if (!InUse || kind != RunnerItemKind.Coin || animatedVisual == null) return;
            if (game != null && game.State != RunnerGame.RunState.Running && game.State != RunnerGame.RunState.Intro) return;
            animationPhase += Time.deltaTime * 4f;
            animatedVisual.localPosition = visualRestPosition + Vector3.up * (Mathf.Sin(animationPhase * 1.4f) * 0.1f);
            animatedVisual.localRotation = Quaternion.Euler(0f, animationPhase * Mathf.Rad2Deg, 0f) * visualRestRotation;
        }

        public void Place(float x, float z)
        {
            Place(x, z, 0f);
        }

        public void Place(float x, float z, float y)
        {
            transform.position = new Vector3(x, y, z);
            forwardDrift = 0f;
            InUse = true;
            CacheCoinVisual();
            if (animatedVisual != null)
            {
                animatedVisual.localPosition = visualRestPosition;
                animatedVisual.localRotation = visualRestRotation;
            }
            gameObject.SetActive(true);
        }

        public void Release()
        {
            InUse = false; // Set before disabling: collection is idempotent.
            gameObject.SetActive(false);
        }

        public float TravelThisTick(float roadTravel, float dt)
        {
            if (kind != RunnerItemKind.RunningPerson) return roadTravel;
            float previous = forwardDrift;
            forwardDrift = Mathf.Min(RunnerRules.PersonDriftBudget, forwardDrift + 0.75f * dt);
            return roadTravel - (forwardDrift - previous);
        }
    }
}
