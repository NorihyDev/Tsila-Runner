using System.Linq;
using NUnit.Framework;
using TsilaRun.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using Object = UnityEngine.Object;

namespace TsilaRun.Tests
{
    public sealed class MovementRegressionTests
    {
        static RunnerPlayer CreatePlayer(GameObject root)
        {
            var player = root.AddComponent<RunnerPlayer>();
            player.body = root.AddComponent<CapsuleCollider>();
            player.body.radius = RunnerRules.PlayerRadius;
            player.visual = new GameObject("Visual").transform;
            player.visual.SetParent(root.transform, false);
            player.ResetPlayer();
            return player;
        }

        [Test]
        public void ShrinkingSilhouetteDoesNotHitBeamAfterHeadHasCleared()
        {
            var standing = new Bounds(Vector3.up * .9f, new Vector3(.64f, 1.8f, .64f));
            var sliding = new Bounds(Vector3.up * RunnerRules.SlideHeight * .5f,
                new Vector3(.64f, RunnerRules.SlideHeight, .64f));
            var beamFrom = new Bounds(new Vector3(0f, RunnerRules.OverheadClearance + .9f, 2f),
                new Vector3(1.9f, 1.8f, .4f));
            var beamTo = beamFrom; beamTo.center -= Vector3.forward * 2f;
            Assert.IsFalse(RunnerRules.SweptOverlap(standing, sliding, beamFrom, beamTo),
                "At first depth contact the interpolated head is already below the beam.");
            Assert.IsTrue(RunnerRules.SweptOverlap(standing, standing, beamFrom, beamTo));
        }

        [Test]
        public void SweepReportsFirstContactTimeForChronologicalResolution()
        {
            var player = new Bounds(Vector3.zero, Vector3.one);
            var from = new Bounds(Vector3.forward * 3f, Vector3.one);
            var to = new Bounds(-Vector3.forward, Vector3.one);
            Assert.IsTrue(RunnerRules.TrySweptOverlap(player, player, from, to, out float contact));
            Assert.AreEqual(.5f, contact, .00001f);
            from.center += Vector3.right * 2f; to.center += Vector3.right * 2f;
            Assert.IsFalse(RunnerRules.TrySweptOverlap(player, player, from, to, out _));
        }

