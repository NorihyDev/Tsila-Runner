using UnityEngine;

namespace TsilaRun
{
    public sealed class RunnerChase : MonoBehaviour
    {
        public const float IntroSeconds = 2.8f;
        public RunnerGame game;
        public RunnerAvatar officer;
        public float IntroProgress { get; private set; }
        RunnerAvatar calibratedOfficer;
        Quaternion modelFacingCorrection = Quaternion.identity;

        public void ResetChase()
        {
            IntroProgress = 0f;
            if (officer == null) return;
            officer.transform.position = new Vector3(-1.2f, 0f, -4.8f);
            FacePlayer();
            officer.animate = true;
            if (officer.animator != null) { officer.animator.Rebind(); if (officer.gameObject.activeInHierarchy) officer.animator.Update(0f); }
        }

        public bool TickIntro(float dt)
        {
            IntroProgress = Mathf.Min(1f, IntroProgress + dt / IntroSeconds);
            if (officer == null) return true;
            officer.transform.position = Vector3.Lerp(new Vector3(-1.2f, 0f, -4.8f), new Vector3(0f, 0f, -3.2f), IntroProgress);
            FacePlayer();
            return IntroProgress >= 1f;
        }

        void Update()
        {
            if (game == null || officer == null || game.player == null || game.State == RunnerGame.RunState.Paused) return;
            bool visible = game.State == RunnerGame.RunState.Intro || game.State == RunnerGame.RunState.GameOver ||
                game.State == RunnerGame.RunState.Running && game.Distance < 60d;
            officer.gameObject.SetActive(visible);
            if (!visible) return;
            if (game.State == RunnerGame.RunState.Running)
            {
                officer.transform.position = new Vector3(game.player.transform.position.x * 0.6f, 0f,
                    Mathf.Lerp(-3.2f, -5.2f, Mathf.Clamp01((float)game.Distance / 60f)));
                FacePlayer();
            }
            if (game.State == RunnerGame.RunState.GameOver)
            {
                Vector3 target = new Vector3(game.player.transform.position.x + 0.8f, 0f, -0.8f);
                officer.transform.position = Vector3.MoveTowards(officer.transform.position, target, 5f * Time.deltaTime);
                FacePlayer();
                officer.animate = Vector3.SqrMagnitude(officer.transform.position - target) > 0.01f;
            }
        }

        void FacePlayer()
        {
            if (game == null || game.player == null || officer == null) return;
            if (calibratedOfficer != officer)
            {
                calibratedOfficer = officer;
                modelFacingCorrection = Quaternion.identity;
                // Imported rigs can face sideways even when their object has zero yaw.
                // Use the anatomical right axis to find the mesh's actual forward.
                Transform leftHip = null, rightHip = null;
                foreach (var bone in officer.GetComponentsInChildren<Transform>(true))
                {
                    if (bone.name.StartsWith("LeftUpLeg")) leftHip = bone;
                    if (bone.name.StartsWith("RightUpLeg")) rightHip = bone;
                }
                if (leftHip != null && rightHip != null)
                {
                    Vector3 right = officer.transform.InverseTransformVector(rightHip.position - leftHip.position);
                    right.y = 0f;
                    Vector3 forward = Vector3.Cross(right, Vector3.up);
                    if (forward.sqrMagnitude > 0.0001f)
                        modelFacingCorrection = Quaternion.Inverse(Quaternion.LookRotation(forward.normalized, Vector3.up));
                }
            }
            Vector3 direction = game.player.transform.position - officer.transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.0001f)
                officer.transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up) * modelFacingCorrection;
        }
    }
}
