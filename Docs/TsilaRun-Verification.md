# Tsila Run verification

Executed with Unity 6000.6.3f1 on Windows on 2026-09-29, in an isolated copy under `Temp/TsilaRunValidation` so the existing Editor session was not replaced.

**Final result: 11 tests passed, 0 failed, 0 skipped.** Unity exited with code 0. Runtime, generator, and tests compiled without C# errors. Results are preserved in [EditorTests.xml](Verification/EditorTests.xml); a representative gameplay render is [Gameplay.png](Verification/Gameplay.png).

## Scope

- Compiled the runtime, Editor generator, and test assemblies using the project's installed packages.
- Generated the scene, materials, primitive prefabs, and persistent UI input asset. Regenerated twice and checked stable scene GUID, root count, one build-list entry, portrait settings, and unchanged SampleScene contents.
- Exercised movement, lane limits, jumping/landing, sliding/standing clearance, swept collisions, coin collection, one-shot game over, restart, focus/background pause, and explicit resume in Editor Play mode.
- Simulated 110 km of world travel at maximum speed. Pool counts remained bounded, active items stayed near the origin, and sampled coins did not intersect obstacles.
- Checked route timing and 10,000 safe-lane transitions. Checked safe-area coordinates and proportional swipe thresholds at five screen sizes.
- Rendered start, gameplay, pause, and game-over screens at 720x1280, 1170x2532, and 1080x2400 (12 images), including simulated top/bottom safe-area insets. Checked text heights against their rectangles and visually inspected representative images.
- Synthetic touch tests use Unity Input System events and explicitly drive its update and UI processing. They cover swipe dispatch before release, one action per gesture, canceled-touch recovery, UI-origin rejection, and tapping Pause. Resume is verified through the wired button event. A synthetic tap on the newly opened Resume panel returned no UI raycast hits in the hidden batch Editor; that physical touch path remains on the device checklist. These checks are not physical touchscreen tests.

## Fixes found during verification

- Saved UI actions and their references as an imported asset so buttons retain their wiring after scene reload.
- Used DestroyImmediate for temporary input assets in the Editor generator.
- Made pool initialization recover from empty/stale managed arrays with the project's disabled scene/domain reload settings.
- Replaced deprecated object lookup calls with FindAnyObjectByType.
- Added a dark HUD background for score contrast against the sky.

## Reproduce

Open Test Runner's EditMode tab and run `TsilaRun.Tests`. Save scene edits first: the tests generate and open the prototype, and integration tests enter Play mode. The suite temporarily enables normal scene/domain reload for deterministic test-runner transitions and restores the previous setting afterward. Graphics are needed for the portrait rendering test; headless runs with `-nographics` skip that test.

Logs are in the ignored `Logs` folder. Portrait renders are in the ignored `Temp/TsilaRunValidation/Screenshots` folder. Final test results are copied to `Docs/Verification/EditorTests.xml`.

## Not performed

No Android/iOS build, phone connection, physical touch test, actual OS background/resume test, Xcode/signing step, or device performance measurement was performed. Android and iOS support modules are absent from the installed Editor. No SDKs were installed, build targets switched, or signing credentials changed. The device checklist and build instructions are in [TsilaRun-Guide.md](TsilaRun-Guide.md).
