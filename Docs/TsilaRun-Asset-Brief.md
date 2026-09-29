# Tsila Run asset handoff

Copy this document into your art/3D creation conversation. Dimensions below come from this project's primitive builders and gameplay rules. Texture sizes and polygon budgets are proposed production targets, not measurements or existing assets.

**Request to the asset creator**

Create original, colorful, stylized 3D assets for Tsila Run, a portrait mobile endless runner for Android and iOS. Give the characters proper clothing, faces, shoes and skin textures. Keep strong silhouettes and readable obstacles. Use original fictional characters, not real people's likenesses or copies of another game's characters. Match the dimensions below and preserve gameplay clearance.

Deliver actual mesh files with UVs and textures when your tools support 3D output. A rendered PNG is a concept/reference image, not an importable 3D character or a UV skin texture. If you can only create images, deliver clearly labeled modeling reference sheets and texture concepts; do not describe them as finished rigged models.

**Coordinates and files**

- Destination project: Unity 6000.6.3f1, Universal Render Pipeline 17.6.0.
- All physical sizes use metres: 1 Unity unit = 1 metre. Tables use width X × height Y × depth Z.
- In Unity, +Y is up, +Z is the running/character-facing direction, and +X is right.
- Final Unity prefab roots must have scale (1,1,1) and rotation (0,0,0). Gameplay code assumes this.
- Character and obstacle roots sit at ground level between their feet/base. Render meshes can be offset beneath those roots. Coin geometry is centered at Y=0.9 inside its gameplay root.
- Road-section root is at the center of its length, at driving surface Y=0; section ends are Z=-12 and Z=+12.
- Prefer FBX meshes, PNG textures, and the editable source file such as .blend if available. Provide a manifest listing each file, dimensions, material slots, pivot and animation clips.
- Keep imported artwork in Assets/TsilaRun/Art, outside Assets/TsilaRun/Generated. The generator replaces its own generated assets; integrating custom art requires updating its visual references or making separate prefab variants.

**Main assets: use these exact existing prefab names**

| Name / suggested mesh file | Visual dimensions / modeling target | Gameplay constraint to preserve |
| --- | --- | --- |
| Tsila / Tsila.fbx | Target standing height 1.8; current primitive head reaches about 1.82. Neutral body including arms about 0.9 wide, 0.65 deep. | Standing collision volume 0.64 × 1.8 × 0.64, center (0,0.9,0); capsule radius 0.32. Arms can extend beyond the core hit volume. Sliding height 0.7, center Y=0.35. |
| Officer / Officer.fbx | Same body scale as Tsila; allow about 0.75 depth including cap brim/backpack. Navy uniform and recognizable cap/badge. | Decorative pursuing character; no gameplay hit collider. |
| RunningPerson / RunningPerson.fbx | Same character scale; target envelope within 0.95 × 1.88 × 0.8. Distinct clothing from Tsila/officer. | Box 0.95 × 1.88 × 0.8, center (0,0.94,0). A moving obstacle that must be dodged. |
| Barrier / Barrier.fbx | Main block 1.7 × 0.85 × 0.9. Existing top trim makes total visual envelope 1.75 × 0.8675 × 0.95. | Box 1.7 × 0.85 × 0.9, center (0,0.425,0). Must remain jumpable. |
| Overhead / Overhead.fbx | Total frame 2.24 × 2.8 × 0.9. Beam 1.9 × 1.8 × 0.9, center Y=1.9; posts at X=±1.05. | Beam box 1.9 × 1.8 × 0.9, center (0,1.9,0). Bottom of beam Y=1.0: keep the opening below clear for sliding. |
| Tower / Tower.fbx | Main body 1.8 × 3.6 × 1.1; existing front warning stripe extends total visual depth to 1.125. | Box 1.8 × 3.6 × 1.1, center (0,1.8,0). Must remain too tall to jump. |
| Coin / Coin.fbx | Disc diameter 0.6, thickness 0.2: 0.6 × 0.6 × 0.2 after facing along Z. Gold face visible to the following camera. | Collection box 0.65 × 0.65 × 0.3, center (0,0.9,0). |
| RoadSection / RoadSection.fbx | Road slab 8 × 0.36 × 24, center (0,-0.18,0). Surface Y=0. | Repeat seamlessly every 24 metres. Three lane centers X=-2.4, 0, +2.4. Flat running surface. |

Collision sizes are separate from visual sizes. Keep the existing gameplay components and simple colliders when replacing render meshes. Do not add colliders to clothing, fingers, scenery or decorative trim.

