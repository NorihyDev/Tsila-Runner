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
            int repeat = Mathf.Max(1, period);
            int index = (int)((section % repeat + repeat) % repeat);
            int validSlot = (slot % repeat + repeat) % repeat;
            gameObject.SetActive(index == validSlot);
        }
    }
}
