# TSILA RUN

**A stylized mobile endless runner by Method.**

Run through three lanes, collect coins, evade the patrol, and keep moving as the island gives way to mountain passes and underground tunnels. Built with Unity, Tsila Run combines animated 3D characters with an English interface designed for portrait screens.

**Status:** Playable prototype with Meshy models and the existing Blender fallback pack integrated · **Primary platforms:** Android and iOS · **Editor:** Unity 6000.6.3f1

<p align="center">
  <img src="Docs/Verification/Presentation-Menu.png" alt="Tsila Run menu with Rajdhani typography and neon platform" width="280" />
  &nbsp;
  <img src="Docs/Verification/Presentation-Run.png" alt="Tsila Run gameplay with sparse tropical scenery and sand ground" width="280" />
</p>

## Features

The supplied Meshy GLBs now replace the player, chaser, road, curbs, tall obstacle, overhead obstacle, coins, magnet and island scenery. Missing models retain the existing artwork. See [Meshy integration](Docs/Meshy-Integration.md) for the asset mapping, preparation workflow and limits.

The mobile presentation uses Rajdhani title and button typography, a neon menu stage, a first-run controls tutorial, and a compact running HUD. Scenery alternates between spaced groups over sand ground, disconnected model kits stay out of the playable lanes, and a full-size tucked roll replaces the prone slide. See [presentation changes and validation](Docs/Presentation-Polish.md).

- **Three-lane running:** automatic forward movement, smooth lane changes, jumping, and sliding. Swipe down while airborne to drop into a slide, or swipe up during a slide to cancel it and jump when clear.
- **Varied obstacles:** low barriers, overhead beams, tall obstacles, and other runners. Later waves can occupy all three lanes, with a jumpable or slidable route; tall towers must be dodged.
- **Animated characters:** idle, run, jump, and full-size roll states, plus a skippable police chase introduction.
- **Changing environments:** island, mountain pass, and underground scenery cycle every 288 metres.
- **Local progression:** distance score, best distance, and a persistent coin wallet.
- **Method presentation:** animated character showcase, branded artwork, rounded controls, and a midnight-blue, white, and mint interface.
- **Complete navigation:** start, pause, resume, restart, results, and return to the main menu.
- **Mobile controls:** responsive swipes, safe-area-aware layouts, and explicit resume after a running game loses focus.

The shop has four locally saved outfit colorways: Island Teal (equipped by default), Sunset Coral, Golden Trail, and Midnight. Collected coins can unlock the latter three. Coin magnet, shield, and speed boost pickups also appear during runs; there are no real-money purchases.

## Requirements

| Component | Project version |
| --- | --- |
| Unity Editor | 6000.6.3f1 |
| Universal Render Pipeline | 17.6.0 |
| Input System | 1.20.0 |
| Unity UI | 2.6.0 |
| Unity Test Framework | 1.8.0 |

Use Unity Hub to install the matching Editor version. Package dependencies are declared in [`Packages/manifest.json`](Packages/manifest.json). Git LFS is used for image assets.

## Quick start

Clone the repository with Git LFS available:

```bash
git lfs install
git clone https://github.com/NorihyDev/Tsila-Runner.git
cd Tsila-Runner
git lfs pull
```

1. Add the cloned project in Unity Hub and open it with **Unity 6000.6.3f1**.
2. Wait for Unity to restore packages and finish importing assets.
3. Open `Assets/TsilaRun/Generated/Scenes/TsilaRun.unity`.
4. Set the Game view to **720 × 1280** or **1080 × 1920** in portrait orientation.
5. Enter Unity Play mode, then select **PLAY** in the game.

If Unity was already in Play mode while scripts changed, stop Play mode, wait for recompilation, and start it again to see the latest fixes.

The generated scene is included in the repository. Regeneration is only needed when rebuilding the prototype or applying generator changes.

## Controls

| Action | Mobile | Editor keyboard |
| --- | --- | --- |
| Move left | Swipe left | A or Left Arrow |
| Move right | Swipe right | D or Right Arrow |
| Jump | Swipe up | Space or Up Arrow |
| Slide | Swipe down | S or Down Arrow |
| Pause | On-screen pause button | Escape during a run, or the pause button |
| Resume | RESUME | Click RESUME |
| Return to the menu | MENU / MAIN MENU | Click MENU / MAIN MENU |