**Environment pieces**

Names in the first column are existing child-object names; suggested filenames remove spaces. Positions are relative to a road-section root. ± means one copy on either side. Cube dimensions are exact before rotation; rotated mountain dimensions are not their final world-space bounding boxes.

| Existing name / suggested file | Size X × Y × Z | Center / placement |
| --- | --- | --- |
| Curb / Curb.fbx | 0.2 × 0.16 × 24 | (±4.1, 0.03, 0) |
| Lane Stripe / LaneStripe.fbx | 0.055 × 0.012 × 24 | (±1.2, 0.008, 0) |
| Island / IslandGround.fbx | 30 × 0.5 × 24 | (0, -0.45, 0) |
| Trunk / TreeTrunk.fbx | 0.45 × 1.6 × 0.45 | (±7, 0.8, 4) |
| Tree / TreeCanopy.fbx | 2.7 × 3.1 × 2.7 | (±7, 2.4, 4); whole tree reaches Y=3.95 |
| Block House / House.fbx | 3.5 × 4.2 × 5 | (±11, 2.1, -5) |
| Roof / HouseRoof.fbx | 3.9 × 0.25 × 5.4 | (±11, 4.3, -5); house plus roof reaches Y=4.425 |
| Cliff / Cliff.fbx | 3 × 10 × 24 | (±5.5, -5, 0) |
| Safety Rail / SafetyRail.fbx | 0.15 × 0.25 × 24 | (±4.25, 0.65, 0) |
| Angular Mountain / Mountain.fbx | 11 × 15 × 13 before rotation | (±14, 2, 0); Unity Euler (0,15,±35) degrees |
| Snow Cap / SnowCap.fbx | 5 × 5 × 6 before rotation | (±14, 8, 0); same rotation as mountain |
| Bridge Deck / BridgeDeck.fbx | 8.5 × 0.5 × 24 | (0, -0.5, 0) |
| Tunnel Wall / TunnelWall.fbx | 1 × 9 × 24 | (±5, 4.5, 0) |
| Light Strip / TunnelLightStrip.fbx | 0.1 × 0.16 × 23 | (±4.45, 3, 0) |
| Stone Rib / TunnelRib.fbx | 0.3 × 9 × 0.8 | (±4.5, 4.5, 0) |
| Tunnel Ceiling / TunnelCeiling.fbx | 11 × 0.6 × 24 | (0, 9, 0); underside Y=8.7 |
| Ceiling Light / TunnelCeilingLight.fbx | 1.5 × 0.08 × 5 | (0, 8.6, 0) |
| Post (Overhead child) / OverheadPost.fbx | 0.14 × 2.8 × 0.7 | (±1.05, 1.4, 0), relative to obstacle root |

Deliver three coherent scenery sets: Island, Mountain Pass, Underground. Each uses the same 24-metre road length and flat ground. Do not introduce slopes, steps or branches without a gameplay change. Keep decorative meshes outside the running lanes. Tunnel walls have inner faces at X=±4.5; ribs protrude to X=±4.35. Keep the follow camera clear at approximately (0,6.5,-10) relative to the player, with slight lateral movement. Avoid discontinuities at module seams.

**Character parts and rig compatibility**

The current game uses separate primitive limbs and procedural rotation, not an Animator or a skinned Humanoid rig. These are the existing neutral-pose part sizes; they are a proportions reference, not a requirement to keep characters block-shaped. Capsule/cylinder heights here are actual dimensions, not their Unity scale fields.

| Existing part name | Size X × Y × Z |
| --- | --- |
| Torso | 0.58 × 0.70 × 0.38 |
| Head | 0.46 × 0.46 × 0.46 |
| Hair or Cap | 0.48 × 0.20 × 0.46 |
| Eye, each | 0.05 × 0.05 × 0.05 |
| Chest Stripe | 0.40 × 0.13 × 0.035 |
| Badge (officer) | 0.10 × 0.13 × 0.035 |
| Backpack | 0.35 × 0.40 × 0.20 |
| Cap Brim (officer) | 0.52 × 0.045 × 0.30 |
| Sleeve, each | 0.18 × 0.36 × 0.18 |
| Forearm, each | 0.14 × 0.30 × 0.14 |
| Hand, each | 0.17 × 0.17 × 0.17 |
| Thigh, each | 0.23 × 0.42 × 0.23 |
| Shin, each | 0.19 × 0.36 × 0.19 |
| Shoe, each | 0.24 × 0.16 × 0.38 |

