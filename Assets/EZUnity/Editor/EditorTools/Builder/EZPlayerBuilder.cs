/* Author:          ezhex1991@outlook.com
 * CreateTime:      2019-04-02 20:41:29
 * Organization:    #ORGANIZATION#
 * Description:     
 */
using System;
using UnityEditor;
using UnityEngine;
using System.IO;

#if UNITY_2018_1_OR_NEWER
using UnityEditor.Build.Reporting;
#endif

namespace EZUnity.Builder
{
    [CreateAssetMenu(fileName = "EZPlayerBuilder", menuName = "EZUnity/EZPlayerBuilder", order = (int)EZAssetMenuOrder.EZPlayerBuilder)]
    public class EZPlayerBuilder : ScriptableObject
    {
        public const string Wildcard_Date = "<Date>";
        public const string Wildcard_Time = "<Time>";
        public const string Wildcard_CompanyName = "<CompanyName>";
        public const string Wildcard_ProductName = "<ProductName>";
        public const string Wildcard_BundleIdentifier = "<BundleIdentifier>";
        public const string Wildcard_BundleVersion = "<BundleVersion>";
        public const string Wildcard_BuildNumber = "<BuildNumber>";
        public const string Wildcard_BuildTarget = "<BuildTarget>";

        public bool configButDontBuild;

        public EZBundleBuilder bundleBuilder;

        [Tooltip("Wildcards: <Date>|<Time>|<CompanyName>|<ProductName>|<BundleIdentifier>|<BundleVersion>|<BuildNumber>|<BuildTarget>")]
        public string locationPathName = "Builds/<ProductName>-<BuildTarget>-<BuildNumber>-<BundleVersion>";
        public SceneAsset[] scenes;

        public string companyName;
        public string productName;
        public string bundleIdentifier;
        public string bundleVersion;
        public int buildNumber;
        public Texture2D icon;

        public CopyInfo[] copyList;

        public void Config(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            if (!string.IsNullOrEmpty(companyName)) PlayerSettings.companyName = companyName;
            if (!string.IsNullOrEmpty(productName)) PlayerSettings.productName = productName;
            if (!string.IsNullOrEmpty(bundleVersion)) PlayerSettings.bundleVersion = bundleVersion;
            if (icon != null)
            {
                Texture2D[] icons = PlayerSettings.GetIconsForTargetGroup(buildTargetGroup, IconKind.Any);
                for (int i = 0; i < icons.Length; i++)
                {
                    icons[i] = icon;
                }
                PlayerSettings.SetIconsForTargetGroup(buildTargetGroup, icons, IconKind.Any);
            }
            if (!string.IsNullOrEmpty(bundleIdentifier)) PlayerSettings.SetApplicationIdentifier(buildTargetGroup, bundleIdentifier);
            switch (buildTargetGroup)
            {
                case BuildTargetGroup.Standalone:
                    PlayerSettings.macOS.buildNumber = buildNumber.ToString();
                    break;
                case BuildTargetGroup.iOS:
                    PlayerSettings.iOS.buildNumber = buildNumber.ToString();
                    break;
                case BuildTargetGroup.Android:
                    PlayerSettings.Android.bundleVersionCode = buildNumber;
                    break;
            }

        }
        public void Execute(BuildTargetGroup buildTargetGroup, BuildTarget buildTarget)
        {
            Config(buildTargetGroup, buildTarget);
            if (configButDontBuild) return;
            if (bundleBuilder != null)
            {
                bundleBuilder.Execute(buildTarget);
            }
            BuildPlayerOptions options = new BuildPlayerOptions();
            string[] scenePaths = new string[scenes.Length];
            for (int i = 0; i < scenePaths.Length; i++)
            {
                scenePaths[i] = AssetDatabase.GetAssetPath(scenes[i]);
            }
            options.scenes = scenePaths;
            if (string.IsNullOrEmpty(locationPathName))
            {
                locationPathName = EditorUtility.SaveFolderPanel("Choose Output Folder", "", "");
                if (string.IsNullOrEmpty(locationPathName)) return;
            }
            string path = locationPathName
                .Replace(Wildcard_BuildTarget, buildTarget.ToString())
                .Replace(Wildcard_BuildNumber, buildNumber.ToString())
                .Replace(Wildcard_BundleIdentifier, bundleIdentifier)
                .Replace(Wildcard_BundleVersion, bundleVersion)
                .Replace(Wildcard_CompanyName, companyName)
                .Replace(Wildcard_Date, DateTime.Now.ToString("yyyyMMdd"))
                .Replace(Wildcard_ProductName, productName)
                .Replace(Wildcard_Time, DateTime.Now.ToString("HHmmss"));
            switch (buildTarget)
            {
                case BuildTarget.StandaloneWindows:
                    options.locationPathName = string.Format("{0}/{1}.exe", path, PlayerSettings.productName);
                    break;
                case BuildTarget.StandaloneWindows64:
                    options.locationPathName = string.Format("{0}/{1}.exe", path, PlayerSettings.productName);
                    break;
                case BuildTarget.Android:
                    options.locationPathName = string.Format("{0}.apk", path);
                    break;
                default:
                    options.locationPathName = path;
                    break;
            }
            options.target = buildTarget;
            options.options = BuildOptions.ShowBuiltPlayer;
#if UNITY_2018_1_OR_NEWER
            BuildReport report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            switch (summary.result)
            {
                case BuildResult.Failed:
                    Debug.LogError("Build Failed");
                    break;
                case BuildResult.Succeeded:
                    Debug.Log("Build Succeeded");
                    CopyFiles(path);
                    break;
            }
#else
            Debug.Log(BuildPipeline.BuildPlayer(options));
            CopyFiles(path);
#endif
        }

        private void CopyFiles(string path)
        {
            for (int i = 0; i < copyList.Length; i++)
            {
                EditorUtility.DisplayProgressBar("Copying Files", "", (float)i / copyList.Length);
                string src = copyList[i].srcPath;
                string dst = Path.Combine(path, copyList[i].dstPath);
                if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(dst)) continue;
                if (File.Exists(src))
                {
                    try
                    {
                        File.Copy(src, dst, true);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning(e.Message);
                    }
                }
                else if (Directory.Exists(src))
                {
                    Directory.CreateDirectory(dst);
                    string[] files = Directory.GetFiles(src);
                    foreach (string filePath in files)
                    {
                        try
                        {
                            if (filePath.EndsWith(".meta")) continue;
                            string newPath = dst + filePath.Substring(src.Length);
                            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
                            File.Copy(filePath, newPath, true);
                        }
                        catch (Exception e)
                        {
                            Debug.LogWarning(e.Message);
                        }
                    }
                }
            }
            EditorUtility.ClearProgressBar();
        }
    }
}
