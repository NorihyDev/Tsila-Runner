# Characters, missions and rocky sand

Tsila remains the starter. Horror Girl replaces the pursuing officer; Horror Skunx replaces the moving person obstacle. The GLBs remain intact in `Assets/TsilaRun/Art/AI`. Prepared FBXs retain the supplied skeletons and motion, with 1,024-pixel mobile atlases and two mesh LODs (roughly 12,000–13,000 and 4,800 triangles). Grounded, in-place animation curves keep both LODs against the road.

The shop keeps the original four entry indices and adds Lucef (the previous officer) at **1,000 coins**, and Mianja (the previous running obstacle) at **5,000 coins**. These entries replace the actual model and reconnect its animation and player rig; collision and movement stay on the player root. Bought characters can be equipped again without another payment. Existing balances, outfit ownership and selections use the same local save.

Eight missions rotate in this order, paying 40 coins each:

1. Collect 30 coins.
2. Avoid 10 obstacles.
3. Run 500 metres.
4. Make 12 jumps.
5. Do 8 rolls.
6. Collect 3 power-ups.
7. Run 200 metres in the rocky biome.
8. Run 150 metres in the tunnel.

Progress accumulates between runs, is saved on purchases, mission completion and normal run/menu/pause transitions, and appears on the main menu and pause screen. Running retains the compact HUD and brief completion banner. Repeated jump inputs before the first physics tick count once.

The rocky biome now has a continuous sand foundation. Decorative bounds are aligned to its surface, with a 5 mm overlap to avoid visible gaps. Unsupported floating rail models were removed. A backing surface beneath the road closes small gaps between the supplied asphalt modules. Pickups intentionally retain their reachable height and coin animation; ground decoration stays outside the playable road envelope. Scene characters are detached from prefab overrides so replacing a prefab cannot add a second visible actor at the origin.

To regenerate the new models, run Blender in a separate background process with `--background --python Tools/prepare_horror.py`, then use **Tools > Tsila Run > Apply Horror Characters and Skins** in Unity. This also rebuilds the six shop entries and mission labels. **Polish Menu and Gameplay** preserves the selected cast and rebuilds Mianja's full-size roll along with the other playable motions.

## Validation

Unity **6000.6.3f1** passed **44 selected Editor tests**, including purchases and old-save compatibility, all eight mission types and biome-specific distance, real character switching, one scene actor per role, both horror LODs staying grounded, full-size rolls, collision sweeps, coins, pooling, simulated touch and restart. Scene regeneration was excluded because it rewrites content; portrait rendering was checked independently.

[Final test report](Verification/Horror-FinalTests.xml). [Capture checks](Verification/Horror-Captures.txt): 37 UI/gameplay captures across four portrait sizes, plus four character reviews, with no clipped legacy or TMP text.

<p>
  <img src="Verification/Horror-Menu.png" alt="Starter Tsila and the compact mission line" width="220" />
  <img src="Verification/Horror-Shop.png" alt="Six shop entries including Lucef and Mianja" width="220" />
  <img src="Verification/Horror-Mountain.png" alt="Grounded rocky scenery over continuous sand" width="220" />
</p>
<p>
  <img src="Verification/Horror-HorrorGirl.png" alt="Horror Girl patrol model" width="180" />
  <img src="Verification/Horror-HorrorSkunx.png" alt="Horror Skunx running obstacle" width="180" />
  <img src="Verification/Horror-Lucef.png" alt="Lucef playable skin" width="180" />
  <img src="Verification/Horror-Mianja.png" alt="Mianja playable skin" width="180" />
</p>
