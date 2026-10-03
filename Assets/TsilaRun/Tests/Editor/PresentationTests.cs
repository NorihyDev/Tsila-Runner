using System.Linq;
using NUnit.Framework;
using TsilaRun.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Object = UnityEngine.Object;

namespace TsilaRun.Tests
{
    public sealed class PresentationTests
    {
        [TestCase("Tsila")]
        [TestCase("Officer")]
        [TestCase("Mianja")]
        public void RollStaysAboveRoadAndBelowOverheadThroughoutBothLods(string name)
        {
            var model = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MeshyPackIntegration.PrefabPath(name)));
            try
            {
                var animator = model.GetComponent<Animator>();
                // Headless tests have no camera to make these renderers visible.
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                animator.Rebind();
                animator.Update(0f);
                var roll = animator.runtimeAnimatorController.animationClips.Single(c => c.name.EndsWith("_Slide"));
                var root = model.GetComponentsInChildren<Transform>().Single(t => t.name == "root");
                Quaternion start = Quaternion.identity;
                for (int frame = 0; frame <= 60; frame++)
                {
                    animator.Play("Slide", 0, Mathf.Min(.999f, frame / 60f));
                    animator.Update(0f);
                    Assert.Less(Vector3.Distance(Vector3.one, root.localScale), .005f, "Keep the runner at full size.");
                    if (frame == 0) start = root.localRotation;
                    if (frame == 30) Assert.Greater(Quaternion.Angle(start, root.localRotation), 150, "Roll must rotate the body.");
                    foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var bounds = BlenderPackIntegration.SkinnedBounds(model, skin);
                        Assert.GreaterOrEqual(bounds.min.y, -.015f, name + " floor at " + frame);
                        Assert.LessOrEqual(bounds.max.y, RunnerRules.SlideHeight, name + " overhead at " + frame);
                        Assert.Less(bounds.size.z, 1.5f, "A tucked roll, rather than a prone pose.");
                    }
                }
            }
            finally { Object.DestroyImmediate(model); }
        }

        [Test]
        public void MenuHasSportFontTwoActionsAndMobileScaler()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var hud = Object.FindAnyObjectByType<RunnerHud>();
            var scaler = hud.GetComponent<CanvasScaler>();
            Assert.AreEqual(new Vector2(1080, 1920), scaler.referenceResolution);
            Assert.AreEqual(.5f, scaler.matchWidthOrHeight);
            Assert.AreEqual(CanvasScaler.ScreenMatchMode.MatchWidthOrHeight, scaler.screenMatchMode);
            Assert.AreEqual(2, hud.startPanel.GetComponentsInChildren<Button>(true).Length);
            Assert.IsNull(hud.startPanel.transform.Find("Controls"));
            Assert.IsNull(hud.startPanel.transform.Find("Outfit Badge"));
            Assert.IsTrue(hud.startPanel.GetComponentsInChildren<TMP_Text>().All(t => t.font.name == "Rajdhani SDF"));
            var play = (RectTransform)hud.playButton.transform;
            var shop = (RectTransform)hud.GetComponent<RunnerShop>().openFromStart.transform;
            Assert.Less(shop.anchorMax.y, play.anchorMin.y);
            Assert.IsNotNull(hud.GetComponent<RunnerTutorial>());
            Assert.IsFalse(hud.GetComponent<RunnerTutorial>().panel.activeSelf);
        }

        [Test]
        public void SceneryStaysOffTheRoadAndUsesOneDeck()
        {
            var road = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MobilePrototypeBuilder.Root + "/Prefabs/RoadSection.prefab"));
            try
            {
                Assert.IsNull(road.transform.Find("Meshy RoadSectionAlt"));
                foreach (var zone in road.GetComponent<RunnerRoadSection>().scenery.Take(2))
                    foreach (Transform prop in zone.transform)
                    {
                        if (prop.name == "Island" || prop.name == "Mountain Sand") continue;
                        var bounds = MeshyPackIntegration.PlacedBounds(prop.gameObject);
                        Assert.IsTrue(bounds.max.x <= -4.2f || bounds.min.x >= 4.2f, prop.name + " intrudes into road: " + bounds);
                    }
                var tunnel = road.GetComponent<RunnerRoadSection>().scenery[2];
                Assert.IsNull(tunnel.transform.Find("Meshy DirtyTunnel"));
                Assert.IsNull(tunnel.transform.Find("Meshy ModularTunnel"));
                Assert.Greater(MeshyPackIntegration.PlacedBounds(tunnel.transform.Find("Tunnel Ceiling").gameObject).min.y, 7.5f,
                    "Keep the ceiling above the chase camera, including its shake margin.");
                foreach (Transform prop in tunnel.transform)
                {
                    var bounds = MeshyPackIntegration.PlacedBounds(prop.gameObject);
                    Assert.IsTrue(bounds.min.y >= 5f || bounds.max.x <= -4.2f || bounds.min.x >= 4.2f, prop.name + " blocks tunnel lanes: " + bounds);
                }
            }
            finally { Object.DestroyImmediate(road); }
        }

        [Test]
        public void IslandHasSparseAlternatingSceneryAndSandGround()
        {
            var road = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MobilePrototypeBuilder.Root + "/Prefabs/RoadSection.prefab"));
            try
            {
                var section = road.GetComponent<RunnerRoadSection>();
                var island = section.scenery[0];
                Assert.AreEqual("TsilaRun/Sand", island.transform.Find("Island").GetComponent<Renderer>().sharedMaterial.shader.name);
                int palms = 0, trees = 0;
                for (int index = 0; index < 12; index++)
                {
                    section.SetLocation(index * RunnerRules.RoadLength);
                    var visible = island.GetComponentsInChildren<ScenerySpacing>().Where(p => p.gameObject.activeSelf).ToArray();
                    Assert.LessOrEqual(visible.Length, 2, "Leave open ground between small groups.");
                    palms += visible.Count(p => p.name.Contains("Palm")); trees += visible.Count(p => p.name.Contains("Tree"));
                }
                Assert.AreEqual(4, palms); Assert.AreEqual(4, trees);
            }
            finally { Object.DestroyImmediate(road); }
        }

        [Test]
        public void IntroOfficerStaysAheadOfCameraAndOutsidePlayer()
        {
            EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var game = Object.FindAnyObjectByType<RunnerGame>();
            game.chase.ResetChase();
            for (int frame = 0; frame <= 60; frame++)
            {
                game.chase.TickIntro(RunnerChase.IntroSeconds / 60f);
                Vector3 position = game.chase.officer.transform.position;
                Assert.Greater(position.z, -6f, "Keep the officer clear of the camera's near foreground.");
                Assert.Less(position.z, -2f, "Keep the officer clear of the player's capsule.");
            }
        }
    }
}
