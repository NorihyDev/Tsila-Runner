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
        public bool InUse { get; private set; }
        float forwardDrift;
        Transform animatedVisual;
        Vector3 visualRestPosition;
        float animationPhase;
        // Prefab roots stay unscaled/unrotated. Visual children may have any primitive shape.
        public Bounds HitBounds => new Bounds(transform.position + hitbox.center, hitbox.size);

        void Awake()
        {
            if (kind != RunnerItemKind.Coin || transform.childCount == 0) return;
            animatedVisual = transform.GetChild(0);
            visualRestPosition = animatedVisual.localPosition;
            animationPhase = Random.value * Mathf.PI * 2f;
        }

        void Update()
        {
            if (!InUse || kind != RunnerItemKind.Coin || animatedVisual == null) return;
            animationPhase += Time.deltaTime * 4f;
            animatedVisual.localPosition = visualRestPosition + Vector3.up * (Mathf.Sin(animationPhase * 1.4f) * 0.1f);
            animatedVisual.localRotation = Quaternion.Euler(0f, animationPhase * Mathf.Rad2Deg, 0f);
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
            if (animatedVisual != null)
            {
                animatedVisual.localPosition = visualRestPosition;
                animatedVisual.localRotation = Quaternion.identity;
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
