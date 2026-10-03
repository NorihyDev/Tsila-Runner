using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TsilaRun.Editor
{
    // Side-view contact sheet for reviewing the actual skinning throughout a stride.
    public static class RunCycleReview
    {
        public static void BuildAndCapture()
        {
            CharacterMotionBuilder.BuildRunCycles();
            Capture();
        }

        public static void Capture()
        {
            const int width = 420, height = 600;
            var sheet = new Texture2D(width * 4, height * 3, TextureFormat.RGB24, false) { hideFlags = HideFlags.HideAndDontSave };
            Directory.CreateDirectory("Screenshots");
            string[] names = { "Tsila", "Officer", "Mianja" };
            try
            {
                for (int row = 0; row < names.Length; row++)
                {
                    EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                    var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPackIntegration.PrefabPath(names[row])));
                    var animator = model.GetComponent<Animator>();
                    animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                    animator.Rebind(); animator.Update(0);
                    var group = model.GetComponent<LODGroup>(); group.ForceLOD(0);
                    var skins = group.GetLODs()[0].renderers.OfType<SkinnedMeshRenderer>().ToArray();
                    var meshes = skins.Select(skin =>
                    {
                        var pose = new GameObject("Review Pose"); pose.transform.SetParent(skin.transform, false);
                        var mesh = new Mesh(); pose.AddComponent<MeshFilter>().sharedMesh = mesh;
                        pose.AddComponent<MeshRenderer>().sharedMaterials = skin.sharedMaterials;
                        skin.enabled = false; return mesh;
                    }).ToArray();
                    var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    floor.transform.position = new Vector3(0, -.055f, 0); floor.transform.localScale = new Vector3(8, .1f, 8);
                    floor.GetComponent<Renderer>().sharedMaterial = MenuPolishBuilder.SandMaterial();
                    RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                    RenderSettings.ambientLight = new Color(.65f, .65f, .7f);
                    var light = new GameObject("Key Light").AddComponent<Light>();
                    light.type = LightType.Directional; light.intensity = 2;
                    light.transform.rotation = Quaternion.Euler(35, -35, 0);
                    var camera = new GameObject("Review Camera").AddComponent<Camera>();
                    camera.transform.position = new Vector3(4, 1.1f, -.35f);
                    camera.transform.LookAt(new Vector3(0, 1.02f, 0));
                    camera.orthographic = true; camera.orthographicSize = 1.06f;
                    camera.aspect = (float)width / height;
                    camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f, .04f, .09f);
                    var target = new RenderTexture(width, height, 24); target.Create(); camera.targetTexture = target;
                    var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
                    try
                    {
                        for (int frame = 0; frame < 4; frame++)
                        {
                            animator.Play("Run", 0, .07f + frame * .25f); animator.Update(0);
                            for (int skin = 0; skin < skins.Length; skin++) skins[skin].BakeMesh(meshes[skin]);
                            camera.Render(); RenderTexture.active = target;
                            pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0); pixels.Apply();
                            sheet.SetPixels(frame * width, (2 - row) * height, width, height, pixels.GetPixels());
                        }
                    }
                    finally
                    {
                        RenderTexture.active = null; camera.targetTexture = null; target.Release();
                        Object.DestroyImmediate(target); Object.DestroyImmediate(pixels);
                        foreach (var mesh in meshes) Object.DestroyImmediate(mesh);
                    }
                }
                sheet.Apply(); File.WriteAllBytes("Screenshots/NaturalRun-Cycles.png", sheet.EncodeToPNG());
                Debug.Log("TSILA_RUN_CAPTURE_OK");
            }
            finally
            {
                Object.DestroyImmediate(sheet);
                EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            }
        }
    }
}
