using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
public class CreateAssetBundles
{
    static string assetBundleDirectory = "Assets/AssetBundles";

    [MenuItem("Assets/Build AssetBundles")]
    static void BuildAllAssetBundles()
    {
        BuildFor(EditorUserBuildSettings.activeBuildTarget);        
        BuildFor(BuildTarget.StandaloneOSX);
        BuildFor(BuildTarget.StandaloneLinux64);        
        BuildFor(BuildTarget.StandaloneWindows);
    }

    static void BuildFor(BuildTarget target)
    {
        var fullDir = Path.Combine(assetBundleDirectory, $"{target}");
        if (!Directory.Exists(fullDir))
        {
            Directory.CreateDirectory(fullDir);
        }
        BuildPipeline.BuildAssetBundles(fullDir, BuildAssetBundleOptions.None, target);
    }
}
