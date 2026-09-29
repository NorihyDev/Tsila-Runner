# Tsila Run implementation notes

## Project inspection

- Unity: 6000.6.3f1 (45d8eee7de74).
- Universal Render Pipeline 17.6.0, selected through Mobile/PC quality profiles.
- Input System 1.20.0; Active Input Handling is Input System only.
- uGUI 2.6.0 and Unity Test Framework 1.8.0 are already installed.
- Existing scene: `Assets/Scenes/SampleScene.unity`; existing scripts are template readme scripts.
- Existing `InputSystem_Actions.inputactions` is preserved. Runner gestures use Enhanced Touch, and UI uses InputSystemUIInputModule.
- No project font files were found. The generator uses Unity's built-in `LegacyRuntime.ttf`.
- The installed editor has Windows and WebGL support, but no Android/iOS playback modules.

## Design and commit boundaries

1. Record inspection and implementation approach.
2. Add bounded runner simulation, recycling, collision handling, scoring, and fair patterns.
3. Add mobile input, safe-area UI, and camera.
4. Add the repeatable Editor generator and generated assets.
5. Add focused verification, fix any failures, and document build/test instructions.

The player remains at z=0. A fixed pool of road sections and obstacle/coin objects moves toward the camera. Distances accumulate separately using a double. No assets are downloaded. Each pattern has a clear lane; the clear lane changes at most one lane between rows. Row spacing uses the maximum run speed and the time required to react, change lanes, jump, and slide, so accelerating cannot invalidate a route already generated.

Only Tsila Run files and intentional generated build-list changes are committed. Pre-existing changes to the sample project, including settings, the template icon, and deleted HubForceResolve files, belong to the user.
