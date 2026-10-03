# Natural running cycle

Tsila, Lucef (the `Officer` animation assets) and Mianja now use a baked running cycle with landing, support, rear push-off, heel recovery and forward knee drive. Knees flex backwards throughout the stride. The legs alternate by half a cycle, with opposite arm swing and bent elbows. Ankle motion follows support and recovery; the body leans slightly forward.

`CharacterMotionBuilder` constructs the run from the neutral imported pose. Its thigh, knee and ankle curves can be regenerated through **Tools > Tsila Run > Rebuild Natural Run Cycles**. Existing animation asset references and speed adjustment continue to work on each playable skin.

The review below shows Tsila, Lucef and Mianja, in that row order, at four points of the stride. Generate it with the batch entry point `TsilaRun.Editor.RunCycleReview.Capture`.

[Side-view skinning review](Verification/NaturalRun-Cycles.png).

Unity 6000.6.3f1 passed the 44 existing gameplay and presentation checks, plus six run checks. The run checks sample 120 poses per stride, verify normal knee flexion and heel recovery, opposite arm swing, floor clearance on both LODs, and matching poses across the loop seam. Measured knee flexion is approximately 20–116 degrees; forward thigh drive reaches 63 degrees.

[Run test report](Verification/NaturalRun-RigTests.xml). [Gameplay regression results](Verification/NaturalRun-Regression.txt).
