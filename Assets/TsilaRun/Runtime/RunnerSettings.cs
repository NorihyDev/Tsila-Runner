using UnityEngine;

namespace TsilaRun
{
    public sealed class RunnerSettings : MonoBehaviour
    {
        const string VolumeKey = "TsilaRun.MusicVolume.v1";
        const string FpsKey = "TsilaRun.TargetFps.v1";
        public RunnerGame game;
        public AudioSource music;
        public float MusicVolume { get; private set; }
        public int TargetFps { get; private set; }

        public void Apply()
        {
            if (music == null) music = GetComponent<AudioSource>();
            MusicVolume = Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, .35f));
            TargetFps = PlayerPrefs.GetInt(FpsKey, 60) >= 120 ? 120 : 60;
            ApplyAudio();
            ApplyFps();
        }

        public void VolumeDown()
        {
            SetVolume(MusicVolume - .1f);
        }

        public void VolumeUp()
        {
            SetVolume(MusicVolume + .1f);
        }

        public void ToggleFps()
        {
            TargetFps = TargetFps >= 120 ? 60 : 120;
            PlayerPrefs.SetInt(FpsKey, TargetFps);
            PlayerPrefs.Save();
            ApplyFps();
        }

        void SetVolume(float value)
        {
            MusicVolume = Mathf.Clamp01(value);
            PlayerPrefs.SetFloat(VolumeKey, MusicVolume);
            PlayerPrefs.Save();
            ApplyAudio();
        }

        void ApplyAudio()
        {
            if (music == null) music = GetComponent<AudioSource>();
            if (music != null) music.volume = MusicVolume;
        }

        void ApplyFps()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = TargetFps;
        }
    }
}
