using UnityEngine;
using UnityEngine.UI;

namespace TsilaRun
{
    [RequireComponent(typeof(Graphic))]
    public sealed class UiGradient : BaseMeshEffect
    {
        public Color top = Color.white, bottom = Color.green;
        public override void ModifyMesh(VertexHelper mesh)
        {
            if (!IsActive() || mesh.currentVertCount == 0) return;
            var rect = ((RectTransform)transform).rect;
            UIVertex vertex = default;
            for (int i = 0; i < mesh.currentVertCount; i++)
            {
                mesh.PopulateUIVertex(ref vertex, i);
                vertex.color *= Color.Lerp(bottom, top, Mathf.InverseLerp(rect.yMin, rect.yMax, vertex.position.y));
                mesh.SetUIVertex(vertex, i);
            }
        }
    }
}
