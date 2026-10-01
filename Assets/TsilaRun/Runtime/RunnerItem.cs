using UnityEngine;

namespace TsilaRun
{
    public enum RunnerItemKind { Barrier, Overhead, Tower, Coin, RunningPerson }

    public sealed class RunnerItem : MonoBehaviour
    {
        public RunnerItemKind kind;
        public BoxCollider hitbox;
        public bool InUse { get; private set; }
        float forwardDrift;
        // Prefab roots stay unscaled/unrotated. Visual children may have any primitive shape.
        public Bounds HitBounds => new Bounds(transform.position + hitbox.center, hitbox.size);

        public void Place(float x, float z)
        {
            Place(x, z, 0f);
        }

        public void Place(float x, float z, float y)
        {
            transform.position = new Vector3(x, y, z);
            forwardDrift = 0f;
            InUse = true;
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
