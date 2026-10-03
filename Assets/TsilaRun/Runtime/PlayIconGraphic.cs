using UnityEngine;
using UnityEngine.UI;

namespace TsilaRun
{
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class PlayIconGraphic : MaskableGraphic
    {
        protected override void OnPopulateMesh(VertexHelper mesh)
        {
            mesh.Clear(); var r = rectTransform.rect;
            mesh.AddVert(new Vector3(r.xMin, r.yMin), color, Vector2.zero);
            mesh.AddVert(new Vector3(r.xMin, r.yMax), color, Vector2.up);
            mesh.AddVert(new Vector3(r.xMax, r.center.y), color, Vector2.right);
            mesh.AddTriangle(0, 1, 2);
        }
    }
}