For the easiest integration with the current procedural animation, supply separate rigid meshes with these pivots and neutral rotations:

| RunnerAvatar field | Existing transform name | Pivot |
| --- | --- | --- |
| leftArm | Left Shoulder | (-0.35,1.35,0) relative to rig root |
| rightArm | Right Shoulder | (+0.35,1.35,0) relative to rig root |
| leftLeg | Left Hip | (-0.17,0.8,0) relative to rig root |
| rightLeg | Right Hip | (+0.17,0.8,0) relative to rig root |
| leftKnee | Knee beneath Left Hip | (0,-0.35,0) relative to Left Hip |
| rightKnee | Knee beneath Right Hip | (0,-0.35,0) relative to Right Hip |

Arms hang downward in the neutral pose; knees are children of hips, shins/shoes children of knees. Local X rotation swings limbs forwards/backwards. Left and right names above follow this project's coordinate convention. Wire these transforms to RunnerAvatar after importing.

If instead delivering a fully skinned character, supply one consistent skeleton for Tsila, Officer and RunningPerson, and the four outfits. Suggested clip names: Idle, Run, Jump, Slide, Stumble, Catch. Run/Idle loop, all clips stay in place, with no forward root motion. Gameplay jump lasts about 0.75 seconds and slide 1.05 seconds. A new Animator integration is required: the current controller scales the visual to 0.7/1.8 height during slides, so that scaling must be replaced when using a proper slide clip. Do not run both animation drivers on the same bones.

**Outfit skins and material names**

Skins are currently cosmetic outfit colors on the same character, not different body sizes or skin tones. Keep face/body materials separate from the outfit material. The current shop replaces the entire shared material on suitRenderers; textured outfits should retain one replaceable outfit slot, or the shop integration must be expanded for multiple slots.

| Existing skin name | Suggested material | Current main color | In-game price |
| --- | --- | --- | --- |
| Island Teal | M_Tsila_IslandTeal | #23CABD | Free |
| Sunset Coral | M_Tsila_SunsetCoral | #FF6B61 | 40 coins |
| Golden Trail | M_Tsila_GoldenTrail | #FFD44A | 100 coins |
| Midnight | M_Tsila_Midnight | #142B47 | 150 coins |

Supporting palette: Road #25465B, Sand/current primitive skin #EACB91, Cream #FFF5D9, Green #70B879. New fictional character complexions can be designed independently of outfit color.

Suggested texture files: Tsila_IslandTeal_BaseColor.png, Tsila_SunsetCoral_BaseColor.png, Tsila_GoldenTrail_BaseColor.png, Tsila_Midnight_BaseColor.png, Officer_BaseColor.png, RunningPerson_BaseColor.png. All outfit variants must fit the same supplied UV layout. Do not bake shadows or background scenery into skin textures.

**Proposed mobile delivery targets**

- Characters: about 3,000–6,000 triangles each, ideally 1–2 materials. Share meshes/skeletons and textures where useful.
- Obstacles/coins: about 100–800 triangles each. Scenery: low-detail modular meshes, shared atlas, simple opaque materials. These are starting art budgets; actual phone profiling decides the final limits.
- Character/outfit base-color textures: 1024×1024 PNG. Environment atlas: 2048×2048 PNG. Small standalone props: 256×256 or 512×512 PNG. Supply optional normal/metallic maps separately and label their conventions; use inexpensive URP-compatible materials when integrating.
- Modeling reference sheets: front, side and back views in the same neutral pose and scale, each 1024×2048, plain background, no perspective distortion. These are reference pictures, separate from UV textures.
- Optional shop thumbnails: 512×512 transparent PNG, full character with consistent framing. Optional coin/pause icons: 256×256 transparent PNG. The current UI uses text and colored buttons, so image slots would need to be added.
- No baked text in UI artwork; labels remain editable in Unity. No required real-time shadows, extra lights, cloth simulation, hair simulation or heavy shader effects.

Start with Tsila, its four outfit textures, Officer and RunningPerson. Then deliver the three obstacle types, coin, road, and scenery pieces. Include a simple preview of each asset next to its filename and physical dimensions, and clearly distinguish finished 3D deliverables from concept images.

Source of measured constraints: Assets/Editor/TsilaRun/MobilePrototypeBuilder.cs, Assets/Editor/TsilaRun/PrimitiveAvatarBuilder.cs, Assets/TsilaRun/Runtime/RunnerRules.cs, RunnerPlayer.cs, RunnerAvatar.cs and RunnerProgress.cs.
