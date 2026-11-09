using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Collection.EditorTools
{
    /// <summary>一键构建 Windows 玩家：输出到 Builds/（不入库）。</summary>
    public static class GameBuilder
    {
        private static readonly string[] Scenes =
        {
            "Assets/_Project/Scenes/SC_MainMenu.unity",
            "Assets/_Project/Scenes/SC_Level01.unity",
            "Assets/_Project/Scenes/SC_Level02.unity",
            "Assets/_Project/Scenes/SC_Level03.unity",
        };

        [MenuItem("Collection/Game/Build Windows", priority = 30)]
        public static void BuildWindows()
        {
            Directory.CreateDirectory("Builds");

            var options = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = "Builds/Ripple.exe",
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            BuildSummary summary = report.summary;

            Debug.Log($"[Build] 结果={summary.result} 大小={summary.totalSize / (1024f * 1024f):0.0} MB " +
                      $"耗时={summary.totalTime.TotalSeconds:0.0}s 输出={options.locationPathName} 错误={summary.totalErrors}");
        }
    }
}
