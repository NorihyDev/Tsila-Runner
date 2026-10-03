using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TsilaRun.Editor
{
    public static class MethodMusicBuilder
    {
        public const string ClipPath = "Assets/TsilaRun/Audio/Music/method-song.mp3";

        [MenuItem("Tools/Tsila Run/Apply Method Music")]
        public static void ApplyMenu()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            ApplyBatch();
        }

        public static void ApplyTo(RunnerGame game)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(ClipPath);
            if (clip == null) return; // Prototype generation also works without the optional song.
            var music = game.GetComponent<AudioSource>();
            if (music == null) music = game.gameObject.AddComponent<AudioSource>();
            music.clip = clip;
            music.playOnAwake = true;
            music.loop = true;
            music.volume = .35f;
            music.spatialBlend = 0f;
            music.dopplerLevel = 0f;
            music.bypassReverbZones = true;
            EditorUtility.SetDirty(music);
        }

        public static void ApplyBatch()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(ClipPath) as AudioImporter;
            if (importer == null) throw new System.IO.FileNotFoundException("Method music is missing.", ClipPath);
            var settings = importer.defaultSampleSettings;
            settings.loadType = AudioClipLoadType.Streaming;
            settings.compressionFormat = AudioCompressionFormat.Vorbis;
            settings.quality = .75f;
            settings.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            settings.preloadAudioData = false;
            importer.defaultSampleSettings = settings;
            importer.loadInBackground = true;
            importer.SaveAndReimport();

            var scene = EditorSceneManager.OpenScene(MobilePrototypeBuilder.ScenePath);
            var game = Object.FindAnyObjectByType<RunnerGame>();
            if (game == null) throw new System.InvalidOperationException("Tsila Run scene has no RunnerGame.");
            ApplyTo(game);
            var music = game.GetComponent<AudioSource>();
            if (music.clip == null || music.clip.samples <= 0 || music.clip.length <= 0)
                throw new System.IO.InvalidDataException("Method music did not import as a playable clip.");
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("TSILA_METHOD_MUSIC_OK length=" + music.clip.length + " seconds, channels=" + music.clip.channels + ", streaming, loop, volume=" + music.volume);
        }
    }
}
