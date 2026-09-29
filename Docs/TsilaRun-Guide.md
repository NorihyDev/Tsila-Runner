# Tsila Run

A portrait, three-lane endless runner made from Unity primitives. No external art, fonts, models, or asset packs are required.

## Open and play

1. Open this project with **Unity 6000.6.3f1** and wait for compilation.
2. Choose **Tools > Tsila Run > Create or Update Mobile Prototype**.
3. If Unity asks about unsaved scenes, save them or cancel generation. The tool owns only `Assets/TsilaRun/Generated`; regenerating replaces that generated content. Put hand-edited variants elsewhere.
4. The tool opens and saves `Assets/TsilaRun/Generated/Scenes/TsilaRun.unity`. You can also open this saved scene directly.
5. Set the Game view to portrait (for example, 720x1280), enter Editor Play mode, and click **PLAY** inside the game.
6. Editor controls: left/right arrows or A/D change lanes; Space/up arrow jumps; S/down arrow slides; Escape pauses. Click RESUME to continue.

On a phone, use short swipes left/right/up/down. The threshold is 5% of the shorter screen dimension and fires before release. Only the first finger is tracked, and each gesture produces at most one action. Touches starting on buttons are excluded. Pause, resume, and restart have on-screen buttons. Returning from another app requires an explicit resume.

Coral barriers can be jumped; yellow overhead beams can be slid under; tall navy towers must be dodged. Following the coin trail gives a clear route through every row. Distance is the score; the best distance is stored locally with PlayerPrefs. Coins are counted for the current run.

## Android phone

The installed Editor currently has **no Android Build Support module**. No modules, SDKs, targets, or signing credentials were changed by this implementation.

1. When ready, use Unity Hub > Installs > 6000.6.3f1 > Add modules to install **Android Build Support**, including **Android SDK & NDK Tools** and **OpenJDK**. Use the dependencies supplied for this exact Editor version.
2. Open **File > Build Profiles**, add/select Android, and explicitly choose **Switch Platform**. Let Unity reimport the assets.
3. Run the generator again after activating a custom profile. It puts Tsila Run first in the active profile's scene list if that profile overrides global scenes; otherwise it updates the global list. Other scene entries remain present.
4. Check Player Settings: portrait orientation, Input System Package (New), a unique application identifier such as `com.yourstudio.tsilarun`, and IL2CPP/ARM64 for a typical modern Android phone. The existing minimum API is 26; verify your phone meets it. Use the Mobile quality profile. Leave custom release signing for your own release setup.
5. Enable Developer options and USB debugging on the phone, connect it, unlock it, and accept its computer authorization prompt.
6. Select the phone under Run Device, choose a Development Build for testing, leave App Bundle off for a direct APK test, and choose **Build And Run**. Save builds outside Assets (for example, Builds/Android).
7. Run the device checklist below and profile on the actual phone before making frame-rate claims.

Unity references: [Android environment setup](https://docs.unity3d.com/6000.1/Documentation/Manual/android-sdksetup.html), [Android device debugging](https://docs.unity3d.com/6000.0/Documentation/Manual/android-debugging-on-an-android-device.html).

## iPhone / iPad

The installed Editor currently has **no iOS Build Support module**. Add it using Unity Hub when you are ready. For the usual local build workflow, use a Mac with a compatible Xcode installation, the same Unity version and iOS module, and your Apple signing/provisioning setup. Activate an iOS build profile yourself, run the generator, set your own bundle identifier and signing team, export the Xcode project, then build/run it on a connected device in Xcode. An Apple Developer Program account is needed for App Store distribution. This Windows machine alone cannot finish a local Xcode build. No signing credentials were changed.

Unity reference: [iOS environment setup](https://docs.unity3d.com/6000.0/Documentation/Manual/ios-environment-setup.html).

## How the code is organized

| File | Responsibility |
| --- | --- |
| RunnerGame | Start/pause/game over, speed, distance, coins, best score |
| RunnerPlayer | Smooth lane changes, ballistic jump, slide, collider size |
| RunnerWorld / RunnerItem | Fixed object pools, row generation, recycling, swept collisions |
| RunnerRules | Shared tuning and conservative route timing |
| RunnerInput | Enhanced Touch and Editor-only keyboard controls |
| RunnerHud / SafeAreaPanel | Screen states, buttons, score display, safe-area adaptation |
| RunnerCamera | Camera following from behind with gentle lateral tracking |
| MobilePrototypeBuilder | Editor-only scene/material/prefab/build-list generation |

Movement uses a flat road at y=0 as the ground, with exact ballistic integration and a landing clamp. Trigger colliders describe hit volumes; explicit swept box tests resolve gameplay collisions without requiring Rigidbody simulation or Unity trigger callbacks. The box is conservative around the capsule, making corner contacts slightly forgiving visually in some directions and stricter in others. Sliding holds its short collider until standing space is clear.

There are nine recycled road sections and 16 prewarmed objects per item type (64 items total). Objects remain near the origin, and total distance accumulates separately in a double. No spawning/destruction happens during normal running. Speed rises from 10 to 22 metres/second. Row spacing budgets reaction, two lane changes, and the longer of jump/slide at maximum speed. A permanently clear lane advances by at most one lane per row. Coins occupy only that clear lane. Score text refreshes only when changed, at most five times per second; it has small string allocations at those updates. Lighting has one directional light, no real-time shadows, and no post-processing.

## Verification and device checklist

Automated tests live in `Assets/TsilaRun/Tests/Editor`. Run **Window > General > Test Runner > EditMode > Run All** (the integration tests enter Play mode themselves). Run them with any important scene edits saved. They generate the prototype scene and change the Editor's open scene, so use a disposable project copy if you need to preserve a particular Editor session. `PrototypeScreenshots.CaptureBatch` is an optional batch entry point that renders portrait UI snapshots into the validation project's Screenshots folder without saving its temporary camera changes.

The final execution results are recorded in `Docs/TsilaRun-Verification.md`. Synthetic touch and lifecycle callbacks are Editor tests, not proof of device behavior.

On an actual device, check:

- Swipe each direction with short and long gestures; hold/drag after a recognized swipe and confirm there is no second action. Add a second finger, cancel a touch, and swipe again.
- Start a drag on Pause and confirm no runner action is dispatched. Tap every screen's buttons, including immediately after returning from the background.
- Jump coral barriers, slide yellow beams, dodge navy towers, collect coins once, and deliberately collide with each obstacle type.
- Restart from game over and pause. Check speed, lane, height, coins, distance, world layout, and time scale all reset.
- Background/foreground, lock/unlock, and interrupt the app. Gameplay must remain paused until RESUME is tapped.
- Check 16:9, 19.5:9, and 20:9 phones, including a notch/home indicator. Confirm readable text, safe button placement, and comfortable targets.
- Run long enough to reach maximum speed; inspect the Unity Profiler for CPU/GPU cost, allocations, and stable object counts. No measured frame rate or device build is claimed without those tests.

This is a prototype: no audio, character animation system, store, missions, or cloud saves are included. Portrait tablets use the same adaptive layout; physical phone tuning and store deployment remain device/release tasks.