        [Test]
        public void EndingSlideSweepsOnlyStandingShapeAndPreservesAuthoredVisualScale()
        {
            var root = new GameObject("Movement test player");
            try
            {
                var player = root.AddComponent<RunnerPlayer>();
                player.body = root.AddComponent<CapsuleCollider>();
                player.visual = new GameObject("Visual").transform;
                player.visual.SetParent(root.transform, false);
                var scale = new Vector3(.8f, 1.1f, .9f);
                player.visual.localScale = scale;
                player.ResetPlayer();
                player.Slide();
                player.Simulate(RunnerRules.SlideSeconds + .01f);
                Assert.IsFalse(player.IsSliding);
                Assert.AreEqual(RunnerRules.StandingHeight, player.MovementStartBounds.size.y, .0001f);
                Assert.AreEqual(player.MovementStartBounds.size, player.HitBounds.size);
                Assert.AreEqual(scale, player.visual.localScale);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RapidLaneReversalRemainsContinuousAndJumpLandsAtRoadLevel()
        {
            var root = new GameObject("Movement test player");
            try
            {
                var player = CreatePlayer(root);
                player.ChangeLane(1); player.Simulate(.06f);
                float beforeReversal = player.transform.position.x;
                player.ChangeLane(-1);
                Assert.AreEqual(beforeReversal, player.transform.position.x);
                player.Simulate(.01f);
                Assert.That(player.transform.position.x, Is.InRange(0f, beforeReversal));
                player.Simulate(.5f);
                Assert.AreEqual(0f, player.transform.position.x, .0001f);
                player.Jump(); player.Simulate(.12f);
                player.Slide(); player.Simulate(.1f);
                Assert.IsTrue(player.IsGrounded);
                Assert.AreEqual(0f, player.transform.position.y);
                player.Simulate(RunnerRules.SlideSeconds + .01f);
                Assert.IsFalse(player.IsSliding);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void ItemBoundsFollowScaledRotatedColliderWithoutPhysicsSync()
        {
            var root = new GameObject("Item bounds test");
            try
            {
                var item = root.AddComponent<RunnerItem>();
                var child = new GameObject("Hitbox"); child.transform.SetParent(root.transform, false);
                child.transform.localPosition = new Vector3(.2f, .3f, .4f);
                item.hitbox = child.AddComponent<BoxCollider>();
                item.hitbox.center = new Vector3(.5f, 1f, .25f);
                item.hitbox.size = new Vector3(1f, 2f, 3f);
                root.transform.position = new Vector3(4f, 2f, 7f);
                root.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                root.transform.localScale = new Vector3(2f, 1.5f, .5f);
                Assert.Less(Vector3.Distance(child.transform.TransformPoint(item.hitbox.center), item.HitBounds.center), .0001f);
                Assert.Less(Vector3.Distance(new Vector3(1.5f, 3f, 2f), item.HitBounds.size), .0001f);
                var first = item.HitBounds;
                root.transform.position += new Vector3(-2f, 1f, 3f);
                Assert.Less(Vector3.Distance(first.center + new Vector3(-2f, 1f, 3f), item.HitBounds.center), .0001f);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void RecycledCoinPreservesOriginalVisualOrientation()
        {
            var root = new GameObject("Coin orientation test");
            try
            {
                var item = root.AddComponent<RunnerItem>(); item.kind = RunnerItemKind.Coin;
                var visual = new GameObject("Tilted coin").transform;
                visual.SetParent(root.transform, false);
                var rotation = Quaternion.Euler(90f, 20f, 10f);
                visual.localRotation = rotation;
                item.Place(0f, 2f);
                visual.localRotation = Quaternion.identity;
                item.Release(); item.Place(0f, 3f);
                Assert.Less(Quaternion.Angle(rotation, visual.localRotation), .0001f);
            }
            finally { Object.DestroyImmediate(root); }
        }

        [Test]
        public void DesktopKeyboardAndMouseDispatchOnceAndEscapePauses()
        {
            var root = new GameObject("Desktop input test");
            root.SetActive(false);
            Keyboard keyboard = null;
            Mouse mouse = null;
            RunnerInput input = null;
            var previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            try
            {
                var game = root.AddComponent<RunnerGame>();
                var player = CreatePlayer(new GameObject("Input player"));
                player.transform.SetParent(root.transform, false);
                game.player = player;
                typeof(RunnerGame).GetProperty("State").SetValue(game, RunnerGame.RunState.Running);
                input = root.AddComponent<RunnerInput>(); input.game = game; input.player = player;
                input.SendMessage("OnEnable");
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
                keyboard = InputSystem.AddDevice<Keyboard>();
                mouse = InputSystem.AddDevice<Mouse>();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D)); InputSystem.Update(); input.SendMessage("Update");
                Assert.AreEqual(2, player.Lane);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); InputSystem.Update(); input.SendMessage("Update");
                Assert.AreEqual(1, player.Lane, "AZERTY left control is available in the runtime polling path.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                Vector2 origin = new Vector2(-1000f, -1000f);
                float swipeDistance = Mathf.Max(100f, Mathf.Min(Screen.width, Screen.height) * .2f);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = origin, buttons = 1 }); InputSystem.Update(); input.SendMessage("Update");
                InputSystem.QueueStateEvent(mouse, new MouseState { position = origin + Vector2.right * swipeDistance, buttons = 1 }); InputSystem.Update(); input.SendMessage("Update");
                Assert.AreEqual(2, player.Lane);
                InputSystem.QueueStateEvent(mouse, new MouseState { position = origin - Vector2.right * swipeDistance, buttons = 1 }); InputSystem.Update(); input.SendMessage("Update");
                Assert.AreEqual(2, player.Lane, "One mouse drag must dispatch only one action.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Escape)); InputSystem.Update(); input.SendMessage("Update");
                Assert.AreEqual(RunnerGame.RunState.Paused, game.State);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q)); InputSystem.Update(); input.SendMessage("Update");
                Assert.AreEqual(2, player.Lane, "Paused keyboard input must not move the player.");
            }
            finally
            {
                if (input != null) input.SendMessage("OnDisable");
                if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
                if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
                InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
                Object.DestroyImmediate(root);
                Time.timeScale = 1f;
            }
        }

        [TestCase("Tsila")]
        [TestCase("Lucef")]
        [TestCase("Mianja")]
        [TestCase("Punky")]
        public void PlayableSlideFitsColliderAndRoadThroughoutBothLods(string name)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(HorrorContentBuilder.SkinFolder + "/" + name + ".prefab");
            Assert.IsNotNull(prefab, "Missing playable skin: " + name);
            var model = Object.Instantiate(prefab);
            try
            {
                var animator = model.GetComponent<Animator>();
                animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                var slide = animator.runtimeAnimatorController.animationClips.Single(c => c.name.EndsWith("_Slide"));
                for (int frame = 0; frame <= 60; frame++)
                {
                    slide.SampleAnimation(model, slide.length * frame / 60f);
                    foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                    {
                        var bounds = BlenderPackIntegration.SkinnedBounds(model, skin);
                        Assert.GreaterOrEqual(bounds.min.y, -.02f, name + " clips the road at frame " + frame);
                        Assert.LessOrEqual(bounds.max.y, RunnerRules.SlideHeight + .02f, name + " protrudes above the slide collider at frame " + frame);
                    }
                }
            }
            finally { Object.DestroyImmediate(model); }
        }
    }
}