Swipes trigger once per gesture after crossing 5% of the screen's shorter dimension. Gestures starting on interactive UI do not control the runner. Keyboard movement is intended for Editor testing.

Returning to the menu ends the current run, saves coins and the best distance, and resets the running state. Local progress uses Unity PlayerPrefs; clearing application data removes that progress.

## Editor tools

All project tools are available under **Tools → Tsila Run**.

| Tool | Purpose |
| --- | --- |
| **Create or Update Mobile Prototype** | Import the Method pack, rebuild the gameplay prefabs and scene, connect references, and register the scene in the appropriate build scene list. |
| **Refresh Modern UI** | Rebuild the English interface while preserving the existing artwork and material edits. |
| **Polish Menu and Gameplay** | Reapply the sports typography, menu stage, sparse scenery, normalized models, and character motions. |
| **Import Asset Pack** | Rebuild the Method art assets from their supplied mesh and animation data. |
| **Apply Blender Character Pack** | Import the supplied FBXs and textures, generate URP materials and LOD prefabs, then patch the existing gameplay prefabs and scene. |
| **Reconcile Blender Pack with Latest Gameplay** | Patch the Blender visuals into the latest gameplay scene, restore power-up references, and refresh the mobile UI without regenerating the world. |

Leave Play mode before using these tools. Scene-changing menu tools prompt you to save unsaved scene changes.

The Blender integration backs up the existing scene and gameplay prefabs under `Logs/BlenderPackBackup-*` before patching them. Use **Apply Blender Character Pack** to reapply imported FBX changes; do not use full prototype regeneration for this task. The source models are reference-based approximations, not exact scans or likenesses.

Full regeneration rewrites content inside `Assets/TsilaRun/Generated` and `Assets/TsilaRunArt/Generated`. Keep custom variants outside those folders, or update the relevant generator to make changes reproducible. Commit Unity `.meta` files alongside their assets to preserve references.

## Project structure

```text
Assets/
├── Editor/TsilaRun/          Scene, UI, and visual generation tools
├── TsilaRun/
│   ├── Runtime/             Gameplay, input, camera, UI, and progression
│   ├── Generated/           Playable scene, gameplay prefabs, and UI assets
│   └── Tests/Editor/        Automated checks and Play mode integration tests
└── TsilaRunArt/
    ├── Data/                Supplied mesh, skeleton, and animation data
    ├── Editor/              Method asset importer
    ├── Generated/           Imported meshes, materials, rigs, and animation clips
    └── Textures/            Method atlas, emission texture, and logo
Docs/                        Setup guides, asset specifications, and verification notes
```

Runtime scripts contain no UnityEditor dependencies. The main responsibilities are separated into small components:

| Component | Responsibility |
| --- | --- |
| `RunnerGame` | Run states, speed, score, saved progression, and menu navigation |
| `RunnerPlayer` | Lane movement, gravity, jumping, sliding, and collider restoration |
| `RunnerWorld` / `RunnerItem` | Object pools, obstacle patterns, recycling, and swept collision checks |
| `RunnerInput` | Mobile gestures and Editor keyboard controls |
| `RunnerAvatar` / `RunnerChase` | Character animation and the introductory pursuit |
| `RunnerHud` / `RunnerShop` | Gameplay displays, screen navigation, and the four-outfit coin shop |
| `RunnerPresentation` | Menu character presentation and backdrop |
| `UiButtonMotion` / `UiPanelMotion` | Button feedback and panel transitions that also work while paused |

`Assets/TsilaRun/BlenderPack/Source` contains the supplied FBXs and textures. `Assets/TsilaRun/BlenderPack/Generated` contains the imported URP materials, animation controllers, clips, and visual prefabs. The playable scene remains at `Assets/TsilaRun/Generated/Scenes/TsilaRun.unity`.

## Android development build

