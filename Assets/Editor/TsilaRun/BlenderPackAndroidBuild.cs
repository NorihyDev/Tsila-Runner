using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TsilaRun.Editor
{
    public static class BlenderPackAndroidBuild
    {
        // Invoked from a separate batch Editor so the user's open Editor stays untouched.
        public static void BuildDevelopment()
        {
            const string output = "Builds/TsilaRun-Development.apk";
            Directory.CreateDirectory("Builds");
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = new[] { MobilePrototypeBuilder.ScenePath },
                locationPathName = output,
                targetGroup = BuildTargetGroup.Android,
                target = BuildTarget.Android,
                options = BuildOptions.Development
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException("Android development build failed: " + report.summary.result);
            Debug.Log("TSILA_ANDROID_BUILD_OK " + Path.GetFullPath(output) + " bytes=" + report.summary.totalSize);
        }
    }
}
