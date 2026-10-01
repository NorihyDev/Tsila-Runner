# Blender pack integration and verification

The supplied Blender exports are integrated into the existing **Tsila Run** scene. The character models are reference-based approximations with textured faces; they are not exact realistic scans or likenesses.

## What changed

- `Assets/TsilaRun/BlenderPack/Source` contains the supplied FBX models and nine textures.
- `Assets/TsilaRun/BlenderPack/Generated` contains URP Lit materials, Generic animation controllers, Idle/Run/Jump/Slide clips, and prefabs. Tsila, Officer_Lucef, and RunningPerson each use a 20-bone skeleton with a shared-skeleton lower-detail LOD.
- The existing player, officer, moving-person obstacle, road, coin, barrier, and overhead gameplay prefabs now use those visuals. Tree, building, and floating-platform visuals were added to recycled road sections. Gameplay scripts, colliders, pools, HUD, and menus remain connected.
- `Assets/TsilaRun/Generated/Scenes/TsilaRun.unity` was patched in place. The integration tool saved a copy of the earlier scene and prefabs under `Logs/BlenderPackBackup-*`.
- The latest scene also includes three power-up prefabs, local coin missions, and four outfit colorways in the shop. The Blender character's body color changes with the selected outfit while face and hair textures stay intact.

## Run in Unity

1. Open the project in Unity **6000.6.3f1** and let the asset import finish.
2. Open `Assets/TsilaRun/Generated/Scenes/TsilaRun.unity` and enter Play mode.
3. Select **PLAY**, complete or skip the pursuit introduction, then use A/D or Left/Right to change lanes, Space/Up to jump, and S/Down to slide. Use the on-screen buttons for pause, resume, restart, shop, and menu.
4. To reapply changed FBX exports, leave Play mode and select **Tools > Tsila Run > Reconcile Blender Pack with Latest Gameplay**. Save or discard any unsaved scene edits when Unity asks. The tool updates the existing scene; do not regenerate the prototype to apply this pack.

## Editor verification

Unity compiled the reconciled integration and ran it successfully. All **24/24 selected Editor tests** passed, including Blender assets, Generic rigs, animation clips, LODs, scene references, synthetic touch, lane changes, jump/slide, collision and coins, chasing, bounded pooling, shop, menu and restart, background/resume state, and safe-area UI. The scene-generation test was excluded because it rewrites the generated scene. Results are in `Logs/ReconciledTests-Final.xml`.

The Editor rendered portrait captures at **720 × 1280**, **1170 × 2532**, **1080 × 2400**, and **2160 × 3840**. The text-fit check passed at each size. The full capture set is in the local `Screenshots` folder; the following selected captures are committed and were visually reviewed:

- [Menu](Verification/BlenderPack-Menu.png)
- [Gameplay](Verification/BlenderPack-Run.png)
- [Pursuit introduction](Verification/BlenderPack-Intro.png)
- [Shop](Verification/SkinShop.png)

These are Editor checks. A physical Android or iOS device was not used, so touch latency, app interruption on a device, thermals, and frame rate are not measured. The rendered 4K layout does not imply the game forces 4K resolution on a phone.

## Android and iOS

Android Build Support, SDK, NDK, and OpenJDK are installed with this Editor. The project uses an Android development profile, IL2CPP, and ARM64. A fresh reconciled development build succeeded in Unity batch mode using `BlenderPackAndroidBuild.BuildDevelopment`. Its output is `Builds/TsilaRun-Development.apk` (**82,723,498 bytes**, about 83 MB). The build log is `Logs/ReconciledAndroidBuild.log`; the APK is ignored by Git. `adb devices` showed no connected phone, so installation and on-device behavior were not tested.

For a phone test, enable USB debugging, connect the phone, and install the APK with `adb install -r Builds/TsilaRun-Development.apk`, or open **File > Build Profiles**, select Android, and use **Build And Run** with the development profile. Launch Tsila Run on the phone and test swipes, menu flow, pause after backgrounding, and sustained performance. Publishing requires an application identifier and signing configuration controlled by the project owner.

An iOS build additionally requires iOS Build Support (not installed here), a Mac with Xcode, and Apple signing and provisioning. The Xcode build and on-device test cannot be completed from this Windows machine.

## Remaining limits

The supplied character clips are prototypes and the source characters are approximations. All three characters have lower-detail LODs, but their high-detail meshes are relatively dense; profile GPU and memory use on a mid-range phone before release. The three additional shop outfits are body colorways on the supplied model, not separate character meshes. No device test or measured frame-rate claim has been made.
