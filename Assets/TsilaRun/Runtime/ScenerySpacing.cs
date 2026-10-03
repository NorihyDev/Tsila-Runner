using UnityEngine;

namespace TsilaRun
{
    // Deterministic placement across recycled road modules; no random popping each frame.
    public sealed class ScenerySpacing : MonoBehaviour
    {
        public int period = 3;
        public int slot;
        public void SetSection(long section)
        {
            int index = (int)((section % period + period) % period);
            gameObject.SetActive(index == slot);
        }
    }
}
