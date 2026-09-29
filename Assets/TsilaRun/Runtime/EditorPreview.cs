using UnityEngine;

namespace TsilaRun
{
    // The EditorOnly root is stripped from builds. Hide its road tiles on entering Editor Play mode.
    public sealed class EditorPreview : MonoBehaviour
    {
        void Awake() { gameObject.SetActive(false); }
    }
}
