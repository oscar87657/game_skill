// GOLDEN STANDARD
// 목적: 수직 슬라이스를 같은 씬 순서와 옵션으로 반복 가능한 macOS Development Build로 만든다.
// 책임: 활성 씬 검증, 출력 경로 준비, ARM64 설정, BuildReport 판정과 요약 로그를 담당한다.
// 불변식: Main 씬이 첫 번째가 아니거나 활성 씬이 없으면 불완전한 Player를 만들지 않고 즉시 실패한다.
// 선택 이유: 수동 Build Settings 클릭 대신 코드로 빌드 계약을 고정하면 로컬과 자동화 결과를 비교할 수 있다.
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace GameSkill.Editor
{
    public static class DesktopBuildPipeline
    {
        private const string MainScenePath =
            "Assets/Scenes/Main.unity";
        private const string DefaultOutputPath =
            "Builds/macOS/game_skill.app";
        private const string OutputEnvironmentVariable =
            "GAME_SKILL_MACOS_BUILD_PATH";

        [MenuItem("Game Skill/Build/macOS Development Build")]
        public static void BuildMacOsDevelopment()
        {
            // 에디터 메뉴와 명령행 자동화가 같은 내부 경로를 사용해 옵션 차이를 만들지 않는다.
            BuildReport report =
                BuildMacOsDevelopmentInternal();
            LogSuccessfulBuild(report);
        }

        public static void BuildMacOsDevelopmentFromCommandLine()
        {
            // 배치 모드는 예외를 프로세스 실패 코드로 전달해 빌드 실패를 성공으로 기록하지 않게 한다.
            BuildReport report =
                BuildMacOsDevelopmentInternal();
            LogSuccessfulBuild(report);
        }

        private static BuildReport
            BuildMacOsDevelopmentInternal()
        {
            // Build Settings에서 활성화된 순서를 그대로 사용하되 시작 Scene 계약을 먼저 검증한다.
            string[] scenePaths =
                CollectEnabledScenePaths();
            ValidateSceneOrder(scenePaths);
            string outputPath =
                ResolveOutputPath();
            string outputDirectory =
                Path.GetDirectoryName(
                    outputPath);
            if (string.IsNullOrWhiteSpace(
                outputDirectory))
            {
                throw new InvalidOperationException(
                    "macOS 빌드 출력 폴더를 결정할 수 없습니다.");
            }

            Directory.CreateDirectory(
                outputDirectory);
            PlayerSettings.SetArchitecture(
                NamedBuildTarget.Standalone,
                (int)OSArchitecture.ARM64);
            var options =
                new BuildPlayerOptions
                {
                    scenes = scenePaths,
                    locationPathName =
                        outputPath,
                    target =
                        BuildTarget.StandaloneOSX,
                    targetGroup =
                        BuildTargetGroup.Standalone,
                    options =
                        BuildOptions.Development
                        | BuildOptions.AllowDebugging
                        | BuildOptions
                            .DetailedBuildReport
                };
            BuildReport report =
                BuildPipeline.BuildPlayer(
                    options);
            if (report.summary.result
                != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    "macOS Development Build 실패: "
                    + report.summary.result
                    + ", errors="
                    + report.summary.totalErrors);
            }

            return report;
        }

        private static string[]
            CollectEnabledScenePaths()
        {
            // 비활성 Scene은 의도적으로 제외하고 배열 순서는 Build Settings와 동일하게 보존한다.
            var paths =
                new List<string>();
            foreach (EditorBuildSettingsScene scene
                in EditorBuildSettings.scenes)
            {
                if (scene.enabled
                    && !string.IsNullOrWhiteSpace(
                        scene.path))
                {
                    paths.Add(
                        scene.path);
                }
            }

            return paths.ToArray();
        }

        private static void ValidateSceneOrder(
            string[] scenePaths)
        {
            // 첫 Scene은 Player 시작점이므로 단순 포함 여부가 아니라 인덱스 0을 강제한다.
            if (scenePaths == null
                || scenePaths.Length == 0)
            {
                throw new InvalidOperationException(
                    "활성화된 빌드 Scene이 없습니다.");
            }

            if (!string.Equals(
                scenePaths[0],
                MainScenePath,
                StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Main 씬이 Build Settings의 첫 번째여야 합니다.");
            }
        }

        private static string ResolveOutputPath()
        {
            // 상대 경로는 Unity 프로세스의 시작 폴더가 아니라 프로젝트 루트에 고정해 메뉴 실행 위치 차이를 없앤다.
            string requestedPath =
                Environment.GetEnvironmentVariable(
                    OutputEnvironmentVariable);
            string selectedPath =
                string.IsNullOrWhiteSpace(
                    requestedPath)
                    ? DefaultOutputPath
                    : requestedPath;
            string projectRoot =
                Path.GetDirectoryName(
                    Application.dataPath);
            if (string.IsNullOrWhiteSpace(
                projectRoot))
            {
                throw new InvalidOperationException(
                    "Unity 프로젝트 루트를 결정할 수 없습니다.");
            }

            string anchoredPath =
                Path.IsPathRooted(
                    selectedPath)
                    ? selectedPath
                    : Path.Combine(
                        projectRoot,
                        selectedPath);
            return Path.GetFullPath(
                anchoredPath);
        }

        private static void LogSuccessfulBuild(
            BuildReport report)
        {
            // 성공 결과에 경로·용량·시간·경고 수를 한 줄로 남겨 Release 문서에 그대로 옮길 수 있게 한다.
            BuildSummary summary =
                report.summary;
            Debug.Log(
                "[Desktop Build] succeeded, output="
                + summary.outputPath
                + ", size="
                + summary.totalSize
                + " bytes, duration="
                + summary.totalTime
                + ", warnings="
                + summary.totalWarnings);
        }
    }
}
