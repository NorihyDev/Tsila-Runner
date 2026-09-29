using UnityEngine;

namespace TsilaRun
{
    public enum RunnerItemKind { Barrier, Overhead, Tower, Coin }

    public sealed class RunnerItem : MonoBehaviour
    {
        public RunnerItemKind kind;
        public BoxCollider hitbox;
        public bool InUse { get; private set; }
        // Prefab roots stay unscaled/unrotated. Visual children may have any primitive shape.
        public Bounds HitBounds => new Bounds(transform.position + hitbox.center, hitbox.size);

        public void Place(float x, float z)
        {
            transform.position = new Vector3(x, 0f, z);
            InUse = true;
            gameObject.SetActive(true);
        }

        public void Release()
        {
            InUse = false; // Set before disabling: collection is idempotent.
            gameObject.SetActive(false);
        }
    }
}