1. Install **Android Build Support**, **Android SDK & NDK Tools**, and **OpenJDK** for this Editor version through Unity Hub if they are missing.
2. Open **File → Build Profiles**, select or create an Android profile, and switch to it.
3. Confirm `TsilaRun.unity` is enabled and first in the active scene list. The prototype generator can register it for the active profile.
4. Review Player Settings: portrait orientation, the Input System, an application identifier you control, and the scripting backend/architectures required for your target devices.
5. Enable USB debugging on the phone, connect it, and accept the computer authorization prompt.
6. Select the device and use **Build And Run** with a Development Build. Save output outside `Assets`, for example in `Builds/Android`.

Test touch gestures, menu navigation, interruptions, background/resume behavior, and performance on the actual device before distribution.

## iOS development build

The standard local workflow requires **iOS Build Support**, a **Mac with a compatible Xcode installation**, and suitable Apple signing and provisioning.

Activate an iOS build profile, configure your bundle identifier and signing setup, export the Xcode project from Unity, and build/run it on a connected device using Xcode. A local Xcode build cannot be completed on Windows alone. Store distribution requires the appropriate Apple developer enrollment and release setup.

## Performance approach

The prototype uses nine recycled road sections and bounded pools of obstacles and coins. The world moves around a player kept near the origin, while distance accumulates separately, avoiding continuously growing world coordinates.

Materials are reused, collision volumes are simple, and the game avoids heavy post-processing and real-time shadows. The menu reuses the gameplay character and camera rather than adding a separate character-rendering camera. UI transitions use unscaled time, so they remain responsive during pause.

**60 FPS is a target, not a measured guarantee.** The interface scales with the display, but a 4K phone rendering mode is not forced, and the supplied textures are not native 4K assets. Device profiling is required to establish actual frame rate, memory use, and thermal behavior.

## Verification and limitations

The presentation changes passed all **34 selected Editor tests** in Unity 6000.6.3f1, covering prepared art assets, full-size roll poses through the Animator on both characters and LODs, scenery and tunnel clearance, sparse placement, simulated touch input, fast-fall and slide cancellation, obstacle waves, swept collisions, coins, power-ups, pooling, the first-run tutorial, shop and menu flow, and restart. The scene-generation test was excluded because it rewrites generated content; portrait captures run separately. See the [test report](Docs/Verification/Presentation-EditorTests.xml).

Editor screenshots were rendered at **720 × 1280**, **1170 × 2532**, **1080 × 2400**, and **2160 × 3840**. The menu, run, and pursuit captures in `Docs/Verification` were visually reviewed. These are Editor renderings; physical-device gestures, background/resume, thermals, and frame rate remain untested. Android build status is recorded in [Blender pack verification](Docs/BlenderPack-Integration.md).

Run the available checks through **Window → General → Test Runner → EditMode**. Save your work first: gameplay tests open the prototype scene and enter Play mode. Exclude the `SceneGeneration` category when validating this integrated scene.

Current scope excludes real-money purchases, cloud saves, music, and gameplay sound effects. Buttons have a brief tap sound. Missions and coin-purchased outfit colorways are local prototype systems. Mountain and tunnel scenery share a flat gameplay surface; terrain slopes are not simulated.

See the [verification record](Docs/TsilaRun-Verification.md) for the distinction between completed checks and outstanding device validation.

## Documentation

- [Setup and mobile build guide](Docs/TsilaRun-Guide.md)
- [Method edition guide](Docs/TsilaRun-Method.md) — French guide with an English UI update section
- [Asset dimensions and handoff brief](Docs/TsilaRun-Asset-Brief.md)
- [Verification history](Docs/TsilaRun-Verification.md)
- [Blender pack integration and verification](Docs/BlenderPack-Integration.md)
- [Mobile presentation, scenery, and animation verification](Docs/Presentation-Polish.md)

## Credits

**Copyright by Method.**

Rajdhani is distributed under the SIL Open Font License included in `Assets/TsilaRun/Art/UI/Fonts/OFL.txt`.

Tsila Run uses the Method artwork, logo, and character assets supplied with this project. No open-source license grant is stated in this README; confirm applicable permissions before redistributing the project or its assets.
